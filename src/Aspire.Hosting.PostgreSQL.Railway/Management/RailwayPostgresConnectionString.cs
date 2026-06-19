using Npgsql;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal static class RailwayPostgresConnectionString
{
    public static string Create(
        string host,
        int port,
        string userName,
        string password,
        string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = host,
            Port = port,
            Username = userName,
            Password = password,
            Database = databaseName,
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
