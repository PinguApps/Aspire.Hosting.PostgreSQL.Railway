namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal interface IRailwayPostgresManagementClient
{
    public Task<IReadOnlyList<RailwayPostgresDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> CreateDatabaseAsync(RailwayPostgresCreateDatabaseRequest request, CancellationToken cancellationToken);

    public Task UpdateReadRegionsAsync(string databaseId, RailwayPostgresUpdateRegionsRequest request, CancellationToken cancellationToken);

    public Task ChangePlanAsync(string databaseId, RailwayPostgresChangePlanRequest request, CancellationToken cancellationToken);

    public Task UpdateBudgetAsync(string databaseId, RailwayPostgresUpdateBudgetRequest request, CancellationToken cancellationToken);

    public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken);

    public Task<RailwayPostgresDatabaseDetails> WaitUntilReadyAsync(
        string databaseId,
        RailwayPostgresReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken);
}
