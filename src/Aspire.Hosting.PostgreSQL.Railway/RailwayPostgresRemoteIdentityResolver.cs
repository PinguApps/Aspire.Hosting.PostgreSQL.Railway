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
        string projectId,
        string environmentId,
        string configuredServiceName,
        RailwayPostgresRemoteIdentityState? cachedIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredServiceName);

        if (cachedIdentity is null || cachedIdentity.ServiceName != configuredServiceName)
        {
            return await ResolveByConfiguredNameAsync(projectId, environmentId, configuredServiceName, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            RailwayPostgresDatabaseDetails cachedService = await _client
                .GetServiceAsync(projectId, environmentId, cachedIdentity.ServiceId, cancellationToken)
                .ConfigureAwait(false);

            if (cachedService.ServiceId != cachedIdentity.ServiceId)
            {
                throw CreateMismatchedCachedDetailException(
                    configuredServiceName,
                    cachedIdentity.ServiceId,
                    cachedService.ServiceId);
            }

            RailwayPostgresRemoteIdentityResolution resolution = RailwayPostgresRemoteIdentityResolution.FoundDatabase(
                cachedService,
                resolvedFromCachedIdentity: true);

            if (cachedService.ServiceName != configuredServiceName)
            {
                resolution = await ResolveDriftedCachedIdentityAsync(
                    projectId,
                    environmentId,
                    configuredServiceName,
                    cachedIdentity,
                    cachedService.ServiceName,
                    cancellationToken).ConfigureAwait(false);
            }

            await VerifyConfiguredNameResolvesToCachedIdentityAsync(
                projectId,
                environmentId,
                configuredServiceName,
                cachedIdentity.ServiceId,
                cancellationToken).ConfigureAwait(false);

            return resolution;
        }
        catch (RailwayPostgresProviderException exception) when (exception.FailureKind == RailwayPostgresProviderFailureKind.NotFound)
        {
            return await ResolveMissingCachedIdentityAsync(projectId, environmentId, configuredServiceName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveByConfiguredNameAsync(
        string projectId,
        string environmentId,
        string configuredServiceName,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? service =
            await _client.FindServiceByNameAsync(projectId, environmentId, configuredServiceName, cancellationToken).ConfigureAwait(false);

        return service is null
            ? RailwayPostgresRemoteIdentityResolution.NotFound()
            : RailwayPostgresRemoteIdentityResolution.FoundDatabase(service);
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveMissingCachedIdentityAsync(
        string projectId,
        string environmentId,
        string configuredServiceName,
        RailwayPostgresRemoteIdentityState cachedIdentity,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? service =
            await _client.FindServiceByNameAsync(projectId, environmentId, configuredServiceName, cancellationToken).ConfigureAwait(false);

        if (service is null)
        {
            return RailwayPostgresRemoteIdentityResolution.NotFound();
        }

        if (service.ServiceId != cachedIdentity.ServiceId)
        {
            throw CreateUnsafeIdentityException(
                configuredServiceName,
                cachedIdentity.ServiceId,
                service.ServiceId);
        }

        return RailwayPostgresRemoteIdentityResolution.FoundDatabase(
            service,
            resolvedFromCachedIdentity: true);
    }

    private async Task<RailwayPostgresRemoteIdentityResolution> ResolveDriftedCachedIdentityAsync(
        string projectId,
        string environmentId,
        string configuredServiceName,
        RailwayPostgresRemoteIdentityState cachedIdentity,
        string currentCachedServiceName,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails? service =
            await _client.FindServiceByNameAsync(projectId, environmentId, configuredServiceName, cancellationToken).ConfigureAwait(false);

        if (service is not null && service.ServiceId != cachedIdentity.ServiceId)
        {
            throw CreateUnsafeIdentityException(
                configuredServiceName,
                cachedIdentity.ServiceId,
                service.ServiceId);
        }

        throw new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Railway PostgreSQL service '{cachedIdentity.ServiceId}' is now named '{currentCachedServiceName}', not configured name '{configuredServiceName}'. Refusing to reconcile a drifted remote identity.");
    }

    private async Task VerifyConfiguredNameResolvesToCachedIdentityAsync(
        string projectId,
        string environmentId,
        string configuredServiceName,
        string cachedServiceId,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDatabaseDetails service =
            await _client.FindServiceByNameAsync(projectId, environmentId, configuredServiceName, cancellationToken).ConfigureAwait(false)
            ?? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Cached Railway PostgreSQL service '{cachedServiceId}' still reports configured name '{configuredServiceName}', but the configured name lookup returned no service. Refusing to reconcile an unverifiable cached remote identity.");

        if (service.ServiceId != cachedServiceId)
        {
            throw CreateUnsafeIdentityException(
                configuredServiceName,
                cachedServiceId,
                service.ServiceId);
        }
    }

    private static RailwayPostgresProviderException CreateUnsafeIdentityException(
        string configuredServiceName,
        string cachedServiceId,
        string resolvedServiceId)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Configured Railway PostgreSQL service name '{configuredServiceName}' resolves to service id '{resolvedServiceId}', but cached identity expected service id '{cachedServiceId}'. Refusing to adopt a different service for the same configured name.");
    }

    private static RailwayPostgresProviderException CreateMismatchedCachedDetailException(
        string configuredServiceName,
        string cachedServiceId,
        string resolvedServiceId)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Railway PostgreSQL service '{cachedServiceId}' detail response returned service id '{resolvedServiceId}' for configured name '{configuredServiceName}'. Refusing to reconcile a mismatched cached remote identity.");
    }
}
