#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal static class RailwayPostgresDeployTimeResolver
{
    public static Task<RailwayPostgresResolvedDeployment> ResolveAsync(
        RailwayPostgresDeploymentState state,
        PostgresServerResource resource,
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

        string serviceName = await ResolveRequiredStringAsync(state.ServiceName, "service name", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string projectId = await ResolveRequiredStringAsync(state.ProjectId, "project id", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string environmentId = await ResolveRequiredStringAsync(state.EnvironmentId, "environment id", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string apiToken = await ResolveRequiredStringAsync(state.ApiToken, "API token", caller, executionContext, cancellationToken).ConfigureAwait(false);

        return new RailwayPostgresResolvedDeployment(
            serviceName,
            projectId,
            environmentId,
            state.OwnershipMode,
            new RailwayPostgresManagementCredentials(apiToken),
            RailwayPostgresProviderDeploymentOptions.Empty);
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
