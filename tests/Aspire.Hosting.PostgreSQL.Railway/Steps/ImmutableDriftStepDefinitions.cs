using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class ImmutableDriftStepDefinitions
{
    private RailwayPostgresDatabaseDetails? _existingDatabase;
    private RailwayPostgresImmutableDrift? _drift;
    private Exception? _exception;

    [Given("an existing Railway PostgreSQL database detail named {string} in region {string} with TLS enabled")]
    public void GivenAnExistingRailwayPostgresDatabaseDetailNamedInRegionWithTlsEnabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: true);
    }

    [Given("an existing Railway PostgreSQL database detail named {string} in region {string} with TLS disabled")]
    public void GivenAnExistingRailwayPostgresDatabaseDetailNamedInRegionWithTlsDisabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: false);
    }

    [When("immutable drift is checked for requested database {string} with default options")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithDefaultOptions(string databaseName)
    {
        CheckDrift(databaseName, _ => { });
    }

    [When("immutable drift is checked for requested database {string} with platform {string}")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithPlatform(string databaseName, string platform)
    {
        CheckDrift(databaseName, options => options.Platform = platform);
    }

    [When("immutable drift is checked for requested database {string} with primary region {string}")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithPrimaryRegion(string databaseName, string primaryRegion)
    {
        CheckDrift(databaseName, options => options.PrimaryRegion = primaryRegion);
    }

    [When("immutable drift is checked for requested database {string} with mutable settings")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithMutableSettings(string databaseName)
    {
        CheckDrift(
            databaseName,
            options =>
            {
                options.SetReadRegions(RailwayPostgresRegion.AwsEuWest2);
                options.SetPlan(RailwayPostgresPlan.PayAsYouGo);
                options.SetBudget(360);
                options.Eviction = false;
            });
    }

    [When("immutable drift exception is created without drift details")]
    public void WhenImmutableDriftExceptionIsCreatedWithoutDriftDetails()
    {
        _exception = Record.Exception(() => new RailwayPostgresImmutableDriftException((RailwayPostgresImmutableDrift)null!));
    }

    [Then("immutable drift detection succeeds")]
    public void ThenImmutableDriftDetectionSucceeds()
    {
        Assert.Null(_exception);
        Assert.Null(_drift);
    }

    [Then("immutable drift detection fails because {string}")]
    public void ThenImmutableDriftDetectionFailsBecause(string failureReason)
    {
        RailwayPostgresImmutableDriftException exception = Assert.IsType<RailwayPostgresImmutableDriftException>(_exception);
        RailwayPostgresImmutableDrift drift =
            exception.Drift ?? throw new InvalidOperationException("Immutable drift exception did not include drift details.");

        Assert.Equal(Enum.Parse<RailwayPostgresImmutableDriftFailureReason>(failureReason), drift.FailureReason);
        _drift = drift;
    }

    [Then("the immutable drift failure message contains {string}")]
    public void ThenTheImmutableDriftFailureMessageContains(string expectedText)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("Immutable drift detection did not fail.");

        Assert.Contains(expectedText, exception.Message, StringComparison.Ordinal);
    }

    [Then("immutable drift exception construction fails with {string}")]
    public void ThenImmutableDriftExceptionConstructionFailsWith(string exceptionTypeName)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("Immutable drift exception construction did not fail.");

        Assert.Equal(exceptionTypeName, exception.GetType().Name);
    }

    private void SetExistingDatabase(string databaseName, string primaryRegion, bool tls)
    {
        _existingDatabase = new RailwayPostgresDatabaseDetails
        {
            DatabaseId = $"db-{databaseName}",
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.railway.io",
            Port = 6379,
            Password = "test-password",
            PrimaryRegion = primaryRegion,
            Tls = tls,
        };
    }

    private void CheckDrift(string databaseName, Action<RailwayPostgresDeploymentOptions> configure)
    {
        RailwayPostgresDatabaseDetails existingDatabase =
            _existingDatabase ?? throw new InvalidOperationException("The existing database detail has not been configured.");

        RailwayPostgresDeploymentOptions options = new();
        configure(options);

        _exception = Record.Exception(() =>
            RailwayPostgresImmutableDriftDetector.Validate(
                databaseName,
                options.ToProviderOptions(),
                existingDatabase));

        _drift = (_exception as RailwayPostgresImmutableDriftException)?.Drift;
    }
}
