namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Railway PostgreSQL template used when creating a new service.
/// </summary>
public enum RailwayPostgresTemplate
{
    /// <summary>
    /// Railway's standard PostgreSQL template.
    /// </summary>
    Standard,

    /// <summary>
    /// Railway's PostgreSQL template with point-in-time recovery.
    /// </summary>
    PointInTimeRecovery,

    /// <summary>
    /// Railway's PostgreSQL template with PostGIS.
    /// </summary>
    PostGis,

    /// <summary>
    /// Railway's PostgreSQL template with pgvector.
    /// </summary>
    PgVector,

    /// <summary>
    /// Railway's TimescaleDB template.
    /// </summary>
    TimescaleDb,
}
