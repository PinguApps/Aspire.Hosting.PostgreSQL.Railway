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

## Child Databases

This package publishes the PostgreSQL server resource. Child database resources remain useful:

```csharp
IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");
```

During deploy, the integration creates missing child databases inside the Railway PostgreSQL service, then gives each child database a connection string with its own database name.
