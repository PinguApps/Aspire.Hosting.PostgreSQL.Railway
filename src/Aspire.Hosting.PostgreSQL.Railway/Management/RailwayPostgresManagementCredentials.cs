using System.Net.Http.Headers;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresManagementCredentials
{
    public RailwayPostgresManagementCredentials(string apiToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);

        ApiToken = apiToken;
    }

    public string ApiToken { get; }

    public AuthenticationHeaderValue CreateAuthorizationHeader()
    {
        return new AuthenticationHeaderValue("Bearer", ApiToken);
    }
}
