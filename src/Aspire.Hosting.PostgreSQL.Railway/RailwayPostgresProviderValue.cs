namespace Aspire.Hosting.PostgreSQL.Railway;

internal sealed class RailwayPostgresProviderValue
{
    public RailwayPostgresProviderValue(RailwayPostgresValue source, object? literalValue)
    {
        Source = source;
        LiteralValue = literalValue;
    }

    public RailwayPostgresValue Source { get; }

    public object? LiteralValue { get; }

    public bool IsParameter => Source.IsParameter;
}
