## Rolling state
- Goal: Address PR #3 review feedback for enum-based Railway PostgreSQL templates.
- Current plan: PR comments fixed locally in commits `4d419e1` and `b52ee94`; all fetched unresolved threads have replies.
- Open questions/risks: Template choice is create-time only; adopted services default to standard SSL assumptions because Railway does not expose selected template metadata here.
- Next actions: user can inspect/push local commits; optional live deploy per template.
- Key paths: `src/Aspire.Hosting.PostgreSQL.Railway/Management/RailwayPostgresManagementClient.cs`, `src/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresDeploymentOptions.cs`, `tests/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresContractTests.cs`, `README.md`.

## Session log
### 2026-06-21 09:52 +01:00 (pingu/additional-templates)
- Add Railway Postgres template enum [infra] (impact: med)
  - Why: support PostGIS, pgvector, and TimescaleDB without stacking boolean options.
  - Change: replaced PITR bool with `RailwayPostgresTemplate`, mapped Standard/PITR/PostGIS/pgvector/TimescaleDB template ids, added PostGIS variable alias parsing, updated DTO/docs/samples/tests and manual app enum usage (files: `RailwayPostgresTemplate.cs`, `RailwayPostgresDeploymentOptions*.cs`, `RailwayPostgresManagementClient.cs`, `RailwayPostgresContractTests.cs`, docs/samples, `V:\temp-aspire-postgresql\src\AppHost\AppHost.cs`)
  - Notes: verified `dotnet test Aspire.Hosting.PostgreSQL.Railway.slnx -c Debug --no-restore` passed 38/39 with live skip, `eng\Validate-TypeScriptAppHostPackage.ps1` passed, and manual AppHost build passed; no deploy run.
### 2026-06-21 16:23 +01:00 (pingu/additional-templates)
- Fix PR #3 review feedback [infra] (impact: med)
  - Why: unresolved PR threads flagged validation diagnostics, PITR compatibility, TCP proxy variables, and pgvector SSL.
  - Change: added obsolete PITR pass-through, included invalid template value in validation, built public URLs from Railway TCP proxy vars, made readiness connection SSL template-aware, added regression tests (files: `RailwayPostgresDeploymentOptions*.cs`, `RailwayPostgresManagementClient.cs`, `RailwayPostgresConnectionString.cs`, `RailwayPostgresContractTests.cs`)
  - Notes: committed locally as `4d419e1`, replied to all four unresolved threads, no push; `dotnet test Aspire.Hosting.PostgreSQL.Railway.slnx -c Debug --no-restore` passed 42/43 with live skip, `eng\Validate-TypeScriptAppHostPackage.ps1` passed.
### 2026-06-21 18:16 +01:00 (pingu/additional-templates)
- Fix PR #3 SSL follow-up feedback [infra] (impact: med)
  - Why: new unresolved threads flagged requested-template SSL leakage for adopted services and non-SSL PostGIS/TimescaleDB templates.
  - Change: only newly-created services use configured template for readiness SSL; adopted services use default SSL; PostGIS/pgvector/TimescaleDB use `SslMode.Disable`; added regression tests (files: `RailwayPostgresCreateFlow.cs`, `RailwayPostgresDeploymentPipeline.cs`, `RailwayPostgresManagementClient.cs`, `RailwayPostgresContractTests.cs`)
  - Notes: committed locally as `b52ee94`, replied to both unresolved threads, no push; `dotnet test Aspire.Hosting.PostgreSQL.Railway.slnx -c Debug --no-restore` passed 46/47 with live skip, `eng\Validate-TypeScriptAppHostPackage.ps1` passed.
