namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal enum RailwayPostgresDeploymentPhase
{
    ResolvingConfiguration,
    LocatingDatabase,
    ValidatingImmutableDrift,
    CreatingDatabase,
    ReconcilingMutableSettings,
    RetrievingOutputs,
}
