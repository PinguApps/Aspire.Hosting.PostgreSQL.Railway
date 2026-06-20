# PinguApps.Aspire.Hosting.PostgreSQL.Railway

`PinguApps.Aspire.Hosting.PostgreSQL.Railway` lets an Aspire AppHost keep using Aspire's normal PostgreSQL resource model locally, then create or adopt a Railway PostgreSQL service during `aspire deploy`.

- Local behaviour: standard Aspire PostgreSQL
- Deploy behaviour: opt-in Railway PostgreSQL create/adopt flow
- Resource of record: `PostgresServerResource`
- Child databases: `postgres.AddDatabase(...)` resources are created inside the Railway PostgreSQL service during deploy
- Required Railway inputs: service name, project id, environment id/name, API token

## Install

```powershell
dotnet add package PinguApps.Aspire.Hosting.PostgreSQL.Railway
```

TypeScript AppHosts consume the same NuGet package through Aspire's generated module flow:

```json
{
  "packages": {
    "Aspire.Hosting.PostgreSQL": "13.4.3",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "<package version>"
  }
}
```

Then run:

```powershell
aspire restore --non-interactive
```

## C# Example

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
        });

IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

builder.AddProject<Projects.Api>("api")
    .WithReference(orders)
    .WaitFor(postgres);

builder.Build().Run();
```

## TypeScript Example

```ts
import {
  createBuilder,
  RailwayPostgresRegions,
  RailwayPostgresRestartPolicy,
  railwayPostgresOwnershipMode,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const serviceName = await builder.addParameter("railway-postgres-service-name");
const projectId = await builder.addParameter("railway-project-id");
const environmentId = await builder.addParameter("railway-environment-id");
const apiToken = await builder.addParameter("railway-api-token", { secret: true });

let postgres = await builder.addPostgres("postgres");
postgres = await postgres.publishToRailway(serviceName, projectId, environmentId, apiToken, {
  ownershipMode: railwayPostgresOwnershipMode.createOrAdopt,
  region: RailwayPostgresRegions.EuWestMetal,
  restartPolicy: RailwayPostgresRestartPolicy.OnFailure,
  restartPolicyMaxRetries: 10,
  memoryGB: 2,
  vCpus: 1,
  sharedMemoryBytes: 524288000,
});

const orders = await postgres.addDatabase("orders");

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(orders);

const app = await builder.build();
await app.run();
```

## Deploy Inputs

| Aspire parameter | Secret | Purpose |
| --- | --- | --- |
| `railway-postgres-service-name` | No | Railway service name and stable remote identity. |
| `railway-project-id` | No | Existing Railway project id. |
| `railway-environment-id` | No | Existing Railway environment id or exact environment name, for example `production`. |
| `railway-api-token` | Yes | Railway API token used only by deployment infrastructure. |

For non-interactive deploys:

```powershell
$env:Parameters__railway_postgres_service_name = $env:RAILWAY_POSTGRES_SERVICE_NAME
$env:Parameters__railway_project_id = $env:RAILWAY_PROJECT_ID
$env:Parameters__railway_environment_id = $env:RAILWAY_ENVIRONMENT_ID
$env:Parameters__railway_api_token = $env:RAILWAY_API_TOKEN
aspire deploy --non-interactive
```

If `railway-environment-id` is not a UUID, the deployment step resolves it by listing environments in the configured Railway project before creating or adopting the PostgreSQL service.

## Deployment Options

`PublishToRailway` can also reconcile selected Railway service settings during deploy:

| Option | Purpose |
| --- | --- |
| `Region` | Railway region enum: `UsWestMetal`, `UsEastMetal`, `EuWestMetal`, or `SoutheastAsiaMetal`. |
| `RestartPolicy` | Railway restart policy: `Always`, `OnFailure`, or `Never`. |
| `RestartPolicyMaxRetries` | Maximum Railway restart attempts. |
| `MemoryGB` | Railway memory limit in GB. |
| `VCpus` | Railway vCPU limit. |
| `SharedMemoryBytes` | Sets Railway service variable `RAILWAY_SHM_SIZE_BYTES` for PostgreSQL shared memory. |

When `Region` changes, the deploy step updates Railway's service instance config and redeploys the PostgreSQL service if the latest deployment manifest is not already in that region.

Healthcheck path and replica count are intentionally not exposed for this PostgreSQL package. Railway healthchecks are HTTP based, while the PostgreSQL template exposes a database socket. Horizontal replicas of the default PostgreSQL template are not PostgreSQL HA/read replicas.

## Behaviour

Local runs do not call Railway and keep normal Aspire PostgreSQL behaviour. During `aspire deploy`, this package creates or adopts the configured Railway PostgreSQL service, reads Railway's PostgreSQL variables, applies the server connection output, and applies child database connection strings for `AddDatabase(...)` resources.

For C# AppHosts, importing `Aspire.Hosting.PostgreSQL.Railway` also makes `.WithReference(postgres)` and `.WithReference(database)` Railway-aware for resources marked with `.PublishToRailway(...)`. Consumers keep normal Aspire reference code while Azure App Service receives output-backed Railway PostgreSQL connection strings during deploy.

## Docs

- [Overview](docs/overview.md)
- [Install](docs/install.md)
- [C# AppHost usage](docs/getting-started-dotnet.md)
- [TypeScript AppHost usage](docs/getting-started-typescript.md)
- [Configuration](docs/configuration.md)
- [Deployment behaviour](docs/deployment-behaviour.md)
- [Outputs and security](docs/outputs-and-security.md)
- [Samples](docs/samples-and-demos.md)
