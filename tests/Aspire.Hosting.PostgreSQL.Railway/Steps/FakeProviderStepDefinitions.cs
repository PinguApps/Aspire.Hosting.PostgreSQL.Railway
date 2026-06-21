using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class FakeProviderStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;

    public FakeProviderStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("the fake Railway provider contains database {string} in region {string}")]
    public void GivenTheFakeRailwayProviderContainsDatabaseInRegion(string databaseName, string primaryRegion)
    {
        _context.FakeProvider.AddDatabase(
            new FakeRailwayPostgresDatabase(
                databaseName,
                $"db-{databaseName}",
                primaryRegion,
                "global-apt-1.railway.io",
                6379,
                "test-password",
                tlsEnabled: true));
    }

    [When("the fake Railway provider is asked to find database {string}")]
    public void WhenTheFakeRailwayProviderIsAskedToFindDatabase(string databaseName)
    {
        _context.LastProviderDatabase = _context.FakeProvider.FindByName(databaseName);
    }

    [Then("the fake Railway provider returns database {string}")]
    public void ThenTheFakeRailwayProviderReturnsDatabase(string databaseName)
    {
        Assert.NotNull(_context.LastProviderDatabase);
        Assert.Equal(databaseName, _context.LastProviderDatabase.DatabaseName);
    }

    [Then("the fake Railway provider recorded a {string} interaction for database {string}")]
    public void ThenTheFakeRailwayProviderRecordedAnInteractionForDatabase(string interactionName, string databaseName)
    {
        FakeRailwayProviderInteraction interaction = Assert.Single(_context.FakeProvider.Interactions);

        Assert.Equal(interactionName, interaction.Name);
        Assert.Equal(databaseName, interaction.DatabaseName);
    }
}
