using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class ManagementClientStepDefinitions : IDisposable
{
    private const string DefaultAccountEmail = "pingu@example.com";
    private const string DefaultApiKey = "secret-key";

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;
    private RailwayPostgresDatabaseDetails? _lastDatabase;
    private Exception? _lastException;

    public ManagementClientStepDefinitions()
    {
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.railway.com/v2/"),
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Given("the Railway management API returns an empty database list")]
    public void GivenTheRailwayManagementApiReturnsAnEmptyDatabaseList()
    {
        _handler.Enqueue(HttpStatusCode.OK, "[]");
    }

    [Given("the Railway management API returns a list containing database {string}")]
    public void GivenTheRailwayManagementApiReturnsAListContainingDatabase(string databaseName)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "db-orders",
                "database_name": "{{databaseName}}",
                "endpoint": "global-apt-1.railway.io",
                "port": 6379,
                "state": "active",
                "primary_region": "eu-west-1",
                "read_regions": ["eu-west-2"]
              }
            ]
            """);
    }

    [Given("the Railway management API returns duplicate databases named {string}")]
    public void GivenTheRailwayManagementApiReturnsDuplicateDatabasesNamed(string databaseName)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "db-orders-1",
                "database_name": "{{databaseName}}"
              },
              {
                "database_id": "db-orders-2",
                "database_name": "{{databaseName}}"
              }
            ]
            """);
    }

    [Given("the Railway management API returns database details for {string}")]
    public void GivenTheRailwayManagementApiReturnsDatabaseDetailsFor(string databaseName)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, includePassword: true));
    }

    [Given("the Railway management API returns database details for {string} with id {string}")]
    public void GivenTheRailwayManagementApiReturnsDatabaseDetailsForWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, includePassword: true, databaseId: databaseId));
    }

    [Given("the Railway management API returns database details without a password")]
    public void GivenTheRailwayManagementApiReturnsDatabaseDetailsWithoutAPassword()
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: false));
    }

    [Given("the Railway management API returns status {int} with error {string}")]
    public void GivenTheRailwayManagementApiReturnsStatusWithError(int statusCode, string error)
    {
        _handler.Enqueue((HttpStatusCode)statusCode, $$"""{ "error": "{{error}}" }""");
    }

    [Given("the Railway management API returns OK for five operations")]
    public void GivenTheRailwayManagementApiReturnsOkForFiveOperations()
    {
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
    }

    [Given("the Railway management API returns a modifying database then an active database")]
    public void GivenTheRailwayManagementApiReturnsAModifyingDatabaseThenAnActiveDatabase()
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: true, state: "active", modifyingState: "updating"));
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: true));
    }

    [Given("the Railway management API waits until cancellation")]
    public void GivenTheRailwayManagementApiWaitsUntilCancellation()
    {
        _handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            };
        });
    }

    [Given("the Railway management API fails before responding with {string}")]
    public void GivenTheRailwayManagementApiFailsBeforeRespondingWith(string failure)
    {
        _handler.Enqueue((_, _) =>
        {
            if (failure == "RequestException")
            {
                throw new HttpRequestException("The Railway host could not be reached.");
            }

            if (failure == "Timeout")
            {
                throw new TaskCanceledException("The Railway request timed out.");
            }

            throw new InvalidOperationException($"Unknown transport failure '{failure}'.");
        });
    }

    [When("the Railway management client lists databases with account {string} and API key {string}")]
    public async Task WhenTheRailwayManagementClientListsDatabasesWithAccountAndApiKey(string accountEmail, string apiKey)
    {
        await CaptureExceptionAsync(async () =>
        {
            IRailwayPostgresManagementClient client = CreateClient(accountEmail, apiKey);
            await client.ListDatabasesAsync(CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Railway management client gets database {string}")]
    public async Task WhenTheRailwayManagementClientGetsDatabase(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().GetDatabaseAsync(databaseId, CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Railway management client finds database {string} by name")]
    public async Task WhenTheRailwayManagementClientFindsDatabaseByName(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().FindDatabaseByNameAsync(databaseName, CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Railway management client creates database {string}")]
    public async Task WhenTheRailwayManagementClientCreatesDatabase(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
        {
            _lastDatabase = await CreateClient().CreateDatabaseAsync(
                new RailwayPostgresCreateDatabaseRequest
                {
                    DatabaseName = databaseName,
                    Platform = "aws",
                    PrimaryRegion = "eu-west-1",
                    ReadRegions = ["eu-west-2"],
                    Plan = "payg",
                    Budget = 50,
                    Eviction = true,
                    Tls = true,
                },
                CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Railway management client updates mutable settings for database {string}")]
    public async Task WhenTheRailwayManagementClientUpdatesMutableSettingsForDatabase(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
        {
            IRailwayPostgresManagementClient client = CreateClient();

            await client.UpdateReadRegionsAsync(
                databaseId,
                new RailwayPostgresUpdateRegionsRequest { ReadRegions = ["eu-west-2"] },
                CancellationToken.None).ConfigureAwait(false);

            await client.ChangePlanAsync(
                databaseId,
                new RailwayPostgresChangePlanRequest { PlanName = "payg" },
                CancellationToken.None).ConfigureAwait(false);

            await client.UpdateBudgetAsync(
                databaseId,
                new RailwayPostgresUpdateBudgetRequest { Budget = 50 },
                CancellationToken.None).ConfigureAwait(false);

            await client.SetEvictionAsync(databaseId, enabled: true, CancellationToken.None).ConfigureAwait(false);
            await client.SetEvictionAsync(databaseId, enabled: false, CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Railway management client waits for database {string} to become ready")]
    public async Task WhenTheRailwayManagementClientWaitsForDatabaseToBecomeReady(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().WaitUntilReadyAsync(
                databaseId,
                new RailwayPostgresReadinessPollingOptions
                {
                    Timeout = TimeSpan.FromSeconds(1),
                    Delay = TimeSpan.Zero,
                },
                CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Railway management client lists databases with a cancelled token")]
    public async Task WhenTheRailwayManagementClientListsDatabasesWithACancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync().ConfigureAwait(false);

        await CaptureExceptionAsync(async () =>
            await CreateClient().ListDatabasesAsync(cancellation.Token).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("a general Railway provider exception is created with constructor {string}")]
    public void WhenAGeneralRailwayProviderExceptionIsCreatedWithConstructor(string constructor)
    {
        _lastException = constructor switch
        {
            "Parameterless" => new RailwayPostgresProviderException(),
            "Message" => new RailwayPostgresProviderException("Provider failure."),
            "MessageAndInner" => new RailwayPostgresProviderException("Provider failure.", new InvalidOperationException()),
            _ => throw new InvalidOperationException($"Unknown constructor '{constructor}'."),
        };
    }

    [Then("the Railway management request uses {word} {string}")]
    public void ThenTheRailwayManagementRequestUses(string method, string path)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);

        Assert.Equal(method, request.Method.Method);
        Assert.Equal(path, request.PathAndQuery);
    }

    [Then("the Railway management request has the expected Basic auth header for account {string} and API key {string}")]
    public void ThenTheRailwayManagementRequestHasTheExpectedBasicAuthHeaderForAccountAndApiKey(string accountEmail, string apiKey)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);
        string expectedParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountEmail}:{apiKey}"));

        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(expectedParameter, request.AuthorizationParameter);
    }

    [Then("the Railway management client returns database {string} with credentials")]
    public void ThenTheRailwayManagementClientReturnsDatabaseWithCredentials(string databaseName)
    {
        RailwayPostgresDatabaseDetails database =
            _lastDatabase ?? throw new InvalidOperationException("No database was returned.");

        Assert.Equal("db-orders", database.DatabaseId);
        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal("global-apt-1.railway.io", database.Endpoint);
        Assert.Equal(6379, database.Port);
        Assert.Equal("redis-password", database.Password);
        Assert.True(database.Tls);
        Assert.Equal("active", database.State);
        Assert.Null(database.ModifyingState);
        Assert.Equal("eu-west-1", database.PrimaryRegion);
        Assert.Equal(["eu-west-2"], database.ReadRegions);
        Assert.Equal("payg", database.Type);
        Assert.Equal(107374182400, database.DbDiskThreshold);
        Assert.Equal(50, database.Budget);
        Assert.True(database.Eviction);
        Assert.Equal("cust-1", database.CustomerId);
    }

    [Then("the Railway management request sequence is:")]
    public void ThenTheRailwayManagementRequestSequenceIs(DataTable table)
    {
        Assert.Equal(table.Rows.Count, _handler.Requests.Count);

        for (int requestIndex = 0; requestIndex < table.Rows.Count; requestIndex++)
        {
            Assert.Equal(table.Rows[requestIndex]["Method"], _handler.Requests[requestIndex].Method.Method);
            Assert.Equal(table.Rows[requestIndex]["Path"], _handler.Requests[requestIndex].PathAndQuery);
        }
    }

    [Then("the Railway management request body contains:")]
    public void ThenTheRailwayManagementRequestBodyContains(DataTable table)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);
        Assert.NotNull(request.Content);

        using JsonDocument document = JsonDocument.Parse(request.Content);

        foreach (DataTableRow row in table.Rows)
        {
            JsonElement value = document.RootElement.GetProperty(row["Property"]);
            Assert.Equal(row["Value"], value.ValueKind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.GetInt32().ToString(),
                _ => value.GetString(),
            });
        }
    }

    [Then("the Railway management client fails with provider kind {string}")]
    public void ThenTheRailwayManagementClientFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_lastException);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway management failure message does not contain {string}")]
    public void ThenTheRailwayManagementFailureMessageDoesNotContain(string value)
    {
        Exception exception = _lastException ?? throw new InvalidOperationException("No exception was captured.");

        Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Railway management client did not request reset-password")]
    public void ThenTheRailwayManagementClientDidNotRequestResetPassword()
    {
        Assert.DoesNotContain(_handler.Requests, request => request.PathAndQuery.Contains("reset-password", StringComparison.Ordinal));
    }

    [Then("the Railway management client operation is cancelled")]
    public void ThenTheRailwayManagementClientOperationIsCancelled()
    {
        Assert.IsAssignableFrom<OperationCanceledException>(_lastException);
    }

    private IRailwayPostgresManagementClient CreateClient(
        string accountEmail = DefaultAccountEmail,
        string apiKey = DefaultApiKey)
    {
        return new RailwayPostgresManagementClient(
            _httpClient,
            new RailwayPostgresManagementCredentials(accountEmail, apiKey));
    }

    private async Task CaptureExceptionAsync(Func<Task> operation)
    {
        _lastException = await Record.ExceptionAsync(operation).ConfigureAwait(false);
    }

    private static string CreateDatabaseDetailsJson(
        string databaseName,
        bool includePassword,
        string state = "active",
        string? modifyingState = null,
        string databaseId = "db-orders")
    {
        string passwordJson = includePassword ? "\"password\": \"redis-password\"," : string.Empty;
        string modifyingStateJson = modifyingState is null ? "null" : $"\"{modifyingState}\"";

        return $$"""
        {
          "database_id": "{{databaseId}}",
          "database_name": "{{databaseName}}",
          "endpoint": "global-apt-1.railway.io",
          "port": 6379,
          {{passwordJson}}
          "tls": true,
          "state": "{{state}}",
          "modifying_state": {{modifyingStateJson}},
          "primary_region": "eu-west-1",
          "read_regions": ["eu-west-2"],
          "type": "payg",
          "db_disk_threshold": 107374182400,
          "budget": 50,
          "eviction": true,
          "customer_id": "cust-1"
        }
        """;
    }
}
