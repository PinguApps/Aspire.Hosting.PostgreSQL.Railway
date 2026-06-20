using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Provides Railway PostgreSQL-aware reference extensions for consuming resources.
/// </summary>
public static class RailwayPostgresReferenceBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    /// <summary>
    /// Injects a PostgreSQL connection string into the destination resource, using Railway outputs when the database has been marked for Railway publishing.
    /// </summary>
    [AspireExportIgnore(Reason = "C# overload shadows Aspire's standard WithReference for Railway-aware PostgreSQL database references.")]
    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<PostgresDatabaseResource> source,
        string? connectionName = null,
        bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        IResourceWithConnectionString publishConnectionResource = GetPublishConnectionResource(source.Resource);

        return builder.WithRailwayPostgresConnectionReference(
            source.Resource,
            source.Resource,
            publishConnectionResource,
            connectionName,
            optional);
    }

    /// <summary>
    /// Injects a PostgreSQL server connection string into the destination resource, using Railway outputs when the server has been marked for Railway publishing.
    /// </summary>
    [AspireExportIgnore(Reason = "C# overload shadows Aspire's standard WithReference for Railway-aware PostgreSQL server references.")]
    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<PostgresServerResource> source,
        string? connectionName = null,
        bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        IResourceWithConnectionString publishConnectionResource = GetPublishConnectionResource(source.Resource);

        return builder.WithRailwayPostgresConnectionReference(
            source.Resource,
            source.Resource,
            publishConnectionResource,
            connectionName,
            optional);
    }

    private static IResourceBuilder<TDestination> WithRailwayPostgresConnectionReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceWithConnectionString relationshipResource,
        IResourceWithConnectionString localConnectionResource,
        IResourceWithConnectionString publishConnectionResource,
        string? connectionName,
        bool optional)
        where TDestination : IResourceWithEnvironment
    {
        connectionName ??= relationshipResource.Name;

        builder.WithReferenceRelationship(relationshipResource);
        builder.Resource.TryGetLastAnnotation<ReferenceEnvironmentInjectionAnnotation>(out ReferenceEnvironmentInjectionAnnotation? annotation);
        ReferenceEnvironmentInjectionFlags flags = annotation?.Flags ?? ReferenceEnvironmentInjectionFlags.All;

        return builder.WithEnvironment(context =>
        {
            IResourceWithConnectionString connectionResource = context.ExecutionContext.Operation == DistributedApplicationOperation.Run
                ? localConnectionResource
                : publishConnectionResource;

            if (flags.HasFlag(ReferenceEnvironmentInjectionFlags.ConnectionString))
            {
                string key = connectionResource.ConnectionStringEnvironmentVariable ?? $"{ConnectionStringEnvironmentName}{connectionName}";
                context.EnvironmentVariables[key] = new ConnectionStringReference(connectionResource, optional);
            }

            if (flags.HasFlag(ReferenceEnvironmentInjectionFlags.ConnectionProperties))
            {
                string prefix = connectionName.Length == 0 ? string.Empty : $"{EncodeEnvironmentName(connectionName)}_";
                SplatConnectionProperties(connectionResource, prefix, context);
            }
        });
    }

    private static IResourceWithConnectionString GetPublishConnectionResource(PostgresServerResource resource)
    {
        RailwayPostgresOutputs? outputs = resource.TryGetRailwayPostgresOutputs();

        return outputs is null
            ? resource
            : RailwayPostgresReferenceConnectionOutput.ForServer(outputs);
    }

    private static IResourceWithConnectionString GetPublishConnectionResource(PostgresDatabaseResource resource)
    {
        RailwayPostgresOutputs? outputs = resource.Parent.TryGetRailwayPostgresOutputs();

        return outputs is null
            ? resource
            : RailwayPostgresReferenceConnectionOutput.ForDatabase(outputs, resource.DatabaseName);
    }

    private static void SplatConnectionProperties(
        IResourceWithConnectionString resource,
        string prefix,
        EnvironmentCallbackContext context)
    {
        foreach (KeyValuePair<string, ReferenceExpression> connectionProperty in resource.GetConnectionProperties())
        {
            context.EnvironmentVariables[prefix + connectionProperty.Key.ToUpperInvariant()] = connectionProperty.Value;
        }

        if (resource.TryGetAnnotationsOfType(out IEnumerable<ConnectionPropertyAnnotation>? propertyAnnotations))
        {
            foreach (ConnectionPropertyAnnotation propertyAnnotation in propertyAnnotations)
            {
                context.EnvironmentVariables[prefix + propertyAnnotation.Name.ToUpperInvariant()] = propertyAnnotation.Value;
            }
        }
    }

    private static string EncodeEnvironmentName(string value)
    {
        char[] characters = new char[value.Length];

        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            characters[i] = char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_';
        }

        return new string(characters);
    }
}
