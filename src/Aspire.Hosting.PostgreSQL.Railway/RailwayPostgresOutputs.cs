using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway;

/// <summary>
/// Supplementary app-facing outputs populated from the deployed Railway PostgreSQL service.
/// </summary>
[AspireExport("pinguapps.railway.postgres.outputs", ExposeProperties = true, ExposeMethods = false)]
public sealed class RailwayPostgresOutputs
{
    internal RailwayPostgresOutputs(PostgresServerResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        ServiceId = new(resource, RailwayPostgresOutputNames.ServiceId);
        Host = new(resource, RailwayPostgresOutputNames.Host);
        Port = new(resource, RailwayPostgresOutputNames.Port);
        UserName = new(resource, RailwayPostgresOutputNames.UserName);
        Password = new(resource, RailwayPostgresOutputNames.Password, secret: true);
        DatabaseName = new(resource, RailwayPostgresOutputNames.DatabaseName);
        ConnectionString = new(resource, RailwayPostgresOutputNames.ConnectionString, secret: true);
        UrlEscapedUserName = new(resource, RailwayPostgresOutputNames.UrlEscapedUserName);
        UrlEscapedPassword = new(resource, RailwayPostgresOutputNames.UrlEscapedPassword, secret: true);
        UrlEscapedDatabaseName = new(resource, RailwayPostgresOutputNames.UrlEscapedDatabaseName);
        Properties =
        [
            ServiceId,
            Host,
            Port,
            UserName,
            Password,
            DatabaseName,
            ConnectionString,
        ];
    }

    /// <summary>The deployed Railway PostgreSQL service id.</summary>
    public RailwayPostgresOutputReference ServiceId { get; }

    /// <summary>The deployed Railway PostgreSQL host.</summary>
    public RailwayPostgresOutputReference Host { get; }

    /// <summary>The deployed Railway PostgreSQL port.</summary>
    public RailwayPostgresOutputReference Port { get; }

    /// <summary>The deployed Railway PostgreSQL user name.</summary>
    public RailwayPostgresOutputReference UserName { get; }

    /// <summary>The deployed Railway PostgreSQL password.</summary>
    public RailwayPostgresOutputReference Password { get; }

    /// <summary>The deployed Railway PostgreSQL database name.</summary>
    public RailwayPostgresOutputReference DatabaseName { get; }

    /// <summary>The deployed Railway PostgreSQL connection string.</summary>
    public RailwayPostgresOutputReference ConnectionString { get; }

    internal RailwayPostgresOutputReference UrlEscapedUserName { get; }

    internal RailwayPostgresOutputReference UrlEscapedPassword { get; }

    internal RailwayPostgresOutputReference UrlEscapedDatabaseName { get; }

    /// <summary>The stable supplementary output references.</summary>
    [AspireExportIgnore(Reason = "TypeScript AppHosts consume named output properties directly.")]
    public IReadOnlyList<RailwayPostgresOutputReference> Properties { get; }

    /// <summary>Returns whether the named supplementary output contains a secret value.</summary>
    [AspireExportIgnore(Reason = "Output secret classification is implementation metadata.")]
    public static bool IsSecret(string outputName)
    {
        ArgumentNullException.ThrowIfNull(outputName);

        return string.Equals(outputName, RailwayPostgresOutputNames.Password, StringComparison.Ordinal)
            || string.Equals(outputName, RailwayPostgresOutputNames.ConnectionString, StringComparison.Ordinal)
            || string.Equals(outputName, RailwayPostgresOutputNames.UrlEscapedPassword, StringComparison.Ordinal);
    }

    internal void Populate(RailwayPostgresDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        ServiceId.SetValue(database.ServiceId);
        Host.SetValue(database.Host);
        Port.SetValue(database.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        UserName.SetValue(database.UserName);
        Password.SetValue(database.Password);
        DatabaseName.SetValue(database.DatabaseName);
        ConnectionString.SetValue(database.ConnectionString);
        UrlEscapedUserName.SetValue(Uri.EscapeDataString(database.UserName));
        UrlEscapedPassword.SetValue(Uri.EscapeDataString(database.Password));
        UrlEscapedDatabaseName.SetValue(Uri.EscapeDataString(database.DatabaseName));
    }
}
