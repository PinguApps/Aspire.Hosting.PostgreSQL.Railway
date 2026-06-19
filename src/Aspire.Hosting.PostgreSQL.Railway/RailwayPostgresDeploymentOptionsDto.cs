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

    /// <summary>
    /// Gets or sets the Railway platform or cloud provider.
    /// </summary>
    public RailwayPostgresCloudPlatform? Platform { get; set; }

    /// <summary>
    /// Gets or sets the primary Railway region.
    /// </summary>
    public RailwayPostgresRegion? PrimaryRegion { get; set; }

    /// <summary>
    /// Gets or sets optional read regions.
    /// </summary>
    public IReadOnlyList<RailwayPostgresRegion>? ReadRegions { get; set; }

    /// <summary>
    /// Gets or sets the Railway plan.
    /// </summary>
    public RailwayPostgresPlan? Plan { get; set; }

    /// <summary>
    /// Gets or sets the monthly Railway budget.
    /// </summary>
    public int? Budget { get; set; }

    /// <summary>
    /// Gets or sets whether eviction is enabled.
    /// </summary>
    public bool? Eviction { get; set; }

    /// <summary>
    /// Gets or sets whether TLS should be enabled.
    /// </summary>
    public bool? Tls { get; set; }

    internal RailwayPostgresOwnershipMode GetOwnershipMode()
    {
        return OwnershipMode ?? RailwayPostgresOwnershipMode.CreateOrAdopt;
    }

    internal RailwayPostgresDeploymentOptions ToDeploymentOptions()
    {
        RailwayPostgresDeploymentOptions options = new();

        if (Platform is not null)
        {
            options.SetPlatform(Platform.Value);
        }

        if (PrimaryRegion is not null)
        {
            options.SetPrimaryRegion(PrimaryRegion.Value);
        }

        if (ReadRegions is not null)
        {
            options.SetReadRegions([.. ReadRegions]);
        }

        if (Plan is not null)
        {
            options.SetPlan(Plan.Value);
        }

        if (Budget is not null)
        {
            options.SetBudget(Budget.Value);
        }

        if (Eviction is not null)
        {
            options.Eviction = Eviction;
        }

        if (Tls is not null)
        {
            options.Tls = Tls;
        }

        return options;
    }
}
