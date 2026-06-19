using System.Net;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class CreateFlowStepDefinitions
{
    private readonly FakeCreateFlowManagementClient _client = new();
    private RailwayPostgresResolvedDeployment? _deployment;
    private RailwayPostgresOwnershipResolutionResult? _ownership;
    private RailwayPostgresCreateFlowResult? _result;
    private Exception? _exception;

    [Given("an Railway create flow deployment for database {string}")]
    public void GivenAnRailwayCreateFlowDeploymentForDatabase(string databaseName)
    {
        RailwayPostgresDeploymentOptions options = new()
        {
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Plan = "payg",
            Budget = "360",
            Eviction = true,
            Tls = true,
        };

        _deployment = new RailwayPostgresResolvedDeployment(
            databaseName,
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("owner@example.com", "management-secret"),
            options.ToProviderOptions());
    }

    [Given("ownership resolution selected create")]
    public void GivenOwnershipResolutionSelectedCreate()
    {
        _ownership = RailwayPostgresOwnershipResolutionResult.Create();
    }

    [Given("ownership resolution selected adopt for database {string} with id {string}")]
    public void GivenOwnershipResolutionSelectedAdoptForDatabaseWithId(string databaseName, string databaseId)
    {
        _ownership = RailwayPostgresOwnershipResolutionResult.Adopt(CreateDatabaseDetails(databaseName, databaseId, includePassword: true));
    }

    [Given("ownership resolution selected adopt for database {string} with id {string} with invalid connection field {string}")]
    public void GivenOwnershipResolutionSelectedAdoptForDatabaseWithInvalidConnectionField(
        string databaseName,
        string databaseId,
        string field)
    {
        RailwayPostgresDatabaseDetails database = CreateDatabaseDetails(databaseName, databaseId, includePassword: true);
        InvalidateConnectionField(database, field);
        _ownership = RailwayPostgresOwnershipResolutionResult.Adopt(database);
    }

    [Given("the Railway create API returns database id {string}")]
    public void GivenTheRailwayCreateApiReturnsDatabaseId(string databaseId)
    {
        RailwayPostgresResolvedDeployment deployment = GetDeployment();
        _client.CreateResponse = new RailwayPostgresDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = deployment.DatabaseName,
        };
    }

    [Given("the Railway create API fails with provider kind {string} and message {string}")]
    public void GivenTheRailwayCreateApiFailsWithProviderKindAndMessage(string failureKind, string message)
    {
        _client.CreateException = new RailwayPostgresProviderException(
            Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind),
            HttpStatusCode.BadRequest,
            message);
    }

    [Given("the Railway readiness API returns active database {string} with id {string}")]
    public void GivenTheRailwayReadinessApiReturnsActiveDatabaseWithId(string databaseName, string databaseId)
    {
        _client.ReadyResponse = CreateDatabaseDetails(databaseName, databaseId, includePassword: true);
    }

    [Given("the Railway readiness API returns active database {string} with id {string} without a password")]
    public void GivenTheRailwayReadinessApiReturnsActiveDatabaseWithIdWithoutAPassword(string databaseName, string databaseId)
    {
        _client.ReadyResponse = CreateDatabaseDetails(databaseName, databaseId, includePassword: false);
    }

    [Given("the Railway readiness API returns active database {string} with id {string} with invalid connection field {string}")]
    public void GivenTheRailwayReadinessApiReturnsActiveDatabaseWithInvalidConnectionField(string databaseName, string databaseId, string field)
    {
        RailwayPostgresDatabaseDetails database = CreateDatabaseDetails(databaseName, databaseId, includePassword: true);
        InvalidateConnectionField(database, field);

        _client.ReadyResponse = database;
    }

    [When("the Railway create flow executes")]
    public async Task WhenTheRailwayCreateFlowExecutes()
    {
        RailwayPostgresCreateFlow flow = new(_client);
        _result = await flow.ExecuteAsync(GetDeployment(), GetOwnership(), CancellationToken.None).ConfigureAwait(false);
    }

    [When("the Railway create flow is attempted")]
    public async Task WhenTheRailwayCreateFlowIsAttempted()
    {
        _exception = await Record.ExceptionAsync(WhenTheRailwayCreateFlowExecutes).ConfigureAwait(false);
    }

    [Then("the Railway create flow creates the database")]
    public void ThenTheRailwayCreateFlowCreatesTheDatabase()
    {
        Assert.True(GetResult().Created);
        Assert.NotNull(_client.LastCreateRequest);
    }

    [Then("the Railway create flow does not create the database")]
    public void ThenTheRailwayCreateFlowDoesNotCreateTheDatabase()
    {
        Assert.False(GetResult().Created);
        Assert.Null(_client.LastCreateRequest);
    }

    [Then("the Railway create request payload is:")]
    public void ThenTheRailwayCreateRequestPayloadIs(DataTable table)
    {
        RailwayPostgresCreateDatabaseRequest request = _client.LastCreateRequest
            ?? throw new InvalidOperationException("No create request was captured.");

        foreach (DataTableRow row in table.Rows)
        {
            object? actualValue = row["Property"] switch
            {
                nameof(RailwayPostgresCreateDatabaseRequest.DatabaseName) => request.DatabaseName,
                nameof(RailwayPostgresCreateDatabaseRequest.Platform) => request.Platform,
                nameof(RailwayPostgresCreateDatabaseRequest.PrimaryRegion) => request.PrimaryRegion,
                nameof(RailwayPostgresCreateDatabaseRequest.Plan) => request.Plan,
                nameof(RailwayPostgresCreateDatabaseRequest.Budget) => request.Budget,
                nameof(RailwayPostgresCreateDatabaseRequest.Eviction) => request.Eviction,
                nameof(RailwayPostgresCreateDatabaseRequest.Tls) => request.Tls,
                _ => throw new InvalidOperationException($"Unknown create request property '{row["Property"]}'."),
            };

            Assert.Equal(row["Value"], Convert.ToString(actualValue, System.Globalization.CultureInfo.InvariantCulture)?.ToLowerInvariant());
        }
    }

    [Then("the Railway create request read regions are {string}")]
    public void ThenTheRailwayCreateRequestReadRegionsAre(string readRegions)
    {
        RailwayPostgresCreateDatabaseRequest request = _client.LastCreateRequest
            ?? throw new InvalidOperationException("No create request was captured.");

        Assert.Equal(readRegions.Split(',', StringSplitOptions.TrimEntries), request.ReadRegions);
    }

    [Then("the Railway create flow returns Redis credentials for database {string}")]
    public void ThenTheRailwayCreateFlowReturnsRedisCredentialsForDatabase(string databaseName)
    {
        RailwayPostgresDatabaseDetails database = GetResult().Database;

        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal("global-apt-1.railway.io", database.Endpoint);
        Assert.Equal(6379, database.Port);
        Assert.Equal("redis-password", database.Password);
        Assert.True(database.Tls);
    }

    [Then("the Railway create flow waits for database {string}")]
    public void ThenTheRailwayCreateFlowWaitsForDatabase(string databaseId)
    {
        string waitedDatabaseId = Assert.Single(_client.WaitedDatabaseIds);
        Assert.Equal(databaseId, waitedDatabaseId);
    }

    [Then("the Railway create flow returns remote identity database {string} with id {string}")]
    public void ThenTheRailwayCreateFlowReturnsRemoteIdentityDatabaseWithId(string databaseName, string databaseId)
    {
        RailwayPostgresRemoteIdentityState remoteIdentity = GetResult().RemoteIdentity;

        Assert.Equal(databaseName, remoteIdentity.DatabaseName);
        Assert.Equal(databaseId, remoteIdentity.ProviderDatabaseId);
    }

    [Then("the Railway create flow fails with {string}")]
    public void ThenTheRailwayCreateFlowFailsWith(string exceptionTypeName)
    {
        Exception exception = _exception ?? throw new InvalidOperationException("The create flow did not fail.");

        Assert.Equal(exceptionTypeName, exception.GetType().Name);
    }

    [Then("the Railway create flow fails with provider kind {string}")]
    public void ThenTheRailwayCreateFlowFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway create flow failure message contains {string}")]
    public void ThenTheRailwayCreateFlowFailureMessageContains(string value)
    {
        Exception exception = _exception ?? throw new InvalidOperationException("The create flow did not fail.");

        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    private RailwayPostgresResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("No deployment was configured.");
    }

    private RailwayPostgresOwnershipResolutionResult GetOwnership()
    {
        return _ownership ?? throw new InvalidOperationException("No ownership resolution was configured.");
    }

    private RailwayPostgresCreateFlowResult GetResult()
    {
        return _result ?? throw new InvalidOperationException("No create flow result was captured.");
    }

    private static RailwayPostgresDatabaseDetails CreateDatabaseDetails(
        string databaseName,
        string databaseId,
        bool includePassword)
    {
        return new RailwayPostgresDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.railway.io",
            Port = 6379,
            Password = includePassword ? "redis-password" : null,
            Tls = true,
            State = "active",
            ModifyingState = null,
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = "payg",
            Budget = 360,
            Eviction = true,
        };
    }

    private static void InvalidateConnectionField(RailwayPostgresDatabaseDetails database, string field)
    {
        switch (field)
        {
            case "password":
                database.Password = null;
                break;
            case "endpoint":
                database.Endpoint = string.Empty;
                break;
            case "port":
                database.Port = 0;
                break;
            case "tls":
                database.Tls = false;
                break;
            default:
                throw new InvalidOperationException($"Unknown connection field '{field}'.");
        }
    }

    private sealed class FakeCreateFlowManagementClient : IRailwayPostgresManagementClient
    {
        public RailwayPostgresCreateDatabaseRequest? LastCreateRequest { get; private set; }

        public RailwayPostgresDatabaseDetails? CreateResponse { get; set; }

        public RailwayPostgresDatabaseDetails? ReadyResponse { get; set; }

        public RailwayPostgresProviderException? CreateException { get; set; }

        public List<string> WaitedDatabaseIds { get; } = [];

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(
            RailwayPostgresCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCreateRequest = request;

            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(CreateResponse ?? throw new InvalidOperationException("No create response was configured."));
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WaitedDatabaseIds.Add(databaseId);

            return Task.FromResult(ReadyResponse ?? throw new InvalidOperationException("No ready response was configured."));
        }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
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
    }
}
