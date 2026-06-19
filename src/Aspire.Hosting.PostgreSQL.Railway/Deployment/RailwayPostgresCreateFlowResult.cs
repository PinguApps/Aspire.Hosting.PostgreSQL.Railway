using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresCreateFlowResult
{
    public RailwayPostgresCreateFlowResult(RailwayPostgresDatabaseDetails database, bool created)
    {
        ArgumentNullException.ThrowIfNull(database);

        Database = database;
        Created = created;
    }

    public RailwayPostgresDatabaseDetails Database { get; }

    public bool Created { get; }

    public RailwayPostgresRemoteIdentityState RemoteIdentity =>
        new(Database.DatabaseName, Database.DatabaseId);
}
