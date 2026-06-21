#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal static class AspireModelInspector
{
    public static bool IsExcludedFromPublish(IResource resource)
    {
        return resource.IsExcludedFromPublish();
    }

    public static RailwayPostgresDeploymentAnnotation GetRailwayAnnotation(PostgresServerResource resource)
    {
        return Assert.Single(resource.Annotations.OfType<RailwayPostgresDeploymentAnnotation>());
    }

    public static RailwayPostgresDeploymentState GetRailwayState(PostgresServerResource resource)
    {
        return resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException("The PostgreSQL resource does not have Railway deployment state.");
    }

    public static bool HasRailwayState(PostgresServerResource resource)
    {
        return resource.GetRailwayPostgresDeploymentState() is not null;
    }

    public static int GetPipelineStepCount(PostgresServerResource resource)
    {
        return resource.Annotations.OfType<PipelineStepAnnotation>().Count();
    }
}
