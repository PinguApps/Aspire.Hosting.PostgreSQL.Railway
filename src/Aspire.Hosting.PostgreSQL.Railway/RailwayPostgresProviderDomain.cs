namespace Aspire.Hosting.PostgreSQL.Railway;

internal static class RailwayPostgresProviderDomain
{
    private static readonly IReadOnlyDictionary<RailwayPostgresRegion, RegionMetadata> _regions =
        new Dictionary<RailwayPostgresRegion, RegionMetadata>
        {
            [RailwayPostgresRegion.AwsUsEast1] = new("us-east-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsUsEast2] = new("us-east-2", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsUsWest1] = new("us-west-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsUsWest2] = new("us-west-2", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsCaCentral1] = new("ca-central-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsEuCentral1] = new("eu-central-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsEuWest1] = new("eu-west-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsEuWest2] = new("eu-west-2", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsSaEast1] = new("sa-east-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsApSouth1] = new("ap-south-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsApNortheast1] = new("ap-northeast-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsApSoutheast1] = new("ap-southeast-1", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsApSoutheast2] = new("ap-southeast-2", RailwayPostgresCloudPlatform.Aws, true),
            [RailwayPostgresRegion.AwsAfSouth1] = new("af-south-1", RailwayPostgresCloudPlatform.Aws, false),
            [RailwayPostgresRegion.GcpUsCentral1] = new("us-central1", RailwayPostgresCloudPlatform.Gcp, false),
            [RailwayPostgresRegion.GcpUsEast4] = new("us-east4", RailwayPostgresCloudPlatform.Gcp, false),
            [RailwayPostgresRegion.GcpEuropeWest1] = new("europe-west1", RailwayPostgresCloudPlatform.Gcp, false),
            [RailwayPostgresRegion.GcpAsiaNortheast1] = new("asia-northeast1", RailwayPostgresCloudPlatform.Gcp, false)
        };

    public static string MapCloudPlatform(RailwayPostgresCloudPlatform platform)
    {
        switch (platform)
        {
            case RailwayPostgresCloudPlatform.Aws:
                return "aws";
            case RailwayPostgresCloudPlatform.Gcp:
                return "gcp";
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, "The Railway PostgreSQL cloud platform is not supported.");
        }
    }

    public static string MapRegion(RailwayPostgresRegion region)
    {
        return GetRegion(region).ProviderValue;
    }

    public static string MapPlan(RailwayPostgresPlan plan)
    {
        switch (plan)
        {
            case RailwayPostgresPlan.Free:
                return "free";
            case RailwayPostgresPlan.PayAsYouGo:
                return "payg";
            case RailwayPostgresPlan.Fixed250Mb:
                return "fixed_250mb";
            case RailwayPostgresPlan.Fixed1Gb:
                return "fixed_1gb";
            case RailwayPostgresPlan.Fixed5Gb:
                return "fixed_5gb";
            case RailwayPostgresPlan.Fixed10Gb:
                return "fixed_10gb";
            case RailwayPostgresPlan.Fixed50Gb:
                return "fixed_50gb";
            case RailwayPostgresPlan.Fixed100Gb:
                return "fixed_100gb";
            case RailwayPostgresPlan.Fixed500Gb:
                return "fixed_500gb";
            default:
                throw new ArgumentOutOfRangeException(nameof(plan), plan, "The Railway PostgreSQL plan is not supported.");
        }
    }

    public static RailwayPostgresCloudPlatform ParseCloudPlatform(string value, string settingName)
    {
        switch (Normalize(value))
        {
            case "aws":
                return RailwayPostgresCloudPlatform.Aws;
            case "gcp":
                return RailwayPostgresCloudPlatform.Gcp;
            default:
                throw new InvalidOperationException(
                    $"Railway PostgreSQL {settingName} '{value}' is not supported. Supported values: aws, gcp.");
        }
    }

    public static RailwayPostgresRegion ParsePrimaryRegion(string value, string settingName)
    {
        string normalizedValue = Normalize(value);

        foreach ((RailwayPostgresRegion region, RegionMetadata metadata) in _regions)
        {
            if (StringComparer.Ordinal.Equals(metadata.ProviderValue, normalizedValue))
            {
                return region;
            }
        }

        throw new InvalidOperationException(
            $"Railway PostgreSQL {settingName} '{value}' is not a supported primary region.");
    }

    public static RailwayPostgresRegion ParseReadRegion(string value, string settingName)
    {
        RailwayPostgresRegion region = ParsePrimaryRegion(value, settingName);
        RegionMetadata metadata = GetRegion(region);

        return metadata.SupportsReadReplica
            ? region
            : throw new InvalidOperationException(
                $"Railway PostgreSQL {settingName} '{metadata.ProviderValue}' is not supported as a read region.");
    }

    public static RailwayPostgresPlan ParsePlan(string value, string settingName)
    {
        switch (Normalize(value))
        {
            case "free":
                return RailwayPostgresPlan.Free;
            case "payg":
                return RailwayPostgresPlan.PayAsYouGo;
            case "fixed_250mb":
                return RailwayPostgresPlan.Fixed250Mb;
            case "fixed_1gb":
                return RailwayPostgresPlan.Fixed1Gb;
            case "fixed_5gb":
                return RailwayPostgresPlan.Fixed5Gb;
            case "fixed_10gb":
                return RailwayPostgresPlan.Fixed10Gb;
            case "fixed_50gb":
                return RailwayPostgresPlan.Fixed50Gb;
            case "fixed_100gb":
                return RailwayPostgresPlan.Fixed100Gb;
            case "fixed_500gb":
                return RailwayPostgresPlan.Fixed500Gb;
            default:
                throw new InvalidOperationException(
                    $"Railway PostgreSQL {settingName} '{value}' is not supported. Supported values: free, payg, fixed_250mb, fixed_1gb, fixed_5gb, fixed_10gb, fixed_50gb, fixed_100gb, fixed_500gb.");
        }
    }

    public static int ParseBudget(string value, string settingName)
    {
        return int.TryParse(value, out int budget) && budget > 0
            ? budget
            : throw new InvalidOperationException($"Railway PostgreSQL {settingName} must be a positive integer.");
    }

    public static RailwayPostgresCloudPlatform GetCloudPlatform(RailwayPostgresRegion region)
    {
        return GetRegion(region).Platform;
    }

    public static bool SupportsReadReplica(RailwayPostgresRegion region)
    {
        return GetRegion(region).SupportsReadReplica;
    }

    private static RegionMetadata GetRegion(RailwayPostgresRegion region)
    {
        return _regions.TryGetValue(region, out RegionMetadata? metadata)
            ? metadata
            : throw new ArgumentOutOfRangeException(nameof(region), region, "The Railway PostgreSQL region is not supported.");
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed class RegionMetadata
    {
        public RegionMetadata(
            string providerValue,
            RailwayPostgresCloudPlatform platform,
            bool supportsReadReplica)
        {
            ProviderValue = providerValue;
            Platform = platform;
            SupportsReadReplica = supportsReadReplica;
        }

        public string ProviderValue { get; }

        public RailwayPostgresCloudPlatform Platform { get; }

        public bool SupportsReadReplica { get; }
    }
}
