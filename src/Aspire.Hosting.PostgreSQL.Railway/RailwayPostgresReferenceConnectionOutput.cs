using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using Npgsql;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresReferenceConnectionOutput : IResourceWithConnectionString
{
    private readonly Func<CancellationToken, ValueTask<string?>> _getConnectionStringAsync;
    private readonly RailwayPostgresOutputs _outputs;

    private RailwayPostgresReferenceConnectionOutput(
        RailwayPostgresOutputs outputs,
        ReferenceExpression databaseNameExpression,
        ReferenceExpression connectionStringExpression,
        Func<CancellationToken, ValueTask<string?>> getConnectionStringAsync)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(databaseNameExpression);
        ArgumentNullException.ThrowIfNull(connectionStringExpression);
        ArgumentNullException.ThrowIfNull(getConnectionStringAsync);

        _outputs = outputs;
        DatabaseNameExpression = databaseNameExpression;
        _getConnectionStringAsync = getConnectionStringAsync;
        ConnectionStringExpression = connectionStringExpression;
    }

    public string Name => "railway-postgres-reference-connection-output";

    public ResourceAnnotationCollection Annotations { get; } = [];

    public ReferenceExpression DatabaseNameExpression { get; }

    public ReferenceExpression ConnectionStringExpression { get; }

    public static RailwayPostgresReferenceConnectionOutput ForServer(RailwayPostgresOutputs outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        return new(
            outputs,
            outputs.DatabaseName.AsReferenceExpression(),
            outputs.ConnectionString.AsReferenceExpression(),
            outputs.ConnectionString.GetValueAsync);
    }

    public static RailwayPostgresReferenceConnectionOutput ForDatabase(RailwayPostgresOutputs outputs, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return new(
            outputs,
            ReferenceExpression.Create($"{databaseName}"),
            ReferenceExpression.Create($"{outputs.ConnectionString};{CreateDatabaseConnectionStringFragment(databaseName)}"),
            async cancellationToken =>
            {
                string? connectionString = await outputs.ConnectionString.GetValueAsync(cancellationToken).ConfigureAwait(false);

                return string.IsNullOrWhiteSpace(connectionString)
                    ? connectionString
                    : RailwayPostgresConnectionString.WithDatabaseName(connectionString, databaseName);
            });
    }

    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return await _getConnectionStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public IEnumerable<KeyValuePair<string, ReferenceExpression>> GetConnectionProperties()
    {
        yield return new("Host", _outputs.Host.AsReferenceExpression());
        yield return new("Port", _outputs.Port.AsReferenceExpression());
        yield return new("Username", _outputs.UserName.AsReferenceExpression());
        yield return new("Password", _outputs.Password.AsReferenceExpression());
        yield return new("Database", DatabaseNameExpression);
        yield return new("DatabaseName", DatabaseNameExpression);
        yield return new(
            "Uri",
            ReferenceExpression.Create($"postgresql://{_outputs.UserName}:{_outputs.Password}@{_outputs.Host}:{_outputs.Port}/{DatabaseNameExpression}"));
        yield return new(
            "JdbcConnectionString",
            ReferenceExpression.Create($"jdbc:postgresql://{_outputs.Host}:{_outputs.Port}/{DatabaseNameExpression}"));
    }

    private static string CreateDatabaseConnectionStringFragment(string databaseName)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }
}
