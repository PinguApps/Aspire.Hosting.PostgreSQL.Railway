using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

[Binding]
public sealed class LiveRailwayScenarioHooks
{
    private readonly RailwayPostgresScenarioContext _context;

    public LiveRailwayScenarioHooks(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [BeforeScenario("live-railway")]
    public void SkipLiveScenarioWithoutCredentials()
    {
        Assert.SkipUnless(
            _context.LiveRailway.HasCredentials,
            "Live Railway scenarios require RAILWAY_API_TOKEN, RAILWAY_PROJECT_ID, and RAILWAY_ENVIRONMENT_ID.");
    }

    [AfterScenario("live-railway")]
    public Task RunLiveScenarioCleanup()
    {
        return _context.LiveRailway.CleanupAsync();
    }
}
