namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed partial class RailwayPostgresManagementClient
{
    private const string GetTcpProxiesQuery = """
        query GetRailwayPostgresTcpProxies($environmentId: String!, $serviceId: String!) {
          tcpProxies(environmentId: $environmentId, serviceId: $serviceId) {
            id environmentId serviceId applicationPort domain proxyPort syncStatus deletedAt
          }
        }
        """;

    private const string CreateTcpProxyMutation = """
        mutation CreateRailwayPostgresTcpProxy($input: TCPProxyCreateInput!) {
          tcpProxyCreate(input: $input) { id }
        }
        """;

    public async Task<RailwayPostgresDatabaseDetails> EnsurePublicProvisioningEndpointAsync(
        RailwayPostgresDatabaseDetails service,
        RailwayPostgresTemplate template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(2));
        CancellationToken boundedToken = deadline.Token;

        try
        {
            RailwayTcpProxy? proxy = await GetProvisioningProxyAsync(service, boundedToken).ConfigureAwait(false);
            bool created = proxy is null;
            bool activate = created || !string.Equals(proxy!.SyncStatus, "ACTIVE", StringComparison.Ordinal);
            if (activate && string.IsNullOrWhiteSpace(service.LatestDeploymentId))
            {
                throw PublicProvisioningFailure("PostgreSQL endpoint activation requires the preceding service deployment identity.");
            }
            string proxyId;
            if (created)
            {
                CreateTcpProxyData result = await SendAsync<CreateTcpProxyData>(CreateTcpProxyMutation,
                    new { input = new { environmentId = service.EnvironmentId, serviceId = service.ServiceId, applicationPort = 5432 } },
                    boundedToken).ConfigureAwait(false);
                proxyId = result.TcpProxyCreate?.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(proxyId))
                {
                    throw PublicProvisioningFailure("Railway did not return a PostgreSQL provisioning proxy identity.");
                }
            }
            else
            {
                proxyId = proxy!.Id;
            }

            if (activate)
            {
                // The creation API stages the route; a new service deployment activates it.
                RedeployServiceInstanceData result = await SendAsync<RedeployServiceInstanceData>(
                    RedeployServiceInstanceMutation,
                    new { environmentId = service.EnvironmentId, serviceId = service.ServiceId },
                    boundedToken).ConfigureAwait(false);
                if (!result.ServiceInstanceRedeploy)
                {
                    throw PublicProvisioningFailure("Railway did not accept PostgreSQL provisioning endpoint activation.");
                }
                service = await WaitUntilReadyAsync(service.ProjectId, service.EnvironmentId, service.ServiceId, template,
                    new RailwayPostgresReadinessPollingOptions { PreviousDeploymentId = service.LatestDeploymentId },
                    boundedToken).ConfigureAwait(false);
            }

            while (true)
            {
                proxy = await GetProvisioningProxyAsync(service, boundedToken).ConfigureAwait(false);
                if (proxy is not null)
                {
                    if (!string.Equals(proxy.Id, proxyId, StringComparison.Ordinal))
                    {
                        throw PublicProvisioningFailure("The PostgreSQL provisioning proxy identity changed during activation.");
                    }
                    if (string.Equals(proxy.SyncStatus, "ACTIVE", StringComparison.Ordinal)
                        && Uri.CheckHostName(proxy.Domain) != UriHostNameType.Unknown
                        && !proxy.Domain.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase)
                        && proxy.ProxyPort is > 0 and <= 65535)
                    {
                        return service.WithProvisioningConnectionString(RailwayPostgresConnectionString.Create(
                            proxy.Domain, proxy.ProxyPort, service.UserName, service.Password, service.DatabaseName, GetSslMode(template)));
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(2), boundedToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw PublicProvisioningFailure("The PostgreSQL public provisioning endpoint did not become active within two minutes.");
        }
    }

    private async Task<RailwayTcpProxy?> GetProvisioningProxyAsync(
        RailwayPostgresDatabaseDetails service, CancellationToken cancellationToken)
    {
        GetTcpProxiesData data = await SendAsync<GetTcpProxiesData>(GetTcpProxiesQuery,
            new { environmentId = service.EnvironmentId, serviceId = service.ServiceId }, cancellationToken).ConfigureAwait(false);
        if (data.TcpProxies is null || data.TcpProxies.Count > 1)
        {
            throw PublicProvisioningFailure("Railway returned missing or ambiguous PostgreSQL provisioning proxy data.");
        }
        RailwayTcpProxy? proxy = data.TcpProxies.SingleOrDefault();
        if (proxy is not null && (string.IsNullOrWhiteSpace(proxy.Id) || proxy.ApplicationPort != 5432
            || !string.Equals(proxy.ServiceId, service.ServiceId, StringComparison.Ordinal)
            || !string.Equals(proxy.EnvironmentId, service.EnvironmentId, StringComparison.Ordinal)
            || proxy.DeletedAt is not null || proxy.SyncStatus is "DELETING" or "DELETED"))
        {
            throw PublicProvisioningFailure("The PostgreSQL provisioning proxy scope, target port or lifecycle is incompatible.");
        }
        return proxy;
    }

    private static RailwayPostgresProviderException PublicProvisioningFailure(string message) =>
        new(RailwayPostgresProviderFailureKind.ProviderContract, statusCode: null, message);

    private sealed class GetTcpProxiesData
    {
        public IReadOnlyList<RailwayTcpProxy>? TcpProxies { get; set; }
    }

    private sealed class CreateTcpProxyData
    {
        public RailwayTcpProxy? TcpProxyCreate { get; set; }
    }

    private sealed class RailwayTcpProxy
    {
        public string Id { get; set; } = string.Empty;
        public string EnvironmentId { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public int ApplicationPort { get; set; }
        public string Domain { get; set; } = string.Empty;
        public int ProxyPort { get; set; }
        public string SyncStatus { get; set; } = string.Empty;
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
