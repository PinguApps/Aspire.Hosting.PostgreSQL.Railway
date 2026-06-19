# Minimal TypeScript AppHost Demo

This sample demonstrates the supported TypeScript AppHost shape for `PinguApps.Aspire.Hosting.PostgreSQL.Railway`.

Run from this directory:

```powershell
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
aspire start --non-interactive --isolated
aspire wait cache --status healthy --timeout 120 --non-interactive
aspire stop --non-interactive
```

For a live non-interactive deploy, provide real Aspire parameter environment variables:

```powershell
$env:Parameters__railway_database_name = "railway-ts-demo"
$env:Parameters__railway_account_email = $env:RAILWAY_EMAIL
$env:Parameters__railway_api_key = $env:RAILWAY_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

`aspire.config.json` references the in-repo package project so the generated TypeScript module matches this checkout. Generated `.aspire/` content is intentionally ignored.
