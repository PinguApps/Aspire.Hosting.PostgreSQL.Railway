using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class OwnershipResolutionStepDefinitions
{
    private readonly FakeOwnershipManagementClient _client = new();
    private RailwayPostgresOwnershipResolutionResult? _result;
    private Exception? _exception;
    private bool _existingDatabaseIsManagedIdentity;

    [Given("the Railway ownership resolver finds no database named {string}")]
    public void GivenTheRailwayOwnershipResolverFindsNoDatabaseNamed(string databaseName)
    {
        _client.DatabaseName = databaseName;
        _client.Database = null;
    }

    [Given("the Railway ownership resolver finds database {string} in region {string} with TLS enabled")]
    public void GivenTheRailwayOwnershipResolverFindsDatabaseInRegionWithTlsEnabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: true);
    }

    [Given("the Railway ownership resolver finds database {string} in region {string} with TLS disabled")]
    public void GivenTheRailwayOwnershipResolverFindsDatabaseInRegionWithTlsDisabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: false);
    }

    [Given("the existing Railway database is the cached managed remote identity")]
    public void GivenTheExistingRailwayDatabaseIsTheCachedManagedRemoteIdentity()
    {
        _existingDatabaseIsManagedIdentity = true;
    }

    [When("ownership is resolved for database {string} with mode {string}")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithMode(string databaseName, string ownershipMode)
    {
        await ResolveAsync(databaseName, ownershipMode, options => options.Tls = true).ConfigureAwait(false);
    }

    [When("ownership is resolved for database {string} with mode {string} and default options")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithModeAndDefaultOptions(string databaseName, string ownershipMode)
    {
        await ResolveAsync(databaseName, ownershipMode, _ => { }).ConfigureAwait(false);
    }

    [When("ownership is resolved for database {string} with mode {string} and primary region {string}")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithModeAndPrimaryRegion(
        string databaseName,
        string ownershipMode,
        string primaryRegion)
    {
        await ResolveAsync(
            databaseName,
            ownershipMode,
            options =>
            {
                options.PrimaryRegion = primaryRegion;
                options.Tls = true;
            }).ConfigureAwait(false);
    }

    [Then("the ownership resolver selects the {string} path")]
    public void ThenTheOwnershipResolverSelectsThePath(string action)
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);
        Assert.Equal(Enum.Parse<RailwayPostgresOwnershipResolutionAction>(action), _result.Action);
    }

    [Then("the ownership resolver selected database {string}")]
    public void ThenTheOwnershipResolverSelectedDatabase(string databaseName)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Database);
        Assert.Equal(databaseName, _result.Database.DatabaseName);
    }

    [Then("the ownership resolver looked up database {string}")]
    public void ThenTheOwnershipResolverLookedUpDatabase(string databaseName)
    {
        string lookup = Assert.Single(_client.Lookups);
        Assert.Equal(databaseName, lookup);
    }

    [Then("ownership resolution fails because {string}")]
    public void ThenOwnershipResolutionFailsBecause(string failureReason)
    {
        RailwayPostgresOwnershipResolutionException exception = Assert.IsType<RailwayPostgresOwnershipResolutionException>(_exception);
        Assert.Equal(Enum.Parse<RailwayPostgresOwnershipResolutionFailureReason>(failureReason), exception.FailureReason);
    }

    [Then("the ownership failure message contains {string}")]
    public void ThenTheOwnershipFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    private void SetExistingDatabase(string databaseName, string primaryRegion, bool tls)
    {
        _client.DatabaseName = databaseName;
        _client.Database = new RailwayPostgresDatabaseDetails
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

    private async Task ResolveAsync(
        string databaseName,
        string ownershipMode,
        Action<RailwayPostgresDeploymentOptions> configure)
    {
        RailwayPostgresDeploymentOptions options = new();
        configure(options);

        RailwayPostgresOwnershipResolutionRequest request = new(
            databaseName,
            Enum.Parse<RailwayPostgresOwnershipMode>(ownershipMode),
            options.ToProviderOptions(),
            _existingDatabaseIsManagedIdentity);

        _exception = await Record.ExceptionAsync(async () =>
            _result = await RailwayPostgresOwnershipResolver
                .ResolveAsync(request, _client, CancellationToken.None)
                .ConfigureAwait(false)).ConfigureAwait(false);
    }

    private sealed class FakeOwnershipManagementClient : IRailwayPostgresManagementClient
    {
        public string? DatabaseName { get; set; }

        public RailwayPostgresDatabaseDetails? Database { get; set; }

        public List<string> Lookups { get; } = [];

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            Lookups.Add(databaseName);

            return Task.FromResult(string.Equals(databaseName, DatabaseName, StringComparison.Ordinal)
                ? Database
                : null);
        }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(RailwayPostgresCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateReadRegionsAsync(string databaseId, RailwayPostgresUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ChangePlanAsync(string databaseId, RailwayPostgresChangePlanRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateBudgetAsync(string databaseId, RailwayPostgresUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
