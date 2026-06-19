using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class SupplementaryOutputsStepDefinitions
{
    private const string ManagementApiKey = "management-secret";

    private RailwayPostgresOutputs? _outputs;
    private RedisResource? _resource;
    private RailwayPostgresResolvedDeployment? _deployment;
    private FakeSupplementaryOutputsManagementClient? _client;
    private Exception? _exception;

    [Given("an Railway PostgreSQL resource with supplementary outputs")]
    public void GivenAnRailwayPostgresResourceWithSupplementaryOutputs()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        IResourceBuilder<RedisResource> redis = builder
            .AddRedis("cache")
            .PublishToRailway(
                "orders-cache",
                builder.AddParameter("railway-account-email", "owner@example.com"),
                builder.AddParameter("railway-api-key", ManagementApiKey, secret: true),
                RailwayPostgresOwnershipMode.CreateOnly,
                options =>
                {
                    options.Platform = "aws";
                    options.PrimaryRegion = "eu-west-1";
                    options.ReadRegions = ["eu-west-2"];
                    options.Plan = "payg";
                    options.Budget = "360";
                    options.Eviction = true;
                    options.Tls = true;
                });

        _outputs = redis.Resource.GetRailwayPostgresOutputs();
        _resource = redis.Resource;
        _deployment = new RailwayPostgresResolvedDeployment(
            "orders-cache",
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("owner@example.com", ManagementApiKey),
            new RailwayPostgresDeploymentOptions
            {
                Platform = "aws",
                PrimaryRegion = "eu-west-1",
                ReadRegions = ["eu-west-2"],
                Plan = "payg",
                Budget = "360",
                Eviction = true,
                Tls = true,
            }.ToProviderOptions());
    }

    [Given("the Railway deployment provider will create database {string} with id {string}")]
    public void GivenTheRailwayDeploymentProviderWillCreateDatabaseWithId(string databaseName, string databaseId)
    {
        _client = new FakeSupplementaryOutputsManagementClient(CreateDatabase(databaseName, databaseId));
    }

    [Given("the Railway deployment provider will create database {string} with id {string} without a password")]
    public void GivenTheRailwayDeploymentProviderWillCreateDatabaseWithIdWithoutAPassword(string databaseName, string databaseId)
    {
        RailwayPostgresDatabaseDetails database = CreateDatabase(databaseName, databaseId);
        database.Password = null;

        _client = new FakeSupplementaryOutputsManagementClient(database);
    }

    [When("the Railway deployment pipeline populates supplementary outputs")]
    public async Task WhenTheRailwayDeploymentPipelinePopulatesSupplementaryOutputs()
    {
        await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            GetDeployment(),
            GetClient(),
            GetOutputs(),
            CancellationToken.None).ConfigureAwait(false);
    }

    [When("the Railway deployment pipeline attempts to populate supplementary outputs")]
    public async Task WhenTheRailwayDeploymentPipelineAttemptsToPopulateSupplementaryOutputs()
    {
        _exception = await Record.ExceptionAsync(WhenTheRailwayDeploymentPipelinePopulatesSupplementaryOutputs).ConfigureAwait(false);
    }

    [Then("the supplementary Railway PostgreSQL outputs are:")]
    public async Task ThenTheSupplementaryRailwayPostgresOutputsAre(DataTable table)
    {
        IReadOnlyDictionary<string, RailwayPostgresOutputReference> outputs = GetOutputReferences();

        foreach (DataTableRow row in table.Rows)
        {
            RailwayPostgresOutputReference output = Assert.Contains(row["Name"], outputs);
            string? value = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(row["Value"], value);
        }
    }

    [Then("only the supplementary Railway PostgreSQL password output is secret")]
    public void ThenOnlyTheSupplementaryRailwayPostgresPasswordOutputIsSecret()
    {
        RailwayPostgresOutputs outputs = GetOutputs();

        foreach (RailwayPostgresOutputReference output in outputs.Properties)
        {
            Assert.Equal(
                string.Equals(output.Name, RailwayPostgresOutputNames.Password, StringComparison.Ordinal),
                output.Secret);
            Assert.Equal(output.Secret, RailwayPostgresOutputs.IsSecret(output.Name));
        }

        ReferenceExpression passwordExpression = outputs.Password.AsReferenceExpression();
        RailwayPostgresOutputReference passwordProvider =
            Assert.IsType<RailwayPostgresOutputReference>(Assert.Single(passwordExpression.ValueProviders));

        Assert.True(passwordProvider.Secret);
    }

    [Then("the Railway management API key is not surfaced as a supplementary output")]
    public async Task ThenTheRailwayManagementApiKeyIsNotSurfacedAsASupplementaryOutput()
    {
        foreach (RailwayPostgresOutputReference output in GetOutputs().Properties)
        {
            string? value = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.DoesNotContain(ManagementApiKey, output.Name, StringComparison.Ordinal);
            Assert.DoesNotContain(ManagementApiKey, value, StringComparison.Ordinal);
        }
    }

    [Then("the supplementary Railway PostgreSQL output names are stable")]
    public void ThenTheSupplementaryRailwayPostgresOutputNamesAreStable()
    {
        Assert.Equal(
            [
                RailwayPostgresOutputNames.Endpoint,
                RailwayPostgresOutputNames.Port,
                RailwayPostgresOutputNames.Password,
                RailwayPostgresOutputNames.Tls,
                RailwayPostgresOutputNames.DatabaseName,
            ],
            GetOutputs().Properties.Select(property => property.Name));
    }

    [Then("each supplementary Railway PostgreSQL output references the Redis resource")]
    public void ThenEachSupplementaryRailwayPostgresOutputReferencesTheRedisResource()
    {
        RedisResource resource = GetResource();

        foreach (ReferenceExpression output in GetOutputs().Properties.Select(property => property.AsReferenceExpression()))
        {
            IValueProvider valueProvider = Assert.Single(output.ValueProviders);
            IValueWithReferences valueWithReferences = Assert.IsAssignableFrom<IValueWithReferences>(valueProvider);
            Assert.Contains(resource, valueWithReferences.References);
        }
    }

    [Then("supplementary Railway PostgreSQL output population fails with provider kind {string}")]
    public void ThenSupplementaryRailwayPostgresOutputPopulationFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the supplementary Railway PostgreSQL output failure message contains {string}")]
    public void ThenTheSupplementaryRailwayPostgresOutputFailureMessageContains(string expectedText)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("Supplementary output population did not fail.");

        Assert.Contains(expectedText, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Railway supplementary output provider did not attempt reset-password")]
    public void ThenTheRailwaySupplementaryOutputProviderDidNotAttemptResetPassword()
    {
        Assert.DoesNotContain(GetClient().Operations, operation => operation.Contains("reset-password", StringComparison.Ordinal));
    }

    private IReadOnlyDictionary<string, RailwayPostgresOutputReference> GetOutputReferences()
    {
        return GetOutputs().Properties.ToDictionary(
            property => property.Name,
            StringComparer.Ordinal);
    }

    private RailwayPostgresOutputs GetOutputs()
    {
        return _outputs ?? throw new InvalidOperationException("The supplementary outputs were not created.");
    }

    private RedisResource GetResource()
    {
        return _resource ?? throw new InvalidOperationException("The Redis resource was not created.");
    }

    private RailwayPostgresResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("The deployment was not created.");
    }

    private FakeSupplementaryOutputsManagementClient GetClient()
    {
        return _client ?? throw new InvalidOperationException("The provider was not created.");
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

    private sealed class FakeSupplementaryOutputsManagementClient : IRailwayPostgresManagementClient
    {
        private readonly RailwayPostgresDatabaseDetails _database;

        public FakeSupplementaryOutputsManagementClient(RailwayPostgresDatabaseDetails database)
        {
            _database = database;
        }

        public List<string> Operations { get; } = [];

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add("GET /redis/databases");

            return Task.FromResult<IReadOnlyList<RailwayPostgresDatabaseSummary>>([]);
        }

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"GET /redis/databases?name={databaseName}");

            return Task.FromResult<RailwayPostgresDatabaseDetails?>(null);
        }

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(
            RailwayPostgresCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add("POST /redis/database");

            return Task.FromResult(new RailwayPostgresDatabaseDetails
            {
                DatabaseId = _database.DatabaseId,
                DatabaseName = request.DatabaseName,
            });
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"WAIT /redis/database/{databaseId}");

            Assert.Equal(_database.DatabaseId, databaseId);
            return Task.FromResult(_database);
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"GET /redis/database/{databaseId}");

            if (!string.Equals(databaseId, _database.DatabaseId, StringComparison.Ordinal))
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    "missing database");
            }

            return Task.FromResult(_database);
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
