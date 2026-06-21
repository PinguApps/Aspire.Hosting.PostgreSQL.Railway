using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Represents an Railway PostgreSQL deployment value supplied either as a literal string or as an Aspire parameter.
/// </summary>
[AspireExportIgnore(Reason = "TypeScript AppHosts use parameter builders and value catalogs instead of raw deployment values.")]
public sealed class RailwayPostgresValue
{
    private RailwayPostgresValue(string literalValue)
    {
        LiteralValue = literalValue;
    }

    private RailwayPostgresValue(ParameterResource parameter)
    {
        Parameter = parameter;
    }

    /// <summary>
    /// Gets the literal string value when this value was created from a string.
    /// </summary>
    public string? LiteralValue { get; }

    /// <summary>
    /// Gets the Aspire parameter when this value was created from a parameter resource.
    /// </summary>
    public ParameterResource? Parameter { get; }

    /// <summary>
    /// Gets a value indicating whether this value is backed by an Aspire parameter.
    /// </summary>
    public bool IsParameter => Parameter is not null;

    /// <summary>
    /// Creates an Railway PostgreSQL deployment value from a literal string.
    /// </summary>
    /// <param name="value">The literal deployment value.</param>
    /// <returns>An Railway PostgreSQL deployment value backed by the supplied literal string.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is null, empty, or whitespace.</exception>
    public static RailwayPostgresValue FromString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new RailwayPostgresValue(value);
    }

    /// <summary>
    /// Creates an Railway PostgreSQL deployment value from an Aspire parameter resource.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    /// <returns>An Railway PostgreSQL deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static RailwayPostgresValue FromParameter(ParameterResource parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return new RailwayPostgresValue(parameter);
    }

    /// <summary>
    /// Creates an Railway PostgreSQL deployment value from an Aspire parameter resource.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    /// <returns>An Railway PostgreSQL deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static RailwayPostgresValue FromParameterResource(ParameterResource parameter) => FromParameter(parameter);

    /// <summary>
    /// Creates an Railway PostgreSQL deployment value from an Aspire parameter resource builder.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource builder.</param>
    /// <returns>An Railway PostgreSQL deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static RailwayPostgresValue FromParameter(IResourceBuilder<ParameterResource> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return FromParameter(parameter.Resource);
    }

    /// <summary>
    /// Converts a literal string to an Railway PostgreSQL deployment value.
    /// </summary>
    /// <param name="value">The literal deployment value.</param>
    public static implicit operator RailwayPostgresValue(string value) => FromString(value);

    /// <summary>
    /// Converts an Aspire parameter resource to an Railway PostgreSQL deployment value.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    public static implicit operator RailwayPostgresValue(ParameterResource parameter) => FromParameter(parameter);
}
