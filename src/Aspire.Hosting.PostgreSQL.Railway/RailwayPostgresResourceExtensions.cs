using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Provides accessors for Railway PostgreSQL metadata attached to Aspire Redis resources.
/// </summary>
public static class RailwayPostgresResourceExtensions
{
    /// <summary>
    /// Gets the supplementary app-facing outputs for a Redis resource marked with <c>PublishToRailway</c>.
    /// </summary>
    /// <param name="resource">The Redis resource.</param>
    /// <returns>The stable Railway PostgreSQL output references.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The Redis resource has not been marked for Railway publishing.</exception>
    [AspireExportIgnore(Reason = "TypeScript AppHosts should access outputs from the Redis resource builder.")]
    public static RailwayPostgresOutputs GetRailwayPostgresOutputs(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' has not been marked for Railway publishing.");
    }

    /// <summary>
    /// Gets the supplementary app-facing outputs for a Redis resource builder marked with <c>publishToRailway</c>.
    /// </summary>
    /// <param name="builder">The Redis resource builder.</param>
    /// <returns>The stable Railway PostgreSQL output references.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The Redis resource has not been marked for Railway publishing.</exception>
    [AspireExport("pinguapps.railway.postgres.getRailwayPostgresOutputs", MethodName = "getRailwayPostgresOutputs")]
    public static RailwayPostgresOutputs GetRailwayPostgresOutputsForTypeScript(this IResourceBuilder<RedisResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Resource.GetRailwayPostgresOutputs();
    }

    internal static RailwayPostgresDeploymentState? GetRailwayPostgresDeploymentState(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresDeploymentAnnotation>()
            .SingleOrDefault()
            ?.State;
    }

    internal static RailwayPostgresConnectionOutput ApplyRailwayPostgresConnectionOutput(
        this RedisResource resource,
        RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(database);

        RailwayPostgresConnectionOutput output = RailwayPostgresConnectionOutput.FromDatabase(database);

        RemoveExistingRailwayConnectionOutput(resource);

        resource.Annotations.Add(new RailwayPostgresConnectionOutputAnnotation(output));
        resource.Annotations.Add(new ConnectionStringRedirectAnnotation(output));

        foreach (KeyValuePair<string, ReferenceExpression> property in output.GetConnectionProperties())
        {
            resource.Annotations.Add(new ConnectionPropertyAnnotation(property.Key, property.Value));
        }

        return output;
    }

    private static void RemoveExistingRailwayConnectionOutput(RedisResource resource)
    {
        for (int annotationIndex = 0; annotationIndex < resource.Annotations.Count; annotationIndex++)
        {
            if (resource.Annotations[annotationIndex] is not RailwayPostgresConnectionOutputAnnotation)
            {
                continue;
            }

            resource.Annotations.RemoveAt(annotationIndex);
            RemoveFollowingAnnotation<ConnectionStringRedirectAnnotation>(resource, annotationIndex);
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Host");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Port");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Password");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Uri");

            return;
        }
    }

    private static void RemoveFollowingAnnotation<TAnnotation>(RedisResource resource, int annotationIndex)
    {
        if (annotationIndex < resource.Annotations.Count
            && resource.Annotations[annotationIndex] is TAnnotation)
        {
            resource.Annotations.RemoveAt(annotationIndex);
        }
    }

    private static void RemoveFollowingConnectionProperty(
        RedisResource resource,
        int annotationIndex,
        string propertyName)
    {
        if (annotationIndex >= resource.Annotations.Count
            || resource.Annotations[annotationIndex] is not ConnectionPropertyAnnotation propertyAnnotation
            || !string.Equals(propertyAnnotation.Name, propertyName, StringComparison.Ordinal))
        {
            return;
        }

        resource.Annotations.RemoveAt(annotationIndex);
    }

    internal static RailwayPostgresOutputs? TryGetRailwayPostgresOutputs(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs;
    }
}
