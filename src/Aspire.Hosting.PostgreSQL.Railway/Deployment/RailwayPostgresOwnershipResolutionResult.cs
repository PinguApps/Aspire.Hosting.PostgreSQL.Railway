using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresOwnershipResolutionResult
{
    private RailwayPostgresOwnershipResolutionResult(
        RailwayPostgresOwnershipResolutionAction action,
        RailwayPostgresDatabaseDetails? database)
    {
        Action = action;
        Database = database;
    }

    public RailwayPostgresOwnershipResolutionAction Action { get; }

    public RailwayPostgresDatabaseDetails? Database { get; }

    public static RailwayPostgresOwnershipResolutionResult Create()
    {
        return new(RailwayPostgresOwnershipResolutionAction.Create, database: null);
    }

    public static RailwayPostgresOwnershipResolutionResult Adopt(RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return new(RailwayPostgresOwnershipResolutionAction.Adopt, database);
    }
}
