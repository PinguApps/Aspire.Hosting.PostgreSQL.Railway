using Aspire.Hosting.PostgreSQL.Railway.Management;

namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresReconciliationException : Exception
{
    public RailwayPostgresReconciliationException()
    {
    }

    public RailwayPostgresReconciliationException(string message)
        : base(message)
    {
    }

    public RailwayPostgresReconciliationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RailwayPostgresReconciliationException(
        string settingName,
        RailwayPostgresProviderFailureKind failureKind,
        string message)
        : base(message)
    {
        SettingName = settingName;
        FailureKind = failureKind;
    }

    public RailwayPostgresReconciliationException(
        string settingName,
        RailwayPostgresProviderFailureKind failureKind,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        SettingName = settingName;
        FailureKind = failureKind;
    }

    public string SettingName { get; } = string.Empty;

    public RailwayPostgresProviderFailureKind FailureKind { get; } = RailwayPostgresProviderFailureKind.Unexpected;
}
