namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresProviderDeploymentOptions
{
    public RailwayPostgresProviderDeploymentOptions(
        RailwayPostgresProviderValue? platform,
        RailwayPostgresProviderValue? primaryRegion,
        IReadOnlyList<RailwayPostgresProviderValue>? readRegions,
        RailwayPostgresProviderValue? plan,
        RailwayPostgresProviderValue? budget,
        RailwayPostgresProviderValue? eviction,
        RailwayPostgresProviderValue? tls,
        IReadOnlySet<string> explicitSettings)
    {
        Platform = platform;
        PrimaryRegion = primaryRegion;
        ReadRegions = readRegions;
        Plan = plan;
        Budget = budget;
        Eviction = eviction;
        Tls = tls;
        ExplicitSettings = explicitSettings;
    }

    public RailwayPostgresProviderValue? Platform { get; }

    public RailwayPostgresProviderValue? PrimaryRegion { get; }

    public IReadOnlyList<RailwayPostgresProviderValue>? ReadRegions { get; }

    public RailwayPostgresProviderValue? Plan { get; }

    public RailwayPostgresProviderValue? Budget { get; }

    public RailwayPostgresProviderValue? Eviction { get; }

    public RailwayPostgresProviderValue? Tls { get; }

    public IReadOnlySet<string> ExplicitSettings { get; }
}
