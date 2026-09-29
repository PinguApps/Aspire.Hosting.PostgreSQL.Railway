## Rolling state
- Goal: Support environment-scoped Railway project tokens for PostgreSQL publishing, then adopt the released package in Template.
- Current plan: Package PR #125 is open, mergeable, and green on current HEAD (CI, PR Agent, Gitar). Pingu will review/merge and publish NuGet before Template adoption.
- Open questions/risks: Package release and Template pin await Pingu's merge/release; no live Railway resources remain.
- Next actions: After Pingu confirms merge and NuGet release, update `V:\Template` PostgreSQL variant, generated secrets/release guidance, and smoke/build tests.
- Key paths: `src/Aspire.Hosting.PostgreSQL.Railway/Management/`, `tests/Aspire.Hosting.PostgreSQL.Railway/RailwayPostgresContractTests.cs`, `V:\Template`.

## Session log
### 2026-09-29 01:52 +01:00 (agent/pin-665-project-token)
- Add explicit project-token mode [auth] (impact: med)
  - Why: PIN-665 requires environment-scoped Railway credentials and scope checks before service operations.
  - Change: Added C#/TypeScript mode propagation, single-header selection, projectToken scope preflight, and HTTP-200 GraphQL error classification/redaction (files: `src/Aspire.Hosting.PostgreSQL.Railway/`); committed `411b71a`, `ad2de35`, `7b30bb9`.
- Verify package contract [tests] (impact: low)
  - Change: Added active header, scope mismatch, secret redaction, and bridge tests; ran `dotnet test` (57 passed, 1 skipped), TypeScript restore/typecheck, and local NuGet package gate (passed).
- Open package PR [build] (impact: low)
  - Change: Pushed branch and opened PR #125; CI and PR Agent passed current code HEAD, local merge probe clean; Gitar approved prior HEAD, then paused automatic reviews due quota.
  - Notes: PR remains unmerged; package release and Template pin await Pingu's merge/release.
- Prepare live Railway proof [infra] (impact: low)
  - Change: Created empty disposable Railway project `d2e082ee-f48b-4e07-982c-4eadb404b0da` with production `055234c4-ef94-424d-9ffc-b17e3c608e57` and staging `3452834d-2daf-4a83-ab27-fb8093f36452` in Chrome.
  - Notes: No token or service created. Browser credential-creation confirmation requested; live proof is pending.

- Address review coverage [tests] (impact: low)
  - Change: Added separate tests for GraphQL authorization redaction, invalid C#/TypeScript enum values, and production pipeline preflight (commits: c87d861, 923c06a, 44660d5).
  - Notes: Replied to both Copilot threads; PR Agent verified them and left zero unresolved threads. No live tokens or services yet.

### 2026-09-29 14:13 +01:00 (agent/pin-665-project-token)
- Prove scoped Railway path [infra] (impact: med)
  - Why: PIN-665 required real Standard/PITR creation and environment isolation before release.
  - Change: Created production/staging project tokens in Chrome; verified `projectToken` scope, Standard/PITR `templateDeployV2` creation in `ams`, repeat adoption, reconciliation, outputs, child databases, and cross-environment rejection.
  - Notes: `template(id)` and global `regions` returned `Not Authorized`; `template(code)` and known region IDs worked. Revoked both tokens and immediately deleted the disposable project; Railway project query returns `Project not found`.
- Fix provider API compatibility [api] (impact: med)
  - Change: Switched public template lookup to code, mapped supported project-token region IDs, added active contract coverage, and updated AGENTS baseline (files: `RailwayPostgresManagementClient.cs`, `RailwayPostgresContractTests.cs`, `AGENTS.md`; commit `1c45537`).
  - Notes: Full suite 61 passed, 1 opt-in live test skipped; local TypeScript NuGet package gate passed.
- Finish package PR review [build] (impact: low)
  - Change: Updated PR #125 and Linear PIN-665 with live evidence; CI, PR Agent, and manually triggered Gitar review passed on current HEAD.
  - Notes: PR remains unmerged; NuGet release and Template work await Pingu's merge/release.
