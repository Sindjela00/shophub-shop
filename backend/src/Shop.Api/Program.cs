using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

app.MapArticleEndpoints();

app.Run();

public partial class Program;
