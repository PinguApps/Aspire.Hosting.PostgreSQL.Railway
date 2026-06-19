# PinguApps.Aspire.Hosting.PostgreSQL.Railway

[![PinguApps.Aspire.Hosting.PostgreSQL.Railway version](https://img.shields.io/nuget/v/PinguApps.Aspire.Hosting.PostgreSQL.Railway?style=for-the-badge&label=PinguApps.Aspire.Hosting.PostgreSQL.Railway)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.PostgreSQL.Railway/) [![PinguApps.Aspire.Hosting.PostgreSQL.Railway downloads](https://img.shields.io/nuget/dt/PinguApps.Aspire.Hosting.PostgreSQL.Railway?style=for-the-badge&label=downloads)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.PostgreSQL.Railway/)

`PinguApps.Aspire.Hosting.PostgreSQL.Railway` lets an Aspire AppHost publish a normal Aspire Redis resource to Railway PostgreSQL during `aspire deploy`.

- Package id: [`PinguApps.Aspire.Hosting.PostgreSQL.Railway`](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.PostgreSQL.Railway/)
- Distribution: NuGet for both C# and TypeScript AppHosts
- Tested Aspire baseline: `13.4.3`
- Provider scope: Railway PostgreSQL through the Railway Developer API
- Local behaviour: standard Aspire Redis
- Deploy behaviour: opt-in Railway create/adopt/reconcile flow

## Install

C# AppHost:

```powershell
dotnet add package PinguApps.Aspire.Hosting.PostgreSQL.Railway
```

TypeScript AppHost:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "<package version>"
  }
}
```

Then run:

```powershell
aspire restore --non-interactive
```

No npm package is required for the integration itself. TypeScript AppHosts consume the same NuGet package through Aspire's generated guest-language module flow.

## Minimal .NET Example

Maintained sample source: [`samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs`](samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs)

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;

var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
```

## Minimal TypeScript AppHost Example

Maintained demo source: [`samples/TypeScriptAppHost/apphost.mts`](samples/TypeScriptAppHost/apphost.mts)

```ts
import {
  createBuilder,
  railwayPostgresCloudPlatform,
  railwayPostgresOwnershipMode,
  railwayPostgresPlan,
  railwayPostgresRegion,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const databaseName = await builder.addParameter("railway-database-name");
const accountEmail = await builder.addParameter("railway-account-email");
const apiKey = await builder.addParameter("railway-api-key", { secret: true });

let cache = await builder.addRedis("cache");
cache = await cache.publishToRailway(databaseName, accountEmail, apiKey, {
  ownershipMode: railwayPostgresOwnershipMode.createOrAdopt,
  platform: railwayPostgresCloudPlatform.aws,
  primaryRegion: railwayPostgresRegion.awsEuWest1,
  plan: railwayPostgresPlan.payAsYouGo,
  eviction: true,
});

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);

const app = await builder.build();
await app.run();
```

## Deploy Inputs

| Input | Purpose |
| --- | --- |
| `railway-database-name` | Explicit remote Railway database name and stable deployment identity. |
| `railway-account-email` | Infrastructure-only Railway account email. |
| `railway-api-key` | Infrastructure-only Railway Management API key. Mark it secret. |

The management API key is never exposed as an application-facing Redis output.

For non-interactive deploys, provide real values as Aspire parameter environment variables:

```powershell
$env:Parameters__railway_database_name = "railway-ts-test"
$env:Parameters__railway_account_email = $env:RAILWAY_EMAIL
$env:Parameters__railway_api_key = $env:RAILWAY_API_KEY
```

## Behaviour Summary

`builder.AddRedis("cache")` remains the resource of record. `PublishToRailway(...)` or `publishToRailway(...)` attaches deploy-time intent to that normal Redis resource.

Local runs keep using standard Aspire Redis behaviour and do not call Railway while the AppHost model is built. During `aspire deploy`, the package resolves parameters, creates or adopts the named Railway database, reconciles explicitly configured mutable settings, fails on unsafe drift, and redirects app-facing Redis connection details to Railway.

## Docs

- [Overview and product contract](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/overview.md)
- [Install and package consumption](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/install.md)
- [C# AppHost usage](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/getting-started-dotnet.md)
- [TypeScript AppHost usage](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/getting-started-typescript.md)
- [Configuration and ownership modes](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/configuration.md)
- [Deployment behaviour](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/deployment-behaviour.md)
- [Outputs and security boundaries](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/outputs-and-security.md)
- [Samples and demos](https://github.com/PinguApps/Aspire.Hosting.PostgreSQL.Railway/blob/main/docs/samples-and-demos.md)
