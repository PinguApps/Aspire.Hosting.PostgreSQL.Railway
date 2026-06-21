#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Aspire.Hosting.Pipelines;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class RemoteIdentityStepDefinitions : IDisposable
{
    private const string AccountEmail = "pingu@example.com";
    private const string ApiKey = "secret-key";

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly FakeDeploymentStateManager _deploymentStateManager = new();
    private readonly HttpClient _httpClient;
    private RailwayPostgresRemoteIdentityState? _cachedIdentity;
    private RailwayPostgresRemoteIdentityResolution? _lastResolution;
    private Exception? _lastException;

    public RemoteIdentityStepDefinitions()
    {
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.railway.com/v2/"),
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Given("cached Railway remote identity is database {string} with id {string}")]
    public void GivenCachedRailwayRemoteIdentityIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new RailwayPostgresRemoteIdentityState(databaseName, databaseId);
    }

    [Given("the Railway identity API returns an empty database list")]
    public void GivenTheRailwayIdentityApiReturnsAnEmptyDatabaseList()
    {
        _handler.Enqueue(HttpStatusCode.OK, "[]");
    }

    [Given("the Railway identity API returns a list containing database {string} with id {string}")]
    public void GivenTheRailwayIdentityApiReturnsAListContainingDatabaseWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "{{databaseId}}",
                "database_name": "{{databaseName}}"
              }
            ]
            """);
    }

    [Given("the Railway identity API returns duplicate databases named {string}")]
    public void GivenTheRailwayIdentityApiReturnsDuplicateDatabasesNamed(string databaseName)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "db-orders-1",
                "database_name": "{{databaseName}}"
              },
              {
                "database_id": "db-orders-2",
                "database_name": "{{databaseName}}"
              }
            ]
            """);
    }

    [Given("the Railway identity API returns details for database {string} with id {string}")]
    public void GivenTheRailwayIdentityApiReturnsDetailsForDatabaseWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, databaseId));
    }

    [Given("the Railway identity API returns not found")]
    public void GivenTheRailwayIdentityApiReturnsNotFound()
    {
        _handler.Enqueue(HttpStatusCode.NotFound, """{ "error": "not found" }""");
    }

    [When("the Railway remote identity resolver resolves configured database {string}")]
    public async Task WhenTheRailwayRemoteIdentityResolverResolvesConfiguredDatabase(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
        {
            IRailwayPostgresManagementClient client = new RailwayPostgresManagementClient(
                _httpClient,
                new RailwayPostgresManagementCredentials(AccountEmail, ApiKey));
            RailwayPostgresRemoteIdentityResolver resolver = new(client);

            _lastResolution = await resolver.ResolveAsync(databaseName, _cachedIdentity, CancellationToken.None)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Railway remote identity cache for Redis resource {string} is saved as database {string} with id {string}")]
    public async Task WhenTheRailwayRemoteIdentityCacheForRedisResourceIsSavedAsDatabaseWithId(
        string resourceName,
        string databaseName,
        string databaseId)
    {
        RailwayPostgresRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);

        await store.SaveAsync(
            resourceName,
            new RailwayPostgresRemoteIdentityState(databaseName, databaseId),
            CancellationToken.None).ConfigureAwait(false);
    }

    [Then("the Railway remote identity resolver returns database {string} with id {string}")]
    public void ThenTheRailwayRemoteIdentityResolverReturnsDatabaseWithId(string databaseName, string databaseId)
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.True(resolution.Found);
        Assert.NotNull(resolution.Database);
        Assert.Equal(databaseName, resolution.Database.DatabaseName);
        Assert.Equal(databaseId, resolution.Database.DatabaseId);
    }

    [Then("the Railway remote identity resolver returns no database")]
    public void ThenTheRailwayRemoteIdentityResolverReturnsNoDatabase()
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.False(resolution.Found);
        Assert.Null(resolution.Database);
    }

    [Then("the Railway remote identity cache is database {string} with id {string}")]
    public void ThenTheRailwayRemoteIdentityCacheIsDatabaseWithId(string databaseName, string databaseId)
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.NotNull(resolution.IdentityState);
        Assert.Equal(databaseName, resolution.IdentityState.DatabaseName);
        Assert.Equal(databaseId, resolution.IdentityState.ProviderDatabaseId);
    }

    [Then("the Railway remote identity cache is empty")]
    public void ThenTheRailwayRemoteIdentityCacheIsEmpty()
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.Null(resolution.IdentityState);
    }

    [Then("the Railway remote identity was resolved from the cached identity")]
    public void ThenTheRailwayRemoteIdentityWasResolvedFromTheCachedIdentity()
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.True(resolution.ResolvedFromCachedIdentity);
    }

    [Then("the Railway remote identity was not resolved from the cached identity")]
    public void ThenTheRailwayRemoteIdentityWasNotResolvedFromTheCachedIdentity()
    {
        RailwayPostgresRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.False(resolution.ResolvedFromCachedIdentity);
    }

    [Then("the Railway remote identity cache for Redis resource {string} loads database {string} with id {string}")]
    public async Task ThenTheRailwayRemoteIdentityCacheForRedisResourceLoadsDatabaseWithId(
        string resourceName,
        string databaseName,
        string databaseId)
    {
        RailwayPostgresRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);
        RailwayPostgresRemoteIdentityState? state = await store.LoadAsync(resourceName, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(state);
        Assert.Equal(databaseName, state.DatabaseName);
        Assert.Equal(databaseId, state.ProviderDatabaseId);
    }

    [Then("the Railway remote identity cache for Redis resource {string} is empty")]
    public async Task ThenTheRailwayRemoteIdentityCacheForRedisResourceIsEmpty(string resourceName)
    {
        RailwayPostgresRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);
        RailwayPostgresRemoteIdentityState? state = await store.LoadAsync(resourceName, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Null(state);
    }

    [Then("the Railway remote identity resolver fails with provider kind {string}")]
    public void ThenTheRailwayRemoteIdentityResolverFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_lastException);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway remote identity failure message contains {string}")]
    public void ThenTheRailwayRemoteIdentityFailureMessageContains(string value)
    {
        Exception exception = _lastException ?? throw new InvalidOperationException("No exception was captured.");

        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Railway identity request sequence is:")]
    public void ThenTheRailwayIdentityRequestSequenceIs(DataTable table)
    {
        Assert.Equal(table.Rows.Count, _handler.Requests.Count);

        for (int requestIndex = 0; requestIndex < table.Rows.Count; requestIndex++)
        {
            Assert.Equal(table.Rows[requestIndex]["Method"], _handler.Requests[requestIndex].Method.Method);
            Assert.Equal(table.Rows[requestIndex]["Path"], _handler.Requests[requestIndex].PathAndQuery);
        }
    }

    private async Task CaptureExceptionAsync(Func<Task> operation)
    {
        _lastException = await Record.ExceptionAsync(operation).ConfigureAwait(false);
    }

    private static string CreateDatabaseDetailsJson(string databaseName, string databaseId)
    {
        return $$"""
        {
          "database_id": "{{databaseId}}",
          "database_name": "{{databaseName}}",
          "endpoint": "global-apt-1.railway.io",
          "port": 6379,
          "password": "redis-password",
          "tls": true,
          "state": "active",
          "modifying_state": null,
          "primary_region": "eu-west-1",
          "read_regions": ["eu-west-2"],
          "type": "payg",
          "budget": 50,
          "eviction": true,
          "customer_id": "cust-1"
        }
        """;
    }

    private sealed class FakeDeploymentStateManager : IDeploymentStateManager
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
}
