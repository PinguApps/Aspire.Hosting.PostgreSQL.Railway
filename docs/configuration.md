# Configuration

Every `PublishToRailway` call needs:

| Parameter | Secret | Purpose |
| --- | --- | --- |
| `railway-postgres-service-name` | No | Railway service name and stable remote identity. |
| `railway-project-id` | No | Railway project that will contain the PostgreSQL service. |
| `railway-environment-id` | No | Railway environment id or exact environment name for the service. |
| `railway-api-token` | Yes | Railway API token for deployment. |

## Ownership Modes

| Mode | Missing service | Existing service |
| --- | --- | --- |
| `CreateOrAdopt` / `createOrAdopt` | Create | Adopt |
| `CreateOnly` / `createOnly` | Create | Fail unless it is the cached verified identity |
| `ExistingOnly` / `existingOnly` | Fail | Adopt |

Use `CreateOrAdopt` for most apps. Use `ExistingOnly` when the Railway service is created outside Aspire.

If `railway-environment-id` is a friendly name such as `production`, the deployment step resolves it to the Railway environment id before service lookup or creation.

## Deployment Options

Optional Railway service settings can be supplied in C#:

```csharp
postgres.PublishToRailway(
    "orders-postgres",
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
```

| Option | Railway setting |
| --- | --- |
| `Region` | Service instance region enum: `UsWestMetal`, `UsEastMetal`, `EuWestMetal`, or `SoutheastAsiaMetal`. |
| `RestartPolicy` | Service restart policy. |
| `RestartPolicyMaxRetries` | Service restart retry limit. |
| `MemoryGB` | Service instance memory limit in GB. |
| `VCpus` | Service instance vCPU limit. |
| `SharedMemoryBytes` | Service variable `RAILWAY_SHM_SIZE_BYTES` for container shared memory. This is not volume storage. |
| `Template` | Railway template for new services: `Standard`, `PointInTimeRecovery`, `PostGis`, `PgVector`, or `TimescaleDb`. Default is `Standard`. |

Railway templates used by `Template`:

| Value | Railway template |
| --- | --- |
| `Standard` | [PostgreSQL](https://railway.com/deploy/postgres) |
| `PointInTimeRecovery` | [Postgres PITR](https://railway.com/deploy/postgres-pitr) |
| `PostGis` | [PostGIS](https://railway.com/deploy/postgis) |
| `PgVector` | [pgvector](https://railway.com/deploy/3jJFCA) |
| `TimescaleDb` | [TimescaleDB](https://railway.com/deploy/VSbF5V) |

When `Region` is set for a new PostgreSQL service, the deploy step applies it before waiting for Railway readiness. For existing volume-backed PostgreSQL services, region changes are rejected because Railway must migrate the attached volume; migrate manually in Railway or create a new service instead.

`Template` is create-time only. If ownership adopts an existing Railway service, the package keeps using that service and does not convert it to another template.

Standard and PITR template connection strings use `Ssl Mode=Require`. PostGIS, pgvector, and TimescaleDB template connection strings use `Ssl Mode=Disable`, matching the upstream Railway template endpoints this package targets.

The package does not expose HTTP healthcheck settings or replica count for PostgreSQL. The Railway PostgreSQL template is a stateful database service, so SQL readiness checks and single-instance defaults are safer than HTTP healthchecks or horizontal replicas.

## Child Databases

This package publishes the PostgreSQL server resource. Child database resources remain useful:

```csharp
IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");
```

During deploy, the integration creates missing child databases inside the Railway PostgreSQL service, then gives each child database a connection string with its own database name.

For PostGIS, pgvector, and TimescaleDB services created by this package, each child database is initialized with the matching extension after it is created.
