namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal enum RailwayPostgresDeploymentPhase
{
    ResolvingConfiguration,
    LocatingDatabase,
    CreatingDatabase,
    ConfiguringService,
    RetrievingOutputs,
}
