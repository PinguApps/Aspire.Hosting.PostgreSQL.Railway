#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class TypeScriptDeploymentStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;
    private readonly TypeScriptDeploymentManagementClient _fakeClient = new();
    private readonly TypeScriptDeploymentStateManager _stateManager = new();
    private RedisResource? _redisResource;
    private RailwayPostgresDatabaseDetails? _firstDeploymentDatabase;
    private RailwayPostgresDatabaseDetails? _secondDeploymentDatabase;
    private string? _databaseName;
    private bool _cleanupRegistered;

    public TypeScriptDeploymentStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("a TypeScript-authored Railway PostgreSQL deployment for database {string}")]
    public void GivenATypeScriptAuthoredRailwayPostgresDeploymentForDatabase(string databaseName)
    {
        _databaseName = databaseName;
        _redisResource = CreateTypeScriptAuthoredRedis(databaseName).Resource;
    }

    [Given("the TypeScript deployment fake provider has no database named {string}")]
    public void GivenTheTypeScriptDeploymentFakeProviderHasNoDatabaseNamed(string databaseName)
    {
        Assert.DoesNotContain(_fakeClient.Databases, database => database.DatabaseName == databaseName);
    }

    [Given("a live TypeScript-authored Railway PostgreSQL deployment with prefix {string}")]
    public async Task GivenALiveTypeScriptAuthoredRailwayPostgresDeploymentWithPrefix(string prefix)
    {
        _databaseName = LiveRailwayTestSession.CreateDisposableDatabaseName(prefix);
        _redisResource = CreateTypeScriptAuthoredRedis(_databaseName).Resource;

        await _context.LiveRailway.RegisterDatabaseDeletionByNameAsync(_databaseName).ConfigureAwait(false);
        _cleanupRegistered = true;
    }

    [When("the TypeScript-authored Railway deployment pipeline runs twice")]
    public async Task WhenTheTypeScriptAuthoredRailwayDeploymentPipelineRunsTwice()
    {
        _firstDeploymentDatabase = await RunPipelineAsync(_fakeClient).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunPipelineAsync(_fakeClient).ConfigureAwait(false);
    }

    [When("the live TypeScript-authored Railway deployment pipeline runs twice")]
    public async Task WhenTheLiveTypeScriptAuthoredRailwayDeploymentPipelineRunsTwice()
    {
        IRailwayPostgresManagementClient client = _context.LiveRailway.CreateManagementClient();

        _firstDeploymentDatabase = await RunPipelineAsync(client).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunPipelineAsync(client).ConfigureAwait(false);
    }

    [Then("the TypeScript-authored Railway deployment created {int} database")]
    [Then("the TypeScript-authored Railway deployment created {int} databases")]
    public void ThenTheTypeScriptAuthoredRailwayDeploymentCreatedDatabase(int createCount)
    {
        Assert.Equal(createCount, _fakeClient.CreateCount);
    }

    [Then("the TypeScript-authored Railway deployments returned the same provider id")]
    public void ThenTheTypeScriptAuthoredRailwayDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("the TypeScript-authored Railway deployment populated Redis outputs for database {string}")]
    public async Task ThenTheTypeScriptAuthoredRailwayDeploymentPopulatedRedisOutputsForDatabase(string databaseName)
    {
        RailwayPostgresDatabaseDetails database = GetSecondDeploymentDatabase();
        RailwayPostgresOutputs outputs = GetRedisResource().GetRailwayPostgresOutputs();

        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal(databaseName, await outputs.DatabaseName.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(database.Endpoint, await outputs.Endpoint.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(database.Password, await outputs.Password.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
    }

    [Then("the live TypeScript-authored Railway deployments returned the same provider id")]
    public void ThenTheLiveTypeScriptAuthoredRailwayDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("only one live TypeScript-authored Railway database exists with the configured name")]
    public async Task ThenOnlyOneLiveTypeScriptAuthoredRailwayDatabaseExistsWithTheConfiguredName()
    {
        RailwayPostgresManagementClient client = _context.LiveRailway.CreateManagementClient();
        IReadOnlyList<RailwayPostgresDatabaseSummary> databases = await client
            .ListDatabasesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(databases, database => database.DatabaseName == GetDatabaseName());
    }

    [Then("the live TypeScript-authored Railway database is registered for deletion")]
    public void ThenTheLiveTypeScriptAuthoredRailwayDatabaseIsRegisteredForDeletion()
    {
        Assert.True(_cleanupRegistered);
        Assert.True(_context.LiveRailway.CleanupActionCount > 0);
    }

    private IResourceBuilder<RedisResource> CreateTypeScriptAuthoredRedis(string databaseName)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<RedisResource> redis = builder.AddRedis("cache");

        return redis.PublishToRailwayForTypeScript(
            builder.AddParameter("railway-database-name", databaseName),
            builder.AddParameter("railway-account-email", "owner@example.com"),
            builder.AddParameter("railway-api-key", "management-secret", secret: true),
            new RailwayPostgresDeploymentOptionsDto
            {
                OwnershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
                Platform = RailwayPostgresCloudPlatform.Aws,
                PrimaryRegion = RailwayPostgresRegion.AwsEuWest1,
                ReadRegions = [RailwayPostgresRegion.AwsEuWest2],
                Plan = RailwayPostgresPlan.PayAsYouGo,
                Budget = 20,
                Eviction = true,
                Tls = true
            });
    }

    private async Task<RailwayPostgresDatabaseDetails> RunPipelineAsync(IRailwayPostgresManagementClient client)
    {
        RedisResource resource = GetRedisResource();

        await RailwayPostgresDeploymentPipeline.ExecuteAsync(resource, CreatePipelineStepContext(resource, client)).ConfigureAwait(false);

        RailwayPostgresRemoteIdentityDeploymentStateStore identityStore = new(_stateManager);
        RailwayPostgresRemoteIdentityState? savedIdentity = await identityStore
            .LoadAsync(resource.Name, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(savedIdentity);

        return await client.GetDatabaseAsync(savedIdentity.ProviderDatabaseId, CancellationToken.None).ConfigureAwait(false);
    }

    private PipelineStepContext CreatePipelineStepContext(RedisResource resource, IRailwayPostgresManagementClient client)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(_stateManager)
            .AddSingleton<IDeploymentStateManager>(_stateManager)
            .AddSingleton(client)
            .AddSingleton<IRailwayPostgresManagementClient>(client)
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

    private RedisResource GetRedisResource()
    {
        return _redisResource ?? throw new InvalidOperationException("The TypeScript-authored Redis resource has not been configured.");
    }

    private string GetDatabaseName()
    {
        return _databaseName ?? throw new InvalidOperationException("The TypeScript-authored database name has not been configured.");
    }

    private RailwayPostgresDatabaseDetails GetFirstDeploymentDatabase()
    {
        return _firstDeploymentDatabase ?? throw new InvalidOperationException("The first TypeScript-authored deployment has not run.");
    }

    private RailwayPostgresDatabaseDetails GetSecondDeploymentDatabase()
    {
        return _secondDeploymentDatabase ?? throw new InvalidOperationException("The second TypeScript-authored deployment has not run.");
    }

    private sealed class TypeScriptDeploymentStateManager : IDeploymentStateManager
    {
        private readonly Dictionary<string, DeploymentStateSection> _sections = [];

        public string StateFilePath => "/tmp/fake-aspire-typescript-state.json";

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

    private sealed class TypeScriptDeploymentManagementClient : IRailwayPostgresManagementClient
    {
        private readonly List<RailwayPostgresDatabaseDetails> _databases = [];

        public IReadOnlyList<RailwayPostgresDatabaseDetails> Databases => _databases;

        public int CreateCount { get; private set; }

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

            RailwayPostgresDatabaseDetails? match = _databases.SingleOrDefault(database => database.DatabaseName == databaseName);

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

            RailwayPostgresDatabaseDetails database = new()
            {
                DatabaseId = $"db-{request.DatabaseName}",
                DatabaseName = request.DatabaseName,
                Endpoint = "global-apt-1.railway.io",
                Port = 6379,
                Password = "redis-password",
                Tls = request.Tls ?? true,
                State = "active",
                ModifyingState = null,
                PrimaryRegion = request.PrimaryRegion,
                ReadRegions = request.ReadRegions,
                Type = request.Plan ?? "payg",
                Budget = request.Budget,
                Eviction = request.Eviction,
            };

            _databases.Add(database);

            return Task.FromResult(new RailwayPostgresDatabaseDetails
            {
                DatabaseId = database.DatabaseId,
                DatabaseName = database.DatabaseName,
                State = database.State,
                ModifyingState = database.ModifyingState,
            });
        }

        public Task UpdateReadRegionsAsync(string databaseId, RailwayPostgresUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, RailwayPostgresChangePlanRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, RailwayPostgresUpdateBudgetRequest request, CancellationToken cancellationToken)
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
    }
}
