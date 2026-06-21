namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal interface IRailwayPostgresDeploymentProgressReporter
{
    public void Report(RailwayPostgresDeploymentProgress progress);
}
