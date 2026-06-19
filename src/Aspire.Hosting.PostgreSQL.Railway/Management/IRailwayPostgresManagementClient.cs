namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal interface IRailwayPostgresManagementClient
{
    public Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
        string projectId,
        string environmentId,
        string serviceName,
        CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> GetServiceAsync(
        string projectId,
        string environmentId,
        string serviceId,
        CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> CreateServiceAsync(
        RailwayPostgresCreateServiceRequest request,
        CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
        string projectId,
        string environmentId,
        string serviceId,
        RailwayPostgresReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken);
}
