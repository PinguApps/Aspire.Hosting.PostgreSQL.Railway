namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresDeploymentState
{
    public RailwayPostgresDeploymentState(
        RailwayPostgresValue serviceName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresValue projectId,
        RailwayPostgresValue environmentId,
        RailwayPostgresValue apiToken,
        RailwayPostgresDeploymentOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(environmentId);
        ArgumentNullException.ThrowIfNull(apiToken);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
        }

        ServiceName = serviceName;
        OwnershipMode = ownershipMode;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        ApiToken = apiToken;
        OptionsSnapshot = new RailwayPostgresDeploymentOptions(options);
    }

    public RailwayPostgresValue ServiceName { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public RailwayPostgresValue ProjectId { get; }

    public RailwayPostgresValue EnvironmentId { get; }

    public RailwayPostgresValue ApiToken { get; }

    public RailwayPostgresDeploymentOptions Options => new(OptionsSnapshot);

    private RailwayPostgresDeploymentOptions OptionsSnapshot { get; }
}
