using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop.Api.Data;
using Shop.Api.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace Shop.Api.IntegrationTests;

/// <summary>
/// Boots the real app against a real, disposable Postgres container (Testcontainers) — the
/// only thing swapped out is on-chain payment verification, which talks to a real blockchain
/// RPC and is covered separately by unit tests instead.
/// </summary>
public class ShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:18-alpine").Build();

    public const string AdminApiKey = "test-admin-key";
    public const string ReceivingWalletAddress = "0x000000000000000000000000000000000000dEaD";

    public FakePaymentVerificationService PaymentVerification { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Admin:ApiKey"] = AdminApiKey,
                ["Payments:RpcUrl"] = "https://fake-rpc.test",
                ["Payments:TokenContractAddress"] = "0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238",
                ["Payments:TokenDecimals"] = "6",
                ["Payments:ReceivingWalletAddress"] = ReceivingWalletAddress,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPaymentVerificationService>();
            services.AddSingleton<IPaymentVerificationService>(PaymentVerification);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ShopApiCollection : ICollectionFixture<ShopApiFactory>
{
    public const string Name = "ShopApi";
}
