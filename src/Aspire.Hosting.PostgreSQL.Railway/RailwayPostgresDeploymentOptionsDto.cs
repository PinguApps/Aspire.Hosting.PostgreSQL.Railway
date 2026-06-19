namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// TypeScript-friendly Railway PostgreSQL deployment options.
/// </summary>
[AspireDto]
public sealed class RailwayPostgresDeploymentOptionsDto
{
    /// <summary>
    /// Gets or sets how deployment should treat a database with the requested name.
    /// </summary>
    public RailwayPostgresOwnershipMode? OwnershipMode { get; set; }

    internal RailwayPostgresOwnershipMode GetOwnershipMode()
    {
        return OwnershipMode ?? RailwayPostgresOwnershipMode.CreateOrAdopt;
    }

    internal RailwayPostgresDeploymentOptions ToDeploymentOptions()
    {
        return new RailwayPostgresDeploymentOptions();
    }
}
