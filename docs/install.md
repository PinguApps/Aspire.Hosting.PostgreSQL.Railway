# Install

## C# AppHost

```powershell
dotnet add package PinguApps.Aspire.Hosting.PostgreSQL.Railway
```

```csharp
using Aspire.Hosting.PostgreSQL.Railway;
```

## TypeScript AppHost

Add the hosting packages to `aspire.config.json`:

```json
{
  "packages": {
    "Aspire.Hosting.PostgreSQL": "13.4.6",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "<package version>"
  }
}
```

For this repository checkout, use the local project path:

```json
{
  "packages": {
    "Aspire.Hosting.PostgreSQL": "13.4.6",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "../../src/Aspire.Hosting.PostgreSQL.Railway/Aspire.Hosting.PostgreSQL.Railway.csproj"
  }
}
```

Then run:

```powershell
aspire restore --non-interactive
```

## Required Parameters

| Parameter | Secret | Purpose |
| --- | --- | --- |
| `railway-postgres-service-name` | No | Railway PostgreSQL service name. |
| `railway-project-id` | No | Existing Railway project id. |
| `railway-environment-id` | No | Existing Railway environment id or exact environment name, for example `production`. |
| `railway-api-token` | Yes | Railway API token. |

Environment variable form for non-interactive deploy:

```powershell
Set-Item Env:Parameters__railway-postgres-service-name $env:RAILWAY_POSTGRES_SERVICE_NAME
Set-Item Env:Parameters__railway-project-id $env:RAILWAY_PROJECT_ID
Set-Item Env:Parameters__railway-environment-id $env:RAILWAY_ENVIRONMENT_ID
Set-Item Env:Parameters__railway-api-token $env:RAILWAY_API_TOKEN
```
