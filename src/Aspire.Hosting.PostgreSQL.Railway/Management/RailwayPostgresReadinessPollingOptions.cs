namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresReadinessPollingOptions
{
    public static RailwayPostgresReadinessPollingOptions Default { get; } = new();

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(2);

    public string? PreviousDeploymentId { get; init; }
}
