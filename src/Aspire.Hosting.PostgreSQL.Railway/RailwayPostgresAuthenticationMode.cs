namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Authentication used for Railway management requests during deployment.
/// </summary>
public enum RailwayPostgresAuthenticationMode
{
    /// <summary>Account or workspace token sent as a Bearer token.</summary>
    Bearer,

    /// <summary>Environment-scoped project token sent in the Project-Access-Token header.</summary>
    ProjectToken,
}
