namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresOwnershipResolutionException : Exception
{
    public RailwayPostgresOwnershipResolutionException()
    {
    }

    public RailwayPostgresOwnershipResolutionException(string message)
        : base(message)
    {
    }

    public RailwayPostgresOwnershipResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RailwayPostgresOwnershipResolutionException(
        RailwayPostgresOwnershipResolutionFailureReason failureReason,
        string message)
        : base(message)
    {
        FailureReason = failureReason;
    }

    public RailwayPostgresOwnershipResolutionFailureReason FailureReason { get; } = RailwayPostgresOwnershipResolutionFailureReason.Unknown;
}
