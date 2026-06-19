namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Stable names for supplementary Railway PostgreSQL app-facing outputs.
/// </summary>
public static class RailwayPostgresOutputNames
{
    /// <summary>The Railway PostgreSQL host endpoint.</summary>
    public const string Endpoint = "Endpoint";

    /// <summary>The Railway PostgreSQL port.</summary>
    public const string Port = "Port";

    /// <summary>The Railway PostgreSQL password.</summary>
    public const string Password = "Password";

    /// <summary>Whether TLS is enabled for the Railway PostgreSQL endpoint.</summary>
    public const string Tls = "Tls";

    /// <summary>The Railway PostgreSQL database name.</summary>
    public const string DatabaseName = "DatabaseName";
}
