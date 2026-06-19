using System.Text.Json.Serialization;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresUpdateBudgetRequest
{
    [JsonPropertyName("budget")]
    public required int Budget { get; init; }
}
