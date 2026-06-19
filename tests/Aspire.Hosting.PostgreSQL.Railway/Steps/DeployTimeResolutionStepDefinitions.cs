using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.PostgreSQL.Railway;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class DeployTimeResolutionStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;

    public DeployTimeResolutionStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [When("the Redis resource is marked for Railway with resolvable parameter inputs")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithResolvableParameterInputs()
    {
        _context.MarkRedisForRailwayWithResolvableParameterInputs();
    }

    [When("the Redis resource is marked for Railway with an unresolved API key parameter")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithAnUnresolvedApiKeyParameter()
    {
        _context.MarkRedisForRailwayWithUnresolvedApiKeyParameter();
    }

    [When("the Railway deployment inputs are resolved")]
    public async Task WhenTheRailwayDeploymentInputsAreResolved()
    {
        await _context.ResolveRailwayDeploymentInputsAsync();
    }

    [When("resolving the Railway deployment inputs is attempted")]
    public async Task WhenResolvingTheRailwayDeploymentInputsIsAttempted()
    {
        await _context.TryResolveRailwayDeploymentInputsAsync();
    }

    [When("executing the Railway deployment pipeline with a missing context is attempted")]
    public async Task WhenExecutingTheRailwayDeploymentPipelineWithAMissingContextIsAttempted()
    {
        await _context.TryExecuteRailwayDeploymentPipelineWithMissingContextAsync();
    }

    [Then("the resolved Railway deployment targets database {string}")]
    public void ThenTheResolvedRailwayDeploymentTargetsDatabase(string databaseName)
    {
        RailwayPostgresResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal(databaseName, deployment.DatabaseName);
        Assert.Equal(RailwayPostgresOwnershipMode.CreateOnly, deployment.OwnershipMode);
    }

    [Then("the resolved Railway management credentials use account email {string}")]
    public void ThenTheResolvedRailwayManagementCredentialsUseAccountEmail(string accountEmail)
    {
        RailwayPostgresResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal(accountEmail, deployment.ManagementCredentials.AccountEmail);
    }

    [Then("the resolved Railway deployment options contain the parameter values")]
    public void ThenTheResolvedRailwayDeploymentOptionsContainTheParameterValues()
    {
        RailwayPostgresResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal("aws", deployment.Options.Platform?.LiteralValue);
        Assert.Equal("eu-west-1", deployment.Options.PrimaryRegion?.LiteralValue);
        RailwayPostgresProviderValue readRegion = Assert.Single(deployment.Options.ReadRegions ?? []);
        Assert.Equal("eu-west-2", readRegion.LiteralValue);
        Assert.Equal("payg", deployment.Options.Plan?.LiteralValue);
        Assert.Equal(360, deployment.Options.Budget?.LiteralValue);
        Assert.Equal(true, deployment.Options.Eviction?.LiteralValue);
        Assert.Equal(true, deployment.Options.Tls?.LiteralValue);
    }

    [Then("the resolved Railway management API key is infrastructure-only")]
    public void ThenTheResolvedRailwayManagementApiKeyIsInfrastructureOnly()
    {
        RailwayPostgresResolvedDeployment deployment = GetResolvedDeployment();
        string apiKey = deployment.ManagementCredentials.ApiKey;

        Assert.Equal("management-secret", apiKey);
        Assert.DoesNotContain(apiKey, deployment.DatabaseName, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, deployment.ManagementCredentials.AccountEmail, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, deployment.Options.ExplicitSettings, StringComparer.Ordinal);
        IResourceWithConnectionString redisConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);
        IEnumerable<KeyValuePair<string, ReferenceExpression>> connectionProperties = redisConnection.GetConnectionProperties();

        Assert.DoesNotContain(apiKey, connectionProperties.Select(property => property.Key), StringComparer.Ordinal);
        Assert.DoesNotContain(apiKey, connectionProperties.Select(property => property.Value.ToString()), StringComparer.Ordinal);
    }

    [Then("the Railway deployment resolution fails with {string}")]
    public void ThenTheRailwayDeploymentResolutionFailsWith(string exceptionTypeName)
    {
        Exception exception =
            _context.DeploymentResolutionException ?? throw new InvalidOperationException("The Railway deployment resolution did not fail.");

        Assert.Equal(exceptionTypeName, exception.GetType().Name);
    }

    [Then("the Railway deployment resolution failure message contains {string}")]
    public void ThenTheRailwayDeploymentResolutionFailureMessageContains(string expectedMessage)
    {
        Exception exception =
            _context.DeploymentResolutionException ?? throw new InvalidOperationException("The Railway deployment resolution did not fail.");

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    private RailwayPostgresResolvedDeployment GetResolvedDeployment()
    {
        return _context.ResolvedDeployment
            ?? throw new InvalidOperationException("The Railway deployment inputs have not been resolved.");
    }
}
