namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresDatabaseDetails
{
    public string ServiceId { get; init; } = string.Empty;

    public string ServiceName { get; init; } = string.Empty;

    public string ProjectId { get; init; } = string.Empty;

    public string EnvironmentId { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string ConnectionString { get; init; } = string.Empty;

    public string ProvisioningConnectionString { get; init; } = string.Empty;

    public string? LatestDeploymentStatus { get; init; }

    public bool HasConnectionVariables =>
        !string.IsNullOrWhiteSpace(Host)
        && Port > 0
        && !string.IsNullOrWhiteSpace(UserName)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(DatabaseName)
        && !string.IsNullOrWhiteSpace(ConnectionString);

    public RailwayPostgresDatabaseDetails WithDatabaseName(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return new RailwayPostgresDatabaseDetails
        {
            ServiceId = ServiceId,
            ServiceName = ServiceName,
            ProjectId = ProjectId,
            EnvironmentId = EnvironmentId,
            Host = Host,
            Port = Port,
            UserName = UserName,
            Password = Password,
            DatabaseName = databaseName,
            ConnectionString = RailwayPostgresConnectionString.WithDatabaseName(ConnectionString, databaseName),
            ProvisioningConnectionString = string.IsNullOrWhiteSpace(ProvisioningConnectionString)
                ? string.Empty
                : RailwayPostgresConnectionString.WithDatabaseName(ProvisioningConnectionString, databaseName),
            LatestDeploymentStatus = LatestDeploymentStatus,
        };
    }
}
