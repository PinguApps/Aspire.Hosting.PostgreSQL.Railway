## Rolling state
- Goal: Fix equivalent Railway region identifiers blocking adoption of the retained VikingOutreach database.
- Branch: `agent/postgres-region-aliases`, based on fresh `origin/main` at `01401c6`.
- Change: Normalise four known aliases for scalar and multi-region deployment metadata comparisons.
- Verification: Release build has zero warnings/errors; focused checks 13/13; full suite 86 passed, one credential-dependent test skipped.
- Regression evidence: All ten equivalent-region cases failed before the fix and pass afterwards; genuinely different regions still fail before writes.
- Consumer evidence: Published 1.1.2 rejected real retained EU metadata; locally packed candidate 9999.0.57 accepted it without requesting redeployment. Both used live project-token reads; all mutations were intercepted and provider writes were zero.
- Next: Complete PR CI/review. Pingu owns merging and package publication; resume VikingOutreach only after his release confirmation.
- Private verification artifacts: Operator-only VIK-57/package-region-validation folder; no credentials or provider metadata committed.

## Session log
### 2026-10-11 (agent/postgres-region-aliases)
- Fix alias equivalence [infra] (impact: med)
  - Why: `ams` and `europe-west4-drams3a` refer to the same retained database region.
  - Change: Normalise comparisons, document aliases, cover both authentication modes and deployment metadata shapes.
  - Notes: Provider ID writes and actual volume-migration rejection remain intact; production deployment is deferred until package publication.
- Verify candidate artifact [tests] (impact: low)
  - Change: Run Release build, contract suite, and private NuGet consumer against live read-only metadata.
  - Notes: Intercepted mutation responses prove the guard/redeployment decision, not a live deployment.
