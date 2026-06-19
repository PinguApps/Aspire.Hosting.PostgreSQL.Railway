namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityState
{
    public RailwayPostgresRemoteIdentityState(string databaseName, string providerDatabaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerDatabaseId);

        DatabaseName = databaseName;
        ProviderDatabaseId = providerDatabaseId;
    }

    public string DatabaseName { get; }

    public string ProviderDatabaseId { get; }
}
