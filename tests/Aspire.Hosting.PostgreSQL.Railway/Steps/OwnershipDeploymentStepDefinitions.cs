#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class OwnershipDeploymentStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;
    private readonly OwnershipDeploymentManagementClient _client = new();
    private readonly OwnershipDeploymentStateManager _deploymentStateManager = new();
    private RedisResource? _redisResource;
    private RailwayPostgresOutputs? _outputs;
    private RailwayPostgresDatabaseDetails? _result;
    private RailwayPostgresRemoteIdentityState? _cachedIdentity;
    private RailwayPostgresRemoteIdentityState? _savedIdentity;
    private Exception? _exception;
    private int _previousCreateCount;
    private string? _liveDatabaseName;
    private bool _liveCleanupRegistered;

    public OwnershipDeploymentStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("an Railway ownership deployment for database {string} with mode {string}")]
    public void GivenAnRailwayOwnershipDeploymentForDatabaseWithMode(string databaseName, string ownershipMode)
    {
        RailwayPostgresOwnershipMode mode = Enum.Parse<RailwayPostgresOwnershipMode>(ownershipMode);

        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<RedisResource> redis = builder
            .AddRedis("cache")
            .PublishToRailway(
                databaseName,
                builder.AddParameter("railway-account-email", "owner@example.com"),
                builder.AddParameter("railway-api-key", "management-secret", secret: true),
                mode,
                options =>
                {
                    options.Platform = "aws";
                    options.PrimaryRegion = "eu-west-1";
                    options.Tls = true;
                });

        _redisResource = redis.Resource;
        _outputs = redis.Resource.GetRailwayPostgresOutputs();
    }

    [Given("the Railway ownership deployment provider has no database named {string}")]
    public void GivenTheRailwayOwnershipDeploymentProviderHasNoDatabaseNamed(string databaseName)
    {
        Assert.DoesNotContain(_client.Databases, database => database.DatabaseName == databaseName);
    }

    [Given("the Railway ownership deployment provider has database {string} with id {string}")]
    public void GivenTheRailwayOwnershipDeploymentProviderHasDatabaseWithId(string databaseName, string databaseId)
    {
        _client.AddDatabase(CreateDatabase(databaseName, databaseId));
    }

    [Given("the Railway ownership deployment provider has duplicate databases named {string}")]
    public void GivenTheRailwayOwnershipDeploymentProviderHasDuplicateDatabasesNamed(string databaseName)
    {
        _client.AddDatabase(CreateDatabase(databaseName, "db-orders-a"));
        _client.AddDatabase(CreateDatabase(databaseName, "db-orders-b"));
    }

    [Given("cached Railway ownership deployment identity is database {string} with id {string}")]
    public void GivenCachedRailwayOwnershipDeploymentIdentityIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new RailwayPostgresRemoteIdentityState(databaseName, databaseId);
    }

    [Given("a live Railway ownership deployment for isolated database prefix {string}")]
    public void GivenALiveRailwayOwnershipDeploymentForIsolatedDatabasePrefix(string prefix)
    {
        _liveDatabaseName = LiveRailwayTestSession.CreateDisposableDatabaseName(prefix);
    }

    [Given("the live Railway ownership provider has an isolated database to adopt")]
    public async Task GivenTheLiveRailwayOwnershipProviderHasAnIsolatedDatabaseToAdopt()
    {
        LiveOwnershipManagementClient client = CreateLiveClient();
        client.CreatedDatabase = databaseId => RegisterLiveDeleteCleanup(client, databaseId);

        RailwayPostgresDatabaseDetails created = await client.CreateDatabaseAsync(
            CreateLiveDatabaseRequest(GetLiveDatabaseName()),
            CancellationToken.None).ConfigureAwait(false);

        await client
            .WaitUntilReadyAsync(created.DatabaseId, RailwayPostgresReadinessPollingOptions.Default, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [When("the Railway ownership deployment pipeline runs")]
    public async Task WhenTheRailwayOwnershipDeploymentPipelineRuns()
    {
        await RunPipelineAsync().ConfigureAwait(false);
    }

    [When("the Railway ownership deployment pipeline runs again")]
    public async Task WhenTheRailwayOwnershipDeploymentPipelineRunsAgain()
    {
        _previousCreateCount = _client.CreateCount;

        await RunPipelineAsync().ConfigureAwait(false);
    }

    [When("the Railway ownership deployment pipeline is attempted")]
    public async Task WhenTheRailwayOwnershipDeploymentPipelineIsAttempted()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);
    }

    [When("the live Railway ownership deployment runs with mode {string}")]
    public async Task WhenTheLiveRailwayOwnershipDeploymentRunsWithMode(string ownershipMode)
    {
        LiveOwnershipManagementClient client = CreateLiveClient();
        client.CreatedDatabase = databaseId => RegisterLiveDeleteCleanup(client, databaseId);
        RailwayPostgresResolvedDeployment deployment = CreateLiveDeployment(
            GetLiveDatabaseName(),
            Enum.Parse<RailwayPostgresOwnershipMode>(ownershipMode));

        try
        {
            _result = await RailwayPostgresDeploymentPipeline.ExecuteAsync(
                deployment,
                client,
                cachedIdentity: null,
                saveIdentityStateAsync: identity =>
                {
                    _savedIdentity = identity;
                    return Task.CompletedTask;
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (client.CreateCount == 0)
            {
                client.Dispose();
            }
        }
    }

    [Then("the Railway ownership deployment succeeds using the {string} path")]
    [Then("the Railway ownership deployment succeeded using the {string} path")]
    public void ThenTheRailwayOwnershipDeploymentSucceedsUsingThePath(string path)
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);

        bool expectedPathUsed = path switch
        {
            "Create" => _client.CreateCount > _previousCreateCount,
            "Adopt" => _client.CreateCount == _previousCreateCount,
            _ => throw new ArgumentOutOfRangeException(nameof(path), path, "Expected ownership path to be 'Create' or 'Adopt'.")
        };

        Assert.True(expectedPathUsed, $"Expected ownership deployment to use the '{path}' path.");
    }

    [Then("the Railway ownership deployment saved remote identity database {string}")]
    public void ThenTheRailwayOwnershipDeploymentSavedRemoteIdentityDatabase(string databaseName)
    {
        Assert.NotNull(_savedIdentity);
        Assert.Equal(databaseName, _savedIdentity.DatabaseName);
        Assert.Equal(_result?.DatabaseId, _savedIdentity.ProviderDatabaseId);
    }

    [Then("the Railway ownership deployment populated Redis outputs for database {string}")]
    public async Task ThenTheRailwayOwnershipDeploymentPopulatedRedisOutputsForDatabase(string databaseName)
    {
        RailwayPostgresOutputs outputs = GetOutputs();

        Assert.Equal(databaseName, await outputs.DatabaseName.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(_result?.Endpoint, await outputs.Endpoint.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(_result?.Password, await outputs.Password.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
    }

    [Then("the Railway ownership deployment created {int} database")]
    [Then("the Railway ownership deployment created {int} databases")]
    public void ThenTheRailwayOwnershipDeploymentCreatedDatabases(int createCount)
    {
        Assert.Equal(createCount, _client.CreateCount);
    }

    [Then("the Railway ownership deployment did not create a database")]
    public void ThenTheRailwayOwnershipDeploymentDidNotCreateADatabase()
    {
        Assert.Equal(0, _client.CreateCount);
    }

    [Then("the Railway ownership deployment fails because {string}")]
    public void ThenTheRailwayOwnershipDeploymentFailsBecause(string failureReason)
    {
        RailwayPostgresOwnershipResolutionException exception = Assert.IsType<RailwayPostgresOwnershipResolutionException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresOwnershipResolutionFailureReason>(failureReason), exception.FailureReason);
    }

    [Then("the Railway ownership deployment fails with provider kind {string}")]
    public void ThenTheRailwayOwnershipDeploymentFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway ownership deployment failure message contains {string}")]
    public void ThenTheRailwayOwnershipDeploymentFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    [Then("the live Railway ownership deployment created a database")]
    public void ThenTheLiveRailwayOwnershipDeploymentCreatedADatabase()
    {
        Assert.NotNull(_result);
        Assert.Equal(GetLiveDatabaseName(), _result.DatabaseName);
    }

    [Then("the live Railway ownership deployment adopted the database")]
    public void ThenTheLiveRailwayOwnershipDeploymentAdoptedTheDatabase()
    {
        Assert.NotNull(_result);
        Assert.Equal(GetLiveDatabaseName(), _result.DatabaseName);
    }

    [Then("the live Railway ownership deployment registered delete cleanup")]
    public void ThenTheLiveRailwayOwnershipDeploymentRegisteredDeleteCleanup()
    {
        Assert.True(_liveCleanupRegistered);
        Assert.True(_context.LiveRailway.CleanupActionCount > 0);
    }

    private async Task RunPipelineAsync()
    {
        _previousCreateCount = _client.CreateCount;

        RedisResource resource = GetRedisResource();
        RailwayPostgresRemoteIdentityDeploymentStateStore identityStore = new(_deploymentStateManager);

        if (_cachedIdentity is not null)
        {
            await identityStore.SaveAsync(resource.Name, _cachedIdentity, CancellationToken.None).ConfigureAwait(false);
        }

        await RailwayPostgresDeploymentPipeline.ExecuteAsync(resource, CreatePipelineStepContext(resource)).ConfigureAwait(false);

        _savedIdentity = await identityStore.LoadAsync(resource.Name, CancellationToken.None).ConfigureAwait(false);
        _cachedIdentity = _savedIdentity;

        if (_savedIdentity is not null)
        {
            _result = await _client.GetDatabaseAsync(_savedIdentity.ProviderDatabaseId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private RailwayPostgresOutputs GetOutputs()
    {
        return _outputs ?? throw new InvalidOperationException("No Railway PostgreSQL outputs were configured.");
    }

    private RedisResource GetRedisResource()
    {
        return _redisResource ?? throw new InvalidOperationException("No ownership deployment was configured.");
    }

    private PipelineStepContext CreatePipelineStepContext(RedisResource resource)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton<IDeploymentStateManager>(_deploymentStateManager)
            .AddSingleton<IRailwayPostgresManagementClient>(_client)
            .BuildServiceProvider();

        PipelineContext pipelineContext = new(
            new DistributedApplicationModel([resource]),
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
            services,
            NullLogger.Instance,
            CancellationToken.None);

        return new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = null!,
        };
    }

    private string GetLiveDatabaseName()
    {
        return _liveDatabaseName ?? throw new InvalidOperationException("No live database name was configured.");
    }

    private LiveOwnershipManagementClient CreateLiveClient()
    {
        return new LiveOwnershipManagementClient(
            _context.LiveRailway.AccountEmail ?? throw new InvalidOperationException("Missing RAILWAY_EMAIL."),
            _context.LiveRailway.ApiKey ?? throw new InvalidOperationException("Missing RAILWAY_API_KEY."));
    }

    private void RegisterLiveDeleteCleanup(LiveOwnershipManagementClient client, string databaseId)
    {
        _context.LiveRailway.RegisterCleanup(async () =>
        {
            try
            {
                await client.DeleteDatabaseIfExistsAsync(databaseId, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                client.Dispose();
            }
        });
        _liveCleanupRegistered = true;
    }

    private RailwayPostgresResolvedDeployment CreateLiveDeployment(
        string databaseName,
        RailwayPostgresOwnershipMode ownershipMode)
    {
        RailwayPostgresDeploymentOptions options = new()
        {
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            Tls = true,
        };

        return new RailwayPostgresResolvedDeployment(
            databaseName,
            ownershipMode,
            new RailwayPostgresManagementCredentials(
                _context.LiveRailway.AccountEmail ?? throw new InvalidOperationException("Missing RAILWAY_EMAIL."),
                _context.LiveRailway.ApiKey ?? throw new InvalidOperationException("Missing RAILWAY_API_KEY.")),
            options.ToProviderOptions());
    }

    private static RailwayPostgresCreateDatabaseRequest CreateLiveDatabaseRequest(string databaseName)
    {
        return new RailwayPostgresCreateDatabaseRequest
        {
            DatabaseName = databaseName,
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            Tls = true,
        };
    }

    private static RailwayPostgresDatabaseDetails CreateDatabase(string databaseName, string databaseId)
    {
        return new RailwayPostgresDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.railway.io",
            Port = 6379,
            Password = "redis-password",
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

    private static RailwayPostgresDatabaseDetails Clone(RailwayPostgresDatabaseDetails database)
    {
        return new RailwayPostgresDatabaseDetails
        {
            DatabaseId = database.DatabaseId,
            DatabaseName = database.DatabaseName,
            Endpoint = database.Endpoint,
            Port = database.Port,
            Password = database.Password,
            Tls = database.Tls,
            State = database.State,
            ModifyingState = database.ModifyingState,
            PrimaryRegion = database.PrimaryRegion,
            ReadRegions = database.ReadRegions is null ? null : [.. database.ReadRegions],
            Type = database.Type,
            DbDiskThreshold = database.DbDiskThreshold,
            Budget = database.Budget,
            Eviction = database.Eviction,
            CustomerId = database.CustomerId,
        };
    }

    private sealed class OwnershipDeploymentStateManager : IDeploymentStateManager
    {
        private readonly Dictionary<string, DeploymentStateSection> _sections = [];

        public string StateFilePath => "/tmp/fake-aspire-state.json";

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_sections.TryGetValue(sectionName, out DeploymentStateSection? section))
            {
                section = new DeploymentStateSection(sectionName, [], version: 0);
                _sections[sectionName] = section;
            }

            return Task.FromResult(section);
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections[section.SectionName] = section;

            return Task.CompletedTask;
        }

        public Task DeleteSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections.Remove(section.SectionName);

            return Task.CompletedTask;
        }

        public Task ClearAllStateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections.Clear();

            return Task.CompletedTask;
        }
    }

    private sealed class OwnershipDeploymentManagementClient : IRailwayPostgresManagementClient
    {
        private readonly List<RailwayPostgresDatabaseDetails> _databases = [];

        public IReadOnlyList<RailwayPostgresDatabaseDetails> Databases => _databases;

        public int CreateCount { get; private set; }

        public void AddDatabase(RailwayPostgresDatabaseDetails database)
        {
            _databases.Add(Clone(database));
        }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<RailwayPostgresDatabaseSummary> summaries =
            [
                .. _databases.Select(database => new RailwayPostgresDatabaseSummary
                {
                    DatabaseId = database.DatabaseId,
                    DatabaseName = database.DatabaseName,
                    Endpoint = database.Endpoint,
                    Port = database.Port,
                    State = database.State,
                    ModifyingState = database.ModifyingState,
                    PrimaryRegion = database.PrimaryRegion,
                    ReadRegions = database.ReadRegions,
                }),
            ];

            return Task.FromResult(summaries);
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RailwayPostgresDatabaseDetails database = _databases.SingleOrDefault(database => database.DatabaseId == databaseId)
                ?? throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    $"Database '{databaseId}' was not found.");

            return Task.FromResult(Clone(database));
        }

        public async Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<RailwayPostgresDatabaseDetails> matches =
                [.. _databases.Where(database => database.DatabaseName == databaseName).Take(2)];

            if (matches.Count > 1)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    statusCode: null,
                    $"Railway PostgreSQL returned more than one database named '{databaseName}'.");
            }

            RailwayPostgresDatabaseDetails? match = matches.SingleOrDefault();

            return match is null
                ? null
                : await GetDatabaseAsync(match.DatabaseId, cancellationToken).ConfigureAwait(false);
        }

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(
            RailwayPostgresCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CreateCount++;

            RailwayPostgresDatabaseDetails database = CreateDatabase(
                request.DatabaseName,
                $"db-{request.DatabaseName}");
            database.PrimaryRegion = request.PrimaryRegion;
            database.ReadRegions = request.ReadRegions;
            database.Type = request.Plan ?? "payg";
            database.Budget = request.Budget;
            database.Eviction = request.Eviction;

            _databases.Add(database);

            return Task.FromResult(new RailwayPostgresDatabaseDetails
            {
                DatabaseId = database.DatabaseId,
                DatabaseName = database.DatabaseName,
                State = database.State,
                ModifyingState = database.ModifyingState,
            });
        }

        public Task UpdateReadRegionsAsync(
            string databaseId,
            RailwayPostgresUpdateRegionsRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(
            string databaseId,
            RailwayPostgresChangePlanRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(
            string databaseId,
            RailwayPostgresUpdateBudgetRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).Budget = request.Budget;

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).Eviction = enabled;

            return Task.CompletedTask;
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return GetDatabaseAsync(databaseId, cancellationToken);
        }

        private RailwayPostgresDatabaseDetails GetMutableDatabase(string databaseId)
        {
            return _databases.SingleOrDefault(database => database.DatabaseId == databaseId)
                ?? throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    $"Database '{databaseId}' was not found.");
        }
    }

    private sealed class LiveOwnershipManagementClient : IRailwayPostgresManagementClient, IDisposable
    {
        private readonly RailwayPostgresManagementCredentials _credentials;
        private readonly HttpClient _httpClient;
        private readonly RailwayPostgresManagementClient _inner;

        public LiveOwnershipManagementClient(string accountEmail, string apiKey)
        {
            _credentials = new RailwayPostgresManagementCredentials(accountEmail, apiKey);
            _httpClient = new HttpClient { BaseAddress = new Uri("https://api.railway.com/v2/") };
            _inner = new RailwayPostgresManagementClient(_httpClient, _credentials);
        }

        public int CreateCount { get; private set; }

        public Action<string>? CreatedDatabase { get; set; }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            return _inner.ListDatabasesAsync(cancellationToken);
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            return _inner.GetDatabaseAsync(databaseId, cancellationToken);
        }

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            return _inner.FindDatabaseByNameAsync(databaseName, cancellationToken);
        }

        public async Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(
            RailwayPostgresCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            RailwayPostgresDatabaseDetails database = await _inner
                .CreateDatabaseAsync(request, cancellationToken)
                .ConfigureAwait(false);

            CreateCount++;
            CreatedDatabase?.Invoke(database.DatabaseId);

            return database;
        }

        public Task UpdateReadRegionsAsync(
            string databaseId,
            RailwayPostgresUpdateRegionsRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.UpdateReadRegionsAsync(databaseId, request, cancellationToken);
        }

        public Task ChangePlanAsync(
            string databaseId,
            RailwayPostgresChangePlanRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.ChangePlanAsync(databaseId, request, cancellationToken);
        }

        public Task UpdateBudgetAsync(
            string databaseId,
            RailwayPostgresUpdateBudgetRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.UpdateBudgetAsync(databaseId, request, cancellationToken);
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            return _inner.SetEvictionAsync(databaseId, enabled, cancellationToken);
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return _inner.WaitUntilReadyAsync(databaseId, pollingOptions, cancellationToken);
        }

        public async Task DeleteDatabaseIfExistsAsync(string databaseId, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, $"redis/database/{Uri.EscapeDataString(databaseId)}");
            request.Headers.Authorization = _credentials.CreateAuthorizationHeader();

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            throw new InvalidOperationException(
                $"Failed to delete live Railway PostgreSQL database '{databaseId}' during test cleanup: {(int)response.StatusCode} {response.StatusCode} {content}");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
