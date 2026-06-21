namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresDeploymentProgress
{
    public RailwayPostgresDeploymentProgress(
        RailwayPostgresDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Phase = phase;
        Message = message;
        ResourceName = resourceName;
        DatabaseName = databaseName;
        ProviderDatabaseId = providerDatabaseId;
    }

    public RailwayPostgresDeploymentPhase Phase { get; }

    public string Message { get; }

    public string? ResourceName { get; }

    public string? DatabaseName { get; }

    public string? ProviderDatabaseId { get; }
}
