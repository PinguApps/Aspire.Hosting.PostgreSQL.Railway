# AGENTS.md
## WORK DIARY

### Purpose
- Keep a small, high-signal diary so future sessions can resume quickly.

### When to read/write
- On session start: read the diary file (if it exists) to regain context.
- Once per response (just before replying): update the diary ONLY if you took meaningful actions (code/config changes, important commands run, decisions made, constraints/bugs discovered, tasks created that affect next steps). Otherwise: do not write.

### Location
- Always read/write inside `.diary/`

### Filename (from git branch)
- If branch is `vk/<suffix>` or `feature/<suffix>` → file is `.diary/<suffix>.md`
  - e.g. `vk/ab12-foo-bar` → `.diary/ab12-foo-bar.md`
- The branch should always have a prefix, but the prefix cannot be guaranteed, just use the suffix in every case after the `/`.

### Format (Markdown, compact)
- The file has two sections:

1) Rolling state (edit in place; keep ≤12 bullets total)
```
## Rolling state
- Goal: <one sentence>
- Current plan: <1–3 bullets>
- Open questions/risks: <0–3 bullets>
- Next actions: <1–5 bullets>
- Key paths: <optional; 1–5 entries>
```

2) Session log (append-only; per response keep ≤5 bullets)
```
## Session log
### <YYYY-MM-DD HH:MM Z> (<branch>)
- <Verb + object> [area] (impact: none|low|med|high)
  - Why: <reason/decision>
  - Change: <what changed> (files: <a,b,c> | cmds: `<...>`)
  - Notes: <gotchas/follow-ups> (optional)
```

### Compression rules
- Prefer deltas over narration (Add/Remove/Refactor/Fix).
- Use short tags for area: [ui] [api] [db] [auth] [infra] [build] [tests] etc...
- Include "Why" for any non-obvious decision and "Notes" for any caveat.
- Do not exceed caps; omit low-value detail.

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
- Management authentication uses a Railway API token and is infrastructure-only.
- Application-facing outputs expose PostgreSQL connection details, never the Railway API token.
- Repeated deploys must target the same intended remote service.
- Child `AddDatabase(...)` resources are created inside the Railway PostgreSQL service during deploy.
- The package must not auto-delete remote Railway services.

### Key Paths
- `src/Aspire.Hosting.PostgreSQL.Railway/` contains the package source.
- `src/Aspire.Hosting.PostgreSQL.Railway/Management/` contains the typed Railway management client layer.
- `src/Aspire.Hosting.PostgreSQL.Railway/Deployment/` contains deploy-time ownership, create, database provisioning, and diagnostics logic.
- `tests/Aspire.Hosting.PostgreSQL.Railway/` contains active xUnit contract tests plus disabled historical Reqnroll references.
- `tests/Aspire.Hosting.PostgreSQL.Railway/README.md` explains the current test layout.
- `samples/AppHostSnippets/RailwayPostgresAppHostSnippets.cs` is the compile-validated sample source used by docs tests.
- `README.md` is the consumer-facing package guide and should stay aligned with shipped behavior.
- `.diary/` contains branch-specific session state and must be maintained per the diary rules above.

### Technical Baseline
- Target framework: `.NET 10`.
- Target Aspire version: `13.4.3`.
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
