## Rolling state
- Goal: Publish the existing release draft weekly or manually when main has advanced, then publish the tagged NuGet packages.
- Current plan: Validate workflows, open PR, and complete review and CI.
- Open questions/risks: A manual dispatch publishes the draft and NuGet packages; avoid running it during verification.
- Next actions: Run actionlint, commit, push, open PR, and review CI.
- Key paths: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`.

## Session log
### 2026-09-28 14:20 +01:00 (agent/weekly-publish-draft)
- Add weekly release draft publishing [build] (impact: med)
  - Why: Mirror Upstash Redis commit `dafedf4` for this repo's release process.
  - Change: Added scheduled/manual draft publisher and reusable tag-aware NuGet publishing (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Caller grants `id-token: write` because this repo's NuGet login uses OIDC; read-only API check found published `v1.0.0` and draft `v1.0.1`.
