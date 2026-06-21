using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class LiveDeployOutputsStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;
    private string? _databaseName;
    private RailwayPostgresDatabaseDetails? _firstDeploymentDatabase;
    private RailwayPostgresDatabaseDetails? _secondDeploymentDatabase;
    private bool _cleanupRegistered;

    public LiveDeployOutputsStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("a live disposable Railway PostgreSQL deployment with prefix {string}")]
    public async Task GivenALiveDisposableRailwayPostgresDeploymentWithPrefix(string prefix)
    {
        _context.AddRedis("cache");

        _databaseName = LiveRailwayTestSession.CreateDisposableDatabaseName(prefix);
        _context.MarkRedisForRailway(_databaseName, RailwayPostgresOwnershipMode.CreateOrAdopt);

        await _context.LiveRailway.RegisterDatabaseDeletionByNameAsync(_databaseName).ConfigureAwait(false);
        _cleanupRegistered = true;
    }

    [When("the live Railway deployment runs")]
    public async Task WhenTheLiveRailwayDeploymentRuns()
    {
        _firstDeploymentDatabase = await RunDeploymentAsync(cachedIdentity: null).ConfigureAwait(false);
    }

    [When("the live Railway deployment runs twice")]
    public async Task WhenTheLiveRailwayDeploymentRunsTwice()
    {
        _firstDeploymentDatabase = await RunDeploymentAsync(cachedIdentity: null).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunDeploymentAsync(
            new RailwayPostgresRemoteIdentityState(GetDatabaseName(), _firstDeploymentDatabase.DatabaseId))
            .ConfigureAwait(false);
    }

    [Then("the live Railway database exists with the configured name")]
    public void ThenTheLiveRailwayDatabaseExistsWithTheConfiguredName()
    {
        Assert.Equal(GetDatabaseName(), GetFirstDeploymentDatabase().DatabaseName);
    }

    [Then("the live Railway database is registered for deletion")]
    public void ThenTheLiveRailwayDatabaseIsRegisteredForDeletion()
    {
        Assert.True(_cleanupRegistered);
        Assert.True(_context.LiveRailway.CleanupActionCount > 0);
    }

    [Then("both live Railway deployments returned the same provider id")]
    public void ThenBothLiveRailwayDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("only one live Railway database exists with the configured name")]
    public async Task ThenOnlyOneLiveRailwayDatabaseExistsWithTheConfiguredName()
    {
        RailwayPostgresManagementClient client = _context.LiveRailway.CreateManagementClient();
        IReadOnlyList<RailwayPostgresDatabaseSummary> databases = await client
            .ListDatabasesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(databases, database => database.DatabaseName == GetDatabaseName());
    }

    [Then("the live Redis connection string matches the provider details")]
    public async Task ThenTheLiveRedisConnectionStringMatchesTheProviderDetails()
    {
        RailwayPostgresDatabaseDetails database = GetFirstDeploymentDatabase();
        IResourceWithConnectionString redisConnection =
            Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);

        string? connectionString = await redisConnection
            .GetConnectionStringAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(
            $"{database.Endpoint}:{database.Port},password={database.Password},ssl=true",
            connectionString);
    }

    [Then("the live supplementary Railway PostgreSQL outputs match the provider details")]
    public async Task ThenTheLiveSupplementaryRailwayPostgresOutputsMatchTheProviderDetails()
    {
        RailwayPostgresDatabaseDetails database = GetFirstDeploymentDatabase();
        RailwayPostgresOutputs outputs = _context.RedisBuilder.Resource.GetRailwayPostgresOutputs();

        await AssertOutputAsync(outputs.Endpoint, database.Endpoint).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Port, database.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Password, database.Password).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Tls, "true").ConfigureAwait(false);
        await AssertOutputAsync(outputs.DatabaseName, database.DatabaseName).ConfigureAwait(false);
    }

    [Then("the live supplementary Railway PostgreSQL password output is secret")]
    public void ThenTheLiveSupplementaryRailwayPostgresPasswordOutputIsSecret()
    {
        RailwayPostgresOutputs outputs = _context.RedisBuilder.Resource.GetRailwayPostgresOutputs();

        Assert.True(outputs.Password.Secret);
        Assert.True(RailwayPostgresOutputs.IsSecret(outputs.Password.Name));
        Assert.All(
            outputs.Properties.Where(output => output.Name != outputs.Password.Name),
            output => Assert.False(output.Secret));
    }

    private async Task<RailwayPostgresDatabaseDetails> RunDeploymentAsync(RailwayPostgresRemoteIdentityState? cachedIdentity)
    {
        RailwayPostgresDatabaseDetails? database = await RailwayPostgresDeploymentPipeline
            .ExecuteAsync(
                CreateDeployment(),
                _context.LiveRailway.CreateManagementClient(),
                cachedIdentity,
                saveIdentityStateAsync: null,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(database);

        _context.RedisBuilder.Resource.ApplyRailwayPostgresConnectionOutput(database);
        _context.RedisBuilder.Resource.GetRailwayPostgresOutputs().Populate(database);

        return database;
    }

    private RailwayPostgresResolvedDeployment CreateDeployment()
    {
        RailwayPostgresDeploymentOptions options = new()
        {
            Tls = true,
        };
        options.SetPlatform(RailwayPostgresCloudPlatform.Aws);
        options.SetPrimaryRegion(RailwayPostgresRegion.AwsEuWest1);

        return new RailwayPostgresResolvedDeployment(
            GetDatabaseName(),
            RailwayPostgresOwnershipMode.CreateOrAdopt,
            new RailwayPostgresManagementCredentials(
                _context.LiveRailway.AccountEmail ?? throw new InvalidOperationException("RAILWAY_EMAIL is not configured."),
                _context.LiveRailway.ApiKey ?? throw new InvalidOperationException("RAILWAY_API_KEY is not configured.")),
            options.ToProviderOptions());
    }

    private string GetDatabaseName()
    {
        return _databaseName ?? throw new InvalidOperationException("The live database name has not been configured.");
    }

    private RailwayPostgresDatabaseDetails GetFirstDeploymentDatabase()
    {
        return _firstDeploymentDatabase ?? throw new InvalidOperationException("The first live deployment has not run.");
    }

    private RailwayPostgresDatabaseDetails GetSecondDeploymentDatabase()
    {
        return _secondDeploymentDatabase ?? throw new InvalidOperationException("The second live deployment has not run.");
    }

    private static async Task AssertOutputAsync(RailwayPostgresOutputReference output, string? expectedValue)
    {
        string? actualValue = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(expectedValue, actualValue);
    }
}
