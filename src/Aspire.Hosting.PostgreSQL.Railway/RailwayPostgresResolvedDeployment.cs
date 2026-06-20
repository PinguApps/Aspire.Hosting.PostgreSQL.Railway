using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresResolvedDeployment
{
    public RailwayPostgresResolvedDeployment(
        string serviceName,
        string projectId,
        string environmentId,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresManagementCredentials managementCredentials,
        RailwayPostgresDeploymentOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentNullException.ThrowIfNull(managementCredentials);

        ServiceName = serviceName;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        OwnershipMode = ownershipMode;
        ManagementCredentials = managementCredentials;
        Options = new RailwayPostgresDeploymentOptions(options ?? new RailwayPostgresDeploymentOptions());
    }

    public string ServiceName { get; }

    public string ProjectId { get; }

    public string EnvironmentId { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public RailwayPostgresManagementCredentials ManagementCredentials { get; }

    public RailwayPostgresDeploymentOptions Options { get; }
}
