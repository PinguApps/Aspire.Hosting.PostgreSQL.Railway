using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresRemoteIdentityResolution
{
    private RailwayPostgresRemoteIdentityResolution(
        RailwayPostgresDatabaseDetails? database,
        RailwayPostgresRemoteIdentityState? identityState,
        bool resolvedFromCachedIdentity)
    {
        Database = database;
        IdentityState = identityState;
        ResolvedFromCachedIdentity = resolvedFromCachedIdentity;
    }

    public RailwayPostgresDatabaseDetails? Database { get; }

    public RailwayPostgresRemoteIdentityState? IdentityState { get; }

    public bool Found => Database is not null;

    public bool ResolvedFromCachedIdentity { get; }

    public static RailwayPostgresRemoteIdentityResolution NotFound() => new(null, null, resolvedFromCachedIdentity: false);

    public static RailwayPostgresRemoteIdentityResolution FoundDatabase(
        RailwayPostgresDatabaseDetails database,
        bool resolvedFromCachedIdentity = false)
    {
        ArgumentNullException.ThrowIfNull(database);

        return new(
            database,
            new RailwayPostgresRemoteIdentityState(database.ProjectId, database.ServiceName, database.ServiceId),
            resolvedFromCachedIdentity);
    }
}
