namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Optional Railway PostgreSQL settings that should be reconciled only when explicitly configured.
/// </summary>
public sealed class RailwayPostgresDeploymentOptions
{
    private readonly HashSet<string> _explicitSettings = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RailwayPostgresDeploymentOptions"/> class.
    /// </summary>
    public RailwayPostgresDeploymentOptions()
    {
    }

    internal RailwayPostgresDeploymentOptions(RailwayPostgresDeploymentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Platform = source.Platform;
        PrimaryRegion = source.PrimaryRegion;
        ReadRegions = source.ReadRegions;
        Plan = source.Plan;
        Budget = source.Budget;
        Eviction = source.Eviction;
        Tls = source.Tls;

        _explicitSettings.Clear();
        _explicitSettings.UnionWith(source._explicitSettings);
    }

    /// <summary>
    /// Gets or sets the Railway platform or cloud provider.
    /// </summary>
    public RailwayPostgresValue? Platform
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Platform));
        }
    }

    /// <summary>
    /// Sets the Railway platform or cloud provider from the public provider enum.
    /// </summary>
    /// <param name="platform">The Railway PostgreSQL cloud platform.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="platform"/> is not supported.</exception>
    public void SetPlatform(RailwayPostgresCloudPlatform platform)
    {
        Platform = RailwayPostgresProviderDomain.MapCloudPlatform(platform);
    }

    /// <summary>
    /// Gets or sets the primary Railway region.
    /// </summary>
    public RailwayPostgresValue? PrimaryRegion
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(PrimaryRegion));
        }
    }

    /// <summary>
    /// Sets the primary Railway region from the public region enum.
    /// </summary>
    /// <param name="region">The primary Railway PostgreSQL region.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="region"/> is not supported.</exception>
    public void SetPrimaryRegion(RailwayPostgresRegion region)
    {
        PrimaryRegion = RailwayPostgresProviderDomain.MapRegion(region);
    }

    /// <summary>
    /// Gets or sets optional read regions.
    /// </summary>
    public IReadOnlyList<RailwayPostgresValue>? ReadRegions
    {
        get;
        set
        {
            field = value is null ? null : Array.AsReadOnly([.. value]);
            TrackExplicitSetting(nameof(ReadRegions));
        }
    }

    /// <summary>
    /// Sets optional read regions from the public region enum.
    /// </summary>
    /// <param name="regions">The Railway PostgreSQL read regions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regions"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A supplied region is not supported.</exception>
    public void SetReadRegions(params RailwayPostgresRegion[] regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        ReadRegions = regions
            .Select(static region => RailwayPostgresValue.FromString(RailwayPostgresProviderDomain.MapRegion(region)))
            .ToArray();
    }

    /// <summary>
    /// Gets or sets the Railway plan.
    /// </summary>
    public RailwayPostgresValue? Plan
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Plan));
        }
    }

    /// <summary>
    /// Sets the Railway plan from the public plan enum.
    /// </summary>
    /// <param name="plan">The Railway PostgreSQL plan.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="plan"/> is not supported.</exception>
    public void SetPlan(RailwayPostgresPlan plan)
    {
        Plan = RailwayPostgresProviderDomain.MapPlan(plan);
    }

    /// <summary>
    /// Gets or sets the budget setting.
    /// </summary>
    public RailwayPostgresValue? Budget
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Budget));
        }
    }

    /// <summary>
    /// Sets the monthly Railway budget.
    /// </summary>
    /// <param name="budget">The monthly budget.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="budget"/> is less than or equal to zero.</exception>
    public void SetBudget(int budget)
    {
        if (budget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), budget, "The Railway PostgreSQL budget must be a positive integer.");
        }

        Budget = budget.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets or sets whether eviction is enabled.
    /// </summary>
    public bool? Eviction
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Eviction));
        }
    }

    /// <summary>
    /// Gets or sets whether TLS should be enabled.
    /// </summary>
    public bool? Tls
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Tls));
        }
    }

    internal IReadOnlySet<string> ExplicitSettings => new HashSet<string>(_explicitSettings);

    internal void Validate()
    {
        if (Tls == false)
        {
            throw new InvalidOperationException("Railway PostgreSQL requires TLS for v1 deployments. Set TLS to true or leave it unset.");
        }
    }

    internal RailwayPostgresProviderDeploymentOptions ToProviderOptions()
    {
        Validate();

        RailwayPostgresProviderValue? platform = null;
        RailwayPostgresProviderValue? primaryRegion = null;
        IReadOnlyList<RailwayPostgresProviderValue>? readRegions = null;
        RailwayPostgresProviderValue? plan = null;
        RailwayPostgresProviderValue? budget = null;
        RailwayPostgresProviderValue? eviction = null;
        RailwayPostgresProviderValue? tls = null;

        RailwayPostgresCloudPlatform? platformLiteral = null;
        RailwayPostgresRegion? primaryRegionLiteral = null;
        RailwayPostgresPlan? planLiteral = null;
        int? budgetLiteral = null;

        if (Platform is not null)
        {
            string? providerValue = null;

            if (Platform.LiteralValue is not null)
            {
                platformLiteral = RailwayPostgresProviderDomain.ParseCloudPlatform(Platform.LiteralValue, "platform");
                providerValue = RailwayPostgresProviderDomain.MapCloudPlatform(platformLiteral.Value);
            }

            platform = new(Platform, providerValue);
        }

        if (PrimaryRegion is not null)
        {
            string? providerValue = null;

            if (PrimaryRegion.LiteralValue is not null)
            {
                primaryRegionLiteral = RailwayPostgresProviderDomain.ParsePrimaryRegion(PrimaryRegion.LiteralValue, "primary region");
                providerValue = RailwayPostgresProviderDomain.MapRegion(primaryRegionLiteral.Value);
            }

            primaryRegion = new(PrimaryRegion, providerValue);
        }

        if (ReadRegions is not null)
        {
            List<RailwayPostgresProviderValue> mappedReadRegions = [];
            HashSet<string> literalReadRegions = new(StringComparer.Ordinal);

            foreach (RailwayPostgresValue readRegion in ReadRegions)
            {
                string? providerValue = null;

                if (readRegion.LiteralValue is not null)
                {
                    RailwayPostgresRegion readRegionLiteral = RailwayPostgresProviderDomain.ParseReadRegion(readRegion.LiteralValue, "read region");
                    providerValue = RailwayPostgresProviderDomain.MapRegion(readRegionLiteral);

                    if (!literalReadRegions.Add(providerValue))
                    {
                        throw new InvalidOperationException($"Railway PostgreSQL read region '{providerValue}' is configured more than once.");
                    }

                    ValidateReadRegionCombination(platformLiteral, primaryRegionLiteral, readRegionLiteral);
                }

                mappedReadRegions.Add(new(readRegion, providerValue));
            }

            readRegions = mappedReadRegions.AsReadOnly();
        }

        ValidatePrimaryRegionCombination(platformLiteral, primaryRegionLiteral);

        if (Plan is not null)
        {
            string? providerValue = null;

            if (Plan.LiteralValue is not null)
            {
                planLiteral = RailwayPostgresProviderDomain.ParsePlan(Plan.LiteralValue, "plan");
                providerValue = RailwayPostgresProviderDomain.MapPlan(planLiteral.Value);
            }

            plan = new(Plan, providerValue);
        }

        if (Budget is not null)
        {
            if (Budget.LiteralValue is not null)
            {
                budgetLiteral = RailwayPostgresProviderDomain.ParseBudget(Budget.LiteralValue, "budget");
            }

            budget = new(Budget, budgetLiteral);
        }

        if (planLiteral is not null && planLiteral != RailwayPostgresPlan.PayAsYouGo && budget is not null)
        {
            throw new InvalidOperationException("Railway PostgreSQL budget can only be configured with the pay-as-you-go plan.");
        }

        if (Eviction is not null)
        {
            eviction = new(RailwayPostgresValue.FromString(Eviction.Value ? "true" : "false"), Eviction.Value);
        }

        if (Tls is not null)
        {
            tls = new(RailwayPostgresValue.FromString(Tls.Value ? "true" : "false"), Tls.Value);
        }

        return new RailwayPostgresProviderDeploymentOptions(
            platform,
            primaryRegion,
            readRegions,
            plan,
            budget,
            eviction,
            tls,
            ExplicitSettings);
    }

    private static void ValidatePrimaryRegionCombination(
        RailwayPostgresCloudPlatform? platformLiteral,
        RailwayPostgresRegion? primaryRegionLiteral)
    {
        if (platformLiteral is null || primaryRegionLiteral is null)
        {
            return;
        }

        RailwayPostgresCloudPlatform primaryRegionPlatform = RailwayPostgresProviderDomain.GetCloudPlatform(primaryRegionLiteral.Value);

        if (platformLiteral.Value != primaryRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Railway PostgreSQL primary region '{RailwayPostgresProviderDomain.MapRegion(primaryRegionLiteral.Value)}' is a {RailwayPostgresProviderDomain.MapCloudPlatform(primaryRegionPlatform)} region and cannot be used with platform '{RailwayPostgresProviderDomain.MapCloudPlatform(platformLiteral.Value)}'.");
        }
    }

    private static void ValidateReadRegionCombination(
        RailwayPostgresCloudPlatform? platformLiteral,
        RailwayPostgresRegion? primaryRegionLiteral,
        RailwayPostgresRegion readRegionLiteral)
    {
        RailwayPostgresCloudPlatform readRegionPlatform = RailwayPostgresProviderDomain.GetCloudPlatform(readRegionLiteral);

        if (platformLiteral is not null && platformLiteral.Value != readRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Railway PostgreSQL read region '{RailwayPostgresProviderDomain.MapRegion(readRegionLiteral)}' is a {RailwayPostgresProviderDomain.MapCloudPlatform(readRegionPlatform)} region and cannot be used with platform '{RailwayPostgresProviderDomain.MapCloudPlatform(platformLiteral.Value)}'.");
        }

        if (primaryRegionLiteral is null)
        {
            return;
        }

        if (primaryRegionLiteral.Value == readRegionLiteral)
        {
            throw new InvalidOperationException(
                $"Railway PostgreSQL read region '{RailwayPostgresProviderDomain.MapRegion(readRegionLiteral)}' cannot match the primary region.");
        }

        RailwayPostgresCloudPlatform primaryRegionPlatform = RailwayPostgresProviderDomain.GetCloudPlatform(primaryRegionLiteral.Value);

        if (primaryRegionPlatform != readRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Railway PostgreSQL read region '{RailwayPostgresProviderDomain.MapRegion(readRegionLiteral)}' cannot be used with primary region '{RailwayPostgresProviderDomain.MapRegion(primaryRegionLiteral.Value)}'.");
        }
    }

    private void TrackExplicitSetting(string settingName)
    {
        _explicitSettings.Add(settingName);
    }
}
