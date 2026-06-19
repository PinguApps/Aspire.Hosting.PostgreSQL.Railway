using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresImmutableDriftDetector
{
    public static void Validate(
        string configuredDatabaseName,
        RailwayPostgresProviderDeploymentOptions options,
        RailwayPostgresDatabaseDetails existingDatabase)
    {
        RailwayPostgresImmutableDrift? drift = Detect(configuredDatabaseName, options, existingDatabase);

        if (drift is not null)
        {
            throw new RailwayPostgresImmutableDriftException(drift);
        }
    }

    public static RailwayPostgresImmutableDrift? Detect(
        string configuredDatabaseName,
        RailwayPostgresProviderDeploymentOptions options,
        RailwayPostgresDatabaseDetails existingDatabase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredDatabaseName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(existingDatabase);

        if (!StringComparer.Ordinal.Equals(configuredDatabaseName, existingDatabase.DatabaseName))
        {
            return CreateDatabaseNameMismatch(configuredDatabaseName, existingDatabase);
        }

        if (options.Platform?.LiteralValue is string requestedPlatform
            && TryGetRemotePlatform(existingDatabase.PrimaryRegion, out string? remotePlatform)
            && !StringComparer.Ordinal.Equals(requestedPlatform, remotePlatform))
        {
            return CreatePlatformMismatch(
                configuredDatabaseName,
                requestedPlatform,
                remotePlatform!,
                existingDatabase.PrimaryRegion);
        }

        if (options.PrimaryRegion?.LiteralValue is string requestedPrimaryRegion
            && !StringComparer.Ordinal.Equals(requestedPrimaryRegion, existingDatabase.PrimaryRegion))
        {
            return CreatePrimaryRegionMismatch(
                configuredDatabaseName,
                requestedPrimaryRegion,
                existingDatabase.PrimaryRegion ?? "<unset>");
        }

        if (options.Tls?.LiteralValue is false)
        {
            return CreateTlsDisabled(
                configuredDatabaseName,
                requestedValue: "disabled",
                actualValue: existingDatabase.Tls ? "enabled" : "disabled");
        }

        return existingDatabase.Tls
            ? null
            : CreateTlsDisabled(
                configuredDatabaseName,
                requestedValue: "required enabled",
                actualValue: "disabled");
    }

    private static bool TryGetRemotePlatform(string? primaryRegion, out string? platform)
    {
        platform = null;

        if (string.IsNullOrWhiteSpace(primaryRegion))
        {
            return false;
        }

        try
        {
            RailwayPostgresRegion region = RailwayPostgresProviderDomain.ParsePrimaryRegion(primaryRegion, "remote primary region");
            platform = RailwayPostgresProviderDomain.MapCloudPlatform(RailwayPostgresProviderDomain.GetCloudPlatform(region));

            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static RailwayPostgresImmutableDrift CreateDatabaseNameMismatch(
        string configuredDatabaseName,
        RailwayPostgresDatabaseDetails existingDatabase)
    {
        string actualName = string.IsNullOrWhiteSpace(existingDatabase.DatabaseName)
            ? "<unset>"
            : existingDatabase.DatabaseName;

        return new(
            RailwayPostgresImmutableDriftFailureReason.DatabaseNameMismatch,
            "database name",
            configuredDatabaseName,
            actualName,
            $"Railway PostgreSQL database identity mismatch. Requested database name '{configuredDatabaseName}', but provider id '{existingDatabase.DatabaseId}' returned name '{actualName}'. The package treats database name as the v1 remote identity and will not rename or adopt a different resource automatically. Configure the intended name or fix the remote database outside Aspire.");
    }

    private static RailwayPostgresImmutableDrift CreatePlatformMismatch(
        string databaseName,
        string requestedPlatform,
        string actualPlatform,
        string? actualPrimaryRegion)
    {
        string actualRegion = actualPrimaryRegion ?? "<unset>";

        return new(
            RailwayPostgresImmutableDriftFailureReason.PlatformMismatch,
            "platform",
            requestedPlatform,
            actualPlatform,
            $"Railway PostgreSQL database '{databaseName}' has immutable platform drift. Requested platform '{requestedPlatform}', but the existing primary region '{actualRegion}' maps to platform '{actualPlatform}'. Platform is create-time-only in v1 and the package will not replace or move the database automatically. Create or select a database on the requested platform outside Aspire.");
    }

    private static RailwayPostgresImmutableDrift CreatePrimaryRegionMismatch(
        string databaseName,
        string requestedPrimaryRegion,
        string actualPrimaryRegion)
    {
        return new(
            RailwayPostgresImmutableDriftFailureReason.PrimaryRegionMismatch,
            "primary region",
            requestedPrimaryRegion,
            actualPrimaryRegion,
            $"Railway PostgreSQL database '{databaseName}' has immutable primary region drift. Requested primary region '{requestedPrimaryRegion}', found '{actualPrimaryRegion}'. Primary region is create-time-only in v1 and the package will not replace or move the database automatically. Create or select a database in the requested primary region outside Aspire.");
    }

    private static RailwayPostgresImmutableDrift CreateTlsDisabled(
        string databaseName,
        string requestedValue,
        string actualValue)
    {
        return new(
            RailwayPostgresImmutableDriftFailureReason.TlsDisabled,
            "TLS",
            requestedValue,
            actualValue,
            $"Railway PostgreSQL database '{databaseName}' has unsafe TLS drift. Requested TLS '{requestedValue}', found '{actualValue}'. TLS is required-on/read-only for v1, and the package will not call provider TLS repair endpoints automatically. Enable TLS outside Aspire or select a TLS-enabled database.");
    }
}
