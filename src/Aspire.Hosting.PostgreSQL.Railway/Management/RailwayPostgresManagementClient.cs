using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresManagementClient : IRailwayPostgresManagementClient
{
    private const string PostgresTemplateId = "b55da7dc-09be-4140-bc65-1284d15d349c";
    private static readonly TimeSpan _createdServiceLookupTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _createdServiceLookupDelay = TimeSpan.FromSeconds(2);

    private const string ListServicesQuery = """
        query ListRailwayServices($projectId: String!, $after: String) {
          project(id: $projectId) {
            services(first: 100, after: $after) {
              edges {
                node {
                  id
                  name
                  projectId
                  deletedAt
                }
              }
              pageInfo {
                hasNextPage
                endCursor
              }
            }
          }
        }
        """;

    private const string ListEnvironmentsQuery = """
        query ListRailwayEnvironments($projectId: String!) {
          environments(projectId: $projectId) {
            edges {
              node {
                id
                name
              }
            }
          }
        }
        """;

    private const string ListRegionsQuery = """
        query ListRailwayRegions {
          regions {
            id
            name
            region
          }
        }
        """;

    private const string GetServiceQuery = """
        query GetRailwayPostgresService($projectId: String!, $environmentId: String!, $serviceId: String!) {
          service(id: $serviceId) {
            id
            name
            projectId
            deletedAt
          }
          serviceInstance(serviceId: $serviceId, environmentId: $environmentId) {
            latestDeployment {
              status
              deploymentStopped
              meta
            }
          }
          variables(projectId: $projectId, environmentId: $environmentId, serviceId: $serviceId)
        }
        """;

    private const string GetTemplateQuery = """
        query GetRailwayPostgresTemplate($id: String!) {
          template(id: $id) {
            serializedConfig
          }
        }
        """;

    private const string DeployTemplateMutation = """
        mutation DeployRailwayPostgresTemplate($input: TemplateDeployV2Input!) {
          templateDeployV2(input: $input) {
            projectId
            workflowId
          }
        }
        """;

    private const string UpdateServiceInstanceMutation = """
        mutation UpdateRailwayPostgresServiceInstance($environmentId: String!, $serviceId: String!, $input: ServiceInstanceUpdateInput!) {
          serviceInstanceUpdate(environmentId: $environmentId, serviceId: $serviceId, input: $input)
        }
        """;

    private const string UpdateServiceInstanceLimitsMutation = """
        mutation UpdateRailwayPostgresServiceInstanceLimits($input: ServiceInstanceLimitsUpdateInput!) {
          serviceInstanceLimitsUpdate(input: $input)
        }
        """;

    private const string GetServiceInstanceDeploymentQuery = """
        query GetRailwayServiceInstanceDeployment($environmentId: String!, $serviceId: String!) {
          serviceInstance(serviceId: $serviceId, environmentId: $environmentId) {
            latestDeployment {
              meta
            }
          }
        }
        """;

    private const string RedeployServiceInstanceMutation = """
        mutation RedeployRailwayPostgresServiceInstance($environmentId: String!, $serviceId: String!) {
          serviceInstanceRedeploy(environmentId: $environmentId, serviceId: $serviceId)
        }
        """;

    private const string UpsertVariableMutation = """
        mutation UpsertRailwayPostgresVariable($input: VariableUpsertInput!) {
          variableUpsert(input: $input)
        }
        """;

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

        _httpClient.BaseAddress ??= new Uri("https://backboard.railway.com/graphql/v2");
    }

    public async Task<string> ResolveEnvironmentIdAsync(
        string projectId,
        string environmentIdOrName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentIdOrName);

        if (Guid.TryParse(environmentIdOrName, out _))
        {
            return environmentIdOrName;
        }

        ListEnvironmentsData data = await SendAsync<ListEnvironmentsData>(
            ListEnvironmentsQuery,
            new { projectId },
            cancellationToken).ConfigureAwait(false);

        List<RailwayEnvironmentNode> matches =
        [
            .. data.Environments.Edges
                .Select(edge => edge.Node)
                .Where(environment => string.Equals(environment.Name, environmentIdOrName, StringComparison.Ordinal))
                .Take(2)
        ];

        if (matches.Count == 0)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.NotFound,
                statusCode: null,
                $"Railway environment '{environmentIdOrName}' was not found in project '{projectId}'.");
        }

        if (matches.Count > 1)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway returned more than one environment named '{environmentIdOrName}' in project '{projectId}'.");
        }

        return matches[0].Id;
    }

    public async Task<RailwayPostgresDatabaseDetails?> FindServiceByNameAsync(
        string projectId,
        string environmentId,
        string serviceName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        List<RailwayServiceNode> matches = [];
        string? after = null;

        do
        {
            ListServicesData data = await SendAsync<ListServicesData>(
                ListServicesQuery,
                new { projectId, after },
                cancellationToken).ConfigureAwait(false);

            matches.AddRange(
                data.Project.Services.Edges
                    .Select(edge => edge.Node)
                    .Where(service => service.DeletedAt is null)
                    .Where(service => string.Equals(service.Name, serviceName, StringComparison.Ordinal)));

            if (matches.Count > 1)
            {
                break;
            }

            after = data.Project.Services.PageInfo.HasNextPage
                ? data.Project.Services.PageInfo.EndCursor
                : null;
        }
        while (!string.IsNullOrWhiteSpace(after));

        if (matches.Count > 1)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway returned more than one service named '{serviceName}' in project '{projectId}'.");
        }

        RailwayServiceNode? match = matches.SingleOrDefault();

        return match is null
            ? null
            : await GetServiceAsync(projectId, environmentId, match.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RailwayPostgresDatabaseDetails> GetServiceAsync(
        string projectId,
        string environmentId,
        string serviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        GetServiceData data = await SendAsync<GetServiceData>(
            GetServiceQuery,
            new { projectId, environmentId, serviceId },
            cancellationToken).ConfigureAwait(false);

        RailwayServiceNode service = data.Service
            ?? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.NotFound,
                statusCode: null,
                $"Railway service '{serviceId}' was not found.");

        if (service.DeletedAt is not null)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.NotFound,
                statusCode: null,
                $"Railway service '{serviceId}' is deleted.");
        }

        IReadOnlyDictionary<string, string> variables = ParseVariables(data.Variables);
        string? status = data.ServiceInstance?.LatestDeployment?.Status;
        string privateHost = GetVariableOrEmpty(variables, "PGHOST");
        int privatePort = ParsePort(GetVariableOrEmpty(variables, "PGPORT"), service.Id);
        string privateUserName = GetVariableOrEmpty(variables, "PGUSER");
        string privatePassword = GetVariableOrEmpty(variables, "PGPASSWORD");
        string privateDatabaseName = GetVariableOrEmpty(variables, "PGDATABASE");
        string publicDatabaseUrl = GetVariableOrEmpty(variables, "DATABASE_PUBLIC_URL");
        RailwayPostgresConnectionDetails? publicConnection = CreatePublicConnectionDetailsOrNull(publicDatabaseUrl);
        string connectionString = publicConnection?.ConnectionString
            ?? CreateConnectionStringOrEmpty(privateHost, privatePort, privateUserName, privatePassword, privateDatabaseName);
        string provisioningConnectionString = CreateProvisioningConnectionStringOrEmpty(
            publicDatabaseUrl,
            connectionString);

        return new RailwayPostgresDatabaseDetails
        {
            ServiceId = service.Id,
            ServiceName = service.Name,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            Host = publicConnection?.Host ?? privateHost,
            Port = publicConnection?.Port ?? privatePort,
            UserName = publicConnection?.UserName ?? privateUserName,
            Password = publicConnection?.Password ?? privatePassword,
            DatabaseName = publicConnection?.DatabaseName ?? privateDatabaseName,
            ConnectionString = connectionString,
            ProvisioningConnectionString = provisioningConnectionString,
            LatestDeploymentStatus = status,
            LatestDeploymentStopped = data.ServiceInstance?.LatestDeployment?.DeploymentStopped,
            LatestDeploymentQueuedReason = GetQueuedReason(data.ServiceInstance?.LatestDeployment?.Meta),
        };
    }

    public async Task<RailwayPostgresDatabaseDetails> CreateServiceAsync(
        RailwayPostgresCreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        GetTemplateData templateData = await SendAsync<GetTemplateData>(
            GetTemplateQuery,
            new { id = PostgresTemplateId },
            cancellationToken).ConfigureAwait(false);

        string? requestedRegionId = request.Options.Region is null
            ? null
            : await ResolveRegionIdAsync(request.Options.Region.Value.ToRailwayIdentifier(), cancellationToken).ConfigureAwait(false);
        JsonNode serializedConfig = CreateSerializedConfig(
            templateData.Template.SerializedConfig,
            request.ServiceName,
            request.Options,
            requestedRegionId);

        await SendAsync<DeployTemplateData>(
            DeployTemplateMutation,
            new
            {
                input = new
                {
                    projectId = request.ProjectId,
                    environmentId = request.EnvironmentId,
                    templateId = PostgresTemplateId,
                    serializedConfig,
                }
            },
            cancellationToken).ConfigureAwait(false);

        return await WaitForCreatedServiceAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
        string projectId,
        string environmentId,
        string serviceId,
        RailwayPostgresReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentNullException.ThrowIfNull(pollingOptions);

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            RailwayPostgresDatabaseDetails service = await GetServiceAsync(projectId, environmentId, serviceId, cancellationToken).ConfigureAwait(false);

            if (service.HasConnectionVariables && IsSuccessfulDeploymentStatus(service.LatestDeploymentStatus))
            {
                return service;
            }

            if (IsTerminalUnsuccessfulDeployment(service))
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    statusCode: null,
                    CreateDeploymentNotReadyMessage(serviceId, service, "failed before becoming SUCCESS"));
            }

            if (stopwatch.Elapsed >= pollingOptions.Timeout)
            {
                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    statusCode: null,
                    CreateReadinessTimeoutMessage(serviceId, service));
            }

            await Task.Delay(pollingOptions.Delay, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ConfigureServiceAsync(
        string projectId,
        string environmentId,
        string serviceId,
        RailwayPostgresDeploymentOptions options,
        bool allowVolumeRegionMigration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
        string? requestedRegionId = null;
        RailwayDeploymentManifestState? latestDeploymentManifest = null;

        if (options.HasServiceInstanceSettings)
        {
            requestedRegionId = options.Region is null
                ? null
                : await ResolveRegionIdAsync(options.Region.Value.ToRailwayIdentifier(), cancellationToken).ConfigureAwait(false);

            if (requestedRegionId is not null)
            {
                latestDeploymentManifest = await GetLatestDeploymentManifestStateAsync(environmentId, serviceId, cancellationToken)
                    .ConfigureAwait(false);

                if (!allowVolumeRegionMigration
                    && latestDeploymentManifest.HasVolumeMount
                    && !latestDeploymentManifest.UsesRegion(requestedRegionId))
                {
                    throw new RailwayPostgresProviderException(
                        RailwayPostgresProviderFailureKind.Validation,
                        statusCode: null,
                        $"Changing the Railway PostgreSQL region for existing service '{serviceId}' requires a Railway volume migration and is not performed automatically. Create a new Railway PostgreSQL service, or migrate the volume/region manually in Railway before deploying.");
                }
            }

            await SendAsync<UpdateServiceInstanceData>(
                UpdateServiceInstanceMutation,
                new
                {
                    environmentId,
                    serviceId,
                    input = new
                    {
                        region = requestedRegionId,
                        multiRegionConfig = CreateMultiRegionConfigOrNull(requestedRegionId),
                        restartPolicyType = options.RestartPolicy is null
                            ? null
                            : ToRailwayRestartPolicy(options.RestartPolicy.Value),
                        restartPolicyMaxRetries = options.RestartPolicyMaxRetries,
                    },
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (options.HasResourceLimits)
        {
            await SendAsync<UpdateServiceInstanceLimitsData>(
                UpdateServiceInstanceLimitsMutation,
                new
                {
                    input = new
                    {
                        environmentId,
                        serviceId,
                        memoryGB = options.MemoryGB,
                        vCPUs = options.VCpus,
                    },
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (options.SharedMemoryBytes is long sharedMemoryBytes)
        {
            await SendAsync<UpsertVariableData>(
                UpsertVariableMutation,
                new
                {
                    input = new
                    {
                        projectId,
                        environmentId,
                        serviceId,
                        name = "RAILWAY_SHM_SIZE_BYTES",
                        value = sharedMemoryBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        skipDeploys = false,
                    },
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (requestedRegionId is not null
            && latestDeploymentManifest?.UsesRegion(requestedRegionId) != true)
        {
            await SendAsync<RedeployServiceInstanceData>(
                RedeployServiceInstanceMutation,
                new
                {
                    environmentId,
                    serviceId,
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TData> SendAsync<TData>(
        string query,
        object variables,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, string.Empty);
        request.Headers.Authorization = _credentials.CreateAuthorizationHeader();
        request.Content = JsonContent.Create(new RailwayGraphQlRequest(query, variables), options: _serializerOptions);

        using HttpResponseMessage response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateFailureException(response.StatusCode, responseContent);
        }

        RailwayGraphQlResponse<TData> deserialized;

        try
        {
            deserialized = JsonSerializer.Deserialize<RailwayGraphQlResponse<TData>>(responseContent, _serializerOptions)
                ?? throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    response.StatusCode,
                    "Railway returned an empty or unrecognized GraphQL response body.");
        }
        catch (JsonException exception)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                response.StatusCode,
                "Railway returned an invalid JSON response body.",
                exception);
        }

        if (deserialized.Errors is { Count: > 0 })
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.Unexpected,
                response.StatusCode,
                $"Railway GraphQL request '{GetOperationName(query)}' failed: {RedactSecrets(string.Join("; ", deserialized.Errors.Select(error => error.Message)))}");
        }

        return deserialized.Data is null
            ? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                response.StatusCode,
                "Railway returned a GraphQL response without data.")
            : deserialized.Data;
    }

    private async Task<RailwayPostgresDatabaseDetails> WaitForCreatedServiceAsync(
        RailwayPostgresCreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        RailwayPostgresProviderException? lastTransientException = null;

        while (true)
        {
            try
            {
                RailwayPostgresDatabaseDetails? service = await FindServiceByNameAsync(
                    request.ProjectId,
                    request.EnvironmentId,
                    request.ServiceName,
                    cancellationToken).ConfigureAwait(false);

                if (service is not null)
                {
                    return service;
                }
            }
            catch (RailwayPostgresProviderException exception) when (IsServiceInstanceNotFound(exception))
            {
                lastTransientException = exception;
            }

            if (stopwatch.Elapsed >= _createdServiceLookupTimeout)
            {
                string suffix = lastTransientException is null
                    ? string.Empty
                    : $" Last transient provider error: {lastTransientException.Message}";

                throw new RailwayPostgresProviderException(
                    RailwayPostgresProviderFailureKind.ProviderContract,
                    statusCode: null,
                    $"Railway PostgreSQL service '{request.ServiceName}' was not visible after the template deployment completed.{suffix}");
            }

            await Task.Delay(_createdServiceLookupDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsServiceInstanceNotFound(RailwayPostgresProviderException exception)
    {
        return exception.Message.Contains("ServiceInstance not found", StringComparison.OrdinalIgnoreCase);
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

        return new RailwayPostgresProviderException(
            failureKind,
            statusCode,
            $"Railway GraphQL request failed with {(int)statusCode} {statusCode}: {ExtractProviderMessage(responseContent)}");
    }

    private static RailwayPostgresProviderException CreateTransportFailureException(Exception exception)
    {
        return new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.Transient,
            statusCode: null,
            "Railway GraphQL request failed before a response was returned.",
            exception);
    }

    private string ExtractProviderMessage(string responseContent)
    {
        return string.IsNullOrWhiteSpace(responseContent)
            ? "No provider response body was returned."
            : RedactSecrets(responseContent);
    }

    private string RedactSecrets(string value)
    {
        return value.Replace(_credentials.ApiToken, "[redacted]", StringComparison.Ordinal);
    }

    private static string GetOperationName(string query)
    {
        ReadOnlySpan<char> span = query.AsSpan().TrimStart();

        foreach (string keyword in new[] { "query", "mutation" })
        {
            if (!span.StartsWith(keyword, StringComparison.Ordinal))
            {
                continue;
            }

            span = span[keyword.Length..].TrimStart();
            int end = 0;

            while (end < span.Length && (char.IsLetterOrDigit(span[end]) || span[end] == '_'))
            {
                end++;
            }

            return end > 0 ? span[..end].ToString() : keyword;
        }

        return "anonymous";
    }

    private static JsonNode CreateSerializedConfig(
        JsonElement serializedConfig,
        string serviceName,
        RailwayPostgresDeploymentOptions options,
        string? requestedRegionId)
    {
        if (serializedConfig.ValueKind != JsonValueKind.Object)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway returned the PostgreSQL template configuration in an unexpected shape.");
        }

        JsonNode config = JsonNode.Parse(serializedConfig.GetRawText())
            ?? throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway returned an empty PostgreSQL template configuration.");

        JsonObject? services = config["services"]?.AsObject();

        if (services is null || services.Count == 0)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway PostgreSQL template configuration did not contain any services.");
        }

        foreach (KeyValuePair<string, JsonNode?> service in services)
        {
            if (service.Value is JsonObject serviceConfig)
            {
                serviceConfig["name"] = serviceName;
                ApplyTemplateDeployOptions(serviceConfig, options, requestedRegionId);
            }
        }

        return config;
    }

    private static void ApplyTemplateDeployOptions(
        JsonObject serviceConfig,
        RailwayPostgresDeploymentOptions options,
        string? requestedRegionId)
    {
        JsonObject deploy = serviceConfig["deploy"] as JsonObject ?? [];
        serviceConfig["deploy"] = deploy;

        if (requestedRegionId is not null)
        {
            deploy["region"] = requestedRegionId;
            deploy["multiRegionConfig"] = JsonSerializer.SerializeToNode(CreateMultiRegionConfigOrNull(requestedRegionId), _serializerOptions);
        }

        if (options.RestartPolicy is RailwayPostgresRestartPolicy restartPolicy)
        {
            deploy["restartPolicyType"] = ToRailwayRestartPolicy(restartPolicy);
        }

        if (options.RestartPolicyMaxRetries is int restartPolicyMaxRetries)
        {
            deploy["restartPolicyMaxRetries"] = restartPolicyMaxRetries;
        }
    }

    private static IReadOnlyDictionary<string, string> ParseVariables(JsonElement variables)
    {
        if (variables.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (variables.ValueKind != JsonValueKind.Object)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway returned service variables in an unexpected shape.");
        }

        Dictionary<string, string> parsed = new(StringComparer.Ordinal);

        foreach (JsonProperty property in variables.EnumerateObject())
        {
            parsed[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString();
        }

        return parsed;
    }

    private static string GetVariableOrEmpty(IReadOnlyDictionary<string, string> variables, string name)
    {
        return variables.TryGetValue(name, out string? value) ? value : string.Empty;
    }

    private static IReadOnlyDictionary<string, object>? CreateMultiRegionConfigOrNull(string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            return null;
        }

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [region] = new
            {
                numReplicas = 1,
            },
        };
    }

    private async Task<RailwayDeploymentManifestState> GetLatestDeploymentManifestStateAsync(
        string environmentId,
        string serviceId,
        CancellationToken cancellationToken)
    {
        GetServiceInstanceDeploymentData data = await SendAsync<GetServiceInstanceDeploymentData>(
            GetServiceInstanceDeploymentQuery,
            new
            {
                environmentId,
                serviceId,
            },
            cancellationToken).ConfigureAwait(false);

        JsonElement? meta = data.ServiceInstance?.LatestDeployment?.Meta;

        return meta is { ValueKind: JsonValueKind.Object }
            ? new RailwayDeploymentManifestState(meta.Value)
            : RailwayDeploymentManifestState.Empty;
    }

    private static bool DeploymentManifestUsesRegion(JsonElement meta, string regionId)
    {
        if (!meta.TryGetProperty("serviceManifest", out JsonElement serviceManifest)
            || !serviceManifest.TryGetProperty("deploy", out JsonElement deploy))
        {
            return false;
        }

        if (deploy.TryGetProperty("region", out JsonElement region)
            && region.ValueKind == JsonValueKind.String
            && string.Equals(region.GetString(), regionId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return deploy.TryGetProperty("multiRegionConfig", out JsonElement multiRegionConfig)
            && multiRegionConfig.ValueKind == JsonValueKind.Object
            && multiRegionConfig.TryGetProperty(regionId, out _);
    }

    private static bool DeploymentManifestHasVolumeMount(JsonElement meta)
    {
        if (meta.TryGetProperty("volumeMounts", out JsonElement volumeMounts)
            && volumeMounts.ValueKind == JsonValueKind.Array
            && volumeMounts.GetArrayLength() > 0)
        {
            return true;
        }

        return meta.TryGetProperty("serviceManifest", out JsonElement serviceManifest)
            && serviceManifest.TryGetProperty("deploy", out JsonElement deploy)
            && deploy.TryGetProperty("requiredMountPath", out JsonElement requiredMountPath)
            && requiredMountPath.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(requiredMountPath.GetString());
    }

    private async Task<string> ResolveRegionIdAsync(string region, CancellationToken cancellationToken)
    {
        ListRegionsData data = await SendAsync<ListRegionsData>(
            ListRegionsQuery,
            new { },
            cancellationToken).ConfigureAwait(false);

        List<string> matchingNames =
        [
            .. data.Regions
                .Where(candidate => string.Equals(candidate.Name, region, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        if (matchingNames.Count == 1)
        {
            return matchingNames[0];
        }

        List<string> matchingIds =
        [
            .. data.Regions
                .Where(candidate => string.Equals(candidate.Id, region, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        if (matchingIds.Count == 1)
        {
            return matchingIds[0];
        }

        List<string> matchingLabels =
        [
            .. data.Regions
                .Where(candidate => string.Equals(candidate.Region, region, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        if (matchingLabels.Count == 1)
        {
            return matchingLabels[0];
        }

        throw new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.Validation,
            statusCode: null,
            $"Railway region '{region}' was not found or was ambiguous.");
    }

    private static string? GetQueuedReason(JsonElement? meta)
    {
        return meta is { ValueKind: JsonValueKind.Object } metaValue
            && metaValue.TryGetProperty("queuedReason", out JsonElement queuedReason)
            && queuedReason.ValueKind == JsonValueKind.String
            ? queuedReason.GetString()
            : null;
    }

    private static bool IsSuccessfulDeploymentStatus(string? status)
    {
        return string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTerminalUnsuccessfulDeployment(RailwayPostgresDatabaseDetails service)
    {
        return service.LatestDeploymentStatus is not null
            && (string.Equals(service.LatestDeploymentStatus, "FAILED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(service.LatestDeploymentStatus, "CRASHED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(service.LatestDeploymentStatus, "REMOVED", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateReadinessTimeoutMessage(
        string serviceId,
        RailwayPostgresDatabaseDetails? service)
    {
        if (service is null)
        {
            return $"Railway PostgreSQL service '{serviceId}' was not ready before the readiness timeout.";
        }

        if (!IsSuccessfulDeploymentStatus(service.LatestDeploymentStatus))
        {
            return CreateDeploymentNotReadyMessage(serviceId, service, "did not become SUCCESS before the readiness timeout");
        }

        return $"Railway PostgreSQL service '{serviceId}' latest deployment was SUCCESS but PostgreSQL connection variables were not exposed before the readiness timeout.";
    }

    private static string CreateDeploymentNotReadyMessage(
        string serviceId,
        RailwayPostgresDatabaseDetails service,
        string reason)
    {
        string status = string.IsNullOrWhiteSpace(service.LatestDeploymentStatus)
            ? "unknown"
            : service.LatestDeploymentStatus;
        string message = $"Railway PostgreSQL service '{serviceId}' latest deployment {reason}. Last status: {status}.";

        return string.IsNullOrWhiteSpace(service.LatestDeploymentQueuedReason)
            ? message
            : $"{message} Queued reason: {service.LatestDeploymentQueuedReason}.";
    }

    private static string ToRailwayRestartPolicy(RailwayPostgresRestartPolicy restartPolicy)
    {
        if (restartPolicy == RailwayPostgresRestartPolicy.Always)
        {
            return "ALWAYS";
        }

        if (restartPolicy == RailwayPostgresRestartPolicy.OnFailure)
        {
            return "ON_FAILURE";
        }

        if (restartPolicy == RailwayPostgresRestartPolicy.Never)
        {
            return "NEVER";
        }

        throw new RailwayPostgresProviderException(
            RailwayPostgresProviderFailureKind.Validation,
            statusCode: null,
            $"Railway PostgreSQL restart policy '{restartPolicy}' is not supported.");
    }

    private static int ParsePort(string value, string serviceId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int port)
            ? port
            : throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Railway PostgreSQL service '{serviceId}' returned invalid PGPORT '{value}'.");
    }

    private static string CreateConnectionStringOrEmpty(
        string host,
        int port,
        string userName,
        string password,
        string databaseName)
    {
        if (string.IsNullOrWhiteSpace(host)
            || port <= 0
            || string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(databaseName))
        {
            return string.Empty;
        }

        return RailwayPostgresConnectionString.Create(host, port, userName, password, databaseName);
    }

    private static string CreateProvisioningConnectionStringOrEmpty(
        string publicDatabaseUrl,
        string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(publicDatabaseUrl))
        {
            return RailwayPostgresConnectionString.CreateFromUri(publicDatabaseUrl);
        }

        return string.IsNullOrWhiteSpace(connectionString)
            ? string.Empty
            : connectionString;
    }

    private static RailwayPostgresConnectionDetails? CreatePublicConnectionDetailsOrNull(string publicDatabaseUrl)
    {
        return string.IsNullOrWhiteSpace(publicDatabaseUrl)
            ? null
            : RailwayPostgresConnectionString.CreateDetailsFromUri(publicDatabaseUrl);
    }

    private sealed class RailwayGraphQlRequest
    {
        public RailwayGraphQlRequest(string query, object variables)
        {
            Query = query;
            Variables = variables;
        }

        public string Query { get; }

        public object Variables { get; }
    }

    private sealed class RailwayGraphQlResponse<TData>
    {
        public TData? Data { get; set; }

        public IReadOnlyList<RailwayGraphQlError>? Errors { get; set; }
    }

    private sealed class RailwayGraphQlError
    {
        public string Message { get; set; } = string.Empty;
    }

    private sealed class ListServicesData
    {
        public RailwayProject Project { get; set; } = new();
    }

    private sealed class ListEnvironmentsData
    {
        public RailwayEnvironmentConnection Environments { get; set; } = new();
    }

    private sealed class ListRegionsData
    {
        public IReadOnlyList<RailwayRegionNode> Regions { get; set; } = [];
    }

    private sealed class RailwayRegionNode
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;
    }

    private sealed class GetTemplateData
    {
        public RailwayTemplate Template { get; set; } = new();
    }

    private sealed class RailwayTemplate
    {
        public JsonElement SerializedConfig { get; set; }
    }

    private sealed class DeployTemplateData
    {
        public RailwayTemplateDeployPayload TemplateDeployV2 { get; set; } = new();
    }

    private sealed class UpdateServiceInstanceData
    {
        public bool ServiceInstanceUpdate { get; set; }
    }

    private sealed class UpdateServiceInstanceLimitsData
    {
        public bool ServiceInstanceLimitsUpdate { get; set; }
    }

    private sealed class GetServiceInstanceDeploymentData
    {
        public RailwayServiceInstance? ServiceInstance { get; set; }
    }

    private sealed class RedeployServiceInstanceData
    {
        public bool ServiceInstanceRedeploy { get; set; }
    }

    private sealed class UpsertVariableData
    {
        public bool VariableUpsert { get; set; }
    }

    private sealed class RailwayTemplateDeployPayload
    {
        public string ProjectId { get; set; } = string.Empty;

        public string? WorkflowId { get; set; }
    }

    private sealed class RailwayProject
    {
        public RailwayServiceConnection Services { get; set; } = new();
    }

    private sealed class RailwayServiceConnection
    {
        public IReadOnlyList<RailwayServiceEdge> Edges { get; set; } = [];

        public RailwayPageInfo PageInfo { get; set; } = new();
    }

    private sealed class RailwayPageInfo
    {
        public bool HasNextPage { get; set; }

        public string? EndCursor { get; set; }
    }

    private sealed class RailwayServiceEdge
    {
        public RailwayServiceNode Node { get; set; } = new();
    }

    private sealed class RailwayEnvironmentConnection
    {
        public IReadOnlyList<RailwayEnvironmentEdge> Edges { get; set; } = [];
    }

    private sealed class RailwayEnvironmentEdge
    {
        public RailwayEnvironmentNode Node { get; set; } = new();
    }

    private sealed class RailwayEnvironmentNode
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class GetServiceData
    {
        public RailwayServiceNode? Service { get; set; }

        public RailwayServiceInstance? ServiceInstance { get; set; }

        public JsonElement Variables { get; set; }
    }

    private sealed class RailwayServiceNode
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;

        public string? DeletedAt { get; set; }
    }

    private sealed class RailwayServiceInstance
    {
        public RailwayDeployment? LatestDeployment { get; set; }
    }

    private sealed class RailwayDeployment
    {
        public string? Status { get; set; }

        public bool? DeploymentStopped { get; set; }

        public JsonElement Meta { get; set; }
    }

    private sealed class RailwayDeploymentManifestState
    {
        public static RailwayDeploymentManifestState Empty { get; } = new();

        private readonly JsonElement? _meta;

        private RailwayDeploymentManifestState()
        {
        }

        public RailwayDeploymentManifestState(JsonElement meta)
        {
            _meta = meta;
            HasVolumeMount = DeploymentManifestHasVolumeMount(meta);
        }

        public bool HasVolumeMount { get; }

        public bool UsesRegion(string regionId)
        {
            return _meta is { ValueKind: JsonValueKind.Object } meta
                && DeploymentManifestUsesRegion(meta, regionId);
        }
    }
}
