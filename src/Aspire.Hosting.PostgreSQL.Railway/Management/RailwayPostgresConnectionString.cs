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

    public static string CreateFromUri(string connectionUri)
    {
        return CreateDetailsFromUri(connectionUri).ConnectionString;
    }

    public static RailwayPostgresConnectionDetails CreateDetailsFromUri(string connectionUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionUri);

        if (!Uri.TryCreate(connectionUri, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway returned DATABASE_PUBLIC_URL in an unexpected format.");
        }

        string[] userInfo = uri.UserInfo.Split(':', count: 2);

        if (userInfo.Length != 2)
        {
            throw new RailwayPostgresProviderException(
                RailwayPostgresProviderFailureKind.ProviderContract,
                statusCode: null,
                "Railway returned DATABASE_PUBLIC_URL without PostgreSQL credentials.");
        }

        string databaseName = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        string host = uri.IdnHost;
        int port = uri.Port > 0 ? uri.Port : 5432;
        string userName = Uri.UnescapeDataString(userInfo[0]);
        string password = Uri.UnescapeDataString(userInfo[1]);

        return new RailwayPostgresConnectionDetails(
            host,
            port,
            userName,
            password,
            databaseName,
            Create(host, port, userName, password, databaseName));
    }

    public static string WithDatabaseName(string connectionString, string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        NpgsqlConnectionStringBuilder builder = new(connectionString)
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }
}

internal sealed class RailwayPostgresConnectionDetails
{
    public RailwayPostgresConnectionDetails(
        string host,
        int port,
        string userName,
        string password,
        string databaseName,
        string connectionString)
    {
        Host = host;
        Port = port;
        UserName = userName;
        Password = password;
        DatabaseName = databaseName;
        ConnectionString = connectionString;
    }

    public string Host { get; }

    public int Port { get; }

    public string UserName { get; }

    public string Password { get; }

    public string DatabaseName { get; }

    public string ConnectionString { get; }
}
