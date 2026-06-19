using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresManagementClient : IRailwayPostgresManagementClient
{
    private const string RedisPath = "redis";
    private const string RedisDatabasePath = $"{RedisPath}/database";
    private const string RedisDatabasesPath = $"{RedisDatabasePath}s";
    private const string UpdateRegionsAction = "update-regions";
    private const string ChangePlanAction = "change-plan";
    private const string UpdateBudgetAction = "update-budget";
    private const string EnableEvictionAction = "enable-eviction";
    private const string DisableEvictionAction = "disable-eviction";

    private static string BuildRedisDatabasePath(string databaseId) => $"{RedisDatabasePath}/{Uri.EscapeDataString(databaseId)}";

    private static string BuildUpdateRegionsPath(string databaseId) => $"{RedisPath}/{UpdateRegionsAction}/{Uri.EscapeDataString(databaseId)}";

    private static string BuildChangePlanPath(string databaseId) => $"{RedisPath}/{Uri.EscapeDataString(databaseId)}/{ChangePlanAction}";

    private static string BuildUpdateBudgetPath(string databaseId) => $"{RedisPath}/{UpdateBudgetAction}/{Uri.EscapeDataString(databaseId)}";

    private static string BuildEvictionPath(string databaseId, bool enabled)
    {
        string action = enabled ? EnableEvictionAction : DisableEvictionAction;

        return $"{RedisPath}/{action}/{Uri.EscapeDataString(databaseId)}";
    }

    private readonly HttpClient _httpClient;
    private readonly RailwayPostgresManagementCredentials _credentials;

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public RailwayPostgresManagementClient(HttpClient httpClient, RailwayPostgresManagementCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(credentials);

        _httpClient = httpClient;
        _credentials = credentials;

        _httpClient.BaseAddress ??= new Uri("https://api.railway.com/v2/");
    }

    public async Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<IReadOnlyList<RailwayPostgresDatabaseSummary>>(
            HttpMethod.Get,
            RedisDatabasesPath,
            requestBody: null,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        return await SendAsync<RailwayPostgresDatabaseDetails>(
            HttpMethod.Get,
            BuildRedisDatabasePath(databaseId),
            requestBody: null,
            requireCredentials: true,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        IReadOnlyList<RailwayPostgresDatabaseSummary> databases = await ListDatabasesAsync(cancellationToken).ConfigureAwait(false);
        List<RailwayPostgresDatabaseSummary> matches = [.. databases.Where(database => database.DatabaseName == databaseName).Take(2)];

        if (matches.Count > 1)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL returned more than one database named '{databaseName}'.");
        }

        RailwayPostgresDatabaseSummary? match = matches.SingleOrDefault();

        if (match is null)
        {
            return null;
        }

        RailwayPostgresDatabaseDetails database = await GetDatabaseAsync(match.DatabaseId, cancellationToken).ConfigureAwait(false);

        if (database.DatabaseId != match.DatabaseId)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL database '{match.DatabaseId}' was listed as '{databaseName}' but detail lookup returned provider id '{database.DatabaseId}'.");
        }

        ThrowIfDatabaseNameMismatch(database, match.DatabaseId, databaseName);

        return database;
    }

    public async Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(
        RailwayPostgresCreateDatabaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await SendAsync<RailwayPostgresDatabaseDetails>(
            HttpMethod.Post,
            RedisDatabasePath,
            request,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateReadRegionsAsync(
        string databaseId,
        RailwayPostgresUpdateRegionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Post,
            BuildUpdateRegionsPath(databaseId),
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePlanAsync(
        string databaseId,
        RailwayPostgresChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Post,
            BuildChangePlanPath(databaseId),
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateBudgetAsync(
        string databaseId,
        RailwayPostgresUpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Patch,
            BuildUpdateBudgetPath(databaseId),
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        await SendOkAsync(
            HttpMethod.Post,
            BuildEvictionPath(databaseId, enabled),
            requestBody: null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
        string databaseId,
        RailwayPostgresReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(pollingOptions);

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            RailwayPostgresDatabaseDetails database = await GetDatabaseAsync(databaseId, cancellationToken).ConfigureAwait(false);

            if (IsReady(database))
            {
                return database;
            }

            if (stopwatch.Elapsed >= pollingOptions.Timeout)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    statusCode: null,
                    $"Railway PostgreSQL database '{databaseId}' did not become active before the readiness timeout.");
            }

            await Task.Delay(pollingOptions.Delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsReady(RailwayPostgresDatabaseDetails database)
    {
        return string.Equals(database.State, "active", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(database.ModifyingState);
    }

    private async Task SendOkAsync(
        HttpMethod method,
        string requestUri,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        await SendAsync<string>(
            method,
            requestUri,
            requestBody,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        object? requestBody,
        bool requireCredentials,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, requestUri);
        request.Headers.Authorization = _credentials.CreateAuthorizationHeader();

        if (requestBody is not null)
        {
            request.Content = JsonContent.Create(requestBody, options: _serializerOptions);
        }

        using HttpResponseMessage response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateFailureException(response.StatusCode, responseContent);
        }

        if (typeof(TResponse) == typeof(string))
        {
            try
            {
                return (TResponse)(object)(JsonSerializer.Deserialize<string>(responseContent, _serializerOptions)
                    ?? throw new RailwayPostgresProviderException(
                        RailwayPostgresProviderFailureKind.ProviderContract,
                        response.StatusCode,
                        "Railway PostgreSQL returned an empty or unrecognized response body."));
            }
            catch (JsonException exception)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    response.StatusCode,
                    "Railway PostgreSQL returned an invalid JSON response body.",
                    exception);
            }
        }

        TResponse deserialized;

        try
        {
            deserialized = JsonSerializer.Deserialize<TResponse>(responseContent, _serializerOptions)
                ?? throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    response.StatusCode,
                    "Railway PostgreSQL returned an empty or unrecognized response body.");
        }
        catch (JsonException exception)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                response.StatusCode,
                "Railway PostgreSQL returned an invalid JSON response body.",
                exception);
        }

        _ = requireCredentials && deserialized is RailwayPostgresDatabaseDetails { Password: null or "" } details
            ? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                response.StatusCode,
                $"Railway PostgreSQL returned database '{details.DatabaseId}' without credentials.")
            : true;

        return deserialized;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw CreateTransportFailureException(exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw CreateTransportFailureException(exception);
        }
    }

    private RailwayPostgresProviderException CreateFailureException(HttpStatusCode statusCode, string responseContent)
    {
        RailwayPostgresProviderFailureKind failureKind = statusCode switch
        {
            HttpStatusCode.BadRequest => RailwayPostgresProviderFailureKind.Validation,
            HttpStatusCode.Unauthorized => RailwayPostgresProviderFailureKind.Authentication,
            HttpStatusCode.Forbidden => RailwayPostgresProviderFailureKind.Authorization,
            HttpStatusCode.NotFound => RailwayPostgresProviderFailureKind.NotFound,
            HttpStatusCode.TooManyRequests => RailwayPostgresProviderFailureKind.RateLimited,
            HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable => RailwayPostgresProviderFailureKind.Transient,
            _ => RailwayPostgresProviderFailureKind.Unexpected,
        };

        string providerMessage = ExtractProviderMessage(responseContent);

        return new RailwayPostgresProviderException(
            failureKind,
            statusCode,
            $"Railway PostgreSQL management API request failed with {(int)statusCode} {statusCode}: {providerMessage}");
    }

    private static RailwayPostgresProviderException CreateTransportFailureException(Exception exception)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.Transient,
            statusCode: null,
            "Railway PostgreSQL management API request failed before a response was returned.",
            exception);
    }

    private string ExtractProviderMessage(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return "No provider response body was returned.";
        }

        string sanitizedContent = RedactSecrets(responseContent);

        try
        {
            using JsonDocument document = JsonDocument.Parse(sanitizedContent);

            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out JsonElement errorElement)
                && errorElement.ValueKind == JsonValueKind.String)
            {
                return errorElement.GetString() ?? "No provider error message was returned.";
            }
        }
        catch (JsonException)
        {
            return sanitizedContent;
        }

        return sanitizedContent;
    }

    private string RedactSecrets(string value)
    {
        return value.Replace(_credentials.ApiKey, "[redacted]", StringComparison.Ordinal);
    }

    private static void ThrowIfDatabaseNameMismatch(
        RailwayPostgresDatabaseDetails database,
        string listedDatabaseId,
        string expectedDatabaseName)
    {
        if (database.DatabaseName == expectedDatabaseName)
        {
            return;
        }

        throw new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Railway PostgreSQL database '{listedDatabaseId}' was listed as '{expectedDatabaseName}' but detail lookup returned '{database.DatabaseName}'.");
    }
}
