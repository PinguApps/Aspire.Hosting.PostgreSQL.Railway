#pragma warning disable ASPIREPIPELINES002

using System.Text.Json.Nodes;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityDeploymentStateStore
{
    private const string SectionPrefix = "Aspire.Hosting.PostgreSQL.Railway.RemoteIdentity";
    private const string ProjectIdKey = "projectId";
    private const string ServiceNameKey = "serviceName";
    private const string ServiceIdKey = "serviceId";

    private readonly IDeploymentStateManager _stateManager;

    public RailwayPostgresRemoteIdentityDeploymentStateStore(IDeploymentStateManager stateManager)
    {
        ArgumentNullException.ThrowIfNull(stateManager);

        _stateManager = stateManager;
    }

    public async Task<RailwayPostgresRemoteIdentityState?> LoadAsync(
        string resourceName,
        string projectId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        DeploymentStateSection section =
            await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);

        string? storedProjectId = section.Data.TryGetPropertyValue(ProjectIdKey, out JsonNode? projectIdValue)
            ? (string?)projectIdValue
            : null;
        string? serviceName = section.Data.TryGetPropertyValue(ServiceNameKey, out JsonNode? serviceNameValue)
            ? (string?)serviceNameValue
            : null;
        string? serviceId = section.Data.TryGetPropertyValue(ServiceIdKey, out JsonNode? serviceIdValue)
            ? (string?)serviceIdValue
            : null;

        return string.IsNullOrWhiteSpace(storedProjectId)
            || !string.Equals(storedProjectId, projectId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(serviceName)
            || string.IsNullOrWhiteSpace(serviceId)
            ? null
            : new RailwayPostgresRemoteIdentityState(storedProjectId, serviceName, serviceId);
    }

    public async Task SaveAsync(
        string resourceName,
        string projectId,
        RailwayPostgresRemoteIdentityState identityState,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(identityState);

        DeploymentStateSection section =
            await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);

        section.Data[ProjectIdKey] = projectId;
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
