# Samples

- C# snippets: [`samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs`](../samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs)
- TypeScript sample AppHost: [`samples/TypeScriptAppHost/apphost.mts`](../samples/TypeScriptAppHost/apphost.mts)

From the repository root, use `.env.example` as the live-deploy credential template:

```powershell
Copy-Item .env.example .env
```

Then fill in:

- `RAILWAY_API_TOKEN`
- `RAILWAY_PROJECT_ID`
- `RAILWAY_ENVIRONMENT_ID`
- `RAILWAY_POSTGRES_SERVICE_NAME`

The TypeScript sample has its own restore/typecheck flow:

```powershell
Push-Location samples\TypeScriptAppHost
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
Pop-Location
```
