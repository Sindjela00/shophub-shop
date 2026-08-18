using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Data;
using Shop.Api.Observability;
using Shop.Api.Services;

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
builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection(ShopOptions.SectionName));
builder.Services.AddHttpClient<IPaymentVerificationService, SepoliaTokenPaymentVerificationService>();
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// First, so every request is counted even if later middleware redirects/short-circuits it.
app.UseMiddleware<TrafficMetricsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}

// Unlike shophub-app's shared, persistent database, every shop gets its own fresh database
// provisioned by shophub-shop-operator specifically for this instance — there's no existing
// schema to protect and no scenario where the migration would be unwanted, so this runs
// unconditionally rather than being gated to Development.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();
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
