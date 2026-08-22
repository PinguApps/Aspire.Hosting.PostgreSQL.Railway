## Rolling state
- Goal: Upgrade xUnit to v4 without breaking .NET 10 test execution or CI reporting.
- Current plan: Migrate `dotnet test` to Microsoft Testing Platform and preserve TRX/filter behavior.
- Open questions/risks: None.
- Next actions: Merge PR #45 after final checks pass.
- Key paths: `Directory.Packages.props`, `global.json`, `.github/workflows/_run-tests.yml`.

## Session log
### 2026-08-22 17:14 +01:00 (renovate/major-xunit-dotnet-monorepo)
- Fix xUnit v4 CI compatibility [tests] (impact: med)
  - Why: xUnit v4 requires Microsoft Testing Platform with the .NET 10 SDK.
  - Change: Opted `dotnet test` into MTP and migrated TRX reporting arguments (files: `global.json`, `.github/workflows/_run-tests.yml` | cmds: `dotnet test Aspire.Hosting.PostgreSQL.Railway.slnx -c Release ...`)
  - Notes: Local suite passed 49/49 plus one credential-gated live skip; PR validation passed.
