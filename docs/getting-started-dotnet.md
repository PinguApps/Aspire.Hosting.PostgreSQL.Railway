# C# AppHost Usage

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;

var builder = DistributedApplication.CreateBuilder(args);

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
            options.Template = RailwayPostgresTemplate.PointInTimeRecovery;
        });

IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

builder.AddProject<Projects.Api>("api")
    .WithReference(orders)
    .WaitFor(postgres);

builder.Build().Run();
```

Run deploys from the AppHost directory:

```powershell
Set-Item Env:Parameters__railway-postgres-service-name "orders-postgres"
Set-Item Env:Parameters__railway-project-id $env:RAILWAY_PROJECT_ID
Set-Item Env:Parameters__railway-environment-id $env:RAILWAY_ENVIRONMENT_ID
Set-Item Env:Parameters__railway-api-token $env:RAILWAY_API_TOKEN
aspire deploy --non-interactive
```

Literal values are also supported in C#:

```csharp
postgres.PublishToRailway(
    "orders-postgres",
    projectId,
    environmentId,
    apiToken,
    RailwayPostgresOwnershipMode.CreateOnly);
```

Local runs behave like standard Aspire PostgreSQL. `PublishToRailway` only records deploy-time intent during AppHost model construction.

Keep normal Aspire references in C# AppHosts. When `Aspire.Hosting.PostgreSQL.Railway` is imported, `.WithReference(postgres)` and `.WithReference(database)` use Railway PostgreSQL outputs during deploy for resources marked with `.PublishToRailway(...)`.
