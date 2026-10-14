# AGENTS.md
## Operating Principles

### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Repository-Specific Guidance

### Preserve Baseline Content
- Everything above this section is the user-authored baseline. Keep it intact.
- Keep this section concise and accurate for the current released state of the repository.

### Repository Overview
- This repository contains the released `PinguApps.Aspire.Hosting.PostgreSQL.Railway` package.
- The package lets an Aspire AppHost opt a normal `PostgresServerResource` into Railway PostgreSQL during `aspire deploy`.
- Consumer usage starts from standard Aspire PostgreSQL, such as `builder.AddPostgres("postgres")`, then adds `.PublishToRailway(...)`.
- Local development should continue to behave like normal Aspire PostgreSQL. Railway behavior is deploy-only and opt-in.

### Current Product Contract
- This package is Railway PostgreSQL only.
- New services use Railway's standard PostgreSQL template by default, with opt-in Postgres PITR, PostGIS, pgvector, and TimescaleDB templates.
- Remote identity is the explicit Railway PostgreSQL service name.
- Supported ownership modes are `CreateOnly`, `ExistingOnly`, and `CreateOrAdopt`.
- Management authentication uses an infrastructure-only Railway token. Bearer account/workspace tokens remain the default; environment-scoped project tokens require explicit `ProjectToken` mode and project/environment scope validation before service operations.
- Project tokens read Railway templates by public code and use known region IDs; `template(id)` and global `regions` queries return `Not Authorized` with project-token authentication.
- Application-facing outputs expose PostgreSQL connection details, never the Railway API token.
- Repeated deploys must target the same intended remote service.
- Matching volume-backed regions retain their provider identifiers and region map; alias equivalence must not rewrite live routing.
- Child `AddDatabase(...)` resources are created inside the Railway PostgreSQL service during deploy.
- `EnablePublicProvisioningEndpoint` defaults to false. When enabled, verify a scoped active port-5432 TCP proxy before child-database setup; retain and reuse it on later deploys. See [deployment behaviour](docs/deployment-behaviour.md) when changing endpoint activation or output selection.
- The package must not auto-delete remote Railway services.

### Key Paths
- `src/Aspire.Hosting.PostgreSQL.Railway/` contains the package source.
- `src/Aspire.Hosting.PostgreSQL.Railway/Management/` contains the typed Railway management client layer.
- `src/Aspire.Hosting.PostgreSQL.Railway/Deployment/` contains deploy-time ownership, create, database provisioning, and diagnostics logic.
- `tests/Aspire.Hosting.PostgreSQL.Railway/` contains active xUnit contract tests plus disabled historical Reqnroll references.
- `tests/Aspire.Hosting.PostgreSQL.Railway/README.md` explains the current test layout.
- `samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs` is the compile-validated sample source used by docs tests.
- `README.md` is the consumer-facing package guide and should stay aligned with shipped behavior.

### Technical Baseline
- Target framework: `.NET 10`.
- Target Aspire version: `13.5.1`.
- Keep Aspire's built-in `PostgresServerResource` as the resource of record.
- Preserve normal local PostgreSQL behavior unless the work is explicitly about deploy-time Railway behavior.
- Keep app-facing PostgreSQL outputs separate from infrastructure-only management credentials.
- Standard/PITR template connection strings use SSL mode `Require`; PostGIS, pgvector, and TimescaleDB template connection strings use SSL mode `Disable`.
- C# consumers should use normal `.WithReference(postgres/database)` and `.WaitFor(postgres)` calls; this package owns the Railway-aware deploy-time reference translation.

### Testing And Docs
- Any behavior change must update or add active coverage in `tests/Aspire.Hosting.PostgreSQL.Railway/`.
- Historical Reqnroll files are currently disabled with `.feature.disabled`; do not re-enable them until they are ported to Railway PostgreSQL semantics.
- Live-provider scenarios must skip cleanly without `RAILWAY_API_TOKEN`, `RAILWAY_PROJECT_ID`, and `RAILWAY_ENVIRONMENT_ID`, and leave the remote account unchanged after the run.
- Keep `README.md`, `AGENTS.md`, samples, and tests in sync with the actual shipped behavior.
