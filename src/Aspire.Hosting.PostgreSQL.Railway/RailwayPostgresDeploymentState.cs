namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresDeploymentState
{
    public RailwayPostgresDeploymentState(
        RailwayPostgresValue databaseName,
        RailwayPostgresOwnershipMode ownershipMode,
        RailwayPostgresValue accountEmail,
        RailwayPostgresValue apiKey,
        RailwayPostgresDeploymentOptions options)
    {
        ArgumentNullException.ThrowIfNull(databaseName);
        ArgumentNullException.ThrowIfNull(accountEmail);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Railway PostgreSQL ownership mode is not supported.");
        }

        options.ToProviderOptions();

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        AccountEmail = accountEmail;
        ApiKey = apiKey;
        OptionsSnapshot = new RailwayPostgresDeploymentOptions(options);
    }

    public RailwayPostgresValue DatabaseName { get; }

    public RailwayPostgresOwnershipMode OwnershipMode { get; }

    public RailwayPostgresValue AccountEmail { get; }

    public RailwayPostgresValue ApiKey { get; }

    public RailwayPostgresDeploymentOptions Options => new(OptionsSnapshot);

    private RailwayPostgresDeploymentOptions OptionsSnapshot { get; }
}
