using System.Runtime.ExceptionServices;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal sealed class LiveRailwayTestSession : IDisposable
{
    private const int MaxDatabaseNameLength = 40;
    private const int UniqueSuffixLength = 8;
    private const int PrefixLength = MaxDatabaseNameLength - UniqueSuffixLength - 1;

    private readonly Stack<Func<Task>> _cleanupActions = [];
    private readonly HttpClient _managementHttpClient = new()
    {
        BaseAddress = new Uri("https://api.railway.com/v2/"),
    };

    public string? AccountEmail => Environment.GetEnvironmentVariable("RAILWAY_EMAIL");

    public string? ApiKey => Environment.GetEnvironmentVariable("RAILWAY_API_KEY");

    public bool HasCredentials => !string.IsNullOrWhiteSpace(AccountEmail) && !string.IsNullOrWhiteSpace(ApiKey);

    public int CleanupActionCount => _cleanupActions.Count;

    public void RegisterCleanup(Func<Task> cleanup)
    {
        ArgumentNullException.ThrowIfNull(cleanup);

        _cleanupActions.Push(cleanup);
    }

    public RailwayPostgresManagementClient CreateManagementClient()
    {
        return new RailwayPostgresManagementClient(
            _managementHttpClient,
            CreateCredentials());
    }

    public static string CreateDisposableDatabaseName(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        string truncatedPrefix = prefix[..Math.Min(prefix.Length, PrefixLength)];
        string uniqueSuffix = $"{Guid.NewGuid():N}"[..UniqueSuffixLength];

        return $"{truncatedPrefix}-{uniqueSuffix}";
    }

    public void Dispose()
    {
        _managementHttpClient.Dispose();
    }

    public Task RegisterDatabaseDeletionByNameAsync(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        RegisterCleanup(() => DeleteDatabaseByNameAsync(databaseName));

        return Task.CompletedTask;
    }

    public async Task CleanupAsync()
    {
        List<Exception>? failures = null;

        try
        {
            while (_cleanupActions.TryPop(out Func<Task>? cleanup))
            {
                try
                {
                    await cleanup().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    (failures ??= []).Add(ex);
                }
            }
        }
        finally
        {
            Dispose();
        }

        if (failures is null)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException("One or more live Railway cleanup actions failed.", failures);
    }

    private RailwayPostgresManagementCredentials CreateCredentials()
    {
        return new RailwayPostgresManagementCredentials(
            AccountEmail ?? throw new InvalidOperationException("RAILWAY_EMAIL is not configured."),
            ApiKey ?? throw new InvalidOperationException("RAILWAY_API_KEY is not configured."));
    }

    private async Task DeleteDatabaseByNameAsync(string databaseName)
    {
        RailwayPostgresManagementClient client = CreateManagementClient();

        RailwayPostgresDatabaseDetails? database = await client
            .FindDatabaseByNameAsync(databaseName, CancellationToken.None)
            .ConfigureAwait(false);

        if (database is null)
        {
            return;
        }

        using HttpRequestMessage request = new(
            HttpMethod.Delete,
            $"redis/database/{Uri.EscapeDataString(database.DatabaseId)}");
        request.Headers.Authorization = CreateCredentials().CreateAuthorizationHeader();

        using HttpResponseMessage response = await _managementHttpClient
            .SendAsync(request, CancellationToken.None)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
