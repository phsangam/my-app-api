using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Connection string resolution order: env var (k8s Secret / RDS) -> appsettings.json (local dev default)
var connString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("OrdersDb")
    ?? "Host=localhost;Port=5432;Database=ordersdb;Username=appuser;Password=apppassword";

builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connString).Build());
builder.Services.AddControllers();

// CORS: allow the local React dev server. In production, UI and API sit behind the same
// ALB/Ingress host so this is not needed there (relative /api/* paths, same-origin).
builder.Services.AddCors(opt => opt.AddPolicy("LocalDev", p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("LocalDev");

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

// Readiness = process alive AND database reachable. This is the correct way to use
// readiness probes: a pod that can't reach its DB should stop receiving traffic.
app.MapGet("/health/ready", async (NpgsqlDataSource db) =>
{
    try
    {
        await using var conn = await db.OpenConnectionAsync();
        return Results.Ok(new { status = "ready", db = "reachable" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "not-ready", error = ex.Message }, statusCode: 503);
    }
});

app.MapGet("/", () => Results.Ok(new { service = "orders-svc", status = "running" }));

// One-time bootstrap so the demo works out of the box (real projects use EF migrations / Flyway).
app.MapPost("/api/orders/init", async (NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS orders (
            id SERIAL PRIMARY KEY,
            item TEXT NOT NULL,
            qty INT NOT NULL
        );
        INSERT INTO orders (item, qty)
        SELECT * FROM (VALUES ('Keyboard', 2), ('Monitor', 1)) AS seed(item, qty)
        WHERE NOT EXISTS (SELECT 1 FROM orders);
        """;
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { initialized = true });
});

app.MapGet("/api/orders", async (NpgsqlDataSource db) =>
{
    var results = new List<object>();
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, item, qty FROM orders ORDER BY id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        results.Add(new { id = reader.GetInt32(0), item = reader.GetString(1), qty = reader.GetInt32(2) });
    return Results.Ok(results);
});

app.MapControllers();
app.Run();
