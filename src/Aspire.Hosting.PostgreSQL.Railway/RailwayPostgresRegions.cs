namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Railway deployment regions for PostgreSQL services.
/// </summary>
public enum RailwayPostgresRegions
{
    /// <summary>US West Metal, California, USA.</summary>
    UsWestMetal,

    /// <summary>US East Metal, Virginia, USA.</summary>
    UsEastMetal,

    /// <summary>EU West Metal, Amsterdam, Netherlands.</summary>
    EuWestMetal,

    /// <summary>Southeast Asia Metal, Singapore.</summary>
    SoutheastAsiaMetal,
}

internal static class RailwayPostgresRegionExtensions
{
    public static string ToRailwayIdentifier(this RailwayPostgresRegions region)
    {
        if (region == RailwayPostgresRegions.UsWestMetal)
        {
            return "us-west2";
        }

        if (region == RailwayPostgresRegions.UsEastMetal)
        {
            return "us-east4-eqdc4a";
        }

        if (region == RailwayPostgresRegions.EuWestMetal)
        {
            return "europe-west4-drams3a";
        }

        if (region == RailwayPostgresRegions.SoutheastAsiaMetal)
        {
            return "asia-southeast1-eqsg3a";
        }

        throw new InvalidOperationException("Railway PostgreSQL region is not supported.");
    }
}
