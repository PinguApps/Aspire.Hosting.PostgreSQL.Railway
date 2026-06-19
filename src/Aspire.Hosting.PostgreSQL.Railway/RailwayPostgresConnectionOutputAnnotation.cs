using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresConnectionOutputAnnotation : IResourceAnnotation
{
    public RailwayPostgresConnectionOutputAnnotation(RailwayPostgresConnectionOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        Output = output;
    }

    public RailwayPostgresConnectionOutput Output { get; }
}
