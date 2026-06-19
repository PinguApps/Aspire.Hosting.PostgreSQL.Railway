namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresImmutableDriftException : InvalidOperationException
{
    public RailwayPostgresImmutableDriftException()
    {
    }

    public RailwayPostgresImmutableDriftException(string message)
        : base(message)
    {
    }

    public RailwayPostgresImmutableDriftException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RailwayPostgresImmutableDriftException(RailwayPostgresImmutableDrift drift)
        : base(GetMessage(drift))
    {
        Drift = drift;
    }

    public RailwayPostgresImmutableDrift? Drift { get; }

    private static string GetMessage(RailwayPostgresImmutableDrift drift)
    {
        ArgumentNullException.ThrowIfNull(drift);

        return drift.Message;
    }
}
