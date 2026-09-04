using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop.Api.Services;
using Testcontainers.Redis;
using Xunit;

namespace Shop.Api.IntegrationTests;

/// <summary>
/// The "light" tier's counterpart to <see cref="ShopApiFactory"/>: boots the same real app
/// against a real, disposable Redis container instead of Postgres. Nothing is stubbed except
/// on-chain payment verification (same as the Postgres factory) — the point is to prove the
/// Redis repositories really do behave like the EF Core ones through the actual HTTP endpoints,
/// not to test them in isolation.
///
/// Setting ConnectionStrings:Redis (and leaving ConnectionStrings:Default unset) is the only
/// switch involved — exactly what shophub-shop-operator does for a databaseKind: light shop.
/// </summary>
public class ShopApiRedisFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7.2-alpine").Build();

    public const string AdminApiKey = "test-admin-key";
    public const string ReceivingWalletAddress = "0x000000000000000000000000000000000000dEaD";

    public FakePaymentVerificationService PaymentVerification { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting, not ConfigureAppConfiguration, specifically for the connection string:
        // Program.cs reads it *eagerly* (to decide which repository set to register before the
        // container is built), and ConfigureAppConfiguration sources aren't layered in yet at
        // that point — the Postgres factory gets away with it only because UseNpgsql evaluates
        // its argument lazily. UseSetting lands in host configuration the builder sees from the
        // start, so the light-tier branch is actually taken.
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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

    // No migration step here, unlike the Postgres factory — Redis has no schema to create.
    public Task InitializeAsync() => _redis.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ShopApiRedisCollection : ICollectionFixture<ShopApiRedisFactory>
{
    public const string Name = "ShopApiRedis";
}
