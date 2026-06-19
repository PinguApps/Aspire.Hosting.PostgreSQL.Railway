using System.Text.Json.Serialization;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresChangePlanRequest
{
    [JsonPropertyName("plan_name")]
    public required string PlanName { get; init; }
}
