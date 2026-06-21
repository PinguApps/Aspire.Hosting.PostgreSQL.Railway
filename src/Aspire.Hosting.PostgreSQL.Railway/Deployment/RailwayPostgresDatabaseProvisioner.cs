using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Npgsql;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresDatabaseProvisioner
{
    private static readonly TimeSpan _provisioningTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _provisioningDelay = TimeSpan.FromSeconds(2);

    public static async Task EnsureDatabasesAsync(
        RailwayPostgresDatabaseDetails service,
        IEnumerable<RailwayPostgresDatabaseProvisioningRequest> databases,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(databases);

        List<RailwayPostgresDatabaseProvisioningRequest> distinctDatabases =
        [
            .. databases
            .Where(static database => !string.IsNullOrWhiteSpace(database.DatabaseName))
            .GroupBy(static database => database.DatabaseName, StringComparer.Ordinal)
            .Select(static group => group.First())
            .Where(database => !string.Equals(database.DatabaseName, service.DatabaseName, StringComparison.Ordinal))
        ];

        if (distinctDatabases.Count == 0)
        {
            return;
        }

        string connectionString = string.IsNullOrWhiteSpace(service.ProvisioningConnectionString)
            ? service.ConnectionString
            : service.ProvisioningConnectionString;

        await ExecuteWithRetryAsync(
            () => EnsureDatabasesOnceAsync(connectionString, distinctDatabases, cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);
    }

    internal static string CreateCreateDatabaseCommandText(RailwayPostgresDatabaseProvisioningRequest database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return string.IsNullOrWhiteSpace(database.CreationScript)
            ? $"CREATE DATABASE {QuoteIdentifier(database.DatabaseName)}"
            : database.CreationScript;
    }

    internal static bool IsTransientProvisioningException(Exception exception)
    {
        return exception is NpgsqlException
            or TimeoutException
            or IOException
            || (exception.InnerException is not null && IsTransientProvisioningException(exception.InnerException));
    }

    private static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + _provisioningTimeout;

        while (true)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return;
            }
            catch (Exception exception) when (IsTransientProvisioningException(exception)
                && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(_provisioningDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task EnsureDatabasesOnceAsync(
        string connectionString,
        IReadOnlyList<RailwayPostgresDatabaseProvisioningRequest> distinctDatabases,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (RailwayPostgresDatabaseProvisioningRequest database in distinctDatabases)
        {
            await EnsureDatabaseAsync(connection, database, cancellationToken).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "PostgreSQL identifiers cannot be parameterized; database names are quoted as identifiers.")]
    private static async Task EnsureDatabaseAsync(
        NpgsqlConnection connection,
        RailwayPostgresDatabaseProvisioningRequest database,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
        existsCommand.Parameters.AddWithValue("databaseName", database.DatabaseName);

        object? exists = await existsCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (exists is not null)
        {
            return;
        }

        await using NpgsqlCommand createCommand = connection.CreateCommand();
        createCommand.CommandText = CreateCreateDatabaseCommandText(database);
        await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string QuoteIdentifier(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
