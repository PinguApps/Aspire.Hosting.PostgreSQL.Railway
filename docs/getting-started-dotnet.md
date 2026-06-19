# C# AppHost Usage

Install the package in the AppHost:

```powershell
dotnet add package PinguApps.Aspire.Hosting.PostgreSQL.Railway
```

Then start with standard Aspire Redis and add `PublishToRailway`.

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

The maintained compile-validated C# sample is [`samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs`](../samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs).

## Deploy

Run deploys from the AppHost directory. For non-interactive deploys, provide the required values as Aspire parameter environment variables:

```powershell
$env:Parameters__railway_database_name = "orders-cache"
$env:Parameters__railway_account_email = $env:RAILWAY_EMAIL
$env:Parameters__railway_api_key = $env:RAILWAY_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

Repeated deploys should target the same configured database name.

## Overload Shapes

C# supports parameter-backed and literal-or-parameter value overloads:

```csharp
PublishToRailway(databaseName, accountEmail, apiKey, ownershipMode, options => { });
PublishToRailway("orders-cache", accountEmail, apiKey, ownershipMode, options => { });
PublishToRailway(RailwayPostgresValue.FromParameter(databaseName), accountEmail, apiKey, ownershipMode, options => { });
```

Use Aspire parameters for management credentials. Literal database names are supported in C#, but parameters are usually easier to promote across environments.

## Local Run

Local runs behave like standard Aspire Redis. `PublishToRailway` records deploy-time intent and does not call Railway during model construction.
