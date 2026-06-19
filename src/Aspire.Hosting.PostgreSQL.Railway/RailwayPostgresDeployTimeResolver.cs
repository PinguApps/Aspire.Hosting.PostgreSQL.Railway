#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal static class RailwayPostgresDeployTimeResolver
{
    public static Task<RailwayPostgresResolvedDeployment> ResolveAsync(
        RailwayPostgresDeploymentState state,
        RedisResource resource,
        PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ResolveAsync(
            state,
            resource,
            context.ExecutionContext,
            context.CancellationToken);
    }

    public static async Task<RailwayPostgresResolvedDeployment> ResolveAsync(
        RailwayPostgresDeploymentState state,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(caller);

        string databaseName = await ResolveRequiredStringAsync(state.DatabaseName, "database name", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string accountEmail = await ResolveRequiredStringAsync(state.AccountEmail, "account email", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string apiKey = await ResolveRequiredStringAsync(state.ApiKey, "API key", caller, executionContext, cancellationToken).ConfigureAwait(false);
        RailwayPostgresProviderDeploymentOptions options = await ResolveOptionsAsync(state.Options, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return new RailwayPostgresResolvedDeployment(
            databaseName,
            state.OwnershipMode,
            new RailwayPostgresManagementCredentials(accountEmail, apiKey),
            options);
    }

    private static async Task<RailwayPostgresProviderDeploymentOptions> ResolveOptionsAsync(
        RailwayPostgresDeploymentOptions source,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        RailwayPostgresDeploymentOptions resolved = new();
        IReadOnlySet<string> explicitSettings = source.ExplicitSettings;

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Platform)))
        {
            resolved.Platform = source.Platform is null
                ? null
                : RailwayPostgresValue.FromString(await ResolveOptionalStringAsync(source.Platform, "platform", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion)))
        {
            resolved.PrimaryRegion = source.PrimaryRegion is null
                ? null
                : RailwayPostgresValue.FromString(await ResolveOptionalStringAsync(source.PrimaryRegion, "primary region", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.ReadRegions)))
        {
            if (source.ReadRegions is null)
            {
                resolved.ReadRegions = null;
            }
            else
            {
                List<RailwayPostgresValue> readRegions = [];

                foreach (RailwayPostgresValue readRegion in source.ReadRegions)
                {
                    string resolvedReadRegion = await ResolveOptionalStringAsync(readRegion, "read region", caller, executionContext, cancellationToken).ConfigureAwait(false);
                    readRegions.Add(RailwayPostgresValue.FromString(resolvedReadRegion));
                }

                resolved.ReadRegions = readRegions;
            }
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Plan)))
        {
            resolved.Plan = source.Plan is null
                ? null
                : RailwayPostgresValue.FromString(await ResolveOptionalStringAsync(source.Plan, "plan", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Budget)))
        {
            resolved.Budget = source.Budget is null
                ? null
                : RailwayPostgresValue.FromString(await ResolveOptionalStringAsync(source.Budget, "budget", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Eviction)))
        {
            resolved.Eviction = source.Eviction;
        }

        if (explicitSettings.Contains(nameof(RailwayPostgresDeploymentOptions.Tls)))
        {
            resolved.Tls = source.Tls;
        }

        return resolved.ToProviderOptions();
    }

    private static async Task<string> ResolveRequiredStringAsync(
        RailwayPostgresValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string resolvedValue = await ResolveStringAsync(value, settingName, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(resolvedValue)
            ? throw new InvalidOperationException($"Railway PostgreSQL deployment requires a non-empty {settingName}.")
            : resolvedValue;
    }

    private static async Task<string> ResolveOptionalStringAsync(
        RailwayPostgresValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string resolvedValue = await ResolveStringAsync(value, settingName, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(resolvedValue)
            ? throw new InvalidOperationException($"Railway PostgreSQL {settingName} resolved to an empty value.")
            : resolvedValue;
    }

    private static async Task<string> ResolveStringAsync(
        RailwayPostgresValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        if (value.LiteralValue is not null)
        {
            return value.LiteralValue;
        }

        ParameterResource parameter = value.Parameter
            ?? throw new InvalidOperationException($"Railway PostgreSQL {settingName} is not backed by a literal value or an Aspire parameter.");

        try
        {
            if (executionContext is null)
            {
                string? parameterValue = await parameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return parameterValue
                    ?? throw new InvalidOperationException($"Railway PostgreSQL {settingName} parameter '{parameter.Name}' resolved to null.");
            }

            ValueProviderContext valueProviderContext = new()
            {
                ExecutionContext = executionContext,
                Caller = caller
            };

            string? contextParameterValue = await parameter.GetValueAsync(valueProviderContext, cancellationToken).ConfigureAwait(false);
            return contextParameterValue
                ?? throw new InvalidOperationException($"Railway PostgreSQL {settingName} parameter '{parameter.Name}' resolved to null.");
        }
        catch (MissingParameterValueException exception)
        {
            throw new InvalidOperationException(
                $"Railway PostgreSQL deployment requires {settingName} parameter '{parameter.Name}'. Provide it through Aspire parameter configuration before deploying.",
                exception);
        }
    }
}
