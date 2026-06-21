using System.Reflection;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Npgsql;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

public sealed class RailwayPostgresContractTests
{
    [Fact]
    public void PublishToRailway_AttachesRailwayMetadataWithoutReplacingPostgresResource()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> serviceName = app.AddParameter("railway-postgres-service-name");
        IResourceBuilder<ParameterResource> projectId = app.AddParameter("railway-project-id");
        IResourceBuilder<ParameterResource> environmentId = app.AddParameter("railway-environment-id");
        IResourceBuilder<ParameterResource> apiToken = app.AddParameter("railway-api-token", secret: true);
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres");

        IResourceBuilder<PostgresServerResource> returned = postgres.PublishToRailway(
            serviceName,
            projectId,
            environmentId,
            apiToken,
            RailwayPostgresOwnershipMode.ExistingOnly);

        Assert.Same(postgres, returned);
        Assert.IsType<PostgresServerResource>(postgres.Resource);
        Assert.True(postgres.Resource.IsExcludedFromPublish());

        RailwayPostgresDeploymentState state = postgres.Resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException("Railway deployment state was not attached.");
        Assert.Equal("railway-postgres-service-name", state.ServiceName.Parameter?.Name);
        Assert.Equal("railway-project-id", state.ProjectId.Parameter?.Name);
        Assert.Equal("railway-environment-id", state.EnvironmentId.Parameter?.Name);
        Assert.Equal("railway-api-token", state.ApiToken.Parameter?.Name);
        Assert.Equal(RailwayPostgresOwnershipMode.ExistingOnly, state.OwnershipMode);
        Assert.Single(postgres.Resource.Annotations.OfType<PipelineStepAnnotation>());
    }

    [Fact]
    public async Task DeployTimeResolver_ResolvesRailwayInputsWithoutLeakingApiTokenIntoConnectionProperties()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                app.AddParameter("railway-postgres-service-name", "orders-postgres"),
                app.AddParameter("railway-project-id", "project-id"),
                app.AddParameter("railway-environment-id", "environment-id"),
                app.AddParameter("railway-api-token", "management-secret", secret: true),
                RailwayPostgresOwnershipMode.CreateOnly);

        RailwayPostgresDeploymentState state = postgres.Resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException("Railway deployment state was not attached.");

        RailwayPostgresResolvedDeployment deployment = await RailwayPostgresDeployTimeResolver.ResolveAsync(
            state,
            postgres.Resource,
            executionContext: null,
            CancellationToken.None);

        Assert.Equal("orders-postgres", deployment.ServiceName);
        Assert.Equal("project-id", deployment.ProjectId);
        Assert.Equal("environment-id", deployment.EnvironmentId);
        Assert.Equal("management-secret", deployment.ManagementCredentials.ApiToken);
        IResourceWithConnectionString connectionResource = Assert.IsAssignableFrom<IResourceWithConnectionString>(postgres.Resource);
        Assert.DoesNotContain(
            "management-secret",
            connectionResource.GetConnectionProperties().Select(property => property.Value.ValueExpression),
            StringComparer.Ordinal);
    }

    [Fact]
    [Trait("Category", "live-railway")]
    public async Task LiveRailwayManagementClient_ResolvesEnvironmentAndCanQueryServices()
    {
        using LiveRailwayTestSession session = new();
        Assert.SkipUnless(
            session.HasCredentials,
            "RAILWAY_API_TOKEN, RAILWAY_PROJECT_ID, and RAILWAY_ENVIRONMENT_ID are required for live Railway tests.");

        RailwayPostgresManagementClient client = session.CreateManagementClient();

        string environmentId = await client.ResolveEnvironmentIdAsync(
            session.ProjectId!,
            session.EnvironmentId!,
            CancellationToken.None);
        RailwayPostgresDatabaseDetails? service = await client.FindServiceByNameAsync(
            session.ProjectId!,
            environmentId,
            LiveRailwayTestSession.CreateDisposableDatabaseName("missing-postgres"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(environmentId));
        Assert.Null(service);
    }

    [Fact]
    public async Task RailwayOutputs_PopulateServerAndChildDatabaseConnectionStrings()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true));
        IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

        RailwayPostgresDatabaseDetails service = CreateServiceDetails(databaseName: "railway");
        postgres.Resource.ApplyRailwayPostgresConnectionOutput(service);
        orders.Resource.ApplyRailwayPostgresConnectionOutput(service.WithDatabaseName("orders"));
        postgres.Resource.GetRailwayPostgresOutputs().Populate(service);

        IResourceWithConnectionString serverConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(postgres.Resource);
        IResourceWithConnectionString databaseConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(orders.Resource);
        string? serverConnectionString = await serverConnection.GetConnectionStringAsync(CancellationToken.None);
        string? databaseConnectionString = await databaseConnection.GetConnectionStringAsync(CancellationToken.None);

        Assert.Equal("railway", new NpgsqlConnectionStringBuilder(serverConnectionString).Database);
        Assert.Equal("orders", new NpgsqlConnectionStringBuilder(databaseConnectionString).Database);

        RailwayPostgresOutputs outputs = postgres.Resource.GetRailwayPostgresOutputs();
        Assert.Equal("svc_123", await outputs.ServiceId.GetValueAsync(CancellationToken.None));
        Assert.Equal("shortline.proxy.rlwy.net", await outputs.Host.GetValueAsync(CancellationToken.None));
        Assert.True(RailwayPostgresOutputs.IsSecret(outputs.Password.Name));
        Assert.True(RailwayPostgresOutputs.IsSecret(outputs.ConnectionString.Name));
    }

    [Fact]
    public void RailwayConnectionOutputs_PreserveAspirePostgresConnectionPropertyNames()
    {
        RailwayPostgresDatabaseDetails service = CreateServiceDetails(databaseName: "orders");
        RailwayPostgresConnectionOutput concreteOutput = new(service);
        Dictionary<string, ReferenceExpression> concreteProperties = concreteOutput
            .GetConnectionProperties()
            .ToDictionary(property => property.Key, property => property.Value);

        Assert.Equal("orders", concreteProperties["Database"].ValueExpression);
        Assert.Equal("orders", concreteProperties["DatabaseName"].ValueExpression);
        Assert.Equal("postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/orders", concreteProperties["Uri"].ValueExpression);
        Assert.Equal("jdbc:postgresql://shortline.proxy.rlwy.net:27543/orders", concreteProperties["JdbcConnectionString"].ValueExpression);

        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true));
        RailwayPostgresReferenceConnectionOutput referenceOutput =
            RailwayPostgresReferenceConnectionOutput.ForDatabase(
                postgres.Resource.GetRailwayPostgresOutputs(),
                "orders");
        Dictionary<string, ReferenceExpression> referenceProperties = referenceOutput
            .GetConnectionProperties()
            .ToDictionary(property => property.Key, property => property.Value);

        Assert.Equal("orders", referenceProperties["Database"].ValueExpression);
        Assert.Equal("orders", referenceProperties["DatabaseName"].ValueExpression);
        Assert.Contains("postgresql://", referenceProperties["Uri"].ValueExpression, StringComparison.Ordinal);
        Assert.Contains("jdbc:postgresql://", referenceProperties["JdbcConnectionString"].ValueExpression, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RailwayOutputReferences_DoNotFailLocalRunBeforeDeployOutputsArePopulated()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true));
        RailwayPostgresOutputReference host = postgres.Resource.GetRailwayPostgresOutputs().Host;
        ValueProviderContext runContext = new()
        {
            ExecutionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
        };
        ValueProviderContext publishContext = new()
        {
            ExecutionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
        };

        string? localValue = await host.GetValueAsync(runContext, CancellationToken.None);
        InvalidOperationException publishException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await host.GetValueAsync(publishContext, CancellationToken.None));

        Assert.Equal(string.Empty, localValue);
        Assert.Contains("deployment pipeline", publishException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RailwayReferenceConnectionOutput_EscapesConnectionStringValues()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true));
        RailwayPostgresOutputs outputs = postgres.Resource.GetRailwayPostgresOutputs();
        RailwayPostgresDatabaseDetails service = new()
        {
            ServiceId = "svc_123",
            ServiceName = "orders-postgres",
            ProjectId = "project-id",
            EnvironmentId = "environment-id",
            Host = "shortline.proxy.rlwy.net",
            Port = 27543,
            UserName = "post;gres",
            Password = "postgres;password\"value",
            DatabaseName = "rail;way",
            ConnectionString = RailwayPostgresConnectionString.Create(
                "shortline.proxy.rlwy.net",
                27543,
                "post;gres",
                "postgres;password\"value",
                "rail;way"),
            ProvisioningConnectionString = string.Empty,
            LatestDeploymentStatus = "SUCCESS",
        };
        outputs.Populate(service);
        RailwayPostgresReferenceConnectionOutput referenceOutput =
            RailwayPostgresReferenceConnectionOutput.ForDatabase(outputs, "orders;db");

        string? connectionString = await referenceOutput.GetConnectionStringAsync(CancellationToken.None);
        NpgsqlConnectionStringBuilder builder = new(connectionString);

        Assert.Equal("post;gres", builder.Username);
        Assert.Equal("postgres;password\"value", builder.Password);
        Assert.Equal("orders;db", builder.Database);
        Assert.Contains("{postgres.outputs.ConnectionString}", referenceOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
        Assert.DoesNotContain("{postgres.outputs.Password}", referenceOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);

        RailwayPostgresReferenceConnectionOutput serverReferenceOutput =
            RailwayPostgresReferenceConnectionOutput.ForServer(outputs);
        Dictionary<string, ReferenceExpression> serverProperties = serverReferenceOutput
            .GetConnectionProperties()
            .ToDictionary(property => property.Key, property => property.Value);
        Dictionary<string, ReferenceExpression> databaseProperties = referenceOutput
            .GetConnectionProperties()
            .ToDictionary(property => property.Key, property => property.Value);

        Assert.Equal("post%3Bgres", await outputs.UrlEscapedUserName.GetValueAsync(CancellationToken.None));
        Assert.Equal("postgres%3Bpassword%22value", await outputs.UrlEscapedPassword.GetValueAsync(CancellationToken.None));
        Assert.Contains("{postgres.outputs.UrlEscapedUserName}", serverProperties["Uri"].ValueExpression, StringComparison.Ordinal);
        Assert.Contains("{postgres.outputs.UrlEscapedPassword}", serverProperties["Uri"].ValueExpression, StringComparison.Ordinal);
        Assert.Contains("{postgres.outputs.UrlEscapedDatabaseName}", serverProperties["Uri"].ValueExpression, StringComparison.Ordinal);
        Assert.Contains("orders%3Bdb", databaseProperties["Uri"].ValueExpression, StringComparison.Ordinal);
        Assert.Contains("orders%3Bdb", databaseProperties["JdbcConnectionString"].ValueExpression, StringComparison.Ordinal);
    }

    [Fact]
    public void PublishToRailway_CapturesDeploymentOptions()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true),
                configure: options =>
                {
                    options.Region = RailwayPostgresRegions.EuWestMetal;
                    options.RestartPolicy = RailwayPostgresRestartPolicy.OnFailure;
                    options.RestartPolicyMaxRetries = 3;
                    options.MemoryGB = 2;
                    options.VCpus = 1;
                    options.SharedMemoryBytes = 524288000;
                    options.Template = RailwayPostgresTemplate.PointInTimeRecovery;
                });

        RailwayPostgresDeploymentOptions options = postgres.Resource.GetRailwayPostgresDeploymentState()
            ?.Options
            ?? throw new InvalidOperationException("Railway deployment state was not attached.");

        Assert.Equal(RailwayPostgresRegions.EuWestMetal, options.Region);
        Assert.Equal(RailwayPostgresRestartPolicy.OnFailure, options.RestartPolicy);
        Assert.Equal(3, options.RestartPolicyMaxRetries);
        Assert.Equal(2, options.MemoryGB);
        Assert.Equal(1, options.VCpus);
        Assert.Equal(524288000, options.SharedMemoryBytes);
        Assert.Equal(RailwayPostgresTemplate.PointInTimeRecovery, options.Template);
    }

    [Fact]
    public void PublishToRailway_PreservesObsoletePointInTimeRecoveryOption()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true),
                configure: options =>
                {
#pragma warning disable CS0618
                    options.PointInTimeRecovery = true;
#pragma warning restore CS0618
                });

        RailwayPostgresDeploymentOptions options = postgres.Resource.GetRailwayPostgresDeploymentState()
            ?.Options
            ?? throw new InvalidOperationException("Railway deployment state was not attached.");

        Assert.Equal(RailwayPostgresTemplate.PointInTimeRecovery, options.Template);
#pragma warning disable CS0618
        Assert.True(options.PointInTimeRecovery);
#pragma warning restore CS0618
    }

    [Fact]
    public void DeploymentOptions_ValidationMessageIncludesUnsupportedTemplate()
    {
        RailwayPostgresDeploymentOptions options = new()
        {
            Template = (RailwayPostgresTemplate)999,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("999", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishToRailway_UsesRailwayOutputsOnlyForPublishReferences()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres");
        IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");

        postgres.PublishToRailway(
            "orders-postgres",
            app.AddParameter("railway-project-id"),
            app.AddParameter("railway-environment-id"),
            app.AddParameter("railway-api-token", secret: true));
        IResourceBuilder<ContainerResource> api = app.AddContainer("api", "example/api")
            .WithReference(orders);

        Assert.Empty(postgres.Resource.Annotations.OfType<ConnectionStringRedirectAnnotation>());
        Assert.Empty(orders.Resource.Annotations.OfType<ConnectionStringRedirectAnnotation>());

        Dictionary<string, object> runEnvironmentVariables = [];
        DistributedApplicationExecutionContext runContext = new(DistributedApplicationOperation.Run);
        EnvironmentCallbackContext runCallbackContext = new(runContext, api.Resource, runEnvironmentVariables, CancellationToken.None);

        foreach (EnvironmentCallbackAnnotation annotation in api.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(runCallbackContext);
        }

        ConnectionStringReference runConnectionString = Assert.IsType<ConnectionStringReference>(runEnvironmentVariables["ConnectionStrings__orders"]);
        Assert.Same(orders.Resource, runConnectionString.Resource);

        Dictionary<string, object> environmentVariables = [];
        DistributedApplicationExecutionContext executionContext = new(DistributedApplicationOperation.Publish);
        EnvironmentCallbackContext callbackContext = new(executionContext, api.Resource, environmentVariables, CancellationToken.None);

        foreach (EnvironmentCallbackAnnotation annotation in api.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(callbackContext);
        }

        ConnectionStringReference connectionString = Assert.IsType<ConnectionStringReference>(environmentVariables["ConnectionStrings__orders"]);
        RailwayPostgresReferenceConnectionOutput referencedOutput =
            Assert.IsType<RailwayPostgresReferenceConnectionOutput>(connectionString.Resource);
        Assert.Contains("{postgres.outputs.ConnectionString}", referencedOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
        Assert.Contains("Database=orders", referencedOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
        Assert.DoesNotContain("railway-api-token", referencedOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishToRailway_UsesRailwayOutputsForReferencesAddedBeforePublishToRailway()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres");
        IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");
        IResourceBuilder<ContainerResource> api = app.AddContainer("api", "example/api")
            .WithReference(orders);

        postgres.PublishToRailway(
            "orders-postgres",
            app.AddParameter("railway-project-id"),
            app.AddParameter("railway-environment-id"),
            app.AddParameter("railway-api-token", secret: true));

        Dictionary<string, object> environmentVariables = [];
        DistributedApplicationExecutionContext executionContext = new(DistributedApplicationOperation.Publish);
        EnvironmentCallbackContext callbackContext = new(executionContext, api.Resource, environmentVariables, CancellationToken.None);

        foreach (EnvironmentCallbackAnnotation annotation in api.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(callbackContext);
        }

        ConnectionStringReference connectionString = Assert.IsType<ConnectionStringReference>(environmentVariables["ConnectionStrings__orders"]);
        RailwayPostgresReferenceConnectionOutput referencedOutput =
            Assert.IsType<RailwayPostgresReferenceConnectionOutput>(connectionString.Resource);
        Assert.Contains("{postgres.outputs.ConnectionString}", referencedOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
        Assert.Contains("Database=orders", referencedOutput.ConnectionStringExpression.ValueExpression, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeploymentOutputs_RedirectDatabasesAddedAfterPublishToRailwayOutputs()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres")
            .PublishToRailway(
                "orders-postgres",
                app.AddParameter("railway-project-id"),
                app.AddParameter("railway-environment-id"),
                app.AddParameter("railway-api-token", secret: true));
        IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");
        IResourceBuilder<ContainerResource> api = app.AddContainer("api", "example/api");
        global::Aspire.Hosting.ResourceBuilderExtensions.WithReference(api, orders);

        RailwayPostgresDatabaseDetails service = CreateServiceDetails(databaseName: "railway");
        postgres.Resource.ApplyRailwayPostgresConnectionOutput(service);
        RailwayPostgresConnectionOutput ordersOutput =
            orders.Resource.ApplyRailwayPostgresConnectionOutput(service.WithDatabaseName("orders"));
        postgres.Resource.GetRailwayPostgresOutputs().Populate(service);
        RailwayPostgresDeploymentPipeline.ApplyRailwayPostgresReferenceOverrides(
            new DistributedApplicationModel(app.Resources),
            orders.Resource,
            ordersOutput);

        Dictionary<string, object> environmentVariables = [];
        DistributedApplicationExecutionContext executionContext = new(DistributedApplicationOperation.Publish);
        EnvironmentCallbackContext callbackContext = new(executionContext, api.Resource, environmentVariables, CancellationToken.None);

        foreach (EnvironmentCallbackAnnotation annotation in api.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(callbackContext);
        }

        ConnectionStringReference connectionString = Assert.IsType<ConnectionStringReference>(environmentVariables["ConnectionStrings__orders"]);
        RailwayPostgresConnectionOutput output = Assert.IsType<RailwayPostgresConnectionOutput>(connectionString.Resource);
        Assert.Equal("orders", new NpgsqlConnectionStringBuilder(await output.GetConnectionStringAsync(CancellationToken.None)).Database);
    }

    [Fact]
    public async Task DeploymentPipeline_AppliesRailwayDeploymentOptions()
    {
        RailwayPostgresDeploymentOptions options = new()
        {
            Region = RailwayPostgresRegions.EuWestMetal,
            RestartPolicy = RailwayPostgresRestartPolicy.Never,
            RestartPolicyMaxRetries = 0,
            MemoryGB = 1,
            VCpus = 0.5,
            SharedMemoryBytes = 268435456,
            Template = RailwayPostgresTemplate.PgVector,
        };
        RailwayPostgresResolvedDeployment deployment = new(
            "orders-postgres",
            "project-id",
            "environment-id",
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("management-secret"),
            options);
        FakeManagementClient client = new(CreateServiceDetails());

        RailwayPostgresDatabaseDetails? database = await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            outputs: null,
            CancellationToken.None);

        Assert.NotNull(database);
        Assert.Equal("project-id", client.ConfiguredProjectId);
        Assert.Equal("environment-id", client.ConfiguredEnvironmentId);
        Assert.Equal("svc_123", client.ConfiguredServiceId);
        Assert.NotNull(client.ConfiguredOptions);
        Assert.Equal(RailwayPostgresRegions.EuWestMetal, client.ConfiguredOptions.Region);
        Assert.Equal(RailwayPostgresRestartPolicy.Never, client.ConfiguredOptions.RestartPolicy);
        Assert.Equal(0, client.ConfiguredOptions.RestartPolicyMaxRetries);
        Assert.Equal(1, client.ConfiguredOptions.MemoryGB);
        Assert.Equal(0.5, client.ConfiguredOptions.VCpus);
        Assert.Equal(268435456, client.ConfiguredOptions.SharedMemoryBytes);
        Assert.Equal(RailwayPostgresTemplate.PgVector, client.CreatedRequest?.Options.Template);
    }

    [Fact]
    public async Task DeploymentPipeline_WaitsForDeploymentQueuedByRailwayOptions()
    {
        RailwayPostgresResolvedDeployment deployment = new(
            "orders-postgres",
            "project-id",
            "environment-id",
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("management-secret"),
            new RailwayPostgresDeploymentOptions
            {
                SharedMemoryBytes = 268435456,
            });
        FakeManagementClient client = new(CreateServiceDetails(latestDeploymentId: "dep_before"))
        {
            ConfigurationQueuedDeployment = true,
        };

        await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            outputs: null,
            CancellationToken.None);

        Assert.Equal("dep_before", client.WaitedPollingOptions?.PreviousDeploymentId);
    }

    [Fact]
    public async Task DeploymentPipeline_SavesRemoteIdentityBeforePostCreateWork()
    {
        RailwayPostgresResolvedDeployment deployment = new(
            "orders-postgres",
            "project-id",
            "environment-id",
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("management-secret"),
            new RailwayPostgresDeploymentOptions
            {
                RestartPolicy = RailwayPostgresRestartPolicy.Never,
            });
        FakeManagementClient client = new(CreateServiceDetails())
        {
            ConfigureException = new InvalidOperationException("Configuration failed."),
        };
        RailwayPostgresRemoteIdentityState? savedIdentity = null;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RailwayPostgresDeploymentPipeline.ExecuteAsync(
                deployment,
                client,
                cachedIdentity: null,
                identity =>
                {
                    savedIdentity = identity;
                    return Task.CompletedTask;
                },
                CancellationToken.None));

        Assert.Equal("Configuration failed.", exception.Message);
        Assert.NotNull(savedIdentity);
        Assert.Equal("project-id", savedIdentity.ProjectId);
        Assert.Equal("orders-postgres", savedIdentity.ServiceName);
        Assert.Equal("svc_123", savedIdentity.ServiceId);
    }

    [Fact]
    public async Task CreateFlow_CreatesRailwayServiceAndWaitsForConnectionVariables()
    {
        RailwayPostgresResolvedDeployment deployment = CreateDeployment(RailwayPostgresOwnershipMode.CreateOnly);
        FakeManagementClient client = new(CreateServiceDetails());
        RailwayPostgresOwnershipResolutionResult ownership = RailwayPostgresOwnershipResolutionResult.Create();

        RailwayPostgresCreateFlowResult result = await new RailwayPostgresCreateFlow(client)
            .ExecuteAsync(deployment, ownership, CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal("orders-postgres", client.CreatedRequest?.ServiceName);
        Assert.Equal("project-id", client.CreatedRequest?.ProjectId);
        Assert.Equal("environment-id", client.CreatedRequest?.EnvironmentId);
        Assert.Equal("svc_123", client.WaitedServiceId);
        Assert.Equal(RailwayPostgresTemplate.Standard, result.Template);
        Assert.Equal("orders-postgres", result.Database.ServiceName);
    }

    [Fact]
    public async Task CreateFlow_UsesDefaultReadinessTemplateWhenAdoptingExistingService()
    {
        RailwayPostgresResolvedDeployment deployment = CreateDeployment(RailwayPostgresOwnershipMode.CreateOrAdopt);
        deployment.Options.Template = RailwayPostgresTemplate.PgVector;
        FakeManagementClient client = new(CreateServiceDetails());
        RailwayPostgresOwnershipResolutionResult ownership = RailwayPostgresOwnershipResolutionResult.Adopt(CreateServiceDetails());

        RailwayPostgresCreateFlowResult result = await new RailwayPostgresCreateFlow(client)
            .ExecuteAsync(deployment, ownership, CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal(RailwayPostgresTemplate.Standard, client.WaitedTemplate);
        Assert.Null(result.Template);
    }

    [Fact]
    public async Task CreateFlow_UsesCachedTemplateWhenAdoptingManagedService()
    {
        RailwayPostgresResolvedDeployment deployment = CreateDeployment(RailwayPostgresOwnershipMode.CreateOrAdopt);
        deployment.Options.Template = RailwayPostgresTemplate.Standard;
        FakeManagementClient client = new(CreateServiceDetails());
        RailwayPostgresOwnershipResolutionResult ownership = RailwayPostgresOwnershipResolutionResult.Adopt(CreateServiceDetails());

        RailwayPostgresCreateFlowResult result = await new RailwayPostgresCreateFlow(client)
            .ExecuteAsync(deployment, ownership, RailwayPostgresTemplate.PostGis, CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal(RailwayPostgresTemplate.PostGis, client.WaitedTemplate);
        Assert.Equal(RailwayPostgresTemplate.PostGis, result.Template);
    }

    [Fact]
    public async Task DeploymentPipeline_ResolvesRailwayEnvironmentNameBeforeRemoteOperations()
    {
        RailwayPostgresResolvedDeployment deployment = new(
            "orders-postgres",
            "project-id",
            "production",
            RailwayPostgresOwnershipMode.CreateOnly,
            new RailwayPostgresManagementCredentials("management-secret"));
        FakeManagementClient client = new(CreateServiceDetails())
        {
            ResolvedEnvironmentId = "environment-id"
        };

        RailwayPostgresDatabaseDetails? database = await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            outputs: null,
            CancellationToken.None);

        Assert.NotNull(database);
        Assert.Equal("production", client.EnvironmentIdForResolution);
        Assert.Equal("environment-id", client.EnvironmentIdForFind);
        Assert.Equal("environment-id", client.CreatedRequest?.EnvironmentId);
        Assert.Equal("environment-id", client.WaitedEnvironmentId);
    }

    [Fact]
    public async Task DeploymentPipeline_UsesDefaultReadinessTemplateWhenConfiguringAdoptedService()
    {
        RailwayPostgresResolvedDeployment deployment = CreateDeployment(RailwayPostgresOwnershipMode.CreateOrAdopt);
        deployment.Options.Template = RailwayPostgresTemplate.PgVector;
        deployment.Options.MemoryGB = 2;
        RailwayPostgresDatabaseDetails existingService = CreateServiceDetails();
        FakeManagementClient client = new(existingService)
        {
            ServiceByName = existingService,
        };

        RailwayPostgresDatabaseDetails? database = await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            outputs: null,
            CancellationToken.None);

        Assert.NotNull(database);
        Assert.Null(client.CreatedRequest);
        Assert.Equal(RailwayPostgresTemplate.PgVector, client.ConfiguredOptions?.Template);
        Assert.Equal(RailwayPostgresTemplate.Standard, client.WaitedTemplate);
    }

    [Fact]
    public async Task DeploymentPipeline_UsesCachedTemplateWhenConfiguringManagedAdoptedService()
    {
        RailwayPostgresResolvedDeployment deployment = CreateDeployment(RailwayPostgresOwnershipMode.CreateOnly);
        deployment.Options.Template = RailwayPostgresTemplate.Standard;
        deployment.Options.MemoryGB = 2;
        RailwayPostgresDatabaseDetails existingService = CreateServiceDetails();
        FakeManagementClient client = new(existingService)
        {
            ServiceByName = existingService,
        };

        RailwayPostgresDatabaseDetails? database = await RailwayPostgresDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            new RailwayPostgresRemoteIdentityState(
                "project-id",
                "orders-postgres",
                "svc_123",
                RailwayPostgresTemplate.TimescaleDb),
            saveIdentityStateAsync: null,
            CancellationToken.None);

        Assert.NotNull(database);
        Assert.Null(client.CreatedRequest);
        Assert.Equal(RailwayPostgresTemplate.Standard, client.ConfiguredOptions?.Template);
        Assert.Equal(RailwayPostgresTemplate.TimescaleDb, client.WaitedTemplate);
    }

    [Fact]
    public async Task RemoteIdentityResolver_AdoptsConfiguredNameWhenCachedServiceWasDeleted()
    {
        RailwayPostgresDatabaseDetails replacement = CreateServiceDetails(serviceId: "svc_new");
        DeletedCachedIdentityManagementClient client = new(replacement);

        RailwayPostgresRemoteIdentityResolution resolution = await new RailwayPostgresRemoteIdentityResolver(client)
            .ResolveAsync(
                "project-id",
                "environment-id",
                "orders-postgres",
                new RailwayPostgresRemoteIdentityState("project-id", "orders-postgres", "svc_deleted"),
                CancellationToken.None);

        Assert.True(resolution.Found);
        Assert.False(resolution.ResolvedFromCachedIdentity);
        Assert.Equal("svc_new", resolution.Database?.ServiceId);
    }

    [Fact]
    public async Task RemoteIdentityStateStore_IgnoresCachedIdentityFromDifferentProject()
    {
        FakeDeploymentStateManager stateManager = new();
        RailwayPostgresRemoteIdentityDeploymentStateStore store = new(stateManager);

        await store.SaveAsync(
            "postgres",
            "project-a",
            new RailwayPostgresRemoteIdentityState(
                "project-a",
                "orders-postgres",
                "svc_project_a",
                RailwayPostgresTemplate.PostGis),
            CancellationToken.None);

        RailwayPostgresRemoteIdentityState? sameProject = await store.LoadAsync(
            "postgres",
            "project-a",
            CancellationToken.None);
        RailwayPostgresRemoteIdentityState? differentProject = await store.LoadAsync(
            "postgres",
            "project-b",
            CancellationToken.None);

        Assert.NotNull(sameProject);
        Assert.Equal("project-a", sameProject.ProjectId);
        Assert.Equal("orders-postgres", sameProject.ServiceName);
        Assert.Equal("svc_project_a", sameProject.ServiceId);
        Assert.Equal(RailwayPostgresTemplate.PostGis, sameProject.Template);
        Assert.Null(differentProject);
    }

    [Fact]
    public async Task ManagementClient_ConfiguresRailwayServiceInstanceAndLimitsAndSharedMemory()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "regions": [
                  { "id": "ams", "name": "europe-west4-drams3a", "region": "Amsterdam" },
                  { "id": "sfo", "name": "us-west2", "region": "California" }
                ]
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "serviceInstance": {
                  "latestDeployment": {
                    "meta": {
                      "serviceManifest": {
                        "deploy": {
                          "multiRegionConfig": {
                            "sfo": { "numReplicas": 1 }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "serviceInstanceUpdate": true } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "serviceInstanceLimitsUpdate": true } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "variables": { "RAILWAY_SHM_SIZE_BYTES": "134217728" } } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "variableUpsert": true } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "serviceInstanceRedeploy": true } }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        await client.ConfigureServiceAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresDeploymentOptions
            {
                Region = RailwayPostgresRegions.EuWestMetal,
                RestartPolicy = RailwayPostgresRestartPolicy.OnFailure,
                RestartPolicyMaxRetries = 7,
                MemoryGB = 2,
                VCpus = 1.5,
                SharedMemoryBytes = 524288000,
            },
            allowVolumeRegionMigration: false,
            CancellationToken.None);

        Assert.Equal(7, handler.Requests.Count);
        Assert.Contains("ListRailwayRegions", handler.Requests[0].Content, StringComparison.Ordinal);
        Assert.Contains("GetRailwayServiceInstanceDeployment", handler.Requests[1].Content, StringComparison.Ordinal);

        using JsonDocument serviceRequest = JsonDocument.Parse(handler.Requests[2].Content!);
        JsonElement serviceVariables = serviceRequest.RootElement.GetProperty("variables");
        Assert.Equal("environment-id", serviceVariables.GetProperty("environmentId").GetString());
        Assert.Equal("svc_123", serviceVariables.GetProperty("serviceId").GetString());
        JsonElement serviceInput = serviceVariables.GetProperty("input");
        Assert.Equal("ams", serviceInput.GetProperty("region").GetString());
        Assert.Equal(
            1,
            serviceInput
                .GetProperty("multiRegionConfig")
                .GetProperty("ams")
                .GetProperty("numReplicas")
                .GetInt32());
        Assert.Equal("ON_FAILURE", serviceInput.GetProperty("restartPolicyType").GetString());
        Assert.Equal(7, serviceInput.GetProperty("restartPolicyMaxRetries").GetInt32());

        using JsonDocument limitsRequest = JsonDocument.Parse(handler.Requests[3].Content!);
        JsonElement limitsInput = limitsRequest.RootElement.GetProperty("variables").GetProperty("input");
        Assert.Equal("environment-id", limitsInput.GetProperty("environmentId").GetString());
        Assert.Equal("svc_123", limitsInput.GetProperty("serviceId").GetString());
        Assert.Equal(2, limitsInput.GetProperty("memoryGB").GetDouble());
        Assert.Equal(1.5, limitsInput.GetProperty("vCPUs").GetDouble());

        Assert.Contains("GetRailwayPostgresVariables", handler.Requests[4].Content, StringComparison.Ordinal);

        using JsonDocument variableRequest = JsonDocument.Parse(handler.Requests[5].Content!);
        JsonElement variableInput = variableRequest.RootElement.GetProperty("variables").GetProperty("input");
        Assert.Equal("project-id", variableInput.GetProperty("projectId").GetString());
        Assert.Equal("environment-id", variableInput.GetProperty("environmentId").GetString());
        Assert.Equal("svc_123", variableInput.GetProperty("serviceId").GetString());
        Assert.Equal("RAILWAY_SHM_SIZE_BYTES", variableInput.GetProperty("name").GetString());
        Assert.Equal("524288000", variableInput.GetProperty("value").GetString());
        Assert.False(variableInput.GetProperty("skipDeploys").GetBoolean());

        Assert.Contains("RedeployRailwayPostgresServiceInstance", handler.Requests[6].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementClient_DoesNotUpsertUnchangedSharedMemory()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "variables": { "RAILWAY_SHM_SIZE_BYTES": "524288000" } } }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        bool deploymentQueued = await client.ConfigureServiceAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresDeploymentOptions
            {
                SharedMemoryBytes = 524288000,
            },
            allowVolumeRegionMigration: false,
            CancellationToken.None);

        Assert.False(deploymentQueued);
        Assert.Single(handler.Requests);
        Assert.Contains("GetRailwayPostgresVariables", handler.Requests[0].Content, StringComparison.Ordinal);
        Assert.DoesNotContain("variableUpsert", handler.Requests[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementClient_RejectsExistingVolumeBackedRegionChange()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "regions": [
                  { "id": "ams", "name": "europe-west4-drams3a", "region": "Amsterdam" },
                  { "id": "sin", "name": "asia-southeast1-eqsg3a", "region": "asia-southeast1-eqsg3a" }
                ]
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "serviceInstance": {
                  "latestDeployment": {
                    "meta": {
                      "volumeMounts": ["/var/lib/postgresql/data"],
                      "serviceManifest": {
                        "deploy": {
                          "multiRegionConfig": {
                            "ams": { "numReplicas": 1 }
                          },
                          "requiredMountPath": "/var/lib/postgresql/data"
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresProviderException exception = await Assert.ThrowsAsync<RailwayPostgresProviderException>(() =>
            client.ConfigureServiceAsync(
                "project-id",
                "environment-id",
                "svc_123",
                new RailwayPostgresDeploymentOptions
                {
                    Region = RailwayPostgresRegions.SoutheastAsiaMetal,
                },
                allowVolumeRegionMigration: false,
                CancellationToken.None));

        Assert.Equal(RailwayPostgresProviderFailureKind.Validation, exception.FailureKind);
        Assert.Contains("volume migration", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ManagementClient_AppliesRegionToTemplateDeployOnCreate()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "template": {
                  "serializedConfig": {
                    "services": {
                      "template-service": {
                        "name": "Postgres",
                        "deploy": {
                          "requiredMountPath": "/var/lib/postgresql/data"
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "regions": [
                  { "id": "sin", "name": "asia-southeast1-eqsg3a", "region": "asia-southeast1-eqsg3a" }
                ]
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "templateDeployV2": { "projectId": "project-id", "workflowId": "workflow-id" } } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ]
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "errors": [
                { "message": "ServiceInstance not found" }
              ]
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ]
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "DEPLOYING" } },
                "variables": {}
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        await client.CreateServiceAsync(
            new RailwayPostgresCreateServiceRequest(
                "orders-postgres",
                "project-id",
                "environment-id",
                new RailwayPostgresDeploymentOptions
                {
                    Region = RailwayPostgresRegions.SoutheastAsiaMetal,
                    RestartPolicy = RailwayPostgresRestartPolicy.OnFailure,
                    RestartPolicyMaxRetries = 7,
                }),
            CancellationToken.None);

        using JsonDocument deployRequest = JsonDocument.Parse(handler.Requests[2].Content!);
        JsonElement deploy = deployRequest.RootElement
            .GetProperty("variables")
            .GetProperty("input")
            .GetProperty("serializedConfig")
            .GetProperty("services")
            .GetProperty("template-service")
            .GetProperty("deploy");

        Assert.Equal("sin", deploy.GetProperty("region").GetString());
        Assert.Equal(1, deploy.GetProperty("multiRegionConfig").GetProperty("sin").GetProperty("numReplicas").GetInt32());
        Assert.Equal("ON_FAILURE", deploy.GetProperty("restartPolicyType").GetString());
        Assert.Equal(7, deploy.GetProperty("restartPolicyMaxRetries").GetInt32());
    }

    [Fact]
    public async Task ManagementClient_ResolvesRailwayEnvironmentName()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "environments": {
                  "edges": [
                    { "node": { "id": "environment-id", "name": "production" } },
                    { "node": { "id": "staging-id", "name": "staging" } }
                  ]
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        string environmentId = await client.ResolveEnvironmentIdAsync(
            "project-id",
            "production",
            CancellationToken.None);

        Assert.Equal("environment-id", environmentId);
        CapturedHttpRequest request = Assert.Single(handler.Requests);
        Assert.Contains("ListRailwayEnvironments", request.Content, StringComparison.Ordinal);
        Assert.Contains("environments(projectId: $projectId)", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementClient_DoesNotListRailwayEnvironmentsWhenEnvironmentValueIsAlreadyGuid()
    {
        FakeHttpMessageHandler handler = new();
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        string environmentId = await client.ResolveEnvironmentIdAsync(
            "project-id",
            "04dc0f90-a13d-4d6a-a8a5-a41240463ddd",
            CancellationToken.None);

        Assert.Equal("04dc0f90-a13d-4d6a-a8a5-a41240463ddd", environmentId);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ManagementClient_PagesServicesWhenResolvingByName()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_other", "name": "other-postgres", "projectId": "project-id", "deletedAt": null } }
                    ],
                    "pageInfo": {
                      "hasNextPage": true,
                      "endCursor": "cursor-1"
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ],
                    "pageInfo": {
                      "hasNextPage": false,
                      "endCursor": null
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails? service = await client.FindServiceByNameAsync(
            "project-id",
            "environment-id",
            "orders-postgres",
            CancellationToken.None);

        Assert.NotNull(service);
        Assert.Equal("svc_123", service.ServiceId);
        Assert.Equal(3, handler.Requests.Count);

        using JsonDocument secondPageRequest = JsonDocument.Parse(handler.Requests[1].Content!);
        Assert.Equal("cursor-1", secondPageRequest.RootElement.GetProperty("variables").GetProperty("after").GetString());
    }

    [Fact]
    public async Task ManagementClient_ParsesPostgresTemplateVariableAliases()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgis.railway.internal",
                  "PGPORT": "5432",
                  "POSTGRES_USER": "postgres",
                  "POSTGRES_PASSWORD": "postgres-password",
                  "POSTGRES_DB": "railway",
                  "DATABASE_URL": "postgres://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway",
                  "DATABASE_PRIVATE_URL": "postgres://postgres:postgres-password@postgis.railway.internal:5432/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.GetServiceAsync(
            "project-id",
            "environment-id",
            "svc_123",
            CancellationToken.None);

        Assert.True(service.HasConnectionVariables);
        Assert.Equal("shortline.proxy.rlwy.net", service.Host);
        Assert.Equal(27543, service.Port);
        Assert.Equal("postgres", service.UserName);
        Assert.Equal("postgres-password", service.Password);
        Assert.Equal("railway", service.DatabaseName);
        Assert.Equal("shortline.proxy.rlwy.net", new NpgsqlConnectionStringBuilder(service.ConnectionString).Host);
        Assert.Equal("shortline.proxy.rlwy.net", new NpgsqlConnectionStringBuilder(service.ProvisioningConnectionString).Host);
    }

    [Fact]
    public async Task ManagementClient_BuildsPublicUrlFromTcpProxyVariables()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "POSTGRES_USER": "post;gres",
                  "POSTGRES_PASSWORD": "postgres/password",
                  "POSTGRES_DB": "railway",
                  "DATABASE_URL": "postgres://postgres:postgres-password@postgis.railway.internal:5432/railway",
                  "RAILWAY_TCP_PROXY_DOMAIN": "shortline.proxy.rlwy.net",
                  "RAILWAY_TCP_PROXY_PORT": "27543"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.GetServiceAsync(
            "project-id",
            "environment-id",
            "svc_123",
            CancellationToken.None);

        Assert.True(service.HasConnectionVariables);
        Assert.Equal("shortline.proxy.rlwy.net", service.Host);
        Assert.Equal(27543, service.Port);
        Assert.Equal("post;gres", service.UserName);
        Assert.Equal("postgres/password", service.Password);
        Assert.Equal("railway", service.DatabaseName);
        Assert.Equal(SslMode.Require, new NpgsqlConnectionStringBuilder(service.ConnectionString).SslMode);
    }

    [Fact]
    public async Task ManagementClient_WaitsWhenCreatedServiceExistsBeforeConnectionVariables()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "DEPLOYING", "deploymentStopped": true } },
                "variables": {}
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.WaitUntilReadyAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresReadinessPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                Delay = TimeSpan.FromMilliseconds(1),
            },
            CancellationToken.None);

        Assert.True(service.HasConnectionVariables);
        Assert.Equal("shortline.proxy.rlwy.net", service.Host);
        Assert.Equal(27543, service.Port);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ManagementClient_WaitsWhenConnectionVariablesExistBeforeDeploymentSucceeds()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "DEPLOYING" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.WaitUntilReadyAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresReadinessPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                Delay = TimeSpan.FromMilliseconds(1),
            },
            CancellationToken.None);

        Assert.Equal("SUCCESS", service.LatestDeploymentStatus);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ManagementClient_IgnoresPreviousSuccessfulDeploymentWhenPollingAfterConfiguration()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "id": "dep_before", "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "id": "dep_after", "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.WaitUntilReadyAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresReadinessPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                Delay = TimeSpan.FromMilliseconds(1),
                PreviousDeploymentId = "dep_before",
            },
            CancellationToken.None);

        Assert.Equal("dep_after", service.LatestDeploymentId);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ManagementClient_IgnoresPreviousFailedDeploymentWhenPollingAfterConfiguration()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "id": "dep_before", "status": "FAILED" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "id": "dep_after", "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.WaitUntilReadyAsync(
            "project-id",
            "environment-id",
            "svc_123",
            new RailwayPostgresReadinessPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                Delay = TimeSpan.FromMilliseconds(1),
                PreviousDeploymentId = "dep_before",
            },
            CancellationToken.None);

        Assert.Equal("dep_after", service.LatestDeploymentId);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ManagementClient_ReadinessTimeoutReportsDeploymentStatusWhenVariablesExist()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "QUEUED", "meta": { "queuedReason": "Waiting for previous deployment" } } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresProviderException exception = await Assert.ThrowsAsync<RailwayPostgresProviderException>(() =>
            client.WaitUntilReadyAsync(
                "project-id",
                "environment-id",
                "svc_123",
                new RailwayPostgresReadinessPollingOptions
                {
                    Timeout = TimeSpan.Zero,
                    Delay = TimeSpan.FromMilliseconds(1),
                },
                CancellationToken.None));

        Assert.Contains("latest deployment did not become SUCCESS", exception.Message, StringComparison.Ordinal);
        Assert.Contains("QUEUED", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Waiting for previous deployment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseProvisioner_TreatsEarlyRailwayProxyDisconnectAsTransient()
    {
        IOException streamEnded = new("Attempted to read past the end of the stream.");
        NpgsqlException exception = new("Exception while reading from stream", streamEnded);

        Assert.True(RailwayPostgresDatabaseProvisioner.IsTransientProvisioningException(exception));
    }

    [Fact]
    public void DatabaseProvisioner_UsesAspireCreationScriptWhenConfigured()
    {
        const string creationScript = "CREATE DATABASE custom_orders";
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<PostgresServerResource> postgres = app.AddPostgres("postgres");
        IResourceBuilder<PostgresDatabaseResource> orders = postgres
            .AddDatabase("orders")
            .WithCreationScript(creationScript);

        RailwayPostgresDatabaseProvisioningRequest request = Assert.Single(
            RailwayPostgresDeploymentPipeline.CreateDatabaseProvisioningRequests([orders.Resource]));

        Assert.Equal("orders", request.DatabaseName);
        Assert.Equal(creationScript, request.CreationScript);
        Assert.Equal(creationScript, RailwayPostgresDatabaseProvisioner.CreateCreateDatabaseCommandText(request));
        Assert.Equal(
            "CREATE DATABASE \"orders\"",
            RailwayPostgresDatabaseProvisioner.CreateCreateDatabaseCommandText(
                new RailwayPostgresDatabaseProvisioningRequest("orders", creationScript: null)));
    }

    [Fact]
    public void DatabaseProvisioner_InitializesTemplateChildDatabases()
    {
        Assert.Equal(
            "CREATE EXTENSION IF NOT EXISTS postgis",
            RailwayPostgresDatabaseProvisioner.CreateInitializeDatabaseCommandText(RailwayPostgresTemplate.PostGis));
        Assert.Equal(
            "CREATE EXTENSION IF NOT EXISTS vector",
            RailwayPostgresDatabaseProvisioner.CreateInitializeDatabaseCommandText(RailwayPostgresTemplate.PgVector));
        Assert.Equal(
            "CREATE EXTENSION IF NOT EXISTS timescaledb",
            RailwayPostgresDatabaseProvisioner.CreateInitializeDatabaseCommandText(RailwayPostgresTemplate.TimescaleDb));
        Assert.Null(RailwayPostgresDatabaseProvisioner.CreateInitializeDatabaseCommandText(RailwayPostgresTemplate.Standard));
        Assert.Null(RailwayPostgresDatabaseProvisioner.CreateInitializeDatabaseCommandText(RailwayPostgresTemplate.PointInTimeRecovery));
    }

    [Fact]
    public async Task ManagementClient_CreatesRailwayPostgresFromOfficialTemplate()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "template": {
                  "serializedConfig": {
                    "services": {
                      "template-service": {
                        "name": "Postgres",
                        "source": { "image": "ghcr.io/railwayapp-templates/postgres-ssl:18" },
                        "variables": {
                          "PGHOST": { "defaultValue": "${{RAILWAY_PRIVATE_DOMAIN}}" },
                          "PGPORT": { "defaultValue": "5432" },
                          "PGUSER": { "defaultValue": "${{POSTGRES_USER}}" },
                          "PGPASSWORD": { "defaultValue": "${{POSTGRES_PASSWORD}}" },
                          "PGDATABASE": { "defaultValue": "${{POSTGRES_DB}}" },
                          "DATABASE_URL": { "defaultValue": "postgresql://${{PGUSER}}:${{POSTGRES_PASSWORD}}@${{RAILWAY_PRIVATE_DOMAIN}}:5432/${{PGDATABASE}}" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "templateDeployV2": { "projectId": "project-id", "workflowId": "workflow-id" } } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ]
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_URL": "postgresql://postgres:postgres-password@postgres.railway.internal:5432/railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);

        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.CreateServiceAsync(
            new RailwayPostgresCreateServiceRequest("orders-postgres", "project-id", "environment-id"),
            CancellationToken.None);

        Assert.Equal("svc_123", service.ServiceId);
        Assert.Equal("orders-postgres", service.ServiceName);
        NpgsqlConnectionStringBuilder appConnectionString = new(service.ConnectionString);
        NpgsqlConnectionStringBuilder provisioningConnectionString = new(service.ProvisioningConnectionString);
        Assert.Equal("shortline.proxy.rlwy.net", appConnectionString.Host);
        Assert.Equal(27543, appConnectionString.Port);
        Assert.Equal("railway", appConnectionString.Database);
        Assert.Equal("shortline.proxy.rlwy.net", provisioningConnectionString.Host);
        Assert.Equal(27543, provisioningConnectionString.Port);
        Assert.Equal("railway", provisioningConnectionString.Database);
        Assert.Equal("Bearer", handler.Requests[0].AuthorizationScheme);
        Assert.Equal("management-secret", handler.Requests[0].AuthorizationParameter);
        Assert.Contains("GetRailwayPostgresTemplate", handler.Requests[0].Content, StringComparison.Ordinal);
        Assert.Contains("templateDeployV2", handler.Requests[1].Content, StringComparison.Ordinal);

        using JsonDocument deployRequest = JsonDocument.Parse(handler.Requests[1].Content!);
        JsonElement input = deployRequest.RootElement
            .GetProperty("variables")
            .GetProperty("input");
        Assert.Equal("project-id", input.GetProperty("projectId").GetString());
        Assert.Equal("environment-id", input.GetProperty("environmentId").GetString());
        Assert.Equal("b55da7dc-09be-4140-bc65-1284d15d349c", input.GetProperty("templateId").GetString());

        JsonElement services = input.GetProperty("serializedConfig").GetProperty("services");
        JsonElement serviceConfig = Assert.Single(services.EnumerateObject()).Value;
        Assert.Equal("orders-postgres", serviceConfig.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ManagementClient_CreatesRailwayPostgresFromPointInTimeRecoveryTemplateWhenConfigured()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "template": {
                  "serializedConfig": {
                    "buckets": {
                      "postgres-pitr-wal": {
                        "name": "Postgres-PITR"
                      }
                    },
                    "services": {
                      "template-service": {
                        "name": "Postgres",
                        "source": { "image": "ghcr.io/railwayapp-templates/postgres-ssl:18" },
                        "variables": {
                          "PGHOST": { "defaultValue": "${{RAILWAY_PRIVATE_DOMAIN}}" },
                          "PGPORT": { "defaultValue": "5432" },
                          "PGUSER": { "defaultValue": "${{POSTGRES_USER}}" },
                          "PGPASSWORD": { "defaultValue": "${{POSTGRES_PASSWORD}}" },
                          "PGDATABASE": { "defaultValue": "${{POSTGRES_DB}}" },
                          "WAL_ARCHIVE_BUCKET": { "defaultValue": "${{Postgres-PITR.BUCKET}}" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "templateDeployV2": { "projectId": "project-id", "workflowId": "workflow-id" } } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ]
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);

        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.CreateServiceAsync(
            new RailwayPostgresCreateServiceRequest(
                "orders-postgres",
                "project-id",
                "environment-id",
                new RailwayPostgresDeploymentOptions
                {
                    Template = RailwayPostgresTemplate.PointInTimeRecovery,
                }),
            CancellationToken.None);

        Assert.Equal("svc_123", service.ServiceId);

        using JsonDocument templateRequest = JsonDocument.Parse(handler.Requests[0].Content!);
        Assert.Equal(
            "ecd2f76a-b636-4b98-9336-608841bb2dd5",
            templateRequest.RootElement.GetProperty("variables").GetProperty("id").GetString());

        using JsonDocument deployRequest = JsonDocument.Parse(handler.Requests[1].Content!);
        JsonElement input = deployRequest.RootElement
            .GetProperty("variables")
            .GetProperty("input");
        Assert.Equal("ecd2f76a-b636-4b98-9336-608841bb2dd5", input.GetProperty("templateId").GetString());
        Assert.Equal("Postgres-PITR", input
            .GetProperty("serializedConfig")
            .GetProperty("buckets")
            .GetProperty("postgres-pitr-wal")
            .GetProperty("name")
            .GetString());

        JsonElement services = input.GetProperty("serializedConfig").GetProperty("services");
        JsonElement serviceConfig = Assert.Single(services.EnumerateObject()).Value;
        Assert.Equal("orders-postgres", serviceConfig.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData(RailwayPostgresTemplate.PostGis)]
    [InlineData(RailwayPostgresTemplate.PgVector)]
    [InlineData(RailwayPostgresTemplate.TimescaleDb)]
    public async Task ManagementClient_UsesNonSslModeForUpstreamTemplatesWhenWaitingUntilReady(RailwayPostgresTemplate template)
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "id": "deployment_1", "status": "SUCCESS" } },
                "variables": {
                  "PGHOST": "postgres.railway.internal",
                  "PGPORT": "5432",
                  "PGUSER": "postgres",
                  "PGPASSWORD": "postgres-password",
                  "PGDATABASE": "railway",
                  "DATABASE_PUBLIC_URL": "postgresql://postgres:postgres-password@shortline.proxy.rlwy.net:27543/railway"
                }
              }
            }
            """);
        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        RailwayPostgresDatabaseDetails service = await client.WaitUntilReadyAsync(
            "project-id",
            "environment-id",
            "svc_123",
            template,
            RailwayPostgresReadinessPollingOptions.Default,
            CancellationToken.None);

        Assert.Equal(SslMode.Disable, new NpgsqlConnectionStringBuilder(service.ConnectionString).SslMode);
        Assert.Equal(SslMode.Disable, new NpgsqlConnectionStringBuilder(service.ProvisioningConnectionString).SslMode);
    }

    [Theory]
    [InlineData(RailwayPostgresTemplate.PostGis, "7101c553-9fac-4cd0-b332-1efab34eee5f")]
    [InlineData(RailwayPostgresTemplate.PgVector, "da106a2a-b086-486e-869f-1c0bfbf6dfc2")]
    [InlineData(RailwayPostgresTemplate.TimescaleDb, "9193cebc-f2e3-47d2-b17e-d97949ef9299")]
    public async Task ManagementClient_UsesConfiguredRailwayPostgresTemplate(
        RailwayPostgresTemplate template,
        string expectedTemplateId)
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "template": {
                  "serializedConfig": {
                    "services": {
                      "template-service": {
                        "name": "Postgres",
                        "source": { "image": "template-image" }
                      }
                    }
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            { "data": { "templateDeployV2": { "projectId": "project-id", "workflowId": "workflow-id" } } }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "project": {
                  "services": {
                    "edges": [
                      { "node": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null } }
                    ]
                  }
                }
              }
            }
            """);
        handler.Enqueue(System.Net.HttpStatusCode.OK, """
            {
              "data": {
                "service": { "id": "svc_123", "name": "orders-postgres", "projectId": "project-id", "deletedAt": null },
                "serviceInstance": { "latestDeployment": { "status": "DEPLOYING" } },
                "variables": {}
              }
            }
            """);

        RailwayPostgresManagementClient client = new(
            new HttpClient(handler),
            new RailwayPostgresManagementCredentials("management-secret"));

        await client.CreateServiceAsync(
            new RailwayPostgresCreateServiceRequest(
                "orders-postgres",
                "project-id",
                "environment-id",
                new RailwayPostgresDeploymentOptions
                {
                    Template = template,
                }),
            CancellationToken.None);

        using JsonDocument templateRequest = JsonDocument.Parse(handler.Requests[0].Content!);
        Assert.Equal(
            expectedTemplateId,
            templateRequest.RootElement.GetProperty("variables").GetProperty("id").GetString());

        using JsonDocument deployRequest = JsonDocument.Parse(handler.Requests[1].Content!);
        JsonElement input = deployRequest.RootElement
            .GetProperty("variables")
            .GetProperty("input");

        Assert.Equal(expectedTemplateId, input.GetProperty("templateId").GetString());
        JsonElement services = input.GetProperty("serializedConfig").GetProperty("services");
        JsonElement serviceConfig = Assert.Single(services.EnumerateObject()).Value;
        Assert.Equal("orders-postgres", serviceConfig.GetProperty("name").GetString());
    }

    [Fact]
    public void TypeScriptBridge_ExportsStableRailwayPostgresContract()
    {
        MethodInfo publish = typeof(RailwayPostgresBuilderExtensions)
            .GetMethod(nameof(RailwayPostgresBuilderExtensions.PublishToRailwayForTypeScript))
            ?? throw new InvalidOperationException("TypeScript publish bridge was not found.");
        AspireExportAttribute publishExport = Assert.Single(publish.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.publishToRailway", publishExport.Id);
        Assert.Equal("publishToRailway", publishExport.MethodName);

        MethodInfo outputs = typeof(RailwayPostgresResourceExtensions)
            .GetMethod(nameof(RailwayPostgresResourceExtensions.GetRailwayPostgresOutputsForTypeScript))
            ?? throw new InvalidOperationException("TypeScript outputs bridge was not found.");
        AspireExportAttribute outputsExport = Assert.Single(outputs.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.getRailwayPostgresOutputs", outputsExport.Id);
        Assert.Equal("getRailwayPostgresOutputs", outputsExport.MethodName);

        Assert.NotNull(typeof(RailwayPostgresDeploymentOptionsDto).GetCustomAttribute<AspireDtoAttribute>());
        Assert.Equal(RailwayPostgresOwnershipMode.CreateOrAdopt, new RailwayPostgresDeploymentOptionsDto().GetOwnershipMode());

        RailwayPostgresDeploymentOptions dtoOptions = new RailwayPostgresDeploymentOptionsDto
        {
            Region = RailwayPostgresRegions.EuWestMetal,
            RestartPolicy = RailwayPostgresRestartPolicy.Always,
            RestartPolicyMaxRetries = 4,
            MemoryGB = 3,
            VCpus = 2,
            SharedMemoryBytes = 134217728,
            Template = RailwayPostgresTemplate.PostGis,
        }.ToDeploymentOptions();
#pragma warning disable CS0618
        RailwayPostgresDeploymentOptions legacyDtoOptions = new RailwayPostgresDeploymentOptionsDto
        {
            PointInTimeRecovery = true,
        }.ToDeploymentOptions();
#pragma warning restore CS0618
        Assert.Equal(RailwayPostgresRegions.EuWestMetal, dtoOptions.Region);
        Assert.Equal(RailwayPostgresRestartPolicy.Always, dtoOptions.RestartPolicy);
        Assert.Equal(4, dtoOptions.RestartPolicyMaxRetries);
        Assert.Equal(3, dtoOptions.MemoryGB);
        Assert.Equal(2, dtoOptions.VCpus);
        Assert.Equal(134217728, dtoOptions.SharedMemoryBytes);
        Assert.Equal(RailwayPostgresTemplate.PostGis, dtoOptions.Template);
        Assert.Equal(RailwayPostgresTemplate.PointInTimeRecovery, legacyDtoOptions.Template);
    }

    private static RailwayPostgresResolvedDeployment CreateDeployment(RailwayPostgresOwnershipMode ownershipMode)
    {
        return new RailwayPostgresResolvedDeployment(
            "orders-postgres",
            "project-id",
            "environment-id",
            ownershipMode,
            new RailwayPostgresManagementCredentials("management-secret"));
    }

    private static RailwayPostgresDatabaseDetails CreateServiceDetails(
        string databaseName = "railway",
        string serviceId = "svc_123",
        string? latestDeploymentId = null)
    {
        return new RailwayPostgresDatabaseDetails
        {
            ServiceId = serviceId,
            ServiceName = "orders-postgres",
            ProjectId = "project-id",
            EnvironmentId = "environment-id",
            Host = "shortline.proxy.rlwy.net",
            Port = 27543,
            UserName = "postgres",
            Password = "postgres-password",
            DatabaseName = databaseName,
            ConnectionString = RailwayPostgresConnectionString.Create(
                "shortline.proxy.rlwy.net",
                27543,
                "postgres",
                "postgres-password",
                databaseName),
            ProvisioningConnectionString = RailwayPostgresConnectionString.Create(
                "shortline.proxy.rlwy.net",
                27543,
                "postgres",
                "postgres-password",
                databaseName),
            LatestDeploymentId = latestDeploymentId,
            LatestDeploymentStatus = "SUCCESS",
        };
    }

    private sealed class FakeManagementClient : IRailwayPostgresManagementClient
    {
        private readonly RailwayPostgresDatabaseDetails _service;

        public FakeManagementClient(RailwayPostgresDatabaseDetails service)
        {
            _service = service;
        }

        public RailwayPostgresCreateServiceRequest? CreatedRequest { get; private set; }

        public string? WaitedServiceId { get; private set; }

        public RailwayPostgresTemplate? WaitedTemplate { get; private set; }

        public RailwayPostgresReadinessPollingOptions? WaitedPollingOptions { get; private set; }

        public string? ResolvedEnvironmentId { get; init; }

        public string? EnvironmentIdForResolution { get; private set; }

        public string? EnvironmentIdForFind { get; private set; }

        public RailwayPostgresDatabaseDetails? ServiceByName { get; init; }

        public string? WaitedEnvironmentId { get; private set; }

        public string? ConfiguredProjectId { get; private set; }

        public string? ConfiguredEnvironmentId { get; private set; }

        public string? ConfiguredServiceId { get; private set; }

        public RailwayPostgresDeploymentOptions? ConfiguredOptions { get; private set; }

        public bool ConfigurationQueuedDeployment { get; init; }

        public Exception? ConfigureException { get; init; }

        public Task<string> ResolveEnvironmentIdAsync(
            string projectId,
            string environmentIdOrName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            EnvironmentIdForResolution = environmentIdOrName;

            return Task.FromResult(ResolvedEnvironmentId ?? environmentIdOrName);
        }

        public Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
            string projectId,
            string environmentId,
            string serviceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            EnvironmentIdForFind = environmentId;
            _ = serviceName;

            return Task.FromResult(ServiceByName is not null && string.Equals(ServiceByName.ServiceName, serviceName, StringComparison.Ordinal)
                ? ServiceByName
                : null);
        }

        public Task<RailwayPostgresDatabaseDetails> GetServiceAsync(
            string projectId,
            string environmentId,
            string serviceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            _ = environmentId;
            _ = serviceId;

            return Task.FromResult(_service);
        }

        public Task<RailwayPostgresDatabaseDetails> CreateServiceAsync(
            RailwayPostgresCreateServiceRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreatedRequest = request;

            return Task.FromResult(_service);
        }

        public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
            string projectId,
            string environmentId,
            string serviceId,
            RailwayPostgresTemplate template,
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            WaitedTemplate = template;
            WaitedEnvironmentId = environmentId;
            WaitedPollingOptions = pollingOptions;
            WaitedServiceId = serviceId;

            return Task.FromResult(_service);
        }

        public Task<bool> ConfigureServiceAsync(
            string projectId,
            string environmentId,
            string serviceId,
            RailwayPostgresDeploymentOptions options,
            bool allowVolumeRegionMigration,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = allowVolumeRegionMigration;

            if (ConfigureException is not null)
            {
                return Task.FromException<bool>(ConfigureException);
            }

            ConfiguredProjectId = projectId;
            ConfiguredEnvironmentId = environmentId;
            ConfiguredServiceId = serviceId;
            ConfiguredOptions = new RailwayPostgresDeploymentOptions(options);

            return Task.FromResult(ConfigurationQueuedDeployment);
        }
    }

    private sealed class DeletedCachedIdentityManagementClient : IRailwayPostgresManagementClient
    {
        private readonly RailwayPostgresDatabaseDetails _replacement;

        public DeletedCachedIdentityManagementClient(RailwayPostgresDatabaseDetails replacement)
        {
            _replacement = replacement;
        }

        public Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
            string projectId,
            string environmentId,
            string serviceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            _ = environmentId;

            return Task.FromResult<RailwayPostgresDatabaseDetails?>(
                string.Equals(serviceName, _replacement.ServiceName, StringComparison.Ordinal)
                    ? _replacement
                    : null);
        }

        public Task<RailwayPostgresDatabaseDetails> GetServiceAsync(
            string projectId,
            string environmentId,
            string serviceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            _ = environmentId;
            _ = serviceId;

            return Task.FromException<RailwayPostgresDatabaseDetails>(
                new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.NotFound,
                    statusCode: null,
                    "Railway service was not found."));
        }
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
