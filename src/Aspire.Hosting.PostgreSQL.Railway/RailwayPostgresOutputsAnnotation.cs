using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresOutputsAnnotation : IResourceAnnotation
{
    public RailwayPostgresOutputsAnnotation(RailwayPostgresOutputs outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        Outputs = outputs;
    }

    public RailwayPostgresOutputs Outputs { get; }
}
