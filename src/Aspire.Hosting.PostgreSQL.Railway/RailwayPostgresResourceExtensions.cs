using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Provides accessors for Railway PostgreSQL metadata attached to Aspire PostgreSQL server resources.
/// </summary>
public static class RailwayPostgresResourceExtensions
{
    /// <summary>
    /// Gets the supplementary app-facing outputs for a PostgreSQL server resource marked with <c>PublishToRailway</c>.
    /// </summary>
    /// <param name="resource">The PostgreSQL server resource.</param>
    /// <returns>The stable Railway PostgreSQL output references.</returns>
    [AspireExportIgnore(Reason = "TypeScript AppHosts should access outputs from the PostgreSQL server resource builder.")]
    public static RailwayPostgresOutputs GetRailwayPostgresOutputs(this PostgresServerResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs
            ?? throw new InvalidOperationException($"PostgreSQL server resource '{resource.Name}' has not been marked for Railway publishing.");
    }

    /// <summary>
    /// Gets the supplementary app-facing outputs for a PostgreSQL server resource builder marked with <c>publishToRailway</c>.
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <returns>The stable Railway PostgreSQL output references.</returns>
    [AspireExport("pinguapps.railway.postgres.getRailwayPostgresOutputs", MethodName = "getRailwayPostgresOutputs")]
    public static RailwayPostgresOutputs GetRailwayPostgresOutputsForTypeScript(this IResourceBuilder<PostgresServerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Resource.GetRailwayPostgresOutputs();
    }

    internal static RailwayPostgresDeploymentState? GetRailwayPostgresDeploymentState(this PostgresServerResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresDeploymentAnnotation>()
            .SingleOrDefault()
            ?.State;
    }

    internal static RailwayPostgresConnectionOutput ApplyRailwayPostgresConnectionOutput(
        this PostgresServerResource resource,
        RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(database);

        return ApplyRailwayPostgresConnectionOutput(resource.Annotations, database);
    }

    internal static RailwayPostgresConnectionOutput ApplyRailwayPostgresConnectionOutput(
        this PostgresDatabaseResource resource,
        RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(database);

        return ApplyRailwayPostgresConnectionOutput(resource.Annotations, database);
    }

    internal static RailwayPostgresOutputs? TryGetRailwayPostgresOutputs(this PostgresServerResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<RailwayPostgresOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs;
    }

    private static RailwayPostgresConnectionOutput ApplyRailwayPostgresConnectionOutput(
        ResourceAnnotationCollection annotations,
        RailwayPostgresDatabaseDetails database)
    {
        RailwayPostgresConnectionOutput output = new(database);

        RemoveExistingRailwayConnectionOutput(annotations);

        annotations.Add(new RailwayPostgresConnectionOutputAnnotation(output));
        annotations.Add(new ConnectionStringRedirectAnnotation(output));

        foreach (KeyValuePair<string, ReferenceExpression> property in output.GetConnectionProperties())
        {
            annotations.Add(new ConnectionPropertyAnnotation(property.Key, property.Value));
        }

        return output;
    }

    private static void RemoveExistingRailwayConnectionOutput(ResourceAnnotationCollection annotations)
    {
        for (int annotationIndex = 0; annotationIndex < annotations.Count; annotationIndex++)
        {
            if (annotations[annotationIndex] is not RailwayPostgresConnectionOutputAnnotation)
            {
                continue;
            }

            annotations.RemoveAt(annotationIndex);
            RemoveFollowingAnnotation<ConnectionStringRedirectAnnotation>(annotations, annotationIndex);
            RemoveFollowingConnectionProperty(annotations, annotationIndex, "Host");
            RemoveFollowingConnectionProperty(annotations, annotationIndex, "Port");
            RemoveFollowingConnectionProperty(annotations, annotationIndex, "Username");
            RemoveFollowingConnectionProperty(annotations, annotationIndex, "Password");
            RemoveFollowingConnectionProperty(annotations, annotationIndex, "Database");

            return;
        }
    }

    private static void RemoveFollowingAnnotation<TAnnotation>(ResourceAnnotationCollection annotations, int annotationIndex)
    {
        if (annotationIndex < annotations.Count
            && annotations[annotationIndex] is TAnnotation)
        {
            annotations.RemoveAt(annotationIndex);
        }
    }

    private static void RemoveFollowingConnectionProperty(
        ResourceAnnotationCollection annotations,
        int annotationIndex,
        string propertyName)
    {
        if (annotationIndex >= annotations.Count
            || annotations[annotationIndex] is not ConnectionPropertyAnnotation propertyAnnotation
            || !string.Equals(propertyAnnotation.Name, propertyName, StringComparison.Ordinal))
        {
            return;
        }

        annotations.RemoveAt(annotationIndex);
    }
}
