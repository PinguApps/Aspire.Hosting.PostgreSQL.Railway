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

        return Create(
            uri.IdnHost,
            uri.Port > 0 ? uri.Port : 5432,
            Uri.UnescapeDataString(userInfo[0]),
            Uri.UnescapeDataString(userInfo[1]),
            databaseName);
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
