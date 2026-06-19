using System.Text.RegularExpressions;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static partial class RailwayPostgresDeploymentDiagnostics
{
    public const string Redacted = "[redacted]";

    public static string Redact(string value, RailwayPostgresResolvedDeployment? deployment = null, RailwayPostgresDatabaseDetails? database = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        string redacted = RedisConnectionStringPattern().Replace(value, Redacted);

        if (deployment is not null)
        {
            redacted = RedactKnownSecret(redacted, deployment.ManagementCredentials.ApiKey);
        }

        if (database is not null)
        {
            redacted = RedactKnownSecret(redacted, database.Password);
        }

        return redacted;
    }

    public static string FormatProviderDatabaseId(string? providerDatabaseId)
    {
        return string.IsNullOrWhiteSpace(providerDatabaseId)
            ? "<unknown>"
            : providerDatabaseId;
    }

    public static RailwayPostgresDeploymentProgress CreateProgress(
        RailwayPostgresDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId,
        RailwayPostgresResolvedDeployment? deployment = null,
        RailwayPostgresDatabaseDetails? database = null)
    {
        return new RailwayPostgresDeploymentProgress(
            phase,
            Redact(message, deployment, database),
            resourceName,
            databaseName,
            providerDatabaseId);
    }

    private static string RedactKnownSecret(string value, string? secret)
    {
        return string.IsNullOrEmpty(secret)
            ? value
            : value.Replace(secret, Redacted, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"rediss?://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RedisConnectionStringPattern();
}
