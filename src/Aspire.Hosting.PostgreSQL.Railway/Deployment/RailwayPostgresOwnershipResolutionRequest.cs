using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresOwnershipResolutionRequest
{
    public RailwayPostgresOwnershipResolutionRequest(
        string databaseName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresProviderDeploymentOptions options,
        bool existingDatabaseIsManagedIdentity = false,
        RailwayPostgresDatabaseDetails? existingDatabase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
        }

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        Options = options;
        ExistingDatabaseIsManagedIdentity = existingDatabaseIsManagedIdentity;
        ExistingDatabase = existingDatabase;
    }

    public string DatabaseName { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public RailwayPostgresProviderDeploymentOptions Options { get; }

    public bool ExistingDatabaseIsManagedIdentity { get; }

    public RailwayPostgresDatabaseDetails? ExistingDatabase { get; }
}
