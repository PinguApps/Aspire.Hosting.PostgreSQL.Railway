using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using System.Text.RegularExpressions;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class LiveProviderStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;
    private bool _olderCleanupActionRan;
    private Exception? _cleanupFailure;
    private List<string> _generatedDatabaseNames = [];

    public LiveProviderStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("live Railway credentials are available")]
    public void GivenLiveRailwayCredentialsAreAvailable()
    {
        Assert.False(string.IsNullOrWhiteSpace(_context.LiveRailway.AccountEmail));
        Assert.False(string.IsNullOrWhiteSpace(_context.LiveRailway.ApiKey));
    }

    [Then("live Railway cleanup is registered through the shared cleanup path")]
    public void ThenLiveRailwayCleanupIsRegisteredThroughTheSharedCleanupPath()
    {
        _context.LiveRailway.RegisterCleanup(static () => Task.CompletedTask);

        Assert.Equal(1, _context.LiveRailway.CleanupActionCount);
    }

    [Given("live Railway cleanup has an older action registered")]
    public void GivenLiveRailwayCleanupHasAnOlderActionRegistered()
    {
        _context.LiveRailway.RegisterCleanup(() =>
        {
            _olderCleanupActionRan = true;

            return Task.CompletedTask;
        });
    }

    [Given("live Railway cleanup has a newer failing action registered")]
    public void GivenLiveRailwayCleanupHasANewerFailingActionRegistered()
    {
        _context.LiveRailway.RegisterCleanup(static () => throw new InvalidOperationException("Cleanup failed."));
    }

    [Given("live Railway cleanup action {string} succeeds")]
    public void GivenLiveRailwayCleanupActionSucceeds(string actionName)
    {
        _context.LiveRailway.RegisterCleanup(() =>
        {
            _context.LiveCleanupLog.Add(actionName);
            return Task.CompletedTask;
        });
    }

    [Given("live Railway cleanup action {string} fails")]
    public void GivenLiveRailwayCleanupActionFails(string actionName)
    {
        _context.LiveRailway.RegisterCleanup(() =>
        {
            _context.LiveCleanupLog.Add(actionName);
            throw new InvalidOperationException(actionName);
        });
    }

    [When("a null live Railway cleanup action is registered")]
    public void WhenANullLiveRailwayCleanupActionIsRegistered()
    {
        _context.LastCleanupException = Record.Exception(() => _context.LiveRailway.RegisterCleanup(null!));
    }

    [When("live Railway cleanup runs")]
    public async Task WhenLiveRailwayCleanupRuns()
    {
        _cleanupFailure = await Record.ExceptionAsync(_context.LiveRailway.CleanupAsync);
        _context.LastCleanupException = _cleanupFailure;
    }

    [When("live disposable database names are generated with prefix {string}")]
    public void WhenLiveDisposableDatabaseNamesAreGeneratedWithPrefix(string prefix)
    {
        _generatedDatabaseNames =
        [
            LiveRailwayTestSession.CreateDisposableDatabaseName(prefix),
            LiveRailwayTestSession.CreateDisposableDatabaseName(prefix),
        ];
    }

    [Then("the older live Railway cleanup action ran")]
    public void ThenTheOlderLiveRailwayCleanupActionRan()
    {
        Assert.True(_olderCleanupActionRan);
    }

    [Then("the live Railway cleanup failure is reported")]
    public void ThenTheLiveRailwayCleanupFailureIsReported()
    {
        InvalidOperationException failure = Assert.IsType<InvalidOperationException>(_cleanupFailure);

        Assert.Equal("Cleanup failed.", failure.Message);
    }

    [Then("live Railway cleanup registration fails for a null cleanup action")]
    public void ThenLiveRailwayCleanupRegistrationFailsForANullCleanupAction()
    {
        Assert.IsType<ArgumentNullException>(_context.LastCleanupException);
    }

    [Then("every live Railway cleanup action has run")]
    public void ThenEveryLiveRailwayCleanupActionHasRun()
    {
        Assert.Equal(["third", "second", "first"], _context.LiveCleanupLog);
        Assert.Equal(0, _context.LiveRailway.CleanupActionCount);
    }

    [Then("live Railway cleanup reports {int} failures")]
    public void ThenLiveRailwayCleanupReportsFailures(int failureCount)
    {
        AggregateException exception = Assert.IsType<AggregateException>(_context.LastCleanupException);

        Assert.Equal(failureCount, exception.InnerExceptions.Count);
    }

    [Then("each live disposable database name is at most {int} characters")]
    public void ThenEachLiveDisposableDatabaseNameIsAtMostCharacters(int maxLength)
    {
        Assert.All(_generatedDatabaseNames, databaseName => Assert.True(databaseName.Length <= maxLength));
    }

    [Then("each live disposable database name ends with an {int} character GUID suffix")]
    public void ThenEachLiveDisposableDatabaseNameEndsWithAnCharacterGuidSuffix(int suffixLength)
    {
        Regex suffixPattern = new($"-[0-9a-f]{{{suffixLength}}}$");

        Assert.All(_generatedDatabaseNames, databaseName => Assert.Matches(suffixPattern, databaseName));
    }

    [Then("the live disposable database names are unique")]
    public void ThenTheLiveDisposableDatabaseNamesAreUnique()
    {
        Assert.Equal(_generatedDatabaseNames.Count, _generatedDatabaseNames.Distinct().Count());
    }
}
