## Rolling state
- Goal: Support environment-scoped Railway project tokens for PostgreSQL publishing, then adopt the released package in Template.
- Current plan: Package PR #125 is open with CI, PR Agent, and Gitar approved; live production/staging creation proof remains before merge.
- Open questions/risks: Browser confirmation is pending before creating two scoped project tokens; `templateDeployV2` and subsequent provider operations have not been proven with them.
- Next actions: Create scoped test tokens after confirmation; run production/staging create/adopt/reconcile/cross-environment smoke; remove the disposable Railway project; update PR evidence and review; after Pingu merges and releases, update Template.
- Key paths: `src/Aspire.Hosting.PostgreSQL.Railway/Management/`, `tests/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresContractTests.cs`, `V:\Template`.

## Session log
### 2026-09-29 01:52 +01:00 (agent/pin-665-project-token)
- Add explicit project-token mode [auth] (impact: med)
  - Why: PIN-665 requires environment-scoped Railway credentials and scope checks before service operations.
  - Change: Added C#/TypeScript mode propagation, single-header selection, projectToken scope preflight, and HTTP-200 GraphQL error classification/redaction (files: `src/Aspire.Hosting.PostgreSQL.Railway/`); committed `411b71a`, `ad2de35`, `7b30bb9`.
- Verify package contract [tests] (impact: low)
  - Change: Added active header, scope mismatch, secret redaction, and bridge tests; ran `dotnet test` (57 passed, 1 skipped), TypeScript restore/typecheck, and local NuGet package gate (passed).
- Open package PR [build] (impact: low)
  - Change: Pushed branch and opened PR #125; CI, PR Agent, Gitar approved current HEAD; local merge probe clean.
  - Notes: PR remains unmerged; package release and Template pin await Pingu's merge/release.
- Prepare live Railway proof [infra] (impact: low)
  - Change: Created empty disposable Railway project `d2e082ee-f48b-4e07-982c-4eadb404b0da` with production `055234c4-ef94-424d-9ffc-b17e3c608e57` and staging `3452834d-2daf-4a83-ab27-fb8093f36452` in Chrome.
  - Notes: No token or service created. Browser credential-creation confirmation requested; live proof is pending.
