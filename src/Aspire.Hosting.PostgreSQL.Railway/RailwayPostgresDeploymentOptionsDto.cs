namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// TypeScript-friendly Railway PostgreSQL deployment options.
/// </summary>
[AspireDto]
public sealed class RailwayPostgresDeploymentOptionsDto
{
    /// <summary>
    /// Gets or sets how deployment should treat a database with the requested name.
    /// </summary>
    public RailwayPostgresOwnershipMode? OwnershipMode { get; set; }

    /// <summary>
    /// Gets or sets the Railway region identifier for the PostgreSQL service.
    /// </summary>
    public string? Region { get; set; }

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

    internal RailwayPostgresOwnershipMode GetOwnershipMode()
    {
        return OwnershipMode ?? RailwayPostgresOwnershipMode.CreateOrAdopt;
    }

    internal RailwayPostgresDeploymentOptions ToDeploymentOptions()
    {
        return new RailwayPostgresDeploymentOptions
        {
            Region = Region,
            RestartPolicy = RestartPolicy,
            RestartPolicyMaxRetries = RestartPolicyMaxRetries,
            MemoryGB = MemoryGB,
            VCpus = VCpus,
            SharedMemoryBytes = SharedMemoryBytes,
        };
    }
}
