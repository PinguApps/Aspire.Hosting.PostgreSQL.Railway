using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresCreateFlow
{
    private readonly IRailwayPostgresManagementClient _client;
    private readonly RailwayPostgresReadinessPollingOptions _readinessPollingOptions;

    public RailwayPostgresCreateFlow(
        IRailwayPostgresManagementClient client,
        RailwayPostgresReadinessPollingOptions? readinessPollingOptions = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
        _readinessPollingOptions = readinessPollingOptions ?? RailwayPostgresReadinessPollingOptions.Default;
    }

    public async Task<RailwayPostgresCreateFlowResult> ExecuteAsync(
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresOwnershipResolutionResult ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(ownership);

        RailwayPostgresDatabaseDetails service;
        bool created;

        if (ownership.Action == RailwayPostgresOwnershipResolutionAction.Create)
        {
            service = await _client.CreateServiceAsync(
                new RailwayPostgresCreateServiceRequest(
                    deployment.ServiceName,
                    deployment.ProjectId,
                    deployment.EnvironmentId),
                cancellationToken).ConfigureAwait(false);
            created = true;
        }
        else
        {
            service = ownership.Database
                ?? throw new InvalidOperationException("Railway PostgreSQL ownership resolution selected adopt without a service.");
            created = false;
        }

        RailwayPostgresDatabaseDetails readyService = await _client
            .WaitUntilReadyAsync(
                deployment.ProjectId,
                deployment.EnvironmentId,
                service.ServiceId,
                _readinessPollingOptions,
                cancellationToken)
            .ConfigureAwait(false);

        ValidateService(deployment, readyService);

        return new RailwayPostgresCreateFlowResult(readyService, created);
    }

    private static void ValidateService(
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresDatabaseDetails service)
    {
        if (service.ServiceName != deployment.ServiceName)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned service '{service.ServiceId}' with name '{service.ServiceName}', not configured name '{deployment.ServiceName}'.");
        }

        if (!service.HasConnectionVariables)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL service '{service.ServiceId}' did not expose complete PostgreSQL connection variables.");
        }
    }
}
