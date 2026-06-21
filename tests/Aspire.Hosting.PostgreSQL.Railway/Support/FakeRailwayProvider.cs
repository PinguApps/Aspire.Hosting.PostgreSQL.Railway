namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal sealed class FakeRailwayProvider
{
    private readonly List<FakeRailwayPostgresDatabase> _databases = [];
    private readonly List<FakeRailwayProviderInteraction> _interactions = [];

    public IReadOnlyList<FakeRailwayPostgresDatabase> Databases => _databases;

    public IReadOnlyList<FakeRailwayProviderInteraction> Interactions => _interactions;

    public void AddDatabase(FakeRailwayPostgresDatabase database)
    {
        _databases.Add(database);
    }

    public FakeRailwayPostgresDatabase? FindByName(string databaseName)
    {
        _interactions.Add(new FakeRailwayProviderInteraction("find-by-name", databaseName));

        return _databases.SingleOrDefault(database => database.DatabaseName == databaseName);
    }
}
