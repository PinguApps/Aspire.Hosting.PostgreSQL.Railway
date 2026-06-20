#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Provides Railway PostgreSQL publishing extensions for Aspire PostgreSQL server resources.
/// </summary>
public static class RailwayPostgresBuilderExtensions
{
    /// <summary>
    /// Provides Railway PostgreSQL publishing extensions for a standard Aspire PostgreSQL server resource.
    /// </summary>
    /// <param name="builder">The existing PostgreSQL server resource builder returned from <c>AddPostgres</c>.</param>
    extension(IResourceBuilder<PostgresServerResource> builder)
    {
        /// <summary>
        /// Marks a standard Aspire PostgreSQL server resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="serviceName">The explicit remote Railway PostgreSQL service name.</param>
        /// <param name="projectId">The Railway project id parameter.</param>
        /// <param name="environmentId">The Railway environment id or exact environment name parameter.</param>
        /// <param name="apiToken">The infrastructure-only Railway API token parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote service.</param>
        /// <param name="configure">Optional Railway PostgreSQL deployment settings.</param>
        /// <returns>The same PostgreSQL server resource builder for normal Aspire chaining.</returns>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<PostgresServerResource> PublishToRailway(
            IResourceBuilder<ParameterResource> serviceName,
            IResourceBuilder<ParameterResource> projectId,
            IResourceBuilder<ParameterResource> environmentId,
            IResourceBuilder<ParameterResource> apiToken,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(serviceName);

            return builder.PublishToRailway(
                RailwayPostgresValue.FromParameter(serviceName),
                projectId,
                environmentId,
                apiToken,
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire PostgreSQL server resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="serviceName">The explicit remote Railway PostgreSQL service name.</param>
        /// <param name="projectId">The Railway project id parameter.</param>
        /// <param name="environmentId">The Railway environment id or exact environment name parameter.</param>
        /// <param name="apiToken">The infrastructure-only Railway API token parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote service.</param>
        /// <param name="configure">Optional Railway PostgreSQL deployment settings.</param>
        /// <returns>The same PostgreSQL server resource builder for normal Aspire chaining.</returns>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<PostgresServerResource> PublishToRailway(
            RailwayPostgresValue serviceName,
            IResourceBuilder<ParameterResource> projectId,
            IResourceBuilder<ParameterResource> environmentId,
            IResourceBuilder<ParameterResource> apiToken,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(projectId);
            ArgumentNullException.ThrowIfNull(environmentId);
            ArgumentNullException.ThrowIfNull(apiToken);

            return builder.PublishToRailway(
                serviceName,
                RailwayPostgresValue.FromParameter(projectId),
                RailwayPostgresValue.FromParameter(environmentId),
                RailwayPostgresValue.FromParameter(apiToken),
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire PostgreSQL server resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="serviceName">The explicit remote Railway PostgreSQL service name.</param>
        /// <param name="projectId">The Railway project id value.</param>
        /// <param name="environmentId">The Railway environment id or exact environment name value.</param>
        /// <param name="apiToken">The infrastructure-only Railway API token value.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote service.</param>
        /// <param name="configure">Optional Railway PostgreSQL deployment settings.</param>
        /// <returns>The same PostgreSQL server resource builder for normal Aspire chaining.</returns>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<PostgresServerResource> PublishToRailway(
            RailwayPostgresValue serviceName,
            RailwayPostgresValue projectId,
            RailwayPostgresValue environmentId,
            RailwayPostgresValue apiToken,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(serviceName);
            ArgumentNullException.ThrowIfNull(projectId);
            ArgumentNullException.ThrowIfNull(environmentId);
            ArgumentNullException.ThrowIfNull(apiToken);

            if (!Enum.IsDefined(ownershipMode))
            {
                throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
            }

            RailwayPostgresDeploymentOptions options = new();
            configure?.Invoke(options);
            options.Validate();

            RemoveExistingRailwayPipelineStep(builder.Resource);
            global::Aspire.Hosting.ResourceBuilderExtensions.ExcludeFromManifest(builder);

            builder.WithAnnotation(
                new RailwayPostgresDeploymentAnnotation(
                    serviceName,
                    ownershipMode,
                    projectId,
                    environmentId,
                    apiToken,
                    options),
                ResourceAnnotationMutationBehavior.Replace);

            RailwayPostgresOutputs outputs = new(builder.Resource);

            builder.WithAnnotation(
                new RailwayPostgresOutputsAnnotation(outputs),
                ResourceAnnotationMutationBehavior.Replace);

            PostgresServerResource resource = builder.Resource;

            return builder.WithPipelineStepFactory(
                $"railway-postgres-{builder.Resource.Name}",
                context => RailwayPostgresDeploymentPipeline.ExecuteAsync(resource, context),
                dependsOn: [WellKnownPipelineSteps.DeployPrereq],
                requiredBy: [WellKnownPipelineSteps.PushPrereq],
                tags: [WellKnownPipelineTags.ProvisionInfrastructure],
                description: "Provision or reconcile the Railway PostgreSQL service.");
        }
    }

    /// <summary>
    /// Marks a standard Aspire PostgreSQL server resource for Railway PostgreSQL deployment from a TypeScript AppHost.
    /// </summary>
    /// <param name="builder">The existing PostgreSQL server resource builder returned from <c>AddPostgres</c>.</param>
    /// <param name="serviceName">The explicit remote Railway PostgreSQL service name parameter.</param>
    /// <param name="projectId">The Railway project id parameter.</param>
    /// <param name="environmentId">The Railway environment id or exact environment name parameter.</param>
    /// <param name="apiToken">The infrastructure-only Railway API token parameter.</param>
    /// <param name="options">Optional Railway PostgreSQL deployment settings.</param>
    /// <returns>The same PostgreSQL server resource builder for normal Aspire chaining.</returns>
    [AspireExport("pinguapps.railway.postgres.publishToRailway", MethodName = "publishToRailway")]
    public static IResourceBuilder<PostgresServerResource> PublishToRailwayForTypeScript(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource> serviceName,
        IResourceBuilder<ParameterResource> projectId,
        IResourceBuilder<ParameterResource> environmentId,
        IResourceBuilder<ParameterResource> apiToken,
        RailwayPostgresDeploymentOptionsDto? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceName);

        RailwayPostgresDeploymentOptionsDto deploymentOptions = options ?? new();

        return builder.PublishToRailway(
            RailwayPostgresValue.FromParameter(serviceName),
            projectId,
            environmentId,
            apiToken,
            deploymentOptions.GetOwnershipMode(),
            targetOptions => CopyOptions(deploymentOptions.ToDeploymentOptions(), targetOptions));
    }

    private static void CopyOptions(RailwayPostgresDeploymentOptions source, RailwayPostgresDeploymentOptions target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        target.Region = source.Region;
        target.RestartPolicy = source.RestartPolicy;
        target.RestartPolicyMaxRetries = source.RestartPolicyMaxRetries;
        target.MemoryGB = source.MemoryGB;
        target.VCpus = source.VCpus;
        target.SharedMemoryBytes = source.SharedMemoryBytes;
    }

    private static void RemoveExistingRailwayPipelineStep(PostgresServerResource resource)
    {
        for (int annotationIndex = 0; annotationIndex < resource.Annotations.Count; annotationIndex++)
        {
            if (resource.Annotations[annotationIndex] is not RailwayPostgresDeploymentAnnotation)
            {
                continue;
            }

            int pipelineStepAnnotationIndex = annotationIndex + 1;

            while (pipelineStepAnnotationIndex < resource.Annotations.Count)
            {
                if (resource.Annotations[pipelineStepAnnotationIndex] is PipelineStepAnnotation)
                {
                    resource.Annotations.RemoveAt(pipelineStepAnnotationIndex);
                    break;
                }

                pipelineStepAnnotationIndex++;
            }

            return;
        }
    }
}
