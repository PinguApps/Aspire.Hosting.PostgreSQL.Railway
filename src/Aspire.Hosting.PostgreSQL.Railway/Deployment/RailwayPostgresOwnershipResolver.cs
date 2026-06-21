using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresOwnershipResolver
{
    public static RailwayPostgresOwnershipResolutionResult Resolve(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingService)
    {
        ArgumentNullException.ThrowIfNull(request);

        switch (request.OwnershipMode)
        {
            case RailwayPostgresOwnershipMode.CreateOnly:
                return ResolveCreateOnly(request, existingService);
            case RailwayPostgresOwnershipMode.ExistingOnly:
                return ResolveExistingOnly(request, existingService);
            case RailwayPostgresOwnershipMode.CreateOrAdopt:
                return ResolveCreateOrAdopt(existingService);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.OwnershipMode,
                    "The Railway PostgreSQL ownership mode is not supported.");
        }
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveCreateOnly(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingService)
    {
        if (existingService is null)
        {
            return RailwayPostgresOwnershipResolutionResult.Create();
        }

        if (request.ExistingServiceIsManagedIdentity)
        {
            return RailwayPostgresOwnershipResolutionResult.Adopt(existingService);
        }

        throw new RailwayPostgresOwnershipResolutionException(
            RailwayPostgresOwnershipResolutionFailureReason.CreateOnlyDatabaseAlreadyExists,
            $"Railway PostgreSQL service '{request.ServiceName}' already exists, but ownership mode is create-only. Choose a different service name, delete the existing service outside Aspire, or use create-or-adopt/existing-only if this deployment should manage it.");
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveExistingOnly(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingService)
    {
        if (existingService is null)
        {
            throw new RailwayPostgresOwnershipResolutionException(
                RailwayPostgresOwnershipResolutionFailureReason.ExistingOnlyDatabaseMissing,
                $"Railway PostgreSQL service '{request.ServiceName}' does not exist, but ownership mode is existing-only. Create the service outside Aspire first or use create-or-adopt/create-only if this deployment may create it.");
        }

        return RailwayPostgresOwnershipResolutionResult.Adopt(existingService);
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveCreateOrAdopt(RailwayPostgresDatabaseDetails? existingService)
    {
        return existingService is null
            ? RailwayPostgresOwnershipResolutionResult.Create()
            : RailwayPostgresOwnershipResolutionResult.Adopt(existingService);
    }
}
