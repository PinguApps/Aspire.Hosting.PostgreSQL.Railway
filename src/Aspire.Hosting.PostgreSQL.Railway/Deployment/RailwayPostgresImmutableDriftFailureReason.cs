namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal enum RailwayPostgresImmutableDriftFailureReason
{
    DatabaseNameMismatch,
    PlatformMismatch,
    PrimaryRegionMismatch,
    TlsDisabled
}
