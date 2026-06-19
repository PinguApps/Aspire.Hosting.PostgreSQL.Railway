namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Describes the cloud platform used by an Railway PostgreSQL database.
/// </summary>
public enum RailwayPostgresCloudPlatform
{
    /// <summary>
    /// Amazon Web Services.
    /// </summary>
    [AspireValue("railwayPostgresCloudPlatform", Name = "aws")]
    Aws,

    /// <summary>
    /// Google Cloud Platform.
    /// </summary>
    [AspireValue("railwayPostgresCloudPlatform", Name = "gcp")]
    Gcp
}
