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

    public static RailwayPostgresDeploymentAnnotation GetRailwayAnnotation(RedisResource resource)
    {
        return Assert.Single(resource.Annotations.OfType<RailwayPostgresDeploymentAnnotation>());
    }

    public static RailwayPostgresDeploymentState GetRailwayState(RedisResource resource)
    {
        return resource.GetRailwayPostgresDeploymentState()
            ?? throw new InvalidOperationException("The Redis resource does not have Railway deployment state.");
    }

    public static bool HasRailwayState(RedisResource resource)
    {
        return resource.GetRailwayPostgresDeploymentState() is not null;
    }

    public static int GetPipelineStepCount(RedisResource resource)
    {
        return resource.Annotations.OfType<PipelineStepAnnotation>().Count();
    }
}
