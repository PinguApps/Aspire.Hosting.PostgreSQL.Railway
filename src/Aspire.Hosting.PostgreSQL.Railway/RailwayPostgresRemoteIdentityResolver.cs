using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityResolver
{
    private readonly IRailwayPostgresManagementClient _client;

    public RailwayPostgresRemoteIdentityResolver(IRailwayPostgresManagementClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
    }

    public async Task<RailwayPostgresRemoteIdentityResolution> ResolveAsync(
        string configuredDatabaseName,
        RailwayPostgresRemoteIdentityState? cachedIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredDatabaseName);

        if (cachedIdentity is null)
        {
            return await ResolveByConfiguredNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);
        }

        if (cachedIdentity.DatabaseName != configuredDatabaseName)
        {
            // The explicit name is the v1 identity. A changed configured name selects a different
            // remote database; this package never calls the provider rename endpoint.
            return await ResolveByConfiguredNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            RailwayPostgresDatabaseDetails cachedDatabase =
                await _client.GetDatabaseAsync(cachedIdentity.ProviderDatabaseId, cancellationToken).ConfigureAwait(false);

            if (cachedDatabase.DatabaseId != cachedIdentity.ProviderDatabaseId)
            {
                throw CreateMismatchedCachedDetailException(
                    configuredDatabaseName,
                    cachedIdentity.ProviderDatabaseId,
                    cachedDatabase.DatabaseId);
            }

            RailwayPostgresRemoteIdentityResolution resolution = RailwayPostgresRemoteIdentityResolution.FoundDatabase(
                cachedDatabase,
                resolvedFromCachedIdentity: true);

            if (cachedDatabase.DatabaseName != configuredDatabaseName)
            {
                resolution = await ResolveDriftedCachedIdentityAsync(
                    configuredDatabaseName,
                    cachedIdentity,
                    cachedDatabase.DatabaseName,
                    cancellationToken).ConfigureAwait(false);
            }

            await VerifyConfiguredNameResolvesToCachedIdentityAsync(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                cancellationToken).ConfigureAwait(false);

            return resolution;
        }
        catch (RailwayPostgresProviderException exception) when (exception.FailureKind == RailwayPostgresProviderFailureKind.NotFound)
        {
            return await ResolveMissingCachedIdentityAsync(configuredDatabaseName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveByConfiguredNameAsync(
        string configuredDatabaseName,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        return database is null
            ? RailwayPostgresRemoteIdentityResolution.NotFound()
            : RailwayPostgresRemoteIdentityResolution.FoundDatabase(database);
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveMissingCachedIdentityAsync(
        string configuredDatabaseName,
        RailwayPostgresRemoteIdentityState cachedIdentity,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        if (database is null)
        {
            return RailwayPostgresRemoteIdentityResolution.NotFound();
        }

        if (database.DatabaseId != cachedIdentity.ProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                database.DatabaseId);
        }

        return RailwayPostgresRemoteIdentityResolution.FoundDatabase(
            database,
            resolvedFromCachedIdentity: true);
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveDriftedCachedIdentityAsync(
        string configuredDatabaseName,
        RailwayPostgresRemoteIdentityState cachedIdentity,
        string currentCachedDatabaseName,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        if (database is not null && database.DatabaseId != cachedIdentity.ProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                database.DatabaseId);
        }

        throw new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Railway PostgreSQL database '{cachedIdentity.ProviderDatabaseId}' is now named '{currentCachedDatabaseName}', not configured name '{configuredDatabaseName}'. Refusing to reconcile a drifted remote identity.");
    }

    private async Task VerifyConfiguredNameResolvesToCachedIdentityAsync(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false)
            ?? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Cached Railway PostgreSQL database '{cachedProviderDatabaseId}' still reports configured name '{configuredDatabaseName}', but the configured name lookup returned no database. Refusing to reconcile an unverifiable cached remote identity.");

        if (database.DatabaseId != cachedProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedProviderDatabaseId,
                database.DatabaseId);
        }
    }

    private static RailwayPostgresProviderException CreateUnsafeIdentityException(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        string resolvedProviderDatabaseId)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Configured Railway PostgreSQL database name '{configuredDatabaseName}' resolves to provider id '{resolvedProviderDatabaseId}', but cached identity expected provider id '{cachedProviderDatabaseId}'. Refusing to adopt a different database for the same configured name.");
    }

    private static RailwayPostgresProviderException CreateMismatchedCachedDetailException(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        string resolvedProviderDatabaseId)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Railway PostgreSQL database '{cachedProviderDatabaseId}' detail response returned provider id '{resolvedProviderDatabaseId}' for configured name '{configuredDatabaseName}'. Refusing to reconcile a mismatched cached remote identity.");
    }
}
