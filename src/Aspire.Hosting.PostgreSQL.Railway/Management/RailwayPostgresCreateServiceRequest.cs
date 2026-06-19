namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresCreateServiceRequest
{
    public RailwayPostgresCreateServiceRequest(
        string serviceName,
        string projectId,
        string environmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);

        ServiceName = serviceName;
        ProjectId = projectId;
        EnvironmentId = environmentId;
    }

    public string ServiceName { get; }

    public string ProjectId { get; }

    public string EnvironmentId { get; }
}
