using Aspire.Hosting;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Samples;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class AppHostSnippetsStepDefinitions
{
    private IReadOnlyList<string> _sampleMethodNames = [];
    private string _typeScriptDemoSource = string.Empty;

    [When("the sample AppHost snippets are loaded")]
    public void WhenTheSampleAppHostSnippetsAreLoaded()
    {
        Action<IDistributedApplicationBuilder>[] snippets =
        [
            RailwayPostgresAppHostSnippets.ConfigureCreateOrAdopt,
            RailwayPostgresAppHostSnippets.ConfigureCreateOnly,
            RailwayPostgresAppHostSnippets.ConfigureExistingOnly,
            RailwayPostgresAppHostSnippets.ConfigureParameterizedOptions,
            RailwayPostgresAppHostSnippets.ConfigureSupplementaryOutputConsumer,
        ];

        foreach (Action<IDistributedApplicationBuilder> snippet in snippets)
        {
            snippet(DistributedApplication.CreateBuilder());
        }

        _sampleMethodNames = snippets
            .Select(snippet => snippet.Method.Name)
            .ToArray();
    }

    [Then("the sample AppHost snippets cover the documented usage patterns")]
    public void ThenTheSampleAppHostSnippetsCoverTheDocumentedUsagePatterns()
    {
        Assert.Contains(nameof(RailwayPostgresAppHostSnippets.ConfigureCreateOrAdopt), _sampleMethodNames);
        Assert.Contains(nameof(RailwayPostgresAppHostSnippets.ConfigureCreateOnly), _sampleMethodNames);
        Assert.Contains(nameof(RailwayPostgresAppHostSnippets.ConfigureExistingOnly), _sampleMethodNames);
        Assert.Contains(nameof(RailwayPostgresAppHostSnippets.ConfigureParameterizedOptions), _sampleMethodNames);
        Assert.Contains(nameof(RailwayPostgresAppHostSnippets.ConfigureSupplementaryOutputConsumer), _sampleMethodNames);
    }

    [When("the TypeScript demo AppHost source is loaded")]
    public void WhenTheTypeScriptDemoAppHostSourceIsLoaded()
    {
        string repositoryRoot = FindRepositoryRoot();
        string demoPath = Path.Combine(repositoryRoot, "samples", "TypeScriptAppHost", "apphost.mts");

        Assert.True(File.Exists(demoPath), $"Expected TypeScript demo AppHost '{demoPath}' to exist.");

        _typeScriptDemoSource = File.ReadAllText(demoPath);
    }

    [Then("the TypeScript demo AppHost uses the documented generated API")]
    public void ThenTheTypeScriptDemoAppHostUsesTheDocumentedGeneratedApi()
    {
        Assert.Contains("from \"./.aspire/modules/aspire.mjs\"", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("railwayPostgresCloudPlatform", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("railwayPostgresOwnershipMode", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("railwayPostgresPlan", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("railwayPostgresRegion", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addRedis(\"cache\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"railway-database-name\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"railway-account-email\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"railway-api-key\", { secret: true })", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("cache.publishToRailway(databaseName, accountEmail, apiKey", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("ownershipMode: railwayPostgresOwnershipMode.createOrAdopt", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("platform: railwayPostgresCloudPlatform.aws", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("primaryRegion: railwayPostgresRegion.awsEuWest1", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("plan: railwayPostgresPlan.payAsYouGo", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("eviction: true", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await worker.withReference(cache)", _typeScriptDemoSource, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Aspire.Hosting.PostgreSQL.Railway.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }
}
