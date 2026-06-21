## Rolling state
- Goal: Add enum-based Railway PostgreSQL template selection.
- Current plan: `RailwayPostgresTemplate` supports Standard, PITR, PostGIS, pgvector, and TimescaleDB; variable aliases cover PostGIS-style templates.
- Open questions/risks: Template choice is create-time only; adopted services are not converted; live Railway test skips without secrets.
- Next actions: optional live deploy per template; consider service-specific validation if multi-service templates are added later.
- Key paths: `src/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresTemplate.cs`, `src/Aspire.Hosting.PostgreSQL.Railway/Management/RailwayPostgresManagementClient.cs`, `tests/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresContractTests.cs`, `README.md`.

## Session log
### 2026-06-21 09:52 +01:00 (pingu/additional-templates)
- Add Railway Postgres template enum [infra] (impact: med)
  - Why: support PostGIS, pgvector, and TimescaleDB without stacking boolean options.
  - Change: replaced PITR bool with `RailwayPostgresTemplate`, mapped Standard/PITR/PostGIS/pgvector/TimescaleDB template ids, added PostGIS variable alias parsing, updated DTO/docs/samples/tests and manual app enum usage (files: `RailwayPostgresTemplate.cs`, `RailwayPostgresDeploymentOptions*.cs`, `RailwayPostgresManagementClient.cs`, `RailwayPostgresContractTests.cs`, docs/samples, `V:\temp-aspire-postgresql\src\AppHost\AppHost.cs`)
  - Notes: verified `dotnet test Aspire.Hosting.PostgreSQL.Railway.slnx -c Debug --no-restore` passed 38/39 with live skip, `eng\Validate-TypeScriptAppHostPackage.ps1` passed, and manual AppHost build passed; no deploy run.
