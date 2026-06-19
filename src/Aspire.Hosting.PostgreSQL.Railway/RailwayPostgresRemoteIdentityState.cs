namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityState
{
    public RailwayPostgresRemoteIdentityState(string serviceName, string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        ServiceName = serviceName;
        ServiceId = serviceId;
    }

    public string ServiceName { get; }

    public string ServiceId { get; }
}
