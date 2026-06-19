#pragma warning disable ASPIREPIPELINES002

using System.Text.Json.Nodes;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityDeploymentStateStore
{
    private const string SectionPrefix = "Aspire.Hosting.PostgreSQL.Railway.RemoteIdentity";
    private const string ServiceNameKey = "serviceName";
    private const string ServiceIdKey = "serviceId";

    private readonly IDeploymentStateManager _stateManager;

    public RailwayPostgresRemoteIdentityDeploymentStateStore(IDeploymentStateManager stateManager)
    {
        ArgumentNullException.ThrowIfNull(stateManager);

        _stateManager = stateManager;
    }

    public async Task<RailwayPostgresRemoteIdentityState?> LoadAsync(string resourceName, CancellationToken cancellationToken)
    {
        DeploymentStateSection section =
            await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);

        string? serviceName = section.Data.TryGetPropertyValue(ServiceNameKey, out JsonNode? serviceNameValue)
            ? (string?)serviceNameValue
            : null;
        string? serviceId = section.Data.TryGetPropertyValue(ServiceIdKey, out JsonNode? serviceIdValue)
            ? (string?)serviceIdValue
            : null;

        return string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(serviceId)
            ? null
            : new RailwayPostgresRemoteIdentityState(serviceName, serviceId);
    }

    public async Task SaveAsync(
        string resourceName,
        RailwayPostgresRemoteIdentityState identityState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identityState);

        DeploymentStateSection section =
            await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);

        section.Data[ServiceNameKey] = identityState.ServiceName;
        section.Data[ServiceIdKey] = identityState.ServiceId;

        await _stateManager.SaveSectionAsync(section, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSectionName(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return $"{SectionPrefix}.{resourceName}";
    }
}
