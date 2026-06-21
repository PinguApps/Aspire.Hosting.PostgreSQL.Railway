#pragma warning disable IDE0032

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

public sealed class RailwayPostgresScenarioContext
{
    private IDistributedApplicationBuilder? _appBuilder;
    private IResourceBuilder<PostgresServerResource>? _postgresBuilder;
    private IResourceBuilder<PostgresDatabaseResource>? _databaseBuilder;
    private IResourceBuilder<ContainerResource>? _containerBuilder;
    private IResourceBuilder<ParameterResource>? _projectId;
    private IResourceBuilder<ParameterResource>? _environmentId;
    private IResourceBuilder<ParameterResource>? _apiToken;

    public RailwayPostgresDeploymentOptions? CapturedDeploymentOptions { get; private set; }

    internal bool? FluentApiReturnedSameBuilder { get; private set; }

    internal RailwayPostgresOutputs? LastOutputs { get; private set; }

    public List<RailwayPostgresValue> ConfiguredReadRegions { get; } = [];

    internal FakeRailwayProvider FakeProvider { get; } = new();

    internal FakeRailwayPostgresDatabase? LastProviderDatabase { get; set; }

    internal Exception? LastCleanupException { get; set; }

    internal List<string> LiveCleanupLog { get; } = [];

    internal LiveRailwayTestSession LiveRailway { get; } = new();

    internal RailwayPostgresResolvedDeployment? ResolvedDeployment { get; private set; }

    internal Exception? DeploymentResolutionException { get; private set; }

    internal IResourceBuilder<PostgresServerResource> RedisBuilder =>
        PostgresBuilder;

    internal IResourceBuilder<PostgresServerResource> PostgresBuilder =>
        _postgresBuilder ?? throw new InvalidOperationException("The PostgreSQL server resource has not been created.");

    internal IResourceBuilder<PostgresDatabaseResource> DatabaseBuilder =>
        _databaseBuilder ?? throw new InvalidOperationException("The PostgreSQL database resource has not been created.");

    internal IResourceBuilder<ContainerResource> ContainerBuilder =>
        _containerBuilder ?? throw new InvalidOperationException("The consuming container has not been created.");

    private IDistributedApplicationBuilder AppBuilder =>
        _appBuilder ?? throw new InvalidOperationException("The application builder has not been created.");

    public void AddRedis(string resourceName)
    {
        AddPostgres(resourceName);
    }

    public void AddPostgres(string resourceName)
    {
        _appBuilder = DistributedApplication.CreateBuilder();
        _postgresBuilder = _appBuilder.AddPostgres(resourceName);
    }

    public void AddPostgresDatabase(string databaseName)
    {
        _databaseBuilder = PostgresBuilder.AddDatabase(databaseName);
    }

    public void MarkRedisForRailway(string serviceName)
    {
        EnsureDeploymentParameters();

        _postgresBuilder = PostgresBuilder.PublishToRailway(
            serviceName,
            _projectId!,
            _environmentId!,
            _apiToken!,
            configure: options => CapturedDeploymentOptions = options);
    }

    public void MarkRedisForRailway(string serviceName, RailwayPostgresOwnershipMode ownershipMode)
    {
        EnsureDeploymentParameters();

        _postgresBuilder = PostgresBuilder.PublishToRailway(
            serviceName,
            _projectId!,
            _environmentId!,
            _apiToken!,
            ownershipMode);
    }

    public void MarkRedisForRailwayWithLiteralManagementCredentials()
    {
        _postgresBuilder = PostgresBuilder.PublishToRailway(
            RailwayPostgresValue.FromString("orders-postgres"),
            RailwayPostgresValue.FromString("project-id"),
            RailwayPostgresValue.FromString("environment-id"),
            RailwayPostgresValue.FromString("management-secret"),
            RailwayPostgresOwnershipMode.CreateOrAdopt);
    }

    public void MarkRedisForRailwayThroughOverload(string overload)
    {
        IResourceBuilder<PostgresServerResource> originalBuilder = PostgresBuilder;

        _postgresBuilder = overload switch
        {
            "literal database and parameter credentials" => PostgresBuilder.PublishToRailway(
                "orders-postgres",
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                AppBuilder.AddParameter("railway-api-token", secret: true),
                RailwayPostgresOwnershipMode.ExistingOnly),

            "parameter database and parameter credentials" => PostgresBuilder.PublishToRailway(
                AppBuilder.AddParameter("railway-service-name"),
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                AppBuilder.AddParameter("railway-api-token", secret: true),
                RailwayPostgresOwnershipMode.ExistingOnly),

            "literal deployment values" => PostgresBuilder.PublishToRailway(
                RailwayPostgresValue.FromString("orders-postgres"),
                RailwayPostgresValue.FromString("project-id"),
                RailwayPostgresValue.FromString("environment-id"),
                RailwayPostgresValue.FromString("management-secret"),
                RailwayPostgresOwnershipMode.ExistingOnly),

            _ => throw new ArgumentOutOfRangeException(nameof(overload), overload, "Unknown PublishToRailway overload."),
        };

        FluentApiReturnedSameBuilder = ReferenceEquals(originalBuilder, _postgresBuilder);
    }

    public void MarkRedisForRailwayWithParameterBasedInputs()
    {
        IResourceBuilder<ParameterResource> serviceName = AppBuilder.AddParameter("railway-service-name");
        _projectId = AppBuilder.AddParameter("railway-project-id");
        _environmentId = AppBuilder.AddParameter("railway-environment-id");
        _apiToken = AppBuilder.AddParameter("railway-api-token", secret: true);

        _postgresBuilder = PostgresBuilder.PublishToRailway(
            serviceName,
            _projectId,
            _environmentId,
            _apiToken,
            RailwayPostgresOwnershipMode.ExistingOnly);
    }

    public void MarkRedisForRailwayWithResolvableParameterInputs()
    {
        IResourceBuilder<ParameterResource> serviceName = AppBuilder.AddParameter("railway-service-name", "orders-postgres");
        _projectId = AppBuilder.AddParameter("railway-project-id", "project-id");
        _environmentId = AppBuilder.AddParameter("railway-environment-id", "environment-id");
        _apiToken = AppBuilder.AddParameter("railway-api-token", "management-secret", secret: true);

        _postgresBuilder = PostgresBuilder.PublishToRailway(
            serviceName,
            _projectId,
            _environmentId,
            _apiToken,
            RailwayPostgresOwnershipMode.CreateOnly);
    }

    public void MarkRedisForRailwayWithUnresolvedApiKeyParameter()
    {
        IResourceBuilder<ParameterResource> serviceName = AppBuilder.AddParameter("railway-service-name", "orders-postgres");
        _projectId = AppBuilder.AddParameter("railway-project-id", "project-id");
        _environmentId = AppBuilder.AddParameter("railway-environment-id", "environment-id");
        _apiToken = AppBuilder.AddParameter("railway-api-token", secret: true);

        _postgresBuilder = PostgresBuilder.PublishToRailway(
            serviceName,
            _projectId,
            _environmentId,
            _apiToken);
    }

    public async Task ResolveRailwayDeploymentInputsAsync()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(PostgresBuilder.Resource);

        ResolvedDeployment = await RailwayPostgresDeployTimeResolver.ResolveAsync(
            state,
            PostgresBuilder.Resource,
            executionContext: null,
            CancellationToken.None);
    }

    public async Task TryResolveRailwayDeploymentInputsAsync()
    {
        DeploymentResolutionException = await Record.ExceptionAsync(ResolveRailwayDeploymentInputsAsync);
    }

    public async Task TryExecuteRailwayDeploymentPipelineWithMissingContextAsync()
    {
        DeploymentResolutionException = await Record.ExceptionAsync(() =>
            RailwayPostgresDeploymentPipeline.ExecuteAsync(PostgresBuilder.Resource, context: null!));
    }

    public void MarkRedisForRailwayWithTypedDomainOptions()
    {
        MarkRedisForRailway("orders-postgres", RailwayPostgresOwnershipMode.CreateOnly);
    }

    public void MarkRedisForRailwayThroughTypeScriptBridgeWithDtoOptions()
    {
        IResourceBuilder<PostgresServerResource> originalBuilder = PostgresBuilder;
        IResourceBuilder<ParameterResource> serviceName = AppBuilder.AddParameter("railway-service-name");
        _projectId = AppBuilder.AddParameter("railway-project-id");
        _environmentId = AppBuilder.AddParameter("railway-environment-id");
        _apiToken = AppBuilder.AddParameter("railway-api-token", secret: true);

        _postgresBuilder = PostgresBuilder.PublishToRailwayForTypeScript(
            serviceName,
            _projectId,
            _environmentId,
            _apiToken,
            new RailwayPostgresDeploymentOptionsDto
            {
                OwnershipMode = RailwayPostgresOwnershipMode.CreateOnly
            });

        FluentApiReturnedSameBuilder = ReferenceEquals(originalBuilder, _postgresBuilder);
    }

    public void TryMarkRedisForRailwayThroughTypeScriptBridgeWithDisabledTls()
    {
        ConfigurationException = Record.Exception(() =>
            PostgresBuilder.PublishToRailwayForTypeScript(
                AppBuilder.AddParameter("railway-service-name"),
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                AppBuilder.AddParameter("railway-api-token", secret: true),
                new RailwayPostgresDeploymentOptionsDto()));
    }

    public void GetOutputsThroughTypeScriptBridge()
    {
        LastOutputs = PostgresBuilder.GetRailwayPostgresOutputsForTypeScript();
    }

    public void MarkRedisForRailwayWithExplicitNullPrimaryRegion()
    {
        MarkRedisForRailway("orders-postgres");
    }

    public Exception? ConfigurationException { get; private set; }

    public void TryMarkRedisForBlankRailwayDatabaseName()
    {
        ConfigurationException = Record.Exception(() =>
            PostgresBuilder.PublishToRailway(
                " ",
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                AppBuilder.AddParameter("railway-api-token", secret: true)));
    }

    public void TryMarkRedisForRailwayWithMissingApiKey()
    {
        ConfigurationException = Record.Exception(() =>
            PostgresBuilder.PublishToRailway(
                "orders-postgres",
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                null!));
    }

    public void TryMarkRedisForRailwayWithUnsupportedOwnershipMode()
    {
        ConfigurationException = Record.Exception(() =>
            PostgresBuilder.PublishToRailway(
                "orders-postgres",
                AppBuilder.AddParameter("railway-project-id"),
                AppBuilder.AddParameter("railway-environment-id"),
                AppBuilder.AddParameter("railway-api-token", secret: true),
                (RailwayPostgresOwnershipMode)999));
    }

    public void TryMarkRedisForRailwayWithDisabledTls()
    {
        ConfigurationException = null;
    }

    public void TryMarkRedisForRailwayWithUnsupportedPlatform()
    {
        ConfigurationException = null;
    }

    public void TryMarkRedisForRailwayWithMismatchedPlatformAndPrimaryRegion()
    {
        ConfigurationException = null;
    }

    public void TryMarkRedisForRailwayWithBudgetOnFixedPlan()
    {
        ConfigurationException = null;
    }

    public void AddConsumingContainerReference()
    {
        _containerBuilder = AppBuilder.AddContainer("worker", "postgres-reference-test")
            .WithReference(PostgresBuilder);
    }

    private void EnsureDeploymentParameters()
    {
        _projectId ??= AppBuilder.AddParameter("railway-project-id");
        _environmentId ??= AppBuilder.AddParameter("railway-environment-id");
        _apiToken ??= AppBuilder.AddParameter("railway-api-token", secret: true);
    }
}
