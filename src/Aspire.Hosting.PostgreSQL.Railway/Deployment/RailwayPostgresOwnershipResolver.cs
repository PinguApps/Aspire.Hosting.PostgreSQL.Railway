using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresOwnershipResolver
{
    public static async Task<RailwayPostgresOwnershipResolutionResult> ResolveAsync(
        RailwayPostgresOwnershipResolutionRequest request,
        IRailwayPostgresManagementClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);

        RailwayPostgresDatabaseDetails? existingDatabase = request.ExistingDatabase;

        existingDatabase ??= await client
            .FindDatabaseByNameAsync(request.DatabaseName, cancellationToken)
            .ConfigureAwait(false);

        return Resolve(request, existingDatabase);
    }

    public static RailwayPostgresOwnershipResolutionResult Resolve(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingDatabase)
    {
        ArgumentNullException.ThrowIfNull(request);

        switch (request.OwnershipMode)
        {
            case RailwayPostgresOwnershipMode.CreateOnly:
                return ResolveCreateOnly(request, existingDatabase);
            case RailwayPostgresOwnershipMode.ExistingOnly:
                return ResolveExistingOnly(request, existingDatabase);
            case RailwayPostgresOwnershipMode.CreateOrAdopt:
                return ResolveCreateOrAdopt(request, existingDatabase);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.OwnershipMode,
                    "The Railway PostgreSQL ownership mode is not supported.");
        }
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveCreateOnly(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingDatabase)
    {
        if (existingDatabase is null)
        {
            return RailwayPostgresOwnershipResolutionResult.Create();
        }

        if (request.ExistingDatabaseIsManagedIdentity)
        {
            ValidateExistingDatabaseCompatibility(request, existingDatabase);

            return RailwayPostgresOwnershipResolutionResult.Adopt(existingDatabase);
        }

        throw new RailwayPostgresOwnershipResolutionException(
            RailwayPostgresOwnershipResolutionFailureReason.CreateOnlyDatabaseAlreadyExists,
            $"Railway PostgreSQL database '{request.DatabaseName}' already exists, but ownership mode is create-only. Choose a different database name, delete the existing database outside Aspire, or use create-or-adopt/existing-only if this deployment should manage it.");
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveExistingOnly(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingDatabase)
    {
        if (existingDatabase is null)
        {
            throw new RailwayPostgresOwnershipResolutionException(
                RailwayPostgresOwnershipResolutionFailureReason.ExistingOnlyDatabaseMissing,
                $"Railway PostgreSQL database '{request.DatabaseName}' does not exist, but ownership mode is existing-only. Create the database outside Aspire first or use create-or-adopt/create-only if this deployment may create it.");
        }

        ValidateExistingDatabaseCompatibility(request, existingDatabase);

        return RailwayPostgresOwnershipResolutionResult.Adopt(existingDatabase);
    }

    private static RailwayPostgresOwnershipResolutionResult ResolveCreateOrAdopt(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails? existingDatabase)
    {
        if (existingDatabase is null)
        {
            return RailwayPostgresOwnershipResolutionResult.Create();
        }

        ValidateExistingDatabaseCompatibility(request, existingDatabase);

        return RailwayPostgresOwnershipResolutionResult.Adopt(existingDatabase);
    }

    private static void ValidateExistingDatabaseCompatibility(
        RailwayPostgresOwnershipResolutionRequest request,
        RailwayPostgresDatabaseDetails existingDatabase)
    {
        RailwayPostgresImmutableDrift? drift = RailwayPostgresImmutableDriftDetector.Detect(
            request.DatabaseName,
            request.Options,
            existingDatabase);

        if (drift is not null)
        {
            throw new RailwayPostgresOwnershipResolutionException(
                RailwayPostgresOwnershipResolutionFailureReason.ExistingDatabaseIncompatible,
                drift.Message);
        }
    }
}
