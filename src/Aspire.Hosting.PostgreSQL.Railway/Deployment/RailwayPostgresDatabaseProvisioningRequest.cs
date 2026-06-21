namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresDatabaseProvisioningRequest
{
    public RailwayPostgresDatabaseProvisioningRequest(string databaseName, string? creationScript)
    {
        DatabaseName = databaseName;
        CreationScript = creationScript;
    }

    public string DatabaseName { get; }

    public string? CreationScript { get; }
}
