namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Describes an Railway PostgreSQL plan accepted by the management API.
/// </summary>
public enum RailwayPostgresPlan
{
    /// <summary>
    /// Railway free plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "free")]
    Free,

    /// <summary>
    /// Railway pay-as-you-go plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "payAsYouGo")]
    PayAsYouGo,

    /// <summary>
    /// Fixed 250 MB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed250Mb")]
    Fixed250Mb,

    /// <summary>
    /// Fixed 1 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed1Gb")]
    Fixed1Gb,

    /// <summary>
    /// Fixed 5 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed5Gb")]
    Fixed5Gb,

    /// <summary>
    /// Fixed 10 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed10Gb")]
    Fixed10Gb,

    /// <summary>
    /// Fixed 50 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed50Gb")]
    Fixed50Gb,

    /// <summary>
    /// Fixed 100 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed100Gb")]
    Fixed100Gb,

    /// <summary>
    /// Fixed 500 GB plan.
    /// </summary>
    [AspireValue("railwayPostgresPlan", Name = "fixed500Gb")]
    Fixed500Gb
}
