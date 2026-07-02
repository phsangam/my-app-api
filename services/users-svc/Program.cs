using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("UsersDb")
    ?? "Host=localhost;Port=5432;Database=usersdb;Username=appuser;Password=apppassword";

builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connString).Build());
builder.Services.AddControllers();
builder.Services.AddCors(opt => opt.AddPolicy("LocalDev", p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("LocalDev");

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (NpgsqlDataSource db) =>
{
    try { await using var conn = await db.OpenConnectionAsync(); return Results.Ok(new { status = "ready", db = "reachable" }); }
    catch (Exception ex) { return Results.Json(new { status = "not-ready", error = ex.Message }, statusCode: 503); }
});

app.MapGet("/", () => Results.Ok(new { service = "users-svc", status = "running" }));

app.MapPost("/api/users/init", async (NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT NOT NULL
        );
        INSERT INTO users (name, email)
        SELECT * FROM (VALUES ('Asha Rao', 'asha@example.com'), ('Rahul Dev', 'rahul@example.com')) AS seed(name, email)
        WHERE NOT EXISTS (SELECT 1 FROM users);
        """;
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { initialized = true });
});

app.MapGet("/api/users", async (NpgsqlDataSource db) =>
{
    var results = new List<object>();
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, name, email FROM users ORDER BY id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        results.Add(new { id = reader.GetInt32(0), name = reader.GetString(1), email = reader.GetString(2) });
    return Results.Ok(results);
});

app.MapControllers();
app.Run();
