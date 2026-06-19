namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Optional Railway PostgreSQL deployment settings.
/// </summary>
public sealed class RailwayPostgresDeploymentOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RailwayPostgresDeploymentOptions"/> class.
    /// </summary>
    public RailwayPostgresDeploymentOptions()
    {
    }

    internal RailwayPostgresDeploymentOptions(RailwayPostgresDeploymentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
    }
}
