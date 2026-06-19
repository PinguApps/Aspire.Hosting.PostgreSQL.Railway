namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Describes an Railway PostgreSQL global database region.
/// </summary>
public enum RailwayPostgresRegion
{
    /// <summary>
    /// AWS US East, N. Virginia.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsUsEast1")]
    AwsUsEast1,

    /// <summary>
    /// AWS US East, Ohio.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsUsEast2")]
    AwsUsEast2,

    /// <summary>
    /// AWS US West, N. California.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsUsWest1")]
    AwsUsWest1,

    /// <summary>
    /// AWS US West, Oregon.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsUsWest2")]
    AwsUsWest2,

    /// <summary>
    /// AWS Canada Central.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsCaCentral1")]
    AwsCaCentral1,

    /// <summary>
    /// AWS Europe, Frankfurt.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsEuCentral1")]
    AwsEuCentral1,

    /// <summary>
    /// AWS Europe, Ireland.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsEuWest1")]
    AwsEuWest1,

    /// <summary>
    /// AWS Europe, London.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsEuWest2")]
    AwsEuWest2,

    /// <summary>
    /// AWS South America, Sao Paulo.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsSaEast1")]
    AwsSaEast1,

    /// <summary>
    /// AWS Asia Pacific, Mumbai.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsApSouth1")]
    AwsApSouth1,

    /// <summary>
    /// AWS Asia Pacific, Tokyo.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsApNortheast1")]
    AwsApNortheast1,

    /// <summary>
    /// AWS Asia Pacific, Singapore.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsApSoutheast1")]
    AwsApSoutheast1,

    /// <summary>
    /// AWS Asia Pacific, Sydney.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsApSoutheast2")]
    AwsApSoutheast2,

    /// <summary>
    /// AWS Africa, Cape Town.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "awsAfSouth1")]
    AwsAfSouth1,

    /// <summary>
    /// Google Cloud Iowa.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "gcpUsCentral1")]
    GcpUsCentral1,

    /// <summary>
    /// Google Cloud Virginia.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "gcpUsEast4")]
    GcpUsEast4,

    /// <summary>
    /// Google Cloud Belgium.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "gcpEuropeWest1")]
    GcpEuropeWest1,

    /// <summary>
    /// Google Cloud Tokyo.
    /// </summary>
    [AspireValue("railwayPostgresRegion", Name = "gcpAsiaNortheast1")]
    GcpAsiaNortheast1
}
