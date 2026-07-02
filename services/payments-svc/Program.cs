using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("PaymentsDb")
    ?? "Host=localhost;Port=5432;Database=paymentsdb;Username=appuser;Password=apppassword";

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

app.MapGet("/", () => Results.Ok(new { service = "payments-svc", status = "running" }));

app.MapPost("/api/payments/init", async (NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS payments (
            id SERIAL PRIMARY KEY,
            order_id INT NOT NULL,
            amount NUMERIC(10,2) NOT NULL,
            status TEXT NOT NULL
        );
        INSERT INTO payments (order_id, amount, status)
        SELECT * FROM (VALUES (1, 49.99, 'PAID'), (2, 199.00, 'PENDING')) AS seed(order_id, amount, status)
        WHERE NOT EXISTS (SELECT 1 FROM payments);
        """;
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { initialized = true });
});

app.MapGet("/api/payments", async (NpgsqlDataSource db) =>
{
    var results = new List<object>();
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, order_id, amount, status FROM payments ORDER BY id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        results.Add(new { id = reader.GetInt32(0), orderId = reader.GetInt32(1), amount = reader.GetDecimal(2), status = reader.GetString(3) });
    return Results.Ok(results);
});

app.MapControllers();
app.Run();
