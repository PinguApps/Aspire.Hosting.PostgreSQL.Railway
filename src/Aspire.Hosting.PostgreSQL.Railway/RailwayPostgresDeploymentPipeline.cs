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
        BaseAddress = new Uri("https://backboard.railway.com/graphql/v2"),
    };

    private static readonly Action<ILogger, string, string?, string?, string?, Exception?> _deploymentProgress =
        LoggerMessage.Define<string, string?, string?, string?>(
            LogLevel.Information,
            new EventId(1, "RailwayPostgresDeploymentProgress"),
            "{Message} Resource='{ResourceName}' Service='{ServiceName}' ServiceId='{ServiceId}'.");

    public static async Task ExecuteAsync(PostgresServerResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        LoggerRailwayPostgresDeploymentProgressReporter progressReporter = new(context.Logger, resource.Name);
        progressReporter.Report(RailwayPostgresDeploymentDiagnostics.CreateProgress(
            RailwayPostgresDeploymentPhase.ResolvingConfiguration,
            $"Resolving Railway PostgreSQL deployment configuration for PostgreSQL server resource '{resource.Name}'.",
            resource.Name,
            databaseName: null,
            providerDatabaseId: null));

        RailwayPostgresDeploymentState state = resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException($"PostgreSQL server resource '{resource.Name}' is missing Railway deployment state.");

        RailwayPostgresResolvedDeployment deployment =
            await RailwayPostgresDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);

        IRailwayPostgresManagementClient client = context.Services.GetService<IRailwayPostgresManagementClient>()
            ?? new RailwayPostgresManagementClient(_managementHttpClient, deployment.ManagementCredentials);
        deployment = await ResolveEnvironmentIdAsync(
            deployment,
            client,
            progressReporter,
            resource.Name,
            context.CancellationToken)
            .ConfigureAwait(false);

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

        await ApplyOutputsAsync(resource, context, result.Database, context.CancellationToken).ConfigureAwait(false);

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

    private static async Task ApplyOutputsAsync(
        PostgresServerResource resource,
        PipelineStepContext context,
        RailwayPostgresDatabaseDetails service,
        CancellationToken cancellationToken)
    {
        List<PostgresDatabaseResource> childDatabases =
        [
            .. context.Model.Resources
            .OfType<PostgresDatabaseResource>()
            .Where(database => ReferenceEquals(database.Parent, resource))
        ];

        await RailwayPostgresDatabaseProvisioner
            .EnsureDatabasesAsync(service, childDatabases.Select(database => database.DatabaseName), cancellationToken)
            .ConfigureAwait(false);

        resource.ApplyRailwayPostgresConnectionOutput(service);
        resource.TryGetRailwayPostgresOutputs()?.Populate(service);

        foreach (PostgresDatabaseResource childDatabase in childDatabases)
        {
            childDatabase.ApplyRailwayPostgresConnectionOutput(service.WithDatabaseName(childDatabase.DatabaseName));
        }
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

        deployment = await ResolveEnvironmentIdAsync(
            deployment,
            client,
            progressReporter,
            resourceName,
            cancellationToken)
            .ConfigureAwait(false);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.ResolvingConfiguration,
            $"Resolved Railway PostgreSQL deployment configuration for service '{deployment.ServiceName}'.",
            resourceName,
            deployment.ServiceName,
            providerDatabaseId: null);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.LocatingDatabase,
            $"Locating Railway PostgreSQL service '{deployment.ServiceName}' by configured name.",
            resourceName,
            deployment.ServiceName,
            providerDatabaseId: null);

        RailwayPostgresRemoteIdentityResolution remoteIdentity =
            await new RailwayPostgresRemoteIdentityResolver(client)
                .ResolveAsync(deployment.ProjectId, deployment.EnvironmentId, deployment.ServiceName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);

        string? locatedServiceId = remoteIdentity.Database?.ServiceId;
        string locatedMessage = remoteIdentity.Database is null
            ? $"No Railway PostgreSQL service named '{deployment.ServiceName}' was found."
            : $"Located Railway PostgreSQL service '{deployment.ServiceName}' with service id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(locatedServiceId)}'.";

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.LocatingDatabase,
            locatedMessage,
            resourceName,
            deployment.ServiceName,
            locatedServiceId);

        RailwayPostgresOwnershipResolutionRequest ownershipRequest = new(
            deployment.ServiceName,
            deployment.OwnershipMode,
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

        if (deployment.Options.HasAny)
        {
            Report(
                progressReporter,
                RailwayPostgresDeploymentPhase.ConfiguringService,
                $"Configuring Railway PostgreSQL service '{deployment.ServiceName}'.",
                resourceName,
                deployment.ServiceName,
                createResult.Database.ServiceId);

            await client.ConfigureServiceAsync(
                deployment.ProjectId,
                deployment.EnvironmentId,
                createResult.Database.ServiceId,
                deployment.Options,
                cancellationToken).ConfigureAwait(false);

            RailwayPostgresDatabaseDetails configuredDatabase = await client
                .WaitUntilReadyAsync(
                    deployment.ProjectId,
                    deployment.EnvironmentId,
                    createResult.Database.ServiceId,
                    RailwayPostgresReadinessPollingOptions.Default,
                    cancellationToken)
                .ConfigureAwait(false);

            createResult = new RailwayPostgresCreateFlowResult(configuredDatabase, createResult.Created);
        }

        ReportCreatedOrAdopted(progressReporter, resourceName, deployment, createResult);

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.RetrievingOutputs,
            $"Retrieved PostgreSQL connection outputs for Railway PostgreSQL service '{deployment.ServiceName}' with service id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(createResult.Database.ServiceId)}'.",
            resourceName,
            deployment.ServiceName,
            createResult.Database.ServiceId);

        return createResult;
    }

    private static async Task<RailwayPostgresResolvedDeployment> ResolveEnvironmentIdAsync(
        RailwayPostgresResolvedDeployment deployment,
        IRailwayPostgresManagementClient client,
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        string resolvedEnvironmentId = await client
            .ResolveEnvironmentIdAsync(deployment.ProjectId, deployment.EnvironmentId, cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(resolvedEnvironmentId, deployment.EnvironmentId, StringComparison.Ordinal))
        {
            return deployment;
        }

        Report(
            progressReporter,
            RailwayPostgresDeploymentPhase.ResolvingConfiguration,
            $"Resolved Railway environment '{deployment.EnvironmentId}' to id '{resolvedEnvironmentId}'.",
            resourceName,
            deployment.ServiceName,
            providerDatabaseId: null);

        return new RailwayPostgresResolvedDeployment(
            deployment.ServiceName,
            deployment.ProjectId,
            resolvedEnvironmentId,
            deployment.OwnershipMode,
            deployment.ManagementCredentials,
            deployment.Options);
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
                $"Creating Railway PostgreSQL service '{deployment.ServiceName}'.",
                resourceName,
                deployment.ServiceName,
                providerDatabaseId: null);
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
            $"{action} Railway PostgreSQL service '{deployment.ServiceName}' with service id '{RailwayPostgresDeploymentDiagnostics.FormatProviderDatabaseId(createResult.Database.ServiceId)}'.",
            resourceName,
            deployment.ServiceName,
            createResult.Database.ServiceId);
    }

    private static void Report(
        IRailwayPostgresDeploymentProgressReporter? progressReporter,
        RailwayPostgresDeploymentPhase phase,
        string message,
        string? resourceName,
        string? serviceName,
        string? providerDatabaseId)
    {
        progressReporter?.Report(RailwayPostgresDeploymentDiagnostics.CreateProgress(
            phase,
            message,
            resourceName,
            serviceName,
            providerDatabaseId));
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
