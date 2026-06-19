using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Samples;

public static class RailwayPostgresAppHostSnippets
{
    public static void ConfigureCreateOrAdopt(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("railway-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("railway-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("railway-api-key", secret: true);

        IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
            .PublishToRailway(
                databaseName,
                accountEmail,
                apiKey,
                RailwayPostgresOwnershipMode.CreateOrAdopt,
                options =>
                {
                    options.SetPlatform(RailwayPostgresCloudPlatform.Aws);
                    options.SetPrimaryRegion(RailwayPostgresRegion.AwsEuWest1);
                    options.SetPlan(RailwayPostgresPlan.PayAsYouGo);
                    options.Eviction = true;
                });

        builder.AddProject<Projects.Api>("api")
            .WithReference(cache);
    }

    public static void ConfigureCreateOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("railway-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("railway-api-key", secret: true);

        builder.AddRedis("cache")
            .PublishToRailway(
                "orders-cache",
                accountEmail,
                apiKey,
                RailwayPostgresOwnershipMode.CreateOnly,
                options =>
                {
                    options.SetPlatform(RailwayPostgresCloudPlatform.Aws);
                    options.SetPrimaryRegion(RailwayPostgresRegion.AwsEuWest1);
                    options.SetReadRegions(RailwayPostgresRegion.AwsEuWest2);
                    options.SetPlan(RailwayPostgresPlan.PayAsYouGo);
                    options.SetBudget(360);
                    options.Eviction = true;
                });
    }

    public static void ConfigureExistingOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("railway-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("railway-api-key", secret: true);

        builder.AddRedis("cache")
            .PublishToRailway(
                "orders-cache",
                accountEmail,
                apiKey,
                RailwayPostgresOwnershipMode.ExistingOnly);
    }

    public static void ConfigureParameterizedOptions(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("railway-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("railway-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("railway-api-key", secret: true);
        IResourceBuilder<ParameterResource> platform = builder.AddParameter("railway-platform");
        IResourceBuilder<ParameterResource> primaryRegion = builder.AddParameter("railway-primary-region");
        IResourceBuilder<ParameterResource> readRegion = builder.AddParameter("railway-read-region");
        IResourceBuilder<ParameterResource> budget = builder.AddParameter("railway-budget");

        builder.AddRedis("cache")
            .PublishToRailway(
                RailwayPostgresValue.FromParameter(databaseName),
                accountEmail,
                apiKey,
                RailwayPostgresOwnershipMode.CreateOnly,
                options =>
                {
                    options.Platform = RailwayPostgresValue.FromParameter(platform);
                    options.PrimaryRegion = RailwayPostgresValue.FromParameter(primaryRegion);
                    options.ReadRegions = [RailwayPostgresValue.FromParameter(readRegion)];
                    options.Plan = "payg";
                    options.Budget = RailwayPostgresValue.FromParameter(budget);
                    options.Eviction = true;
                });
    }

    public static void ConfigureSupplementaryOutputConsumer(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("railway-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("railway-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("railway-api-key", secret: true);

        IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
            .PublishToRailway(
                databaseName,
                accountEmail,
                apiKey,
                RailwayPostgresOwnershipMode.CreateOrAdopt);

        RailwayPostgresOutputs outputs = cache.Resource.GetRailwayPostgresOutputs();

        builder.AddContainer("redis-dashboard", "redis-dashboard")
            .WithEnvironment("RAILWAY_POSTGRES_ENDPOINT", outputs.Endpoint)
            .WithEnvironment("RAILWAY_POSTGRES_PORT", outputs.Port)
            .WithEnvironment("RAILWAY_POSTGRES_PASSWORD", outputs.Password)
            .WithEnvironment("RAILWAY_POSTGRES_TLS", outputs.Tls)
            .WithEnvironment("RAILWAY_POSTGRES_DATABASE_NAME", outputs.DatabaseName);
    }
}
