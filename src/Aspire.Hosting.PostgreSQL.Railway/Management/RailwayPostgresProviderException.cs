using System.Net;

namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal sealed class RailwayPostgresProviderException : Exception
{
    public RailwayPostgresProviderException()
    {
    }

    public RailwayPostgresProviderException(string message)
        : base(message)
    {
    }

    public RailwayPostgresProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RailwayPostgresProviderException(
        RailwayPostgresProviderFailureKind failureKind,
        HttpStatusCode? statusCode,
        string message)
        : base(message)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public RailwayPostgresProviderException(
        RailwayPostgresProviderFailureKind failureKind,
        HttpStatusCode? statusCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public RailwayPostgresProviderFailureKind FailureKind { get; } = RailwayPostgresProviderFailureKind.Unexpected;

    public HttpStatusCode? StatusCode { get; }
}
