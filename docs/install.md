# Install

## C# AppHost

```powershell
dotnet add package PinguApps.Aspire.Hosting.PostgreSQL.Railway
```

Import the namespace in the AppHost:

```csharp
using Aspire.Hosting.PostgreSQL.Railway;
```

## TypeScript AppHost

TypeScript AppHosts also consume this integration through NuGet, but the package is added through `aspire.config.json`, not `dotnet add package`.

For a released package:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "<package version>"
  }
}
```

When validating this repository checkout instead of the published package, point the package entry at the local project:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "../../src/Aspire.Hosting.PostgreSQL.Railway/Aspire.Hosting.PostgreSQL.Railway.csproj"
  }
}
```

Then generate the TypeScript surface:

```powershell
aspire restore --non-interactive
```

Aspire loads the .NET hosting assembly, reads its export metadata, and generates the TypeScript module consumed by the AppHost. That is why the TypeScript examples import from `./.aspire/modules/aspire.mjs` after `aspire restore` rather than from an npm package owned by this repository.

An npm package is not required for the integration itself. Adding one would create a second distribution surface for the same deploy-time behaviour.

Use npm only for normal TypeScript tooling such as `typescript` or `tsx`.

## Required Parameters

Every AppHost needs:

| Parameter | Secret | Purpose |
| --- | --- | --- |
| `railway-database-name` | No | Remote Railway PostgreSQL database name and repeated-deploy identity. |
| `railway-account-email` | No | Railway account email used by deployment infrastructure. |
| `railway-api-key` | Yes | Railway Management API key used by deployment infrastructure. |

The account email and Management API key are deployment inputs. Application resources receive Redis connection details only.

For non-interactive deploys, provide real values as Aspire parameter environment variables:

```powershell
$env:Parameters__railway_database_name = "railway-ts-test"
$env:Parameters__railway_account_email = $env:RAILWAY_EMAIL
$env:Parameters__railway_api_key = $env:RAILWAY_API_KEY
```
