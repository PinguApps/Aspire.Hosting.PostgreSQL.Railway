using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresConnectionOutput : IResourceWithConnectionString
{
    public RailwayPostgresConnectionOutput(RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        ServiceId = database.ServiceId;
        Host = database.Host;
        Port = database.Port;
        UserName = database.UserName;
        Password = database.Password;
        DatabaseName = database.DatabaseName;
        ConnectionString = database.ConnectionString;
        ConnectionStringExpression = ReferenceExpression.Create($"{ConnectionString}");
    }

    public string Name => "railway-postgres-connection-output";

    public ResourceAnnotationCollection Annotations { get; } = [];

    public string ServiceId { get; }

    public string Host { get; }

    public int Port { get; }

    public string UserName { get; }

    public string Password { get; }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    public ReferenceExpression ConnectionStringExpression { get; }

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(ConnectionString);
    }

    public IEnumerable<KeyValuePair<string, ReferenceExpression>> GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        yield return new("Username", ReferenceExpression.Create($"{UserName}"));
        yield return new("Password", ReferenceExpression.Create($"{Password}"));
        yield return new("Database", ReferenceExpression.Create($"{DatabaseName}"));
        yield return new("DatabaseName", ReferenceExpression.Create($"{DatabaseName}"));
        yield return new("Uri", ReferenceExpression.Create($"{CreateUri()}"));
        yield return new("JdbcConnectionString", ReferenceExpression.Create($"{CreateJdbcConnectionString()}"));
    }

    private string CreateUri()
    {
        return $"postgresql://{Uri.EscapeDataString(UserName)}:{Uri.EscapeDataString(Password)}@{Host}:{Port.ToString(System.Globalization.CultureInfo.InvariantCulture)}/{Uri.EscapeDataString(DatabaseName)}";
    }

    private string CreateJdbcConnectionString()
    {
        return $"jdbc:postgresql://{Host}:{Port.ToString(System.Globalization.CultureInfo.InvariantCulture)}/{Uri.EscapeDataString(DatabaseName)}";
    }
}
