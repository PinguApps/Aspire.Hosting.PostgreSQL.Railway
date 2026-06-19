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
        Assert.Equal("postgres.railway.internal", await outputs.Host.GetValueAsync(CancellationToken.None));
        Assert.True(RailwayPostgresOutputs.IsSecret(outputs.Password.Name));
        Assert.True(RailwayPostgresOutputs.IsSecret(outputs.ConnectionString.Name));
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
        Assert.Equal("orders-postgres", result.Database.ServiceName);
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
                  "DATABASE_URL": "Host=postgres.railway.internal;Port=5432;Username=postgres;Password=postgres-password;Database=railway;SSL Mode=Require"
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
        Assert.Equal("railway", new NpgsqlConnectionStringBuilder(service.ConnectionString).Database);
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

    private static RailwayPostgresDatabaseDetails CreateServiceDetails(string databaseName = "railway")
    {
        return new RailwayPostgresDatabaseDetails
        {
            ServiceId = "svc_123",
            ServiceName = "orders-postgres",
            ProjectId = "project-id",
            EnvironmentId = "environment-id",
            Host = "postgres.railway.internal",
            Port = 5432,
            UserName = "postgres",
            Password = "postgres-password",
            DatabaseName = databaseName,
            ConnectionString = RailwayPostgresConnectionString.Create(
                "postgres.railway.internal",
                5432,
                "postgres",
                "postgres-password",
                databaseName),
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

        public Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
            string projectId,
            string environmentId,
            string serviceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            _ = environmentId;
            _ = serviceName;

            return Task.FromResult<RailwayPostgresDatabaseDetails?>(null);
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
            RailwayPostgresReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = projectId;
            _ = environmentId;
            _ = pollingOptions;
            WaitedServiceId = serviceId;

            return Task.FromResult(_service);
        }
    }
}
