using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Data;
using Shop.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<AdminApiKeyFilter>();
builder.Services.AddScoped<OrderService>();
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddHttpClient<IPaymentVerificationService, SepoliaTokenPaymentVerificationService>();
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;
