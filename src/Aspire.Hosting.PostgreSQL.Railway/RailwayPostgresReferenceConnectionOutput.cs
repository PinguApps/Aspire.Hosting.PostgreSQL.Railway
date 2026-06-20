using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresReferenceConnectionOutput : IResourceWithConnectionString
{
    private readonly Func<CancellationToken, ValueTask<string?>> _getDatabaseNameAsync;
    private readonly RailwayPostgresOutputs _outputs;

    private RailwayPostgresReferenceConnectionOutput(
        RailwayPostgresOutputs outputs,
        ReferenceExpression databaseNameExpression,
        Func<CancellationToken, ValueTask<string?>> getDatabaseNameAsync)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(databaseNameExpression);
        ArgumentNullException.ThrowIfNull(getDatabaseNameAsync);

        _outputs = outputs;
        DatabaseNameExpression = databaseNameExpression;
        _getDatabaseNameAsync = getDatabaseNameAsync;
        ConnectionStringExpression = ReferenceExpression.Create(
            $"Host={outputs.Host};Port={outputs.Port};Username={outputs.UserName};Password={outputs.Password};Database={databaseNameExpression};SSL Mode=Require");
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
            outputs.DatabaseName.GetValueAsync);
    }

    public static RailwayPostgresReferenceConnectionOutput ForDatabase(RailwayPostgresOutputs outputs, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return new(
            outputs,
            ReferenceExpression.Create($"{databaseName}"),
            cancellationToken => ValueTask.FromResult<string?>(databaseName));
    }

    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        string? host = await _outputs.Host.GetValueAsync(cancellationToken).ConfigureAwait(false);
        string? port = await _outputs.Port.GetValueAsync(cancellationToken).ConfigureAwait(false);
        string? userName = await _outputs.UserName.GetValueAsync(cancellationToken).ConfigureAwait(false);
        string? password = await _outputs.Password.GetValueAsync(cancellationToken).ConfigureAwait(false);
        string? databaseName = await _getDatabaseNameAsync(cancellationToken).ConfigureAwait(false);

        return $"Host={host};Port={port};Username={userName};Password={password};Database={databaseName};SSL Mode=Require";
    }

    public IEnumerable<KeyValuePair<string, ReferenceExpression>> GetConnectionProperties()
    {
        yield return new("Host", _outputs.Host.AsReferenceExpression());
        yield return new("Port", _outputs.Port.AsReferenceExpression());
        yield return new("Username", _outputs.UserName.AsReferenceExpression());
        yield return new("Password", _outputs.Password.AsReferenceExpression());
        yield return new("Database", DatabaseNameExpression);
    }
}
