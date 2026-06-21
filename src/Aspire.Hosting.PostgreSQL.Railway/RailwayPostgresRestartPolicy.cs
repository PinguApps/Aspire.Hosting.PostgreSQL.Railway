namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Railway restart policies for the PostgreSQL service.
/// </summary>
public enum RailwayPostgresRestartPolicy
{
    /// <summary>
    /// Restart the service whenever it stops.
    /// </summary>
    Always,

    /// <summary>
    /// Restart the service only when it exits with an error.
    /// </summary>
    OnFailure,

    /// <summary>
    /// Do not automatically restart the service.
    /// </summary>
    Never,
}
