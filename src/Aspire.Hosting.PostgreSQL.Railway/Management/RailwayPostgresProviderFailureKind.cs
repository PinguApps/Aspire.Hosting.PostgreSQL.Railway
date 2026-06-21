namespace Aspire.Hosting.PostgreSQL.Railway.Management;

internal enum RailwayPostgresProviderFailureKind
{
    Validation,
    Authentication,
    Authorization,
    NotFound,
    RateLimited,
    Transient,
    ProviderContract,
    Unexpected,
}
