using System.Net.Http.Headers;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresManagementCredentials
{
    public RailwayPostgresManagementCredentials(
        string apiToken,
        RailwayPostgresAuthenticationMode authenticationMode = RailwayPostgresAuthenticationMode.Bearer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);

        ApiToken = apiToken;
        AuthenticationMode = authenticationMode;
    }

    public string ApiToken { get; }

    public RailwayPostgresAuthenticationMode AuthenticationMode { get; }

    public void ApplyTo(HttpRequestMessage request)
    {
        if (AuthenticationMode == RailwayPostgresAuthenticationMode.ProjectToken)
        {
            request.Headers.Add("Project-Access-Token", ApiToken);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
        }
    }
}
