## Rolling state
- Goal: Publish the existing release draft weekly or manually when main has advanced, then publish the tagged NuGet packages.
- Current plan: Complete PR #123 review and CI, then leave it ready to merge.
- Open questions/risks: A manual dispatch publishes the draft and NuGet packages; avoid running it during verification.
- Next actions: Push review fixes, await current-HEAD CI and reviewer verdicts, complete final audit.
- Key paths: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`.

## Session log
### 2026-09-28 14:20 +01:00 (agent/weekly-publish-draft)
- Add weekly release draft publishing [build] (impact: med)
  - Why: Mirror Upstash Redis commit `dafedf4` for this repo's release process.
  - Change: Added scheduled/manual draft publisher and reusable tag-aware NuGet publishing (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Caller grants `id-token: write` because this repo's NuGet login uses OIDC; read-only API check found published `v1.0.0` and draft `v1.0.1`.
- Address release publishing review [build] (impact: med)
  - Why: Prevent automatic publishing of an ambiguous draft or an older package version.
  - Change: Fail with multiple drafts and reject malformed or non-increasing draft tags (file: `.github/workflows/publish-release-draft.yml`).
  - Notes: PR Agent accepted the zero-releases rebuttal; actionlint and focused Bash version comparisons passed.
