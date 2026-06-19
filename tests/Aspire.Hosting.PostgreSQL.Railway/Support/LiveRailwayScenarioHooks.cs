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
            "Live Railway scenarios require RAILWAY_EMAIL and RAILWAY_API_KEY.");
    }

    [AfterScenario("live-railway")]
    public Task RunLiveScenarioCleanup()
    {
        return _context.LiveRailway.CleanupAsync();
    }
}
