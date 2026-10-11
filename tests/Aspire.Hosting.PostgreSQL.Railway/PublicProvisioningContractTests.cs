using System.Net;
using System.Text.Json;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Npgsql;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests;

public sealed class PublicProvisioningContractTests
{
    [Fact]
    public async Task ActiveProxyIsReusedWithoutCreatingOrRedeploying()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies());
        handler.Enqueue(HttpStatusCode.OK, Proxies());
        RailwayPostgresManagementClient client = Client(handler);

        RailwayPostgresDatabaseDetails result = await client.EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.PointInTimeRecovery, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Contains("tcpProxies", request.Content!, StringComparison.Ordinal);
            Assert.DoesNotContain("postgres-password", request.Content!, StringComparison.Ordinal);
            Assert.Null(request.AuthorizationParameter);
            Assert.Equal("project-secret", request.ProjectAccessToken);
        });
        NpgsqlConnectionStringBuilder provisioning = new(result.ProvisioningConnectionString);
        Assert.Equal("public.proxy.rlwy.net", provisioning.Host);
        Assert.Equal(15432, provisioning.Port);
        Assert.Equal(SslMode.Require, provisioning.SslMode);
        Assert.Equal("postgres-password", provisioning.Password);
        Assert.Equal("postgres.railway.internal", result.Host);
        Assert.Equal(Service().ConnectionString, result.ConnectionString);
        Assert.Equal("child", new NpgsqlConnectionStringBuilder(result.WithDatabaseName("child").ProvisioningConnectionString).Database);
    }

    [Fact]
    public async Task NewProxyRequiresANewSuccessfulDeploymentAndAuthoritativeActiveReadback()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, """{"data":{"tcpProxies":[]}}""");
        handler.Enqueue(HttpStatusCode.OK, """{"data":{"tcpProxyCreate":{"id":"proxy-1"}}}""");
        handler.Enqueue(HttpStatusCode.OK, """{"data":{"serviceInstanceRedeploy":true}}""");
        handler.Enqueue(HttpStatusCode.OK, ServiceResponse("prior"));
        handler.Enqueue(HttpStatusCode.OK, ServiceResponse("new-deployment"));
        handler.Enqueue(HttpStatusCode.OK, Proxies());

        RailwayPostgresDatabaseDetails result = await Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.Standard, CancellationToken.None);

        Assert.Equal("new-deployment", result.LatestDeploymentId);
        Assert.Equal(6, handler.Requests.Count);
        Assert.Contains("tcpProxyCreate", handler.Requests[1].Content!, StringComparison.Ordinal);
        using JsonDocument creation = JsonDocument.Parse(handler.Requests[1].Content!);
        JsonElement input = creation.RootElement.GetProperty("variables").GetProperty("input");
        Assert.Equal("svc-1", input.GetProperty("serviceId").GetString());
        Assert.Equal("environment-id", input.GetProperty("environmentId").GetString());
        Assert.Equal(5432, input.GetProperty("applicationPort").GetInt32());
        Assert.Contains("serviceInstanceRedeploy", handler.Requests[2].Content!, StringComparison.Ordinal);
        Assert.Contains("GetRailwayPostgresService", handler.Requests[3].Content!, StringComparison.Ordinal);
        Assert.Contains("GetRailwayPostgresService", handler.Requests[4].Content!, StringComparison.Ordinal);
        Assert.Contains("tcpProxies", handler.Requests[5].Content!, StringComparison.Ordinal);
        Assert.Equal("public.proxy.rlwy.net", new NpgsqlConnectionStringBuilder(result.ProvisioningConnectionString).Host);
    }

    [Fact]
    public async Task InterruptedProxyActivationReusesTheExistingProxy()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies(status: "CREATING"));
        handler.Enqueue(HttpStatusCode.OK, """{"data":{"serviceInstanceRedeploy":true}}""");
        handler.Enqueue(HttpStatusCode.OK, ServiceResponse("new-deployment"));
        handler.Enqueue(HttpStatusCode.OK, Proxies());

        RailwayPostgresDatabaseDetails result = await Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.PgVector, CancellationToken.None);

        Assert.Equal(4, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.DoesNotContain("tcpProxyCreate", request.Content!, StringComparison.Ordinal));
        Assert.Equal(SslMode.Disable, new NpgsqlConnectionStringBuilder(result.ProvisioningConnectionString).SslMode);
    }

    [Theory]
    [InlineData(15432, "environment-id", "svc-1", "ACTIVE")]
    [InlineData(5432, "foreign-environment", "svc-1", "ACTIVE")]
    [InlineData(5432, "environment-id", "foreign-service", "ACTIVE")]
    [InlineData(5432, "environment-id", "svc-1", "DELETING")]
    public async Task IncompatibleProxyIsRefusedWithoutMutation(int port, string environment, string service, string status)
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies(port, environment, service, status));

        RailwayPostgresProviderException failure = await Assert.ThrowsAsync<RailwayPostgresProviderException>(() =>
            Client(handler).EnsurePublicProvisioningEndpointAsync(Service(), RailwayPostgresTemplate.Standard, CancellationToken.None));

        Assert.Equal(RailwayPostgresProviderFailureKind.ProviderContract, failure.FailureKind);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("postgres-password", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangedProxyIdentityIsRefusedBeforeProvisioning()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies());
        handler.Enqueue(HttpStatusCode.OK, Proxies(id: "replacement"));

        await Assert.ThrowsAsync<RailwayPostgresProviderException>(() => Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.Standard, CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task AmbiguousProxiesAreRefusedWithoutMutation()
    {
        FakeHttpMessageHandler handler = new();
        using JsonDocument proxyData = JsonDocument.Parse(Proxies());
        JsonElement proxy = proxyData.RootElement.GetProperty("data").GetProperty("tcpProxies")[0];
        handler.Enqueue(HttpStatusCode.OK, JsonSerializer.Serialize(new { data = new { tcpProxies = new[] { proxy, proxy } } }));

        await Assert.ThrowsAsync<RailwayPostgresProviderException>(() => Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.Standard, CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DeclinedActivationIsRefusedBeforeProvisioning()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies(status: "CREATING"));
        handler.Enqueue(HttpStatusCode.OK, """{"data":{"serviceInstanceRedeploy":false}}""");

        await Assert.ThrowsAsync<RailwayPostgresProviderException>(() => Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.Standard, CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CallerCancellationBoundsEndpointReadback()
    {
        FakeHttpMessageHandler handler = new();
        handler.Enqueue(HttpStatusCode.OK, Proxies());
        handler.Enqueue(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(handler).EnsurePublicProvisioningEndpointAsync(
            Service(), RailwayPostgresTemplate.Standard, cancellation.Token));
    }

    private static RailwayPostgresManagementClient Client(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), new RailwayPostgresManagementCredentials("project-secret", RailwayPostgresAuthenticationMode.ProjectToken));

    private static RailwayPostgresDatabaseDetails Service() => new()
    {
        ProjectId = "project-id",
        EnvironmentId = "environment-id",
        ServiceId = "svc-1",
        ServiceName = "postgres",
        Host = "postgres.railway.internal",
        Port = 5432,
        UserName = "postgres",
        Password = "postgres-password",
        DatabaseName = "railway",
        ConnectionString = RailwayPostgresConnectionString.Create("postgres.railway.internal", 5432, "postgres", "postgres-password", "railway"),
        LatestDeploymentId = "prior",
        LatestDeploymentStatus = "SUCCESS",
    };

    private static string Proxies(int port = 5432, string environment = "environment-id", string service = "svc-1", string status = "ACTIVE", string id = "proxy-1") =>
        JsonSerializer.Serialize(new
        {
            data = new
            {
                tcpProxies = new[] { new
        {
            id, environmentId = environment, serviceId = service, applicationPort = port,
            domain = "public.proxy.rlwy.net", proxyPort = 15432, syncStatus = status,
        } }
            }
        });

    private static string ServiceResponse(string deploymentId) => JsonSerializer.Serialize(new
    {
        data = new
        {
            service = new { id = "svc-1", name = "postgres", projectId = "project-id" },
            serviceInstance = new { latestDeployment = new { id = deploymentId, status = "SUCCESS", deploymentStopped = false } },
            variables = new { PGHOST = "postgres.railway.internal", PGPORT = "5432", PGUSER = "postgres", PGPASSWORD = "postgres-password", PGDATABASE = "railway" },
        },
    });
}
