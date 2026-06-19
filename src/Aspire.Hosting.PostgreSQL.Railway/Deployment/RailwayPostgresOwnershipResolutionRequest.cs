using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresOwnershipResolutionRequest
{
    public RailwayPostgresOwnershipResolutionRequest(
        string serviceName,
        RailwayPostgresOwnershipMode ownershipMode,
        bool existingServiceIsManagedIdentity = false,
        RailwayPostgresDatabaseDetails? existingService = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
        }

        ServiceName = serviceName;
        OwnershipMode = ownershipMode;
        ExistingServiceIsManagedIdentity = existingServiceIsManagedIdentity;
        ExistingService = existingService;
    }

    public string ServiceName { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public bool ExistingServiceIsManagedIdentity { get; }

    public RailwayPostgresDatabaseDetails? ExistingService { get; }
}
