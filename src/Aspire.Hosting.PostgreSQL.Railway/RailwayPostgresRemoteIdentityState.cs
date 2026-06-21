namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityState
{
    public RailwayPostgresRemoteIdentityState(string projectId, string serviceName, string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        ProjectId = projectId;
        ServiceName = serviceName;
        ServiceId = serviceId;
    }

    public string ProjectId { get; }

    public RailwayPostgresRemoteIdentityState WithProjectId(string projectId)
    {
        return new RailwayPostgresRemoteIdentityState(projectId, ServiceName, ServiceId);
    }

    public string ServiceName { get; }

    public string ServiceId { get; }
}
