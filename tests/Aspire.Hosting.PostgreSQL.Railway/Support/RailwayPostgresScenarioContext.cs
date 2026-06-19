#pragma warning disable IDE0032

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

public sealed class RailwayPostgresScenarioContext
{
    private IDistributedApplicationBuilder? _appBuilder;
    private IResourceBuilder<RedisResource>? _redisBuilder;
    private IResourceBuilder<ContainerResource>? _containerBuilder;
    private IResourceBuilder<ParameterResource>? _accountEmail;
    private IResourceBuilder<ParameterResource>? _apiKey;

    public RailwayPostgresDeploymentOptions? CapturedDeploymentOptions { get; private set; }

    internal bool? FluentApiReturnedSameBuilder { get; private set; }

    internal RailwayPostgresOutputs? LastOutputs { get; private set; }

    public List<RailwayPostgresValue> ConfiguredReadRegions { get; } = ["eu-west-2"];

    internal FakeRailwayProvider FakeProvider { get; } = new();

    internal FakeRailwayPostgresDatabase? LastProviderDatabase { get; set; }

    internal Exception? LastCleanupException { get; set; }

    internal List<string> LiveCleanupLog { get; } = [];

    internal LiveRailwayTestSession LiveRailway { get; } = new();

    internal RailwayPostgresResolvedDeployment? ResolvedDeployment { get; private set; }

    internal Exception? DeploymentResolutionException { get; private set; }

    internal IResourceBuilder<RedisResource> RedisBuilder =>
        _redisBuilder ?? throw new InvalidOperationException("The Redis resource has not been created.");

    internal IResourceBuilder<ContainerResource> ContainerBuilder =>
        _containerBuilder ?? throw new InvalidOperationException("The consuming container has not been created.");

    private IDistributedApplicationBuilder AppBuilder =>
        _appBuilder ?? throw new InvalidOperationException("The application builder has not been created.");

    public void AddRedis(string resourceName)
    {
        _appBuilder = DistributedApplication.CreateBuilder();
        _redisBuilder = _appBuilder.AddRedis(resourceName);
    }

    public void MarkRedisForRailway(string databaseName)
    {
        _accountEmail ??= AppBuilder.AddParameter("railway-account-email");
        _apiKey ??= AppBuilder.AddParameter("railway-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToRailway(
            databaseName,
            _accountEmail,
            _apiKey,
            configure: options =>
            {
                CapturedDeploymentOptions = options;
                options.PrimaryRegion = "eu-west-1";
                options.ReadRegions = ConfiguredReadRegions;
                options.Tls = true;
            });
    }

    public void MarkRedisForRailway(string databaseName, RailwayPostgresOwnershipMode ownershipMode)
    {
        _accountEmail ??= AppBuilder.AddParameter("railway-account-email");
        _apiKey ??= AppBuilder.AddParameter("railway-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToRailway(
            databaseName,
            _accountEmail,
            _apiKey,
            ownershipMode);
    }

    public void MarkRedisForRailwayWithLiteralManagementCredentials()
    {
        _redisBuilder = RedisBuilder.PublishToRailway(
            RailwayPostgresValue.FromString("orders-cache"),
            RailwayPostgresValue.FromString("owner@example.com"),
            RailwayPostgresValue.FromString("management-secret"),
            RailwayPostgresOwnershipMode.CreateOrAdopt);
    }

    public void MarkRedisForRailwayThroughOverload(string overload)
    {
        IResourceBuilder<RedisResource> originalBuilder = RedisBuilder;

        _redisBuilder = overload switch
        {
            "literal database and parameter credentials" => RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                RailwayPostgresOwnershipMode.ExistingOnly),

            "parameter database and parameter credentials" => RedisBuilder.PublishToRailway(
                AppBuilder.AddParameter("railway-database-name"),
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                RailwayPostgresOwnershipMode.ExistingOnly),

            "literal deployment values" => RedisBuilder.PublishToRailway(
                RailwayPostgresValue.FromString("orders-cache"),
                RailwayPostgresValue.FromString("owner@example.com"),
                RailwayPostgresValue.FromString("management-secret"),
                RailwayPostgresOwnershipMode.ExistingOnly),

            _ => throw new ArgumentOutOfRangeException(nameof(overload), overload, "Unknown PublishToRailway overload."),
        };

        FluentApiReturnedSameBuilder = ReferenceEquals(originalBuilder, _redisBuilder);
    }

    public void MarkRedisForRailwayWithParameterBasedInputs()
    {
        IResourceBuilder<ParameterResource> databaseName = AppBuilder.AddParameter("railway-database-name");
        _accountEmail = AppBuilder.AddParameter("railway-account-email");
        _apiKey = AppBuilder.AddParameter("railway-api-key", secret: true);
        IResourceBuilder<ParameterResource> primaryRegion = AppBuilder.AddParameter("railway-primary-region");
        IResourceBuilder<ParameterResource> readRegion = AppBuilder.AddParameter("railway-read-region");

        _redisBuilder = RedisBuilder.PublishToRailway(
            databaseName,
            _accountEmail,
            _apiKey,
            RailwayPostgresOwnershipMode.ExistingOnly,
            options =>
            {
                options.PrimaryRegion = RailwayPostgresValue.FromParameter(primaryRegion);
                options.ReadRegions = [RailwayPostgresValue.FromParameter(readRegion)];
                options.Plan = "payg";
            });
    }

    public void MarkRedisForRailwayWithResolvableParameterInputs()
    {
        IResourceBuilder<ParameterResource> databaseName = AppBuilder.AddParameter("railway-database-name", "orders-cache");
        _accountEmail = AppBuilder.AddParameter("railway-account-email", "owner@example.com");
        _apiKey = AppBuilder.AddParameter("railway-api-key", "management-secret", secret: true);
        IResourceBuilder<ParameterResource> platform = AppBuilder.AddParameter("railway-platform", "aws");
        IResourceBuilder<ParameterResource> primaryRegion = AppBuilder.AddParameter("railway-primary-region", "eu-west-1");
        IResourceBuilder<ParameterResource> readRegion = AppBuilder.AddParameter("railway-read-region", "eu-west-2");
        IResourceBuilder<ParameterResource> budget = AppBuilder.AddParameter("railway-budget", "360");

        _redisBuilder = RedisBuilder.PublishToRailway(
            databaseName,
            _accountEmail,
            _apiKey,
            RailwayPostgresOwnershipMode.CreateOnly,
            options =>
            {
                options.Platform = RailwayPostgresValue.FromParameter(platform);
                options.PrimaryRegion = RailwayPostgresValue.FromParameter(primaryRegion);
                options.ReadRegions = [RailwayPostgresValue.FromParameter(readRegion)];
                options.Plan = "payg";
                options.Budget = RailwayPostgresValue.FromParameter(budget);
                options.Eviction = true;
                options.Tls = true;
            });
    }

    public void MarkRedisForRailwayWithUnresolvedApiKeyParameter()
    {
        IResourceBuilder<ParameterResource> databaseName = AppBuilder.AddParameter("railway-database-name", "orders-cache");
        _accountEmail = AppBuilder.AddParameter("railway-account-email", "owner@example.com");
        _apiKey = AppBuilder.AddParameter("railway-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToRailway(
            databaseName,
            _accountEmail,
            _apiKey);
    }

    public async Task ResolveRailwayDeploymentInputsAsync()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(RedisBuilder.Resource);

        ResolvedDeployment = await RailwayPostgresDeployTimeResolver.ResolveAsync(
            state,
            RedisBuilder.Resource,
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
            RailwayPostgresDeploymentPipeline.ExecuteAsync(RedisBuilder.Resource, context: null!));
    }

    public void MarkRedisForRailwayWithTypedDomainOptions()
    {
        _accountEmail ??= AppBuilder.AddParameter("railway-account-email");
        _apiKey ??= AppBuilder.AddParameter("railway-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToRailway(
            "orders-cache",
            _accountEmail,
            _apiKey,
            RailwayPostgresOwnershipMode.CreateOnly,
            options =>
            {
                options.SetPlatform(RailwayPostgresCloudPlatform.Aws);
                options.SetPrimaryRegion(RailwayPostgresRegion.AwsEuWest1);
                options.SetReadRegions(RailwayPostgresRegion.AwsEuWest2);
                options.SetPlan(RailwayPostgresPlan.PayAsYouGo);
                options.SetBudget(360);
                options.Eviction = true;
                options.Tls = true;
            });
    }

    public void MarkRedisForRailwayThroughTypeScriptBridgeWithDtoOptions()
    {
        IResourceBuilder<RedisResource> originalBuilder = RedisBuilder;
        IResourceBuilder<ParameterResource> databaseName = AppBuilder.AddParameter("railway-database-name");
        _accountEmail = AppBuilder.AddParameter("railway-account-email");
        _apiKey = AppBuilder.AddParameter("railway-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToRailwayForTypeScript(
            databaseName,
            _accountEmail,
            _apiKey,
            new RailwayPostgresDeploymentOptionsDto
            {
                OwnershipMode = RailwayPostgresOwnershipMode.CreateOnly,
                Platform = RailwayPostgresCloudPlatform.Aws,
                PrimaryRegion = RailwayPostgresRegion.AwsEuWest1,
                ReadRegions = [RailwayPostgresRegion.AwsEuWest2],
                Plan = RailwayPostgresPlan.PayAsYouGo,
                Budget = 360,
                Eviction = true,
                Tls = true
            });

        FluentApiReturnedSameBuilder = ReferenceEquals(originalBuilder, _redisBuilder);
    }

    public void TryMarkRedisForRailwayThroughTypeScriptBridgeWithDisabledTls()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailwayForTypeScript(
                AppBuilder.AddParameter("railway-database-name"),
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                new RailwayPostgresDeploymentOptionsDto
                {
                    Tls = false
                }));
    }

    public void GetOutputsThroughTypeScriptBridge()
    {
        LastOutputs = RedisBuilder.GetRailwayPostgresOutputsForTypeScript();
    }

    public void MarkRedisForRailwayWithExplicitNullPrimaryRegion()
    {
        _redisBuilder = RedisBuilder.PublishToRailway(
            "orders-cache",
            AppBuilder.AddParameter("railway-account-email"),
            AppBuilder.AddParameter("railway-api-key", secret: true),
            configure: options => options.PrimaryRegion = null);
    }

    public Exception? ConfigurationException { get; private set; }

    public void TryMarkRedisForBlankRailwayDatabaseName()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                " ",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true)));
    }

    public void TryMarkRedisForRailwayWithMissingApiKey()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                null!));
    }

    public void TryMarkRedisForRailwayWithUnsupportedOwnershipMode()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                (RailwayPostgresOwnershipMode)999));
    }

    public void TryMarkRedisForRailwayWithDisabledTls()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                configure: options => options.Tls = false));
    }

    public void TryMarkRedisForRailwayWithUnsupportedPlatform()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                configure: options => options.Platform = "azure"));
    }

    public void TryMarkRedisForRailwayWithMismatchedPlatformAndPrimaryRegion()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                configure: options =>
                {
                    options.SetPlatform(RailwayPostgresCloudPlatform.Aws);
                    options.SetPrimaryRegion(RailwayPostgresRegion.GcpUsCentral1);
                }));
    }

    public void TryMarkRedisForRailwayWithBudgetOnFixedPlan()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToRailway(
                "orders-cache",
                AppBuilder.AddParameter("railway-account-email"),
                AppBuilder.AddParameter("railway-api-key", secret: true),
                configure: options =>
                {
                    options.SetPlan(RailwayPostgresPlan.Fixed1Gb);
                    options.SetBudget(360);
                }));
    }

    public void AddConsumingContainerReference()
    {
        _containerBuilder = AppBuilder.AddContainer("worker", "redis-reference-test")
            .WithReference(RedisBuilder);
    }
}
