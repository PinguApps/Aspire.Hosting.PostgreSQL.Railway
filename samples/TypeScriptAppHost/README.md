# Minimal TypeScript AppHost Demo

This sample demonstrates the TypeScript AppHost shape for `PinguApps.Aspire.Hosting.PostgreSQL.Railway`.

Run from this directory:

```powershell
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
aspire start --non-interactive --isolated
aspire wait postgres --status healthy --timeout 120 --non-interactive
aspire stop --non-interactive
```

For a live non-interactive deploy:

```powershell
$env:Parameters__railway_postgres_service_name = $env:RAILWAY_POSTGRES_SERVICE_NAME
$env:Parameters__railway_project_id = $env:RAILWAY_PROJECT_ID
$env:Parameters__railway_environment_id = $env:RAILWAY_ENVIRONMENT_ID
$env:Parameters__railway_api_token = $env:RAILWAY_API_TOKEN
aspire deploy --non-interactive
```

`aspire.config.json` references the in-repo package project so the generated TypeScript module matches this checkout.
