namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal interface IRailwayPostgresManagementClient
{
    public Task<string> ResolveEnvironmentIdAsync(
        string projectId,
        string environmentIdOrName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(environmentIdOrName);
    }

    public Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
        string projectId,
        string environmentId,
        string serviceName,
        CancellationToken cancellationToken)
    {
        return Task.FromException<RailwayPostgresDatabaseDetails?>(new NotSupportedException("The Railway PostgreSQL management client does not support service lookup by name."));
    }

    public Task<RailwayPostgresDatabaseDetails> GetServiceAsync(
        string projectId,
        string environmentId,
        string serviceId,
        CancellationToken cancellationToken)
    {
        return Task.FromException<RailwayPostgresDatabaseDetails>(new NotSupportedException("The Railway PostgreSQL management client does not support service lookup by id."));
    }

    public Task<RailwayPostgresDatabaseDetails> CreateServiceAsync(
        RailwayPostgresCreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromException<RailwayPostgresDatabaseDetails>(new NotSupportedException("The Railway PostgreSQL management client does not support service creation."));
    }

    public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
        string projectId,
        string environmentId,
        string serviceId,
        RailwayPostgresReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken)
    {
        return Task.FromException<RailwayPostgresDatabaseDetails>(new NotSupportedException("The Railway PostgreSQL management client does not support readiness polling."));
    }
}
