using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresResolvedDeployment
{
    public RailwayPostgresResolvedDeployment(
        string databaseName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresManagementCredentials managementCredentials,
        RailwayPostgresProviderDeploymentOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(managementCredentials);
        ArgumentNullException.ThrowIfNull(options);

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        ManagementCredentials = managementCredentials;
        Options = options;
    }

    public string DatabaseName { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public RailwayPostgresManagementCredentials ManagementCredentials { get; }

    public RailwayPostgresProviderDeploymentOptions Options { get; }
}
