namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;

internal sealed class CapturedHttpRequest
{
    public CapturedHttpRequest(
        HttpMethod method,
        string pathAndQuery,
        string? authorizationScheme,
        string? authorizationParameter,
        string? projectAccessToken,
        string? content)
    {
        Method = method;
        PathAndQuery = pathAndQuery;
        AuthorizationScheme = authorizationScheme;
        AuthorizationParameter = authorizationParameter;
        ProjectAccessToken = projectAccessToken;
        Content = content;
    }

    public HttpMethod Method { get; }

    public string PathAndQuery { get; }

    public string? AuthorizationScheme { get; }

    public string? AuthorizationParameter { get; }

    public string? ProjectAccessToken { get; }

    public string? Content { get; }
}
