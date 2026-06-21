using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using Aspire.Hosting.PostgreSQL.Railway.Management;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class RedisConnectionOutputStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;
    private Exception? _exception;

    public RedisConnectionOutputStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [When("Railway PostgreSQL connection output is applied with endpoint {string}, port {int}, password {string}, and TLS enabled")]
    public void WhenRailwayPostgresConnectionOutputIsAppliedWithEndpointPortPasswordAndTlsEnabled(
        string endpoint,
        int port,
        string password)
    {
        _context.RedisBuilder.Resource.ApplyRailwayPostgresConnectionOutput(CreateDatabase(endpoint, port, password, tls: true));
    }

    [When("applying Railway PostgreSQL connection output with endpoint {string} is attempted")]
    public void WhenApplyingRailwayPostgresConnectionOutputWithEndpointIsAttempted(string endpoint)
    {
        _exception = Record.Exception(() =>
            _context.RedisBuilder.Resource.ApplyRailwayPostgresConnectionOutput(CreateDatabase(endpoint, 6379, "redis-password", tls: true)));
    }

    [When("applying Railway PostgreSQL connection output without an endpoint is attempted")]
    public void WhenApplyingRailwayPostgresConnectionOutputWithoutAnEndpointIsAttempted()
    {
        _exception = Record.Exception(() =>
            _context.RedisBuilder.Resource.ApplyRailwayPostgresConnectionOutput(CreateDatabase(endpoint: null, 6379, "redis-password", tls: true)));
    }

    [When("applying Railway PostgreSQL connection output without a password is attempted")]
    public void WhenApplyingRailwayPostgresConnectionOutputWithoutAPasswordIsAttempted()
    {
        _exception = Record.Exception(() =>
            _context.RedisBuilder.Resource.ApplyRailwayPostgresConnectionOutput(CreateDatabase("global-apt-1.railway.io", 6379, password: null, tls: true)));
    }

    [Then("the Redis connection string reference resolves to {string}")]
    public async Task ThenTheRedisConnectionStringReferenceResolvesTo(string expectedConnectionString)
    {
        IResourceWithConnectionString redisConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);

        string? connectionString = await redisConnection
            .GetConnectionStringAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Then("the Redis connection properties contain:")]
    public void ThenTheRedisConnectionPropertiesContain(DataTable table)
    {
        Dictionary<string, ReferenceExpression> properties = _context.RedisBuilder.Resource.Annotations
            .OfType<ConnectionPropertyAnnotation>()
            .ToDictionary(annotation => annotation.Name, annotation => annotation.Value, StringComparer.OrdinalIgnoreCase);

        foreach (DataTableRow row in table.Rows)
        {
            ReferenceExpression property = Assert.Contains(row["Name"], properties);

            Assert.Equal(row["Value"], property.ValueExpression);
        }
    }

    [Then("the Redis connection output does not contain {string}")]
    public void ThenTheRedisConnectionOutputDoesNotContain(string unexpectedValue)
    {
        RailwayPostgresConnectionOutput output = GetOutput();

        Assert.DoesNotContain(unexpectedValue, output.ConnectionString, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Host, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Port.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Password, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Uri, StringComparison.Ordinal);
    }

    [Then("the Redis resource has no Railway connection output")]
    public void ThenTheRedisResourceHasNoRailwayConnectionOutput()
    {
        Assert.DoesNotContain(
            _context.RedisBuilder.Resource.Annotations,
            annotation => annotation is RailwayPostgresConnectionOutputAnnotation or ConnectionStringRedirectAnnotation);
    }

    [Then("the Redis connection properties still use the standard Redis surface")]
    public void ThenTheRedisConnectionPropertiesStillUseTheStandardRedisSurface()
    {
        AspireModelAssertions.AssertRedisConnectionProperties(_context.RedisBuilder.Resource);
    }

    [Then("Railway PostgreSQL connection output fails with provider kind {string}")]
    public void ThenRailwayPostgresConnectionOutputFailsWithProviderKind(string failureKind)
    {
        RailwayPostgresProviderException exception = Assert.IsType<RailwayPostgresProviderException>(_exception);

        Assert.Equal(Enum.Parse<RailwayPostgresProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Railway PostgreSQL connection output failure message contains {string}")]
    public void ThenTheRailwayPostgresConnectionOutputFailureMessageContains(string expectedMessage)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("The Railway PostgreSQL connection output did not fail.");

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    private static RailwayPostgresDatabaseDetails CreateDatabase(
        string? endpoint,
        int port,
        string? password,
        bool tls)
    {
        return new RailwayPostgresDatabaseDetails
        {
            DatabaseId = "db-orders-cache",
            DatabaseName = "orders-cache",
            Endpoint = endpoint!,
            Port = port,
            Password = password,
            Tls = tls,
            State = "active",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = "payg",
            Budget = 100,
            Eviction = true,
        };
    }

    private RailwayPostgresConnectionOutput GetOutput()
    {
        return Assert
            .Single(_context.RedisBuilder.Resource.Annotations.OfType<RailwayPostgresConnectionOutputAnnotation>())
            .Output;
    }
}
