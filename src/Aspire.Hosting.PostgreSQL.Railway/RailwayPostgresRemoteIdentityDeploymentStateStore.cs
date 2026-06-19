#pragma warning disable ASPIREPIPELINES002

using System.Text.Json.Nodes;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityDeploymentStateStore
{
    private const string SectionPrefix = "Aspire.Hosting.PostgreSQL.Railway.RemoteIdentity";
    private const string DatabaseNameKey = "databaseName";
    private const string ProviderDatabaseIdKey = "providerDatabaseId";

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

        string? databaseName = section.Data.TryGetPropertyValue(DatabaseNameKey, out JsonNode? databaseNameValue)
            ? (string?)databaseNameValue
            : null;
        string? providerDatabaseId = section.Data.TryGetPropertyValue(ProviderDatabaseIdKey, out JsonNode? providerDatabaseIdValue)
            ? (string?)providerDatabaseIdValue
            : null;

        return string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(providerDatabaseId)
            ? null
            : new RailwayPostgresRemoteIdentityState(databaseName, providerDatabaseId);
    }

    public async Task SaveAsync(
        string resourceName,
        RailwayPostgresRemoteIdentityState identityState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identityState);

        DeploymentStateSection section =
            await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);

        section.Data[DatabaseNameKey] = identityState.DatabaseName;
        section.Data[ProviderDatabaseIdKey] = identityState.ProviderDatabaseId;

        await _stateManager.SaveSectionAsync(section, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSectionName(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return $"{SectionPrefix}.{resourceName}";
    }
}
