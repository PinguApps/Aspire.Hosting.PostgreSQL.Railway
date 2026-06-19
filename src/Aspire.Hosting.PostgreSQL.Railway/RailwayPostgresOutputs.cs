using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Supplementary app-facing outputs populated from the deployed Railway PostgreSQL database.
/// </summary>
[AspireExport("pinguapps.railway.postgres.outputs", ExposeProperties = true, ExposeMethods = false)]
public sealed class RailwayPostgresOutputs
{
    internal RailwayPostgresOutputs(RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Endpoint = new(resource, RailwayPostgresOutputNames.Endpoint);
        Port = new(resource, RailwayPostgresOutputNames.Port);
        Password = new(resource, RailwayPostgresOutputNames.Password, secret: true);
        Tls = new(resource, RailwayPostgresOutputNames.Tls);
        DatabaseName = new(resource, RailwayPostgresOutputNames.DatabaseName);
        Properties =
        [
            Endpoint,
            Port,
            Password,
            Tls,
            DatabaseName,
        ];
    }

    /// <summary>The deployed Railway PostgreSQL host endpoint.</summary>
    public RailwayPostgresOutputReference Endpoint { get; }

    /// <summary>The deployed Railway PostgreSQL port.</summary>
    public RailwayPostgresOutputReference Port { get; }

    /// <summary>The deployed Railway PostgreSQL password.</summary>
    public RailwayPostgresOutputReference Password { get; }

    /// <summary>Whether TLS is enabled for the deployed Railway PostgreSQL endpoint.</summary>
    public RailwayPostgresOutputReference Tls { get; }

    /// <summary>The deployed Railway PostgreSQL database name.</summary>
    public RailwayPostgresOutputReference DatabaseName { get; }

    /// <summary>The stable supplementary output references.</summary>
    [AspireExportIgnore(Reason = "TypeScript AppHosts consume named output properties directly.")]
    public IReadOnlyList<RailwayPostgresOutputReference> Properties { get; }

    /// <summary>Returns whether the named supplementary output contains a secret value.</summary>
    [AspireExportIgnore(Reason = "Output secret classification is implementation metadata.")]
    public static bool IsSecret(string outputName)
    {
        ArgumentNullException.ThrowIfNull(outputName);

        return string.Equals(outputName, RailwayPostgresOutputNames.Password, StringComparison.Ordinal);
    }

    internal void Populate(RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        RailwayPostgresConnectionOutput connectionOutput = RailwayPostgresConnectionOutput.FromDatabase(database);

        Endpoint.SetValue(connectionOutput.Host);
        Port.SetValue(connectionOutput.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Password.SetValue(connectionOutput.Password);
        Tls.SetValue(connectionOutput.Tls.ToString().ToLowerInvariant());
        DatabaseName.SetValue(database.DatabaseName);
    }
}
