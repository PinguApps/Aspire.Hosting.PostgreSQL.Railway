using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresConnectionOutputAnnotation : IResourceAnnotation
{
    public RailwayPostgresConnectionOutputAnnotation(IResourceWithConnectionString output)
    {
        ArgumentNullException.ThrowIfNull(output);

        Output = output;
    }

    public IResourceWithConnectionString Output { get; }
}
