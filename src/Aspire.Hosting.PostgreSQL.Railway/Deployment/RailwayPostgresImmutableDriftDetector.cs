using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresImmutableDriftDetector
{
    public static RailwayPostgresImmutableDrift? Detect(
        string serviceName,
        RailwayPostgresProviderDeploymentOptions options,
        RailwayPostgresDatabaseDetails service)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(service);

        return null;
    }
}
