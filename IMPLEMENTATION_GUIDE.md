# Aspire.Hosting.PostgreSQL.Railway Implementation Guide

## Assumptions

- This repository should mirror `V:\Aspire.Hosting.PostgreSQL.Railway` as closely as practical.
- The public package identity will be `PinguApps.Aspire.Hosting.PostgreSQL.Railway`.
- The local resource of record should be Aspire's built-in `PostgresServerResource`.
- Local development should keep normal Aspire PostgreSQL behavior and must not call Railway.
- Deploy behavior should be opt-in through a `PublishToRailway(...)` extension.
- Ownership modes should match the Railway package shape: `CreateOnly`, `ExistingOnly`, `CreateOrAdopt`.
- Railway deployment needs an existing project id, existing environment id, API token, and PostgreSQL service name.
- Railway PostgreSQL exposes `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, `PGDATABASE`, and `DATABASE_URL`.
- Public contract choice: publish the `PostgresServerResource`; child `AddDatabase(...)` resources should be created inside the Railway PostgreSQL service during deploy.

## External References Checked

- Railway PostgreSQL docs: https://docs.railway.com/databases/postgresql
- Railway Public API docs: https://docs.railway.com/integrations/api/api-cookbook
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

- [ ] Stage 3: Implement Railway provider client.
  - Add typed GraphQL client using Railway Public API.
  - Support locating projects/environments/services, deploying the PostgreSQL template, reading variables, and failure mapping.
  - Keep Railway API token infrastructure-only.
  - Progress: source GraphQL client is implemented; focused create-flow/contract tests pass.
  - Verify remaining: fake HTTP/client tests cover request shapes, auth, errors, and secret redaction.
  - Commit: provider client.

- [ ] Stage 4: Port deployment pipeline.
  - Recreate create/update/create-or-update ownership semantics.
  - Use cached remote identity to avoid accidental drift.
  - Populate Aspire PostgreSQL connection outputs from Railway service variables.
  - Fail clearly on unsafe identity/provider drift.
  - Verify: ownership, create flow, deploy-time resolution, diagnostics, and connection output tests pass.
  - Commit: deployment pipeline.

- [ ] Stage 5: Port test suite and fake Railway harness.
  - Rename features/steps/support from Railway PostgreSQL to Railway PostgreSQL.
  - Replace provider concepts with Railway project/environment/service/template concepts.
  - Preserve live-test pattern, skipping cleanly without `.env` credentials.
  - Progress: stale Upstash/Redis Reqnroll features are preserved as `.feature.disabled`; a Railway-specific xUnit contract suite now covers the current server/resource contract.
  - Verify remaining: rebuild the broader fake Railway harness or remove disabled historical feature files once equivalent Railway coverage exists.
  - Commit: tests.

- [ ] Stage 6: Port TypeScript AppHost support.
  - Preserve Aspire export attributes and generated guest-language module expectations.
  - Update TypeScript sample/fixture to call `publishToRailway`.
  - Verify: TypeScript fixture tests and validation script pass.
  - Commit: TypeScript support.

- [ ] Stage 7: Documentation and repository guidance.
  - Update `README.md`, `docs/`, samples, `.env.example`, and `AGENTS.md`.
  - Document Railway credentials and expected Aspire parameter environment variables.
  - Verify: docs samples compile and no Railway leftovers remain except historical references if intentionally kept.
  - Commit: docs.

- [ ] Stage 8: Final verification.
  - Run format/build/tests.
  - If Railway credentials are available in `.env`, run live deployment tests.
  - Record any live-test blocker explicitly.
  - Commit: final verification fixes if needed.

## Open Questions

- Decision for v1: deploy into an existing Railway project/environment only.
- Decision for v1: the Railway PostgreSQL service name is the stable remote identity, matching the Upstash database-name semantics.
- Decision for v1: app-facing output uses resolved Railway variables from deploy time, not Railway reference expressions.
- Decision for v1: expose the standard Aspire PostgreSQL connection string plus individual Railway PostgreSQL outputs: service id, host, port, user name, password, database name, and connection string.
