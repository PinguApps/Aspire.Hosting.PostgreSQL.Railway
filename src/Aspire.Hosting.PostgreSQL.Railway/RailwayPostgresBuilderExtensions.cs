#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Provides Railway PostgreSQL publishing extensions for Aspire Redis resources.
/// </summary>
public static class RailwayPostgresBuilderExtensions
{
    /// <summary>
    /// Provides Railway PostgreSQL publishing extensions for a standard Aspire Redis resource.
    /// </summary>
    /// <param name="builder">The existing Redis resource builder returned from <c>AddRedis</c>.</param>
    extension(IResourceBuilder<RedisResource> builder)
    {
        /// <summary>
        /// Marks a standard Aspire Redis resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Railway PostgreSQL database name.</param>
        /// <param name="accountEmail">The infrastructure-only Railway account email parameter.</param>
        /// <param name="apiKey">The infrastructure-only Railway API key parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Railway PostgreSQL settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<RedisResource> PublishToRailway(
            IResourceBuilder<ParameterResource> databaseName,
            IResourceBuilder<ParameterResource> accountEmail,
            IResourceBuilder<ParameterResource> apiKey,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(databaseName);

            return builder.PublishToRailway(
                RailwayPostgresValue.FromParameter(databaseName),
                accountEmail,
                apiKey,
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire Redis resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Railway PostgreSQL database name.</param>
        /// <param name="accountEmail">The infrastructure-only Railway account email parameter.</param>
        /// <param name="apiKey">The infrastructure-only Railway API key parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Railway PostgreSQL settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<RedisResource> PublishToRailway(
            RailwayPostgresValue databaseName,
            IResourceBuilder<ParameterResource> accountEmail,
            IResourceBuilder<ParameterResource> apiKey,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(accountEmail);
            ArgumentNullException.ThrowIfNull(apiKey);

            return builder.PublishToRailway(
                databaseName,
                RailwayPostgresValue.FromParameter(accountEmail),
                RailwayPostgresValue.FromParameter(apiKey),
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire Redis resource for Railway PostgreSQL deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Railway PostgreSQL database name.</param>
        /// <param name="accountEmail">The infrastructure-only Railway account email value.</param>
        /// <param name="apiKey">The infrastructure-only Railway API key value.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Railway PostgreSQL settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
        public IResourceBuilder<RedisResource> PublishToRailway(
            RailwayPostgresValue databaseName,
            RailwayPostgresValue accountEmail,
            RailwayPostgresValue apiKey,
            RailwayPostgresOwnershipMode ownershipMode = RailwayPostgresOwnershipMode.CreateOrAdopt,
            Action<RailwayPostgresDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(databaseName);
            ArgumentNullException.ThrowIfNull(accountEmail);
            ArgumentNullException.ThrowIfNull(apiKey);

            if (!Enum.IsDefined(ownershipMode))
            {
                throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
            }

            RailwayPostgresDeploymentOptions options = new();
            configure?.Invoke(options);
            options.ToProviderOptions();

            RemoveExistingRailwayPipelineStep(builder.Resource);
            global::Aspire.Hosting.ResourceBuilderExtensions.ExcludeFromManifest(builder);

            builder.WithAnnotation(
                new RailwayPostgresDeploymentAnnotation(
                    databaseName,
                    ownershipMode,
                    accountEmail,
                    apiKey,
                    options),
                ResourceAnnotationMutationBehavior.Replace);

            builder.WithAnnotation(
                new RailwayPostgresOutputsAnnotation(new RailwayPostgresOutputs(builder.Resource)),
                ResourceAnnotationMutationBehavior.Replace);

            RedisResource resource = builder.Resource;

            return builder.WithPipelineStepFactory(
                $"railway-postgres-{builder.Resource.Name}",
                context => RailwayPostgresDeploymentPipeline.ExecuteAsync(resource, context),
                dependsOn: [WellKnownPipelineSteps.DeployPrereq],
                requiredBy: [WellKnownPipelineSteps.Deploy],
                tags: [WellKnownPipelineTags.ProvisionInfrastructure],
                description: "Provision or reconcile the Railway PostgreSQL database.");
        }
    }

    /// <summary>
    /// Marks a standard Aspire Redis resource for Railway PostgreSQL deployment from a TypeScript AppHost.
    /// </summary>
    /// <param name="builder">The existing Redis resource builder returned from <c>AddRedis</c>.</param>
    /// <param name="databaseName">The explicit remote Railway PostgreSQL database name parameter.</param>
    /// <param name="accountEmail">The infrastructure-only Railway account email parameter.</param>
    /// <param name="apiKey">The infrastructure-only Railway API key parameter.</param>
    /// <param name="options">Optional Railway PostgreSQL settings to reconcile at deploy time.</param>
    /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/>, <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The ownership mode is not a defined ownership mode.</exception>
    /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
    [AspireExport("pinguapps.railway.postgres.publishToRailway", MethodName = "publishToRailway")]
    public static IResourceBuilder<RedisResource> PublishToRailwayForTypeScript(
        this IResourceBuilder<RedisResource> builder,
        IResourceBuilder<ParameterResource> databaseName,
        IResourceBuilder<ParameterResource> accountEmail,
        IResourceBuilder<ParameterResource> apiKey,
        RailwayPostgresDeploymentOptionsDto? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(databaseName);

        RailwayPostgresDeploymentOptionsDto deploymentOptions = options ?? new();

        return builder.PublishToRailway(
            RailwayPostgresValue.FromParameter(databaseName),
            accountEmail,
            apiKey,
            deploymentOptions.GetOwnershipMode(),
            targetOptions => CopyOptions(deploymentOptions.ToDeploymentOptions(), targetOptions));
    }

    private static void CopyOptions(RailwayPostgresDeploymentOptions source, RailwayPostgresDeploymentOptions target)
    {
        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Platform)))
        {
            target.Platform = source.Platform;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion)))
        {
            target.PrimaryRegion = source.PrimaryRegion;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.ReadRegions)))
        {
            target.ReadRegions = source.ReadRegions;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Plan)))
        {
            target.Plan = source.Plan;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Budget)))
        {
            target.Budget = source.Budget;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Eviction)))
        {
            target.Eviction = source.Eviction;
        }

        if (source.ExplicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Tls)))
        {
            target.Tls = source.Tls;
        }
    }

    private static void RemoveExistingRailwayPipelineStep(RedisResource resource)
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
