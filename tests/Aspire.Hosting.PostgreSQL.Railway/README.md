# Test Matrix

The active test suite currently uses xUnit contract tests for the Railway PostgreSQL integration.

The copied Upstash Redis Reqnroll feature files are preserved as `.feature.disabled` historical references while their behaviour is ported or replaced with Railway-specific coverage. They are intentionally not generated or run.

Run:

```powershell
dotnet test tests\Aspire.Hosting.PostgreSQL.Railway\Aspire.Hosting.PostgreSQL.Railway.Tests.csproj
```
