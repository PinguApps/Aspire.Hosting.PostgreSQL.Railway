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
    "Aspire.Hosting.PostgreSQL": "13.4.3",
    "PinguApps.Aspire.Hosting.PostgreSQL.Railway": "<package version>"
  }
}
```

For this repository checkout, use the local project path:

```json
{
  "packages": {
    "Aspire.Hosting.PostgreSQL": "13.4.3",
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
| `railway-environment-id` | No | Existing Railway environment id. |
| `railway-api-token` | Yes | Railway API token. |

Environment variable form for non-interactive deploy:

```powershell
$env:Parameters__railway_postgres_service_name = $env:RAILWAY_POSTGRES_SERVICE_NAME
$env:Parameters__railway_project_id = $env:RAILWAY_PROJECT_ID
$env:Parameters__railway_environment_id = $env:RAILWAY_ENVIRONMENT_ID
$env:Parameters__railway_api_token = $env:RAILWAY_API_TOKEN
```
