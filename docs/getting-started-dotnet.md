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
        RailwayPostgresOwnershipMode.CreateOrAdopt);

IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

builder.AddProject<Projects.Api>("api")
    .WithReference(orders);

builder.Build().Run();
```

Run deploys from the AppHost directory:

```powershell
$env:Parameters__railway_postgres_service_name = "orders-postgres"
$env:Parameters__railway_project_id = $env:RAILWAY_PROJECT_ID
$env:Parameters__railway_environment_id = $env:RAILWAY_ENVIRONMENT_ID
$env:Parameters__railway_api_token = $env:RAILWAY_API_TOKEN
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
