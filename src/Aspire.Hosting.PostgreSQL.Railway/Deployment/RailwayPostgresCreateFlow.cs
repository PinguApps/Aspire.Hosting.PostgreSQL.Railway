using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresCreateFlow
{
    private readonly IRailwayPostgresManagementClient _client;
    private readonly RailwayPostgresReadinessPollingOptions _readinessPollingOptions;

    public RailwayPostgresCreateFlow(
        IRailwayPostgresManagementClient client,
        RailwayPostgresReadinessPollingOptions? readinessPollingOptions = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
        _readinessPollingOptions = readinessPollingOptions ?? RailwayPostgresReadinessPollingOptions.Default;
    }

    public async Task<RailwayPostgresCreateFlowResult> ExecuteAsync(
        RailwayPostgresResolvedDeployment deployment,
        RailwayPostgresOwnershipResolutionResult ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(ownership);

        if (ownership.Action != RailwayPostgresOwnershipResolutionAction.Create)
        {
            RailwayPostgresDatabaseDetails adoptedDatabase = ownership.Database
                ?? throw new InvalidOperationException("Railway PostgreSQL ownership resolution selected adopt without a database.");

            ValidateConnectionDetails(deployment.DatabaseName, adoptedDatabase);

            return new RailwayPostgresCreateFlowResult(adoptedDatabase, created: false);
        }

        RailwayPostgresCreateDatabaseRequest request = BuildCreateRequest(deployment);
        RailwayPostgresDatabaseDetails createdDatabase;

        try
        {
            createdDatabase = await _client.CreateDatabaseAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (RailwayPostgresProviderException exception)
        {
            throw new InvalidOperationException(
                $"Failed to create Railway PostgreSQL database '{deployment.DatabaseName}': {exception.Message}",
                exception);
        }

        string databaseId = string.IsNullOrWhiteSpace(createdDatabase.DatabaseId)
            ? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL create response for database '{deployment.DatabaseName}' did not include a provider database id.")
            : createdDatabase.DatabaseId;

        RailwayPostgresDatabaseDetails readyDatabase = await _client
            .WaitUntilReadyAsync(databaseId, _readinessPollingOptions, cancellationToken)
            .ConfigureAwait(false);

        ValidateCreatedDatabase(deployment.DatabaseName, databaseId, readyDatabase);
        ValidateConnectionDetails(deployment.DatabaseName, readyDatabase);

        return new RailwayPostgresCreateFlowResult(readyDatabase, created: true);
    }

    private static RailwayPostgresCreateDatabaseRequest BuildCreateRequest(RailwayPostgresResolvedDeployment deployment)
    {
        RailwayPostgresProviderDeploymentOptions options = deployment.Options;
        bool tls = GetOptionalBoolean(options.Tls, nameof(RailwayPostgresDeploymentOptions.Tls)) ?? true;

        if (!tls)
        {
            throw new InvalidOperationException("Railway PostgreSQL requires TLS for v1 deployments. Set TLS to true or leave it unset.");
        }

        return new RailwayPostgresCreateDatabaseRequest
        {
            DatabaseName = deployment.DatabaseName,
            Platform = GetRequiredString(options.Platform, nameof(RailwayPostgresDeploymentOptions.Platform), "platform"),
            PrimaryRegion = GetRequiredString(options.PrimaryRegion, nameof(RailwayPostgresDeploymentOptions.PrimaryRegion), "primary region"),
            ReadRegions = options.ReadRegions is null ? null : [.. options.ReadRegions.Select(GetReadRegion)],
            Plan = GetOptionalString(options.Plan, nameof(RailwayPostgresDeploymentOptions.Plan)),
            Budget = GetOptionalInt32(options.Budget, nameof(RailwayPostgresDeploymentOptions.Budget)),
            Eviction = GetOptionalBoolean(options.Eviction, nameof(RailwayPostgresDeploymentOptions.Eviction)),
            Tls = true,
        };
    }

    private static void ValidateCreatedDatabase(
        string configuredDatabaseName,
        string createdDatabaseId,
        RailwayPostgresDatabaseDetails readyDatabase)
    {
        if (readyDatabase.DatabaseId != createdDatabaseId)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL readiness lookup for created database '{createdDatabaseId}' returned provider id '{readyDatabase.DatabaseId}'.");
        }

        if (readyDatabase.DatabaseName != configuredDatabaseName)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL readiness lookup for created database '{createdDatabaseId}' returned database name '{readyDatabase.DatabaseName}', not configured name '{configuredDatabaseName}'.");
        }
    }

    private static void ValidateConnectionDetails(
        string configuredDatabaseName,
        RailwayPostgresDatabaseDetails database)
    {
        string databaseId = database.DatabaseId;

        if (database.DatabaseName != configuredDatabaseName)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned database '{databaseId}' with name '{database.DatabaseName}', not configured name '{configuredDatabaseName}'.");
        }

        if (string.IsNullOrWhiteSpace(database.Password))
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned database '{databaseId}' without credentials.");
        }

        if (string.IsNullOrWhiteSpace(database.Endpoint))
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned database '{databaseId}' without an endpoint.");
        }

        if (database.Port <= 0)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned database '{databaseId}' without a valid port.");
        }

        if (!database.Tls)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned database '{databaseId}' with TLS disabled.");
        }
    }

    private static string GetRequiredString(
        RailwayPostgresProviderValue? value,
        string optionName,
        string settingName)
    {
        string? literalValue = GetOptionalString(value, optionName);

        return string.IsNullOrWhiteSpace(literalValue)
            ? throw new InvalidOperationException($"Railway PostgreSQL create requires an explicit {settingName}. Configure {optionName} before deploying a new database.")
            : literalValue;
    }

    private static string? GetOptionalString(RailwayPostgresProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue as string
            ?? throw new InvalidOperationException($"Railway PostgreSQL option {optionName} was not resolved to a provider string value.");
    }

    private static int? GetOptionalInt32(RailwayPostgresProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue is int intValue
            ? intValue
            : throw new InvalidOperationException($"Railway PostgreSQL option {optionName} was not resolved to a provider integer value.");
    }

    private static bool? GetOptionalBoolean(RailwayPostgresProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue is bool boolValue
            ? boolValue
            : throw new InvalidOperationException($"Railway PostgreSQL option {optionName} was not resolved to a provider boolean value.");
    }

    private static string GetReadRegion(RailwayPostgresProviderValue value)
    {
        return GetRequiredString(value, nameof(RailwayPostgresDeploymentOptions.ReadRegions), "read region");
    }
}
