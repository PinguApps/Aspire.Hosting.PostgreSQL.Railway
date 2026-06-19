using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresDeploymentAnnotation : IResourceAnnotation
{
    public RailwayPostgresDeploymentAnnotation(
        RailwayPostgresValue databaseName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresValue accountEmail,
        RailwayPostgresValue apiKey,
        RailwayPostgresDeploymentOptions options)
    {
        State = new RailwayPostgresDeploymentState(
            databaseName,
            ownershipMode,
            accountEmail,
            apiKey,
            options);
    }

    public RailwayPostgresDeploymentState State { get; }
}
