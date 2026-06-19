namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Describes how deployment should treat an Railway PostgreSQL database with the requested name.
/// </summary>
public enum RailwayPostgresOwnershipMode
{
    /// <summary>
    /// Deployment may create a missing database or adopt an existing database with the requested name.
    /// </summary>
    [AspireValue("railwayPostgresOwnershipMode", Name = "createOrAdopt")]
    CreateOrAdopt,

    /// <summary>
    /// Deployment must create a new database and fail if one already exists.
    /// </summary>
    [AspireValue("railwayPostgresOwnershipMode", Name = "createOnly")]
    CreateOnly,

    /// <summary>
    /// Deployment must use an existing database and fail if it cannot be found.
    /// </summary>
    [AspireValue("railwayPostgresOwnershipMode", Name = "existingOnly")]
    ExistingOnly
}
