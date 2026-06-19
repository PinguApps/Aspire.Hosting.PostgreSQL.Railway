using System.Text.Json.Serialization;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresUpdateRegionsRequest
{
    [JsonPropertyName("read_regions")]
    public required IReadOnlyList<string> ReadRegions { get; init; }
}
