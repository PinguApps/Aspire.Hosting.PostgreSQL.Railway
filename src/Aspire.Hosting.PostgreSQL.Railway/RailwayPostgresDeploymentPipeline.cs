#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway.Deployment;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal static class RailwayPostgresDeploymentPipeline
{
    private static readonly HttpClient _managementHttpClient = new()
    {
        BaseAddress = new Uri("https://api.railway.com/v2/"),
    };

    private static readonly Action<ILogger, string, string?, string?, string?, Exception?> _deploymentProgress =
        LoggerMessage.Define<string, string?, string?, string?>(
            LogLevel.Information,
            new EventId(1, "RailwayPostgresDeploymentProgress"),
            "{Message} Resource='{ResourceName}' Database='{DatabaseName}' ProviderDatabaseId='{ProviderDatabaseId}'.");

    public static async Task ExecuteAsync(RedisResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        LoggerRailwayPostgresDeploymentProgressReporter progressReporter = new(context.Logger, resource.Name);
        progressReporter.Report(RailwayPostgresDeploymentDiagnostics.CreateProgress(
            RailwayPostgresDeploymentPhase.ResolvingConfiguration,
            $"Resolving Railway PostgreSQL deployment configuration for Redis resource '{resource.Name}'.",
            resource.Name,
            databaseName: null,
            providerDatabaseId: null));

        RailwayPostgresDeploymentState state = resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' is missing Railway deployment state.");

        RailwayPostgresResolvedDeployment deployment =
            await RailwayPostgresDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);

        IRailwayPostgresManagementClient client = context.Services.GetService<IRailwayPostgresManagementClient>()
            ?? new RailwayPostgresManagementClient(_managementHttpClient, deployment.ManagementCredentials);
        RailwayPostgresRemoteIdentityDeploymentStateStore identityStore = new(
            context.Services.GetRequiredService<IDeploymentStateManager>());
        RailwayPostgresRemoteIdentityState? cachedIdentity =
            await identityStore.LoadAsync(resource.Name, context.CancellationToken).ConfigureAwait(false);

        RailwayPostgresCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progressReporter,
            resource.Name,
            context.CancellationToken)
            .ConfigureAwait(false);

        resource.ApplyRailwayPostgresConnectionOutput(result.Database);
        resource.TryGetRailwayPostgresOutputs()?.Populate(result.Database);

        await identityStore.SaveAsync(resource.Name, result.RemoteIdentity, context.CancellationToken).ConfigureAwait(false);
    }

    internal static async Task<RailwayPostgresDatabaseDetails?> ExecuteAsync(
        RailwayPostgresResolvedDeployment deployment,
        IRailwayPostgresManagementClient client,
        RailwayPostgresOutputs? outputs,
        CancellationToken cancellationToken)
    {
        RailwayPostgresCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity: null,
            progressReporter: null,
            resourceName: null,
            cancellationToken).ConfigureAwait(false);

        outputs?.Populate(result.Database);

        return result.Database;
    }

    internal static async Task<RailwayPostgresDatabaseDetails?> ExecuteAsync(
        RailwayPostgresResolvedDeployment deployment,
        IRailwayPostgresManagementClient client,
        RailwayPostgresRemoteIdentityState? cachedIdentity,
        Func<RailwayPostgresRemoteIdentityState, Task>? saveIdentityStateAsync,
        CancellationToken cancellationToken)
    {
        RailwayPostgresCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progressReporter: null,
            resourceName: null,
            cancellationToken).ConfigureAwait(false);

        if (saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(result.RemoteIdentity).ConfigureAwait(false);
        }

        return result.Database;
    }

    internal static async Task<RailwayPostgresDatabaseDetails?> ExecuteAsync(
        RailwayPostgresResolvedDeployment deployment,
        IRailwayPostgresManagementClient client,
        RailwayPostgresRemoteIdentityState? cachedIdentity,
        Func<RailwayPostgresRemoteIdentityState, Task>? saveIdentityStateAsync,
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        RailwayPostgresCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progressReporter,
            resourceName,
            cancellationToken).ConfigureAwait(false);

        if (saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(result.RemoteIdentity).ConfigureAwait(false);
        }

        return result.Database;
    }

    private static async Task<RailwayPostgresCreateFlowResult> ExecuteCoreAsync(
        RailwayPostgresResolvedDeployment deployment,
        IRailwayPostgresManagementClient client,
        RailwayPostgresRemoteIdentityState? cachedIdentity,
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(client);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.ResolvingConfiguration,
            $"Resolved Railway PostgreSQL deployment configuration for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            providerDatabaseId: null,
            deployment,
            database: null);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.LocatingDatabase,
            $"Locating Railway PostgreSQL database '{deployment.DatabaseName}' by configured name.",
            resourceName,
            deployment.DatabaseName,
            providerDatabaseId: null,
            deployment,
            database: null);

        RailwayPostgresRemoteIdentityResolution remoteIdentity =
            await new RailwayPostgresRemoteIdentityResolver(client)
                .ResolveAsync(deployment.DatabaseName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);

        string? locatedProviderDatabaseId = remoteIdentity.Database?.DatabaseId;
        string locatedMessage = remoteIdentity.Database is null
            ? $"No Railway PostgreSQL database named '{deployment.DatabaseName}' was found."
            : $"Located Railway PostgreSQL database '{deployment.DatabaseName}' with provider id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(locatedProviderDatabaseId)}'.";

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.LocatingDatabase,
            locatedMessage,
            resourceName,
            deployment.DatabaseName,
            locatedProviderDatabaseId,
            deployment,
            remoteIdentity.Database);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.ValidatingImmutableDrift,
            $"Validating immutable Railway PostgreSQL settings for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            locatedProviderDatabaseId,
            deployment,
            remoteIdentity.Database);

        RailwayPostgresOwnershipResolutionRequest ownershipRequest = new(
            deployment.DatabaseName,
            deployment.OwnershipMode,
            deployment.Options,
            remoteIdentity.ResolvedFromCachedIdentity,
            remoteIdentity.Database);
        RailwayPostgresOwnershipResolutionResult ownership = RailwayPostgresOwnershipResolver.Resolve(
            ownershipRequest,
            remoteIdentity.Database);

        ReportOwnership(progressReporter, resourceName, deployment, ownership);

        RailwayPostgresCreateFlowResult createResult =
            await new RailwayPostgresCreateFlow(client)
                .ExecuteAsync(deployment, ownership, cancellationToken)
                .ConfigureAwait(false);

        ReportCreatedOrAdopted(progressReporter, resourceName, deployment, createResult);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.ReconcilingMutableSettings,
            $"Reconciling explicit mutable Railway PostgreSQL settings for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            createResult.Database.DatabaseId,
            deployment,
            createResult.Database);

        RailwayPostgresDatabaseDetails reconciledDatabase = await new RailwayPostgresReconciler(client)
            .ReconcileAsync(createResult.Database, deployment.Options, cancellationToken)
            .ConfigureAwait(false);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.RetrievingOutputs,
            $"Retrieved Redis connection outputs for Railway PostgreSQL database '{deployment.DatabaseName}' with provider id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(reconciledDatabase.DatabaseId)}'.",
            resourceName,
            deployment.DatabaseName,
            reconciledDatabase.DatabaseId,
            deployment,
            reconciledDatabase);

        RailwayPostgresCreateFlowResult result = new(reconciledDatabase, createResult.Created);

        return result;
    }

    private static void ReportOwnership(
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        string? resourceName,
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresOwnershipResolutionResult ownership)
    {
        if (ownership.Action == RailwayPostgresOwnershipResolutionAction.Create)
        {
            Report(
                progressReporter,
                RailwayPostgresDeploymentPhase.CreatingDatabase,
                $"Creating Railway PostgreSQL database '{deployment.DatabaseName}'.",
                resourceName,
                deployment.DatabaseName,
                providerDatabaseId: null,
                deployment,
                database: null);
        }
    }

    private static void ReportCreatedOrAdopted(
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        string? resourceName,
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresCreateFlowResult createResult)
    {
        string action = createResult.Created ? "Created" : "Using existing";

        Report(
            progressReporter,
            createResult.Created ? RailwayPostgresDeploymentPhase.CreatingDatabase : RailwayPostgresDeploymentPhase.LocatingDatabase,
            $"{action} Railway PostgreSQL database '{deployment.DatabaseName}' with provider id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(createResult.Database.DatabaseId)}'.",
            resourceName,
            deployment.DatabaseName,
            createResult.Database.DatabaseId,
            deployment,
            createResult.Database);
    }

    private static void Report(
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        RailwayPostgresDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId,
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresDatabaseDetails? database)
    {
        progressReporter?.Report(RailwayPostgresDeploymentDiagnostics.CreateProgress(
            phase,
            message,
            resourceName,
            databaseName,
            providerDatabaseId,
            deployment,
            database));
    }

    private sealed class LoggerRailwayPostgresDeploymentProgressReporter : IRailwayPostgresDeploymentProgressReporter
    {
        private readonly ILogger _logger;
        private readonly string _resourceName;

        public LoggerRailwayPostgresDeploymentProgressReporter(ILogger logger, string resourceName)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

            _logger = logger;
            _resourceName = resourceName;
        }

        public void Report(RailwayPostgresDeploymentProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            _deploymentProgress(
                _logger,
                progress.Message,
                progress.ResourceName ?? _resourceName,
                progress.DatabaseName,
                progress.ProviderDatabaseId,
                null);
        }
    }
}
