using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Samples;

public static class RailwayPostgresAppHostSnippets
{
    public static void ConfigureCreateOrAdopt(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> serviceName = builder.AddParameter("railway-postgres-service-name");
        IResourceBuilder<ParameterResource> projectId = builder.AddParameter("railway-project-id");
        IResourceBuilder<ParameterResource> environmentId = builder.AddParameter("railway-environment-id");
        IResourceBuilder<ParameterResource> apiToken = builder.AddParameter("railway-api-token", secret: true);

        IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
            .PublishToRailway(
                serviceName,
                projectId,
                environmentId,
                apiToken,
                RailwayPostgresOwnershipMode.CreateOrAdopt,
                options =>
                {
                    options.Region = RailwayPostgresRegions.EuWestMetal;
                    options.RestartPolicy = RailwayPostgresRestartPolicy.OnFailure;
                    options.RestartPolicyMaxRetries = 10;
                    options.MemoryGB = 2;
                    options.VCpus = 1;
                    options.SharedMemoryBytes = 524288000;
                });

        IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

        builder.AddProject<Projects.Api>("api")
            .WithReference(orders)
            .WaitFor(postgres);
    }

    public static void ConfigureCreateOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> projectId = builder.AddParameter("railway-project-id");
        IResourceBuilder<ParameterResource> environmentId = builder.AddParameter("railway-environment-id");
        IResourceBuilder<ParameterResource> apiToken = builder.AddParameter("railway-api-token", secret: true);

        builder.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                projectId,
                environmentId,
                apiToken,
                RailwayPostgresOwnershipMode.CreateOnly);
    }

    public static void ConfigureExistingOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> projectId = builder.AddParameter("railway-project-id");
        IResourceBuilder<ParameterResource> environmentId = builder.AddParameter("railway-environment-id");
        IResourceBuilder<ParameterResource> apiToken = builder.AddParameter("railway-api-token", secret: true);

        builder.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                projectId,
                environmentId,
                apiToken,
                RailwayPostgresOwnershipMode.ExistingOnly);
    }

    public static void ConfigureSupplementaryOutputConsumer(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> serviceName = builder.AddParameter("railway-postgres-service-name");
        IResourceBuilder<ParameterResource> projectId = builder.AddParameter("railway-project-id");
        IResourceBuilder<ParameterResource> environmentId = builder.AddParameter("railway-environment-id");
        IResourceBuilder<ParameterResource> apiToken = builder.AddParameter("railway-api-token", secret: true);

        IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
            .PublishToRailway(
                serviceName,
                projectId,
                environmentId,
                apiToken,
                RailwayPostgresOwnershipMode.CreateOrAdopt);

        RailwayPostgresOutputs outputs = postgres.Resource.GetRailwayPostgresOutputs();

        builder.AddContainer("postgres-admin", "postgres-admin")
            .WithEnvironment("RAILWAY_POSTGRES_SERVICE_ID", outputs.ServiceId)
            .WithEnvironment("RAILWAY_POSTGRES_HOST", outputs.Host)
            .WithEnvironment("RAILWAY_POSTGRES_PORT", outputs.Port)
            .WithEnvironment("RAILWAY_POSTGRES_USERNAME", outputs.UserName)
            .WithEnvironment("RAILWAY_POSTGRES_PASSWORD", outputs.Password)
            .WithEnvironment("RAILWAY_POSTGRES_DATABASE_NAME", outputs.DatabaseName);
    }
}
