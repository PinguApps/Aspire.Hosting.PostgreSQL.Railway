using System.Runtime.ExceptionServices;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class ReconcileMutableSettingsStepDefinitions
{
    private readonly FakeReconcileManagementClient _client = new();
    private RailwayPostgresDatabaseDetails? _database;
    private RailwayPostgresDatabaseDetails? _result;
    private RailwayPostgresRemoteIdentityState? _cachedIdentity;
    private RailwayPostgresRemoteIdentityState? _savedIdentity;
    private Exception? _exception;

    [Given("the Railway reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public void GivenTheRailwayReconcileTargetDatabaseHasSettingsWithEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        SetDatabase(readRegions, plan, budget, eviction: true);
    }

    [Given("the Railway reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction disabled")]
    public void GivenTheRailwayReconcileTargetDatabaseHasSettingsWithEvictionDisabled(
        string readRegions,
        string plan,
        int budget)
    {
        SetDatabase(readRegions, plan, budget, eviction: false);
    }

    [Given("the Railway reconcile target database has read regions {string}, coarse plan {string}, fixed plan {string}, budget {int}, and eviction disabled")]
    public void GivenTheRailwayReconcileTargetDatabaseHasFixedPlanWithEvictionDisabled(
        string readRegions,
        string coarsePlan,
        string fixedPlan,
        int budget)
    {
        SetDatabase(readRegions, coarsePlan, budget, eviction: false);

        RailwayPostgresDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.DbDiskThreshold = GetFixedPlanBytes(fixedPlan);
    }

    [Given("the Railway reconcile provider fails plan mutations")]
    public void GivenTheRailwayReconcileProviderFailsPlanMutations()
    {
        _client.FailingMutation = "plan";
    }

    [Given("the Railway reconcile provider does not persist budget mutations")]
    public void GivenTheRailwayReconcileProviderDoesNotPersistBudgetMutations()
    {
        _client.IgnoredMutation = "budget";
    }

    [Given("cached Railway remote identity for deployment is database {string} with id {string}")]
    public void GivenCachedRailwayRemoteIdentityForDeploymentIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new RailwayPostgresRemoteIdentityState(databaseName, databaseId);
    }

    [Given("the Railway reconcile provider has database {string} with id {string}")]
    public void GivenTheRailwayReconcileProviderHasDatabaseWithId(string databaseName, string databaseId)
    {
        _client.Databases.Add(CreateDatabase(databaseName, databaseId, "eu-west-1", "payg", 100, eviction: false));
    }

    [Given("the Railway reconcile target database provider name is {string}")]
    public void GivenTheRailwayReconcileTargetDatabaseProviderNameIs(string databaseName)
    {
        RailwayPostgresDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.DatabaseName = databaseName;
    }

    [Given("the Railway reconcile target database has no password")]
    public void GivenTheRailwayReconcileTargetDatabaseHasNoPassword()
    {
        RailwayPostgresDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.Password = null;
    }

    [When("Railway PostgreSQL reconciliation runs with read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public async Task WhenRailwayPostgresReconciliationRunsWithSettingsAndEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        await ReconcileAsync(options =>
        {
            options.ReadRegions = ParseReadRegions(readRegions);
            options.Plan = plan;
            options.SetBudget(budget);
            options.Eviction = true;
        }).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation runs with only plan {string}")]
    public async Task WhenRailwayPostgresReconciliationRunsWithOnlyPlan(string plan)
    {
        await ReconcileAsync(options => options.Plan = plan).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation runs with only {word} set to {string}")]
    public async Task WhenRailwayPostgresReconciliationRunsWithOnlySettingSetTo(string settingName, string value)
    {
        await ReconcileAsync(options => ApplySetting(options, settingName, value)).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation runs with only read regions set to {string}")]
    public async Task WhenRailwayPostgresReconciliationRunsWithOnlyReadRegionsSetTo(string value)
    {
        await ReconcileAsync(options => ApplySetting(options, "read regions", value)).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation runs with only TLS enabled")]
    public async Task WhenRailwayPostgresReconciliationRunsWithOnlyTlsEnabled()
    {
        await ReconcileAsync(options => options.Tls = true).ConfigureAwait(false);
    }

    [When("the Railway PostgreSQL deployment pipeline runs for existing-only with only plan {string}")]
    public async Task WhenTheRailwayPostgresDeploymentPipelineRunsForExistingOnlyWithOnlyPlan(string plan)
    {
        await TryRunDeploymentPipelineAsync(
            RailwayPostgresOwnershipMode.ExistingOnly,
            options => options.Plan = plan).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation is attempted with only plan {string}")]
    public async Task WhenRailwayPostgresReconciliationIsAttemptedWithOnlyPlan(string plan)
    {
        await TryReconcileAsync(options => options.Plan = plan).ConfigureAwait(false);
    }

    [When("Railway PostgreSQL reconciliation is attempted with only budget {int}")]
    public async Task WhenRailwayPostgresReconciliationIsAttemptedWithOnlyBudget(int budget)
    {
        await TryReconcileAsync(options => options.SetBudget(budget)).ConfigureAwait(false);
    }

    [When("a general Railway reconciliation exception is created with constructor {string}")]
    public void WhenAGeneralRailwayReconciliationExceptionIsCreatedWithConstructor(string constructor)
    {
        _exception = constructor switch
        {
            "Parameterless" => new RailwayPostgresReconciliationException(),
            "Message" => new RailwayPostgresReconciliationException("Reconciliation failure."),
            "MessageAndInner" => new RailwayPostgresReconciliationException("Reconciliation failure.", new InvalidOperationException()),
            _ => throw new InvalidOperationException($"Unknown constructor '{constructor}'."),
        };
    }

    [Then("Railway PostgreSQL reconciliation succeeds")]
    public void ThenRailwayPostgresReconciliationSucceeds()
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);
    }

    [Then("the Railway reconcile provider recorded no mutation calls")]
    public void ThenTheRailwayReconcileProviderRecordedNoMutationCalls()
    {
        Assert.Empty(_client.Mutations);
    }

    [Then("the Railway reconcile provider did not attempt reset-password")]
    public void ThenTheRailwayReconcileProviderDidNotAttemptResetPassword()
    {
        Assert.DoesNotContain(_client.Operations, operation => operation.Contains("reset-password", StringComparison.Ordinal));
    }

    [Then("the Railway reconcile provider recorded mutation calls in order:")]
    public void ThenTheRailwayReconcileProviderRecordedMutationCallsInOrder(DataTable table)
    {
        string[] expectedMutations = [.. table.Rows.Select(row => row["mutation"])];

        Assert.Equal(expectedMutations, _client.Mutations);
    }

    [Then("the Railway reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public void ThenTheRailwayReconcileTargetDatabaseHasSettingsWithEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        AssertDatabase(readRegions, plan, budget, eviction: true);
    }

    [Then("the Railway reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction disabled")]
    public void ThenTheRailwayReconcileTargetDatabaseHasSettingsWithEvictionDisabled(
        string readRegions,
        string plan,
        int budget)
    {
        AssertDatabase(readRegions, plan, budget, eviction: false);
    }

    [Then("Railway PostgreSQL reconciliation fails for setting {string}")]
    public void ThenRailwayPostgresReconciliationFailsForSetting(string settingName)
    {
        RailwayPostgresReconciliationException exception = Assert.IsType<RailwayPostgresReconciliationException>(_exception);

        Assert.Equal(settingName, exception.SettingName);
    }

    [Then("Railway PostgreSQL reconciliation fails with provider kind {string}")]
    public void ThenRailwayPostgresReconciliationFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresReconciliationException exception = Assert.IsType<RailwayPostgresReconciliationException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("Railway PostgreSQL deployment fails with provider kind {string}")]
    public void ThenRailwayPostgresDeploymentFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway PostgreSQL reconciliation failure message contains {string}")]
    public void ThenTheRailwayPostgresReconciliationFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    [Then("the Railway PostgreSQL deployment saved remote identity database {string} with id {string}")]
    public void ThenTheRailwayPostgresDeploymentSavedRemoteIdentityDatabaseWithId(string databaseName, string databaseId)
    {
        Assert.NotNull(_savedIdentity);
        Assert.Equal(databaseName, _savedIdentity.DatabaseName);
        Assert.Equal(databaseId, _savedIdentity.ProviderDatabaseId);
    }

    private void SetDatabase(string readRegions, string plan, int budget, bool eviction)
    {
        _database = CreateDatabase("orders-cache", "db-orders-cache", readRegions, plan, budget, eviction);

        _client.Database = _database;
    }

    private async Task ReconcileAsync(Action<RailwayPostgresDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => ReconcileCoreAsync(configure)).ConfigureAwait(false);

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
        }
    }

    private async Task TryReconcileAsync(Action<RailwayPostgresDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => ReconcileCoreAsync(configure)).ConfigureAwait(false);
    }

    private async Task ReconcileCoreAsync(Action<RailwayPostgresDeploymentOptions> configure)
    {
        RailwayPostgresDeploymentOptions options = new();
        configure(options);

        RailwayPostgresDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        _result = await new RailwayPostgresReconciler(_client)
            .ReconcileAsync(database, options.ToProviderOptions(), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task TryRunDeploymentPipelineAsync(
        RailwayPostgresOwnershipMode ownershipMode,
        Action<RailwayPostgresDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => RunDeploymentPipelineAsync(ownershipMode, configure)).ConfigureAwait(false);
    }

    private async Task RunDeploymentPipelineAsync(
        RailwayPostgresOwnershipMode ownershipMode,
        Action<RailwayPostgresDeploymentOptions> configure)
    {
        RailwayPostgresDeploymentOptions options = new();
        configure(options);

        _ = _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        _result = await RailwayPostgresDeploymentPipeline
            .ExecuteAsync(
                new RailwayPostgresResolvedDeployment(
                    "orders-cache",
                    ownershipMode,
                    new RailwayPostgresManagementCredentials("owner@example.com", "management-secret"),
                    options.ToProviderOptions()),
                _client,
                _cachedIdentity,
                identityState =>
                {
                    _savedIdentity = identityState;
                    return Task.CompletedTask;
                },
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private void AssertDatabase(string readRegions, string plan, int budget, bool eviction)
    {
        RailwayPostgresDatabaseDetails database =
            _result ?? throw new InvalidOperationException("Reconciliation has not completed.");

        Assert.Equal(ParseRegionNames(readRegions).Order(StringComparer.Ordinal), (database.ReadRegions ?? []).Order(StringComparer.Ordinal));
        Assert.Equal(plan, database.Type);
        Assert.Equal(budget, database.Budget);
        Assert.Equal(eviction, database.Eviction);
    }

    private static IReadOnlyList<RailwayPostgresValue> ParseReadRegions(string readRegions)
    {
        return [.. ParseRegionNames(readRegions).Select(RailwayPostgresValue.FromString)];
    }

    private static IReadOnlyList<string> ParseRegionNames(string readRegions)
    {
        return [.. readRegions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
    }

    private static void ApplySetting(
        RailwayPostgresDeploymentOptions options,
        string settingName,
        string value)
    {
        switch (settingName)
        {
            case "read regions":
                options.ReadRegions = ParseReadRegions(value);
                break;
            case "plan":
                options.Plan = value;
                break;
            case "budget":
                options.SetBudget(int.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case "eviction":
                options.Eviction = bool.Parse(value);
                break;
            default:
                throw new InvalidOperationException($"Unknown setting '{settingName}'.");
        }
    }

    private static RailwayPostgresDatabaseDetails CreateDatabase(
        string databaseName,
        string databaseId,
        string readRegions,
        string plan,
        int budget,
        bool eviction)
    {
        return new RailwayPostgresDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.railway.io",
            Port = 6379,
            Password = "test-password",
            Tls = true,
            State = "active",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ParseRegionNames(readRegions),
            Type = plan,
            DbDiskThreshold = plan == "payg" ? 100L * 1024L * 1024L * 1024L : null,
            Budget = budget,
            Eviction = eviction,
        };
    }

    private static long GetFixedPlanBytes(string fixedPlan)
    {
        long? fixedPlanBytes = GetFixedPlanBytesOrNull(fixedPlan);

        return fixedPlanBytes
            ?? throw new InvalidOperationException($"Unknown fixed plan '{fixedPlan}'.");
    }

    private static long? GetFixedPlanBytesOrNull(string fixedPlan)
    {
        const long mebibyte = 1024L * 1024L;
        const long gibibyte = 1024L * mebibyte;

        switch (fixedPlan)
        {
            case "fixed_250mb":
                return 250L * mebibyte;
            case "fixed_1gb":
                return 1L * gibibyte;
            case "fixed_5gb":
                return 5L * gibibyte;
            case "fixed_10gb":
                return 10L * gibibyte;
            case "fixed_50gb":
                return 50L * gibibyte;
            case "fixed_100gb":
                return 100L * gibibyte;
            case "fixed_500gb":
                return 500L * gibibyte;
            default:
                return null;
        }
    }

    private sealed class FakeReconcileManagementClient : IRailwayPostgresManagementClient
    {
        public RailwayPostgresDatabaseDetails? Database { get; set; }

        public List<RailwayPostgresDatabaseDetails> Databases { get; } = [];

        public string? FailingMutation { get; set; }

        public string? IgnoredMutation { get; set; }

        public List<string> Mutations { get; } = [];

        public List<string> Operations { get; } = [];

        public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            Operations.Add($"GET /redis/database/{databaseId}");

            RailwayPostgresDatabaseDetails database = GetDatabase(databaseId);

            return Task.FromResult(Clone(database));
        }

        public Task UpdateReadRegionsAsync(string databaseId, RailwayPostgresUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "read regions", database => database.ReadRegions = request.ReadRegions);

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, RailwayPostgresChangePlanRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "plan", database => ApplyPlanMutation(database, request.PlanName));

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, RailwayPostgresUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "budget", database => database.Budget = request.Budget);

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "eviction", database => database.Eviction = enabled);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            Operations.Add("GET /redis/databases");

            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            Operations.Add($"GET /redis/databases?name={databaseName}");

            RailwayPostgresDatabaseDetails? database = GetDatabases()
                .SingleOrDefault(database => database.DatabaseName == databaseName);

            return Task.FromResult(database is not null
                ? Clone(database)
                : null);
        }

        public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(RailwayPostgresCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            Operations.Add("POST /redis/database");

            throw new NotSupportedException();
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            Operations.Add($"WAIT /redis/database/{databaseId}");

            RailwayPostgresDatabaseDetails database = GetDatabase(databaseId);

            return Task.FromResult(Clone(database));
        }

        private void Mutate(string databaseId, string mutation, Action<RailwayPostgresDatabaseDetails> apply)
        {
            Operations.Add($"MUTATE {mutation} /redis/database/{databaseId}");
            Mutations.Add(mutation);

            if (FailingMutation == mutation)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.Validation,
                    statusCode: null,
                    $"Provider rejected {mutation}.");
            }

            if (IgnoredMutation == mutation)
            {
                return;
            }

            apply(GetDatabase(databaseId));
        }

        private static void ApplyPlanMutation(RailwayPostgresDatabaseDetails database, string planName)
        {
            long? fixedPlanBytes = GetFixedPlanBytesOrNull(planName);

            if (fixedPlanBytes is not null)
            {
                database.Type = "pro";
                database.DbDiskThreshold = fixedPlanBytes;

                return;
            }

            database.Type = planName;
            database.DbDiskThreshold = planName == "payg" ? 100L * 1024L * 1024L * 1024L : null;
        }

        private RailwayPostgresDatabaseDetails GetDatabase(string databaseId)
        {
            RailwayPostgresDatabaseDetails? database = GetDatabases()
                .SingleOrDefault(database => database.DatabaseId == databaseId);

            Assert.NotNull(database);

            return database;
        }

        private IEnumerable<RailwayPostgresDatabaseDetails> GetDatabases()
        {
            if (Database is not null)
            {
                yield return Database;
            }

            foreach (RailwayPostgresDatabaseDetails database in Databases)
            {
                yield return database;
            }
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
