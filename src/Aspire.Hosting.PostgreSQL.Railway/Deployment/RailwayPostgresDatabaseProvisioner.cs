using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Npgsql;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal static class RailwayPostgresDatabaseProvisioner
{
    public static async Task EnsureDatabasesAsync(
        RailwayPostgresDatabaseDetails service,
        IEnumerable<string> databaseNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(databaseNames);

        List<string> distinctDatabaseNames =
        [
            .. databaseNames
            .Where(static databaseName => !string.IsNullOrWhiteSpace(databaseName))
            .Distinct(StringComparer.Ordinal)
            .Where(databaseName => !string.Equals(databaseName, service.DatabaseName, StringComparison.Ordinal))
        ];

        if (distinctDatabaseNames.Count == 0)
        {
            return;
        }

        string connectionString = string.IsNullOrWhiteSpace(service.ProvisioningConnectionString)
            ? service.ConnectionString
            : service.ProvisioningConnectionString;

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (string databaseName in distinctDatabaseNames)
        {
            await EnsureDatabaseAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "PostgreSQL identifiers cannot be parameterized; database names are quoted as identifiers.")]
    private static async Task EnsureDatabaseAsync(
        NpgsqlConnection connection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        object? exists = await existsCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (exists is not null)
        {
            return;
        }

        await using NpgsqlCommand createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string QuoteIdentifier(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
