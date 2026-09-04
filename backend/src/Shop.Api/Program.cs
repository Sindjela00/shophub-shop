using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Data;
using Shop.Api.Observability;
using Shop.Api.Repositories;
using Shop.Api.Repositories.EfCore;
using Shop.Api.Repositories.Redis;
using Shop.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    // Wide open, but only ever applied under the Development environment check below —
    // the frontend's origin isn't fixed yet (dev server port, deployed domain, etc.).
    options.AddPolicy(DevCorsPolicy, policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<AdminApiKeyFilter>();
builder.Services.AddScoped<OrderService>();
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));

// SHOP_NAME is a flat env var (not Shop__Name), set directly on the container by
// shophub-shop-operator, so it's read as a top-level configuration key rather than bound
// from a section the way PaymentOptions is.
builder.Services.Configure<ShopOptions>(options =>
{
    var shopName = builder.Configuration["SHOP_NAME"];
    if (!string.IsNullOrWhiteSpace(shopName))
    {
        options.Name = shopName;
    }
});
builder.Services.AddHttpClient<IPaymentVerificationService, SepoliaTokenPaymentVerificationService>();

// Which storage backend a shop gets is decided by shophub-shop-operator at provisioning time
// (Shop.spec.databaseKind), not by this app — it just wires up whichever connection string
// actually shows up in its own config. "standard" tier sets ConnectionStrings:Default
// (Postgres, via CNPG); "light" tier sets ConnectionStrings:Redis instead. Exactly one of the
// two is expected to be present; Redis takes precedence if somehow both are (shouldn't happen
// in practice — the operator only ever sets one or the other for a given shop).
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var useRedis = !string.IsNullOrWhiteSpace(redisConnectionString);

if (useRedis)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString!));
    builder.Services.AddScoped<IArticleRepository, RedisArticleRepository>();
    builder.Services.AddScoped<ICategoryRepository, RedisCategoryRepository>();
    builder.Services.AddScoped<ICartRepository, RedisCartRepository>();
    builder.Services.AddScoped<IOrderRepository, RedisOrderRepository>();
}
else
{
    builder.Services.AddDbContext<ShopDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
    builder.Services.AddScoped<IArticleRepository, EfArticleRepository>();
    builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
    builder.Services.AddScoped<ICartRepository, EfCartRepository>();
    builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
}

var app = builder.Build();

// First, so every request is counted even if later middleware redirects/short-circuits it.
app.UseMiddleware<TrafficMetricsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}

// Only the "standard" (Postgres/EF Core) tier has a schema to migrate at all — Redis has none,
// there's nothing for this block to do on the "light" tier.
//
// Unlike shophub-app's shared, persistent database, every shop gets its own fresh database
// provisioned by shophub-shop-operator specifically for this instance — there's no existing
// schema to protect and no scenario where the migration would be unwanted, so this runs
// unconditionally rather than being gated to Development.
//
// Multi-replica shops all boot against that same fresh database at once, so without
// coordination every pod races to run MigrateAsync and whichever loses hits Postgres
// mid-CREATE TABLE from the winner. A session-level Postgres advisory lock serializes
// them: only the lock holder actually runs migrations, the rest block on pg_advisory_lock
// until it's released, then run MigrateAsync themselves too — but by then EF Core's own
// migration-history check sees everything already applied and no-ops.
if (!useRedis)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    var connection = dbContext.Database.GetDbConnection();
    await connection.OpenAsync();

    // Fixed, well-known key for this app's migration lock — arbitrary but constant so every
    // replica of the same shop contends for the same lock (a distinct key per shop database
    // isn't needed since each shop already gets its own isolated Postgres database).
    const long MigrationLockKey = 72061217;

    try
    {
        await using (var lockCommand = connection.CreateCommand())
        {
            lockCommand.CommandText = "SELECT pg_advisory_lock($1)";
            var keyParam = lockCommand.CreateParameter();
            keyParam.Value = MigrationLockKey;
            lockCommand.Parameters.Add(keyParam);
            await lockCommand.ExecuteNonQueryAsync();
        }

        await dbContext.Database.MigrateAsync();
    }
    finally
    {
        await using var unlockCommand = connection.CreateCommand();
        unlockCommand.CommandText = "SELECT pg_advisory_unlock($1)";
        var keyParam = unlockCommand.CreateParameter();
        keyParam.Value = MigrationLockKey;
        unlockCommand.Parameters.Add(keyParam);
        await unlockCommand.ExecuteNonQueryAsync();
    }
}

app.UseHttpsRedirection();

// The storefront frontend is bundled into wwwroot by the Docker build (see backend/Dockerfile) —
// absent in local `dotnet run` without a prior `npm run build`, in which case these just serve
// nothing and every request falls through to the controllers/404 below, same as before bundling.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

// Client-side routes (e.g. /admin/categories) have nothing on disk to match — hand them the SPA
// shell so React Router can take over, instead of a bare 404 on refresh/direct navigation. Placed
// last so it only ever catches requests MapControllers didn't already handle.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
