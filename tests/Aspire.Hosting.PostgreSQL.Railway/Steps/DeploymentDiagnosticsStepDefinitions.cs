using System.Runtime.ExceptionServices;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class DeploymentDiagnosticsStepDefinitions
{
    private readonly DiagnosticManagementClient _client = new();
    private readonly CapturingProgressReporter _progressReporter = new();
    private RailwayPostgresResolvedDeployment? _deployment;
    private Exception? _exception;
    private string? _redactedMessage;

    [Given("an Railway diagnostic deployment for database {string}")]
    public void GivenAnRailwayDiagnosticDeploymentForDatabase(string databaseName)
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
            RailwayPostgresOwnershipMode.CreateOrAdopt,
            new RailwayPostgresManagementCredentials("owner@example.com", "api-key-secret"),
            options.ToProviderOptions());
    }

    [Given("the Railway diagnostic provider has no existing database")]
    public void GivenTheRailwayDiagnosticProviderHasNoExistingDatabase()
    {
        _client.Database = null;
    }

    [Given("the Railway diagnostic provider has existing database {string} with id {string}")]
    public void GivenTheRailwayDiagnosticProviderHasExistingDatabaseWithId(string databaseName, string databaseId)
    {
        _client.Database = CreateDatabase(databaseName, databaseId, plan: "payg");
    }

    [Given("the Railway diagnostic provider fails plan mutations")]
    public void GivenTheRailwayDiagnosticProviderFailsPlanMutations()
    {
        _client.Database = CreateDatabase("orders-cache", "db-orders", plan: "free");
        _client.FailPlanMutation = true;
    }

    [When("the Railway diagnostic deployment pipeline runs")]
    public async Task WhenTheRailwayDiagnosticDeploymentPipelineRuns()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
        }
    }

    [When("the Railway diagnostic deployment pipeline is attempted")]
    public async Task WhenTheRailwayDiagnosticDeploymentPipelineIsAttempted()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);
    }

    [When("the Railway diagnostic message {string} is redacted")]
    public void WhenTheRailwayDiagnosticMessageIsRedacted(string message)
    {
        RailwayPostgresResolvedDeployment deployment = GetDeployment();
        RailwayPostgresDatabaseDetails database = _client.Database
            ?? CreateDatabase(deployment.DatabaseName, "db-orders", plan: "payg");

        _redactedMessage = RailwayPostgresDeploymentDiagnostics.Redact(message, deployment, database);
    }

    [Then("the Railway diagnostic progress phases are:")]
    public void ThenTheRailwayDiagnosticProgressPhasesAre(DataTable table)
    {
        RailwayPostgresDeploymentPhase[] expectedPhases =
            [.. table.Rows.Select(row => Enum.Parse<RailwayPostgresDeploymentPhase>(row["phase"].Trim()))];

        Assert.Equal(expectedPhases, _progressReporter.Progress.Select(progress => progress.Phase));
    }

    [Then("the Railway diagnostic progress contains {string}")]
    public void ThenTheRailwayDiagnosticProgressContains(string expectedText)
    {
        Assert.Contains(
            _progressReporter.Progress,
            progress => progress.Message.Contains(expectedText, StringComparison.Ordinal));
    }

    [Then("the Railway diagnostic progress contains provider id {string}")]
    public void ThenTheRailwayDiagnosticProgressContainsProviderId(string providerDatabaseId)
    {
        Assert.Contains(
            _progressReporter.Progress,
            progress => string.Equals(progress.ProviderDatabaseId, providerDatabaseId, StringComparison.Ordinal));
    }

    [Then("the redacted Railway diagnostic message does not contain {string}")]
    public void ThenTheRedactedRailwayDiagnosticMessageDoesNotContain(string unexpectedText)
    {
        Assert.DoesNotContain(unexpectedText, GetRedactedMessage(), StringComparison.Ordinal);
    }

    [Then("the redacted Railway diagnostic message contains {string}")]
    public void ThenTheRedactedRailwayDiagnosticMessageContains(string expectedText)
    {
        Assert.Contains(expectedText, GetRedactedMessage(), StringComparison.Ordinal);
    }

    [Then("the Railway diagnostic deployment failure message contains {string}")]
    public void ThenTheRailwayDiagnosticDeploymentFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    private async Task RunPipelineAsync()
    {
        await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            GetDeployment(),
            _client,
            cachedIdentity: null,
            saveIdentityStateAsync: null,
            _progressReporter,
            resourceName: "cache",
            CancellationToken.None).ConfigureAwait(false);
    }

    private RailwayPostgresResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("No diagnostic deployment was configured.");
    }

    private string GetRedactedMessage()
    {
        return _redactedMessage ?? throw new InvalidOperationException("No diagnostic message was redacted.");
    }

    private static RailwayPostgresDatabaseDetails CreateDatabase(string databaseName, string databaseId, string plan)
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
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = plan,
            Budget = 360,
            Eviction = true,
        };
    }

    private sealed class CapturingProgressReporter : IRailwayPostgresDeploymentProgressReporter
    {
        public List<RailwayPostgresDeploymentProgress> Progress { get; } = [];

        public void Report(RailwayPostgresDeploymentProgress progress)
        {
            Progress.Add(progress);
        }
    }

    private sealed class DiagnosticManagementClient : IRailwayPostgresManagementClient
    {
        public RailwayPostgresDatabaseDetails? Database { get; set; }

        public bool FailPlanMutation { get; set; }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            return Task.FromResult(Database?.DatabaseName == databaseName ? Clone(Database) : null);
        }

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Clone(GetDatabase(databaseId)));
        }

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(RailwayPostgresCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            Database = CreateDatabase(request.DatabaseName, $"db-{request.DatabaseName}", request.Plan ?? "payg");

            return Task.FromResult(Clone(Database));
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Clone(GetDatabase(databaseId)));
        }

        public Task UpdateReadRegionsAsync(string databaseId, RailwayPostgresUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, RailwayPostgresChangePlanRequest request, CancellationToken cancellationToken)
        {
            if (FailPlanMutation)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.Validation,
                    statusCode: null,
                    "Provider rejected plan.");
            }

            GetDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, RailwayPostgresUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).Budget = request.Budget;

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).Eviction = enabled;

            return Task.CompletedTask;
        }

        private RailwayPostgresDatabaseDetails GetDatabase(string databaseId)
        {
            RailwayPostgresDatabaseDetails? database = Database;

            Assert.NotNull(database);
            Assert.Equal(databaseId, database.DatabaseId);

            return database;
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
