namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Optional Railway PostgreSQL deployment settings.
/// </summary>
public sealed class RailwayPostgresDeploymentOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RailwayPostgresDeploymentOptions"/> class.
    /// </summary>
    public RailwayPostgresDeploymentOptions()
    {
    }

    internal RailwayPostgresDeploymentOptions(RailwayPostgresDeploymentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Region = source.Region;
        RestartPolicy = source.RestartPolicy;
        RestartPolicyMaxRetries = source.RestartPolicyMaxRetries;
        MemoryGB = source.MemoryGB;
        VCpus = source.VCpus;
        SharedMemoryBytes = source.SharedMemoryBytes;
        PointInTimeRecovery = source.PointInTimeRecovery;

        Validate();
    }

    /// <summary>
    /// Gets or sets the Railway region for the PostgreSQL service.
    /// </summary>
    public RailwayPostgresRegions? Region { get; set; }

    /// <summary>
    /// Gets or sets the Railway restart policy for the PostgreSQL service.
    /// </summary>
    public RailwayPostgresRestartPolicy? RestartPolicy { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of restart attempts Railway should make for the PostgreSQL service.
    /// </summary>
    public int? RestartPolicyMaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the Railway memory limit, in GB, for the PostgreSQL service instance.
    /// </summary>
    public double? MemoryGB { get; set; }

    /// <summary>
    /// Gets or sets the Railway vCPU limit for the PostgreSQL service instance.
    /// </summary>
    public double? VCpus { get; set; }

    /// <summary>
    /// Gets or sets the PostgreSQL container shared memory size in bytes.
    /// </summary>
    public long? SharedMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets whether new Railway PostgreSQL services use Railway's Postgres PITR template.
    /// </summary>
    public bool PointInTimeRecovery { get; set; }

    internal bool HasServiceInstanceSettings =>
        Region is not null
        || RestartPolicy is not null
        || RestartPolicyMaxRetries is not null;

    internal bool HasResourceLimits =>
        MemoryGB is not null
        || VCpus is not null;

    internal bool HasServiceVariables =>
        SharedMemoryBytes is not null;

    internal bool HasAny =>
        HasServiceInstanceSettings
        || HasResourceLimits
        || HasServiceVariables;

    internal void Validate()
    {
        if (Region is not null && !Enum.IsDefined(Region.Value))
        {
            throw new InvalidOperationException("Railway PostgreSQL region is not supported.");
        }

        if (RestartPolicy is not null && !Enum.IsDefined(RestartPolicy.Value))
        {
            throw new InvalidOperationException("Railway PostgreSQL restart policy is not supported.");
        }

        if (RestartPolicyMaxRetries is < 0)
        {
            throw new InvalidOperationException("Railway PostgreSQL restart policy max retries cannot be negative.");
        }

        if (MemoryGB is <= 0)
        {
            throw new InvalidOperationException("Railway PostgreSQL memory limit must be greater than zero.");
        }

        if (VCpus is <= 0)
        {
            throw new InvalidOperationException("Railway PostgreSQL vCPU limit must be greater than zero.");
        }

        if (SharedMemoryBytes is <= 0)
        {
            throw new InvalidOperationException("Railway PostgreSQL shared memory size must be greater than zero.");
        }
    }
}
