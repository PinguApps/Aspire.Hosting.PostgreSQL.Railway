namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal sealed class FakeRailwayPostgresDatabase
{
    public FakeRailwayPostgresDatabase(
        string databaseName,
        string databaseId,
        string primaryRegion,
        string endpoint,
        int port,
        string password,
        bool tlsEnabled)
    {
        DatabaseName = databaseName;
        DatabaseId = databaseId;
        PrimaryRegion = primaryRegion;
        Endpoint = endpoint;
        Port = port;
        Password = password;
        TlsEnabled = tlsEnabled;
    }

    public string DatabaseName { get; }

    public string DatabaseId { get; }

    public string PrimaryRegion { get; }

    public string Endpoint { get; }

    public int Port { get; }

    public string Password { get; }

    public bool TlsEnabled { get; }
}
