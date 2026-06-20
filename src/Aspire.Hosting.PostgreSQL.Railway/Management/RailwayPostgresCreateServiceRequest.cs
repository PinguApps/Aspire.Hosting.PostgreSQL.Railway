namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresCreateServiceRequest
{
    public RailwayPostgresCreateServiceRequest(
        string serviceName,
        string projectId,
        string environmentId,
        RailwayPostgresDeploymentOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);

        ServiceName = serviceName;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        Options = options is null
            ? new RailwayPostgresDeploymentOptions()
            : new RailwayPostgresDeploymentOptions(options);
    }

    public string ServiceName { get; }

    public string ProjectId { get; }

    public string EnvironmentId { get; }

    public RailwayPostgresDeploymentOptions Options { get; }
}
