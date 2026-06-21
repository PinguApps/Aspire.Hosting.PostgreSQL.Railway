namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal enum RailwayPostgresOwnershipResolutionFailureReason
{
    Unknown,
    CreateOnlyDatabaseAlreadyExists,
    ExistingOnlyDatabaseMissing,
    ExistingDatabaseIncompatible
}
