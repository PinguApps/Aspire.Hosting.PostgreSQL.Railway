using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresDeploymentAnnotation : IResourceAnnotation
{
    public RailwayPostgresDeploymentAnnotation(
        RailwayPostgresValue serviceName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresValue projectId,
        RailwayPostgresValue environmentId,
        RailwayPostgresValue apiToken,
        RailwayPostgresDeploymentOptions options)
    {
        State = new RailwayPostgresDeploymentState(
            serviceName,
            ownershipMode,
            projectId,
            environmentId,
            apiToken,
            options);
    }

    public RailwayPostgresDeploymentState State { get; }
}
