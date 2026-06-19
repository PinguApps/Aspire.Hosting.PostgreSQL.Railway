namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal sealed class FakeRailwayProviderInteraction
{
    public FakeRailwayProviderInteraction(string name, string databaseName)
    {
        Name = name;
        DatabaseName = databaseName;
    }

    public string Name { get; }

    public string DatabaseName { get; }
}
