namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityState
{
    public RailwayPostgresRemoteIdentityState(
        string projectId,
        string serviceName,
        string serviceId,
        RailwayPostgresTemplate? template = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        if (template is not null && !Enum.IsDefined(template.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(template), template, "The Railway PostgreSQL template is not supported.");
        }

        ProjectId = projectId;
        ServiceName = serviceName;
        ServiceId = serviceId;
        Template = template;
    }

    public string ProjectId { get; }

    public RailwayPostgresRemoteIdentityState WithProjectId(string projectId)
    {
        return new RailwayPostgresRemoteIdentityState(projectId, ServiceName, ServiceId, Template);
    }

    public string ServiceName { get; }

    public string ServiceId { get; }

    public RailwayPostgresTemplate? Template { get; }
}
