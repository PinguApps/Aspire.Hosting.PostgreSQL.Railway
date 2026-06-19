using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresReconciler
{
    public RailwayPostgresReconciler(IRailwayPostgresManagementClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
    }

    public Task<RailwayPostgresDatabaseDetails> ReconcileAsync(
        RailwayPostgresDatabaseDetails service,
        RailwayPostgresProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(service);
    }
}
