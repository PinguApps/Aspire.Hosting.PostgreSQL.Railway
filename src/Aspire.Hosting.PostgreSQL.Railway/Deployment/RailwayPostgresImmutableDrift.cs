namespace Aspire.Hosting.PostgreSQL.Railway.Deployment;

internal sealed class RailwayPostgresImmutableDrift
{
    public RailwayPostgresImmutableDrift(
        RailwayPostgresImmutableDriftFailureReason failureReason,
        string settingName,
        string requestedValue,
        string actualValue,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingName);
        ArgumentNullException.ThrowIfNull(requestedValue);
        ArgumentNullException.ThrowIfNull(actualValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        FailureReason = failureReason;
        SettingName = settingName;
        RequestedValue = requestedValue;
        ActualValue = actualValue;
        Message = message;
    }

    public RailwayPostgresImmutableDriftFailureReason FailureReason { get; }

    public string SettingName { get; }

    public string RequestedValue { get; }

    public string ActualValue { get; }

    public string Message { get; }
}
