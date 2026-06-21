# Aspire.Hosting.PostgreSQL.Railway Implementation Guide

This is a historical migration log for the initial repository port. For current consumer guidance, use `README.md` and `docs/`.

## Assumptions

- This repository should mirror `V:\Aspire.Hosting.PostgreSQL.Railway` as closely as practical.
- The public package identity will be `PinguApps.Aspire.Hosting.PostgreSQL.Railway`.
- The local resource of record should be Aspire's built-in `PostgresServerResource`.
- Local development should keep normal Aspire PostgreSQL behavior and must not call Railway.
- Deploy behavior should be opt-in through a `PublishToRailway(...)` extension.
- Ownership modes should match the Railway package shape: `CreateOnly`, `ExistingOnly`, `CreateOrAdopt`.
- Railway deployment needs an existing project id, existing environment id or exact environment name, API token, and PostgreSQL service name.
- Railway PostgreSQL exposes PostgreSQL variables such as `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, `PGDATABASE`, `DATABASE_PUBLIC_URL`, `DATABASE_URL`, and TCP proxy variables when enabled.
- Public contract choice: publish the `PostgresServerResource`; child `AddDatabase(...)` resources should be created inside the Railway PostgreSQL service during deploy.

## External References Checked

- Railway PostgreSQL docs: https://docs.railway.com/databases/postgresql
- Railway Public API docs: https://docs.railway.com/integrations/api/api-cookbook
- Railway environments API docs: https://docs.railway.com/integrations/api/manage-environments
- Railway services API docs: https://docs.railway.com/integrations/api/manage-services
- Railway variables API docs: https://docs.railway.com/integrations/api/manage-variables

## Stage Checklist

- [x] Stage 0: Inspect source and target repositories.
  - Verify: target repo is empty except `.git`; source repo has full Railway package structure.
  - Commit: planning guide.

- [x] Stage 1: Bootstrap repository structure from Upstash Redis.
  - Copy source repo content excluding `.git`, `.vs`, `artifacts`, `.env`, and other generated/local-only files.
  - Rename solution/project folders and package metadata to PostgreSQL Railway.
  - Verify: `rg "Upstash|upstash"` has no matches; Redis resource/test remnants remain for Stage 2.
  - Commit: bootstrapped structure.

- [x] Stage 2: Port public API and application model surface.
  - Replace Redis-specific extension methods with PostgreSQL server extensions.
  - Preserve deploy-only behavior, pipeline registration pattern, TypeScript export attributes, DTO style, and output annotations.
  - Verify: source package builds cleanly with `dotnet build src\Aspire.Hosting.PostgreSQL.Railway\Aspire.Hosting.PostgreSQL.Railway.csproj --no-restore`.
  - Commit: public API surface.

- [x] Stage 3: Implement Railway provider client.
  - Add typed GraphQL client using Railway Public API.
  - Support locating projects/environments/services, deploying the PostgreSQL template, reading variables, and failure mapping.
  - Keep Railway API token infrastructure-only.
  - Verify: source GraphQL client is implemented; PostgreSQL template deployment uses `templateDeployV2` with the official template `serializedConfig`; active tests cover template deployment and private/public connection-string separation.
  - Commit: provider client.

- [x] Stage 4: Port deployment pipeline.
  - Recreate create/update/create-or-update ownership semantics.
  - Use cached remote identity to avoid accidental drift.
  - Populate Aspire PostgreSQL connection outputs from Railway service variables.
  - Fail clearly on unsafe identity/provider drift.
  - Verify: ownership, create flow, deploy-time resolution, diagnostics, connection output tests, and live TypeScript sample deployment pass.
  - Commit: deployment pipeline.

- [x] Stage 5: Port test suite and fake Railway harness.
  - Rename active coverage from Upstash Redis to Railway PostgreSQL.
  - Replace provider concepts with Railway project/environment/service/template concepts.
  - Preserve live-test pattern, skipping cleanly without `.env` credentials.
  - Verify: stale Upstash/Redis Reqnroll features are preserved as `.feature.disabled`; the active Railway-specific xUnit contract suite passes and live deploy was verified with `.env` credentials.
  - Commit: tests.

- [x] Stage 6: Port TypeScript AppHost support.
  - Preserve Aspire export attributes and generated guest-language module expectations.
  - Update TypeScript sample/fixture to call `publishToRailway`.
  - Verify: `aspire restore --non-interactive` and `npm run typecheck` pass in `samples/TypeScriptAppHost`.
  - Commit: TypeScript support.

- [x] Stage 7: Documentation and repository guidance.
  - Update `README.md`, `docs/`, samples, `.env.example`, and `AGENTS.md`.
  - Document Railway credentials and expected Aspire parameter environment variables.
  - Verify: docs and active samples use Railway PostgreSQL service/project/environment/token terminology; stale Reqnroll features remain intentionally disabled as historical references.
  - Commit: docs.

- [x] Stage 8: Final verification.
  - Run format/build/tests.
  - If Railway credentials are available in `.env`, run live deployment tests.
  - Verify: `dotnet test`, package build, `aspire restore`, TypeScript typecheck, and live `aspire deploy` all pass. Live deploy adopted Railway service `orders-postgres` and completed the custom PostgreSQL step.
  - Commit: final verification fixes if needed.

## Open Questions

- Decision for v1: deploy into an existing Railway project/environment only.
- Decision for v1: the Railway PostgreSQL service name is the stable remote identity.
- Decision for v1: app-facing output uses resolved Railway variables from deploy time, not Railway reference expressions.
- Decision for v1: expose the standard Aspire PostgreSQL connection string plus individual Railway PostgreSQL outputs: service id, host, port, user name, password, database name, and connection string.
