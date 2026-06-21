using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresCreateFlowResult
{
    public RailwayPostgresCreateFlowResult(RailwayPostgresDatabaseDetails database, bool created)
        : this(database, created, template: null)
    {
    }

    public RailwayPostgresCreateFlowResult(
        RailwayPostgresDatabaseDetails database,
        bool created,
        RailwayPostgresTemplate? template)
    {
        ArgumentNullException.ThrowIfNull(database);

        Database = database;
        Created = created;
        Template = template;
    }

    public RailwayPostgresDatabaseDetails Database { get; }

    public bool Created { get; }

    public RailwayPostgresTemplate? Template { get; }

    public RailwayPostgresRemoteIdentityState RemoteIdentity =>
        new(Database.ProjectId, Database.ServiceName, Database.ServiceId, Template);
}
