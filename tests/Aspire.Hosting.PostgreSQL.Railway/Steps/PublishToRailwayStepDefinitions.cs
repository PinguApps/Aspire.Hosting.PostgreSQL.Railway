using Aspire.Hosting;
using Aspire.Hosting.PostgreSQL.Railway;
using PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Support;
using Reqnroll;
using System.Reflection;
using Xunit;

namespace PinguApps.Aspire.Hosting.PostgreSQL.Railway.Tests.Steps;

[Binding]
public sealed class PublishToRailwayStepDefinitions
{
    private readonly RailwayPostgresScenarioContext _context;

    public PublishToRailwayStepDefinitions(RailwayPostgresScenarioContext context)
    {
        _context = context;
    }

    [Given("a standard Aspire Redis resource named {string}")]
    public void GivenAStandardAspireRedisResourceNamed(string resourceName)
    {
        _context.AddRedis(resourceName);
    }

    [When("the Redis resource is marked for Railway database {string}")]
    public void WhenTheRedisResourceIsMarkedForRailwayDatabase(string databaseName)
    {
        _context.MarkRedisForRailway(databaseName);
    }

    [When("the Redis resource is marked for Railway database {string} with ownership mode {string}")]
    public void WhenTheRedisResourceIsMarkedForRailwayDatabaseWithOwnershipMode(string databaseName, string ownershipMode)
    {
        _context.MarkRedisForRailway(databaseName, Enum.Parse<RailwayPostgresOwnershipMode>(ownershipMode));
    }

    [When("the Redis resource is marked for Railway with literal management credentials")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithLiteralManagementCredentials()
    {
        _context.MarkRedisForRailwayWithLiteralManagementCredentials();
    }

    [When("the Redis resource is marked for Railway through the {string} overload")]
    public void WhenTheRedisResourceIsMarkedForRailwayThroughTheOverload(string overload)
    {
        _context.MarkRedisForRailwayThroughOverload(overload);
    }

    [When("the Redis resource is marked for Railway with parameter-based inputs")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithParameterBasedInputs()
    {
        _context.MarkRedisForRailwayWithParameterBasedInputs();
    }

    [When("the Redis resource is marked for Railway with typed domain options")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithTypedDomainOptions()
    {
        _context.MarkRedisForRailwayWithTypedDomainOptions();
    }

    [When("the Redis resource is marked for Railway through the TypeScript bridge with DTO options")]
    public void WhenTheRedisResourceIsMarkedForRailwayThroughTheTypeScriptBridgeWithDtoOptions()
    {
        _context.MarkRedisForRailwayThroughTypeScriptBridgeWithDtoOptions();
    }

    [When("the Redis resource is marked for Railway through the TypeScript bridge with disabled TLS")]
    public void WhenTheRedisResourceIsMarkedForRailwayThroughTheTypeScriptBridgeWithDisabledTls()
    {
        _context.TryMarkRedisForRailwayThroughTypeScriptBridgeWithDisabledTls();
    }

    [When("the Redis resource is marked for Railway with an explicitly unset primary region")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithAnExplicitlyUnsetPrimaryRegion()
    {
        _context.MarkRedisForRailwayWithExplicitNullPrimaryRegion();
    }

    [When("the Redis resource is marked for a blank Railway database name")]
    public void WhenTheRedisResourceIsMarkedForABlankRailwayDatabaseName()
    {
        _context.TryMarkRedisForBlankRailwayDatabaseName();
    }

    [When("the Redis resource is marked for Railway with a missing API key value")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithAMissingApiKeyValue()
    {
        _context.TryMarkRedisForRailwayWithMissingApiKey();
    }

    [When("the Redis resource is marked for Railway with an unsupported ownership mode")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithAnUnsupportedOwnershipMode()
    {
        _context.TryMarkRedisForRailwayWithUnsupportedOwnershipMode();
    }

    [When("the Redis resource is marked for Railway with disabled TLS")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithDisabledTls()
    {
        _context.TryMarkRedisForRailwayWithDisabledTls();
    }

    [When("the Redis resource is marked for Railway with unsupported platform")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithUnsupportedPlatform()
    {
        _context.TryMarkRedisForRailwayWithUnsupportedPlatform();
    }

    [When("the Redis resource is marked for Railway with mismatched platform and primary region")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithMismatchedPlatformAndPrimaryRegion()
    {
        _context.TryMarkRedisForRailwayWithMismatchedPlatformAndPrimaryRegion();
    }

    [When("the Redis resource is marked for Railway with a fixed plan budget")]
    public void WhenTheRedisResourceIsMarkedForRailwayWithAFixedPlanBudget()
    {
        _context.TryMarkRedisForRailwayWithBudgetOnFixedPlan();
    }

    [Given("a consuming container references the Redis resource")]
    [When("a consuming container references the Redis resource")]
    public void WhenAConsumingContainerReferencesTheRedisResource()
    {
        _context.AddConsumingContainerReference();
    }

    [Then("the resource remains a standard Aspire Redis resource")]
    public void ThenTheResourceRemainsAStandardAspireRedisResource()
    {
        AspireModelAssertions.AssertStandardRedisResource(_context.RedisBuilder.Resource);
    }

    [Then("the resource is excluded from publish")]
    public void ThenTheResourceIsExcludedFromPublish()
    {
        Assert.True(AspireModelInspector.IsExcludedFromPublish(_context.RedisBuilder.Resource));
    }

    [Then("the resource has Railway deployment metadata for database {string}")]
    public void ThenTheResourceHasRailwayDeploymentMetadataForDatabase(string databaseName)
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Equal(databaseName, state.DatabaseName.LiteralValue);
        Assert.Equal(RailwayPostgresOwnershipMode.CreateOrAdopt, state.OwnershipMode);
        Assert.Equal("railway-account-email", state.AccountEmail.Parameter?.Name);
        Assert.Equal("railway-api-key", state.ApiKey.Parameter?.Name);
        Assert.Equal("eu-west-1", state.Options.PrimaryRegion?.LiteralValue);
        Assert.Equal(["eu-west-2"], state.Options.ReadRegions?.Select(region => region.LiteralValue));
        Assert.Equal(true, state.Options.Tls);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion), state.Options.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.ReadRegions), state.Options.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Tls), state.Options.ExplicitSettings);
    }

    [Then("the resource has Railway ownership mode {string}")]
    public void ThenTheResourceHasRailwayOwnershipMode(string ownershipMode)
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Equal(Enum.Parse<RailwayPostgresOwnershipMode>(ownershipMode), state.OwnershipMode);
    }

    [Then("the resource stores parameter references for the required Railway inputs")]
    public void ThenTheResourceStoresParameterReferencesForTheRequiredRailwayInputs()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Equal("railway-database-name", state.DatabaseName.Parameter?.Name);
        Assert.Equal("railway-account-email", state.AccountEmail.Parameter?.Name);
        Assert.Equal("railway-api-key", state.ApiKey.Parameter?.Name);
        Assert.Null(state.ApiKey.LiteralValue);
    }

    [Then("the resource stores parameter references for optional Railway inputs")]
    public void ThenTheResourceStoresParameterReferencesForOptionalRailwayInputs()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Equal("railway-primary-region", state.Options.PrimaryRegion?.Parameter?.Name);
        RailwayPostgresValue readRegion = Assert.Single(state.Options.ReadRegions ?? []);
        Assert.Equal("railway-read-region", readRegion.Parameter?.Name);
        Assert.Equal("payg", state.Options.Plan?.LiteralValue);
    }

    [Then("the Railway state distinguishes the explicitly unset primary region from an unspecified plan")]
    public void ThenTheRailwayStateDistinguishesTheExplicitlyUnsetPrimaryRegionFromAnUnspecifiedPlan()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Null(state.Options.PrimaryRegion);
        Assert.Null(state.Options.Plan);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion), state.Options.ExplicitSettings);
        Assert.DoesNotContain(nameof(RailwayPostgresDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("the provider domain maps the typed options to Railway payload values")]
    public void ThenTheProviderDomainMapsTheTypedOptionsToRailwayPayloadValues()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);
        RailwayPostgresProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.Equal(RailwayPostgresOwnershipMode.CreateOnly, state.OwnershipMode);
        Assert.Equal("aws", providerOptions.Platform?.LiteralValue);
        Assert.Equal("eu-west-1", providerOptions.PrimaryRegion?.LiteralValue);
        RailwayPostgresProviderValue readRegion = Assert.Single(providerOptions.ReadRegions ?? []);
        Assert.Equal("eu-west-2", readRegion.LiteralValue);
        Assert.Equal("payg", providerOptions.Plan?.LiteralValue);
        Assert.Equal(360, providerOptions.Budget?.LiteralValue);
        Assert.Equal(true, providerOptions.Eviction?.LiteralValue);
        Assert.Equal("true", providerOptions.Eviction?.Source.LiteralValue);
        Assert.Equal(true, providerOptions.Tls?.LiteralValue);
        Assert.Equal("true", providerOptions.Tls?.Source.LiteralValue);
    }

    [Then("the TypeScript DTO deployment metadata maps to provider payload values")]
    public void ThenTheTypeScriptDtoDeploymentMetadataMapsToProviderPayloadValues()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);
        RailwayPostgresProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.Equal(RailwayPostgresOwnershipMode.CreateOnly, state.OwnershipMode);
        Assert.Equal("aws", providerOptions.Platform?.LiteralValue);
        Assert.Equal("eu-west-1", providerOptions.PrimaryRegion?.LiteralValue);
        RailwayPostgresProviderValue readRegion = Assert.Single(providerOptions.ReadRegions ?? []);
        Assert.Equal("eu-west-2", readRegion.LiteralValue);
        Assert.Equal("payg", providerOptions.Plan?.LiteralValue);
        Assert.Equal(360, providerOptions.Budget?.LiteralValue);
        Assert.Equal(true, providerOptions.Eviction?.LiteralValue);
        Assert.Equal(true, providerOptions.Tls?.LiteralValue);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Platform), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.ReadRegions), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Plan), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Budget), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Eviction), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Tls), providerOptions.ExplicitSettings);
    }

    [Then("the TypeScript output bridge returns the supplementary Railway PostgreSQL outputs")]
    public void ThenTheTypeScriptOutputBridgeReturnsTheSupplementaryRailwayPostgresOutputs()
    {
        _context.GetOutputsThroughTypeScriptBridge();

        RailwayPostgresOutputs outputs = _context.LastOutputs ?? throw new InvalidOperationException("The outputs were not captured.");

        Assert.Same(_context.RedisBuilder.Resource.GetRailwayPostgresOutputs(), outputs);
        Assert.NotNull(outputs.Endpoint);
        Assert.NotNull(outputs.Port);
        Assert.NotNull(outputs.Password);
        Assert.NotNull(outputs.Tls);
        Assert.NotNull(outputs.DatabaseName);
    }

    [Then("the provider domain preserves explicit settings for reconcile")]
    public void ThenTheProviderDomainPreservesExplicitSettingsForReconcile()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);
        RailwayPostgresProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Platform), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.PrimaryRegion), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.ReadRegions), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Plan), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Budget), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Eviction), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(RailwayPostgresDeploymentOptions.Tls), providerOptions.ExplicitSettings);
    }

    [Then("the TypeScript export metadata matches the approved Railway PostgreSQL contract")]
    public void ThenTheTypeScriptExportMetadataMatchesTheApprovedRailwayPostgresContract()
    {
        MethodInfo publishMethod = typeof(RailwayPostgresBuilderExtensions).GetMethod(nameof(RailwayPostgresBuilderExtensions.PublishToRailwayForTypeScript))
            ?? throw new InvalidOperationException("The TypeScript publish bridge was not found.");
        Assert.Equal("PinguApps.Aspire.Hosting.PostgreSQL.Railway", publishMethod.DeclaringType?.Assembly.GetName().Name);
        AspireExportAttribute publishExport = Assert.Single(publishMethod.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.publishToRailway", publishExport.Id);
        Assert.Equal("publishToRailway", publishExport.MethodName);

        MethodInfo outputsMethod = typeof(RailwayPostgresResourceExtensions).GetMethod(nameof(RailwayPostgresResourceExtensions.GetRailwayPostgresOutputsForTypeScript))
            ?? throw new InvalidOperationException("The TypeScript outputs bridge was not found.");
        AspireExportAttribute outputsExport = Assert.Single(outputsMethod.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.getRailwayPostgresOutputs", outputsExport.Id);
        Assert.Equal("getRailwayPostgresOutputs", outputsExport.MethodName);

        Assert.NotNull(typeof(RailwayPostgresDeploymentOptionsDto).GetCustomAttribute<AspireDtoAttribute>());
        AssertOutputExportMetadata();
        AssertValueCatalogMetadata();
    }

    [Then("the provider domain preserves parameter-backed option sources")]
    public void ThenTheProviderDomainPreservesParameterBackedOptionSources()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);
        RailwayPostgresProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.True(providerOptions.PrimaryRegion?.IsParameter);
        Assert.Null(providerOptions.PrimaryRegion?.LiteralValue);
        RailwayPostgresProviderValue readRegion = Assert.Single(providerOptions.ReadRegions ?? []);
        Assert.True(readRegion.IsParameter);
        Assert.Null(readRegion.LiteralValue);
        Assert.Equal("payg", providerOptions.Plan?.LiteralValue);
    }

    [Then("the Railway configuration fails with {string}")]
    public void ThenTheRailwayConfigurationFailsWith(string exceptionTypeName)
    {
        Exception configurationException =
            _context.ConfigurationException ?? throw new InvalidOperationException("The Railway configuration did not fail.");

        Assert.Equal(exceptionTypeName, configurationException.GetType().Name);
    }

    [Then("the Railway configuration failure message contains {string}")]
    public void ThenTheRailwayConfigurationFailureMessageContains(string expectedMessage)
    {
        Exception configurationException =
            _context.ConfigurationException ?? throw new InvalidOperationException("The Railway configuration did not fail.");

        Assert.Contains(expectedMessage, configurationException.Message, StringComparison.Ordinal);
    }

    [Then("mutating captured callback options cannot mutate deployment metadata")]
    public void ThenMutatingCapturedCallbackOptionsCannotMutateDeploymentMetadata()
    {
        RailwayPostgresDeploymentOptions capturedOptions =
            _context.CapturedDeploymentOptions ?? throw new InvalidOperationException("The deployment options were not captured.");

        capturedOptions.PrimaryRegion = "us-east-1";
        capturedOptions.Plan = "payg";
        capturedOptions.Tls = false;

        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        Assert.Equal("eu-west-1", state.Options.PrimaryRegion?.LiteralValue);
        Assert.Null(state.Options.Plan);
        Assert.Equal(true, state.Options.Tls);
        Assert.DoesNotContain(nameof(RailwayPostgresDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("the explicit setting snapshot cannot mutate deployment metadata")]
    public void ThenTheExplicitSettingSnapshotCannotMutateDeploymentMetadata()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        if (state.Options.ExplicitSettings is ISet<string> exposedSettings)
        {
            exposedSettings.Add(nameof(RailwayPostgresDeploymentOptions.Plan));
        }

        Assert.DoesNotContain(nameof(RailwayPostgresDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("mutating the configured read regions cannot mutate deployment metadata")]
    public void ThenMutatingTheConfiguredReadRegionsCannotMutateDeploymentMetadata()
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        _context.ConfiguredReadRegions.Add("us-east-1");

        Assert.Equal(["eu-west-2"], state.Options.ReadRegions?.Select(region => region.LiteralValue));
    }

    [Then("the resource keeps the standard Redis connection properties")]
    public void ThenTheResourceKeepsTheStandardRedisConnectionProperties()
    {
        AspireModelAssertions.AssertRedisConnectionProperties(_context.RedisBuilder.Resource);
    }

    [Then("the Redis reference chain is configured for the consuming container")]
    public void ThenTheRedisReferenceChainIsConfiguredForTheConsumingContainer()
    {
        AspireModelAssertions.AssertContainerHasEnvironmentCallback(_context.ContainerBuilder.Resource);
    }

    [Then("the resource has one Railway deployment pipeline step")]
    public void ThenTheResourceHasOneRailwayDeploymentPipelineStep()
    {
        Assert.Equal(1, AspireModelInspector.GetPipelineStepCount(_context.RedisBuilder.Resource));
    }

    [Then("the resource has no Railway deployment metadata")]
    public void ThenTheResourceHasNoRailwayDeploymentMetadata()
    {
        Assert.False(AspireModelInspector.HasRailwayState(_context.RedisBuilder.Resource));
    }

    [Then("the resource has no Railway deployment pipeline step")]
    public void ThenTheResourceHasNoRailwayDeploymentPipelineStep()
    {
        Assert.Equal(0, AspireModelInspector.GetPipelineStepCount(_context.RedisBuilder.Resource));
    }

    [Then("the resource has no supplementary Railway PostgreSQL outputs")]
    public void ThenTheResourceHasNoSupplementaryRailwayPostgresOutputs()
    {
        Assert.DoesNotContain(
            _context.RedisBuilder.Resource.Annotations,
            annotation => annotation is RailwayPostgresOutputsAnnotation);
    }

    [Then("the fake Railway provider has no recorded interactions")]
    public void ThenTheFakeRailwayProviderHasNoRecordedInteractions()
    {
        Assert.Empty(_context.FakeProvider.Interactions);
    }

    [Then("the app-facing Redis outputs and references do not contain {string}")]
    public async Task ThenTheAppFacingRedisOutputsAndReferencesDoNotContain(string unexpectedValue)
    {
        AspireModelAssertions.AssertRedisConnectionPropertiesDoNotContain(_context.RedisBuilder.Resource, unexpectedValue);
        await AspireModelAssertions.AssertContainerEnvironmentDoesNotContainAsync(_context.ContainerBuilder.Resource, unexpectedValue);

        RailwayPostgresOutputs outputs = _context.RedisBuilder.Resource.GetRailwayPostgresOutputs();

        foreach (RailwayPostgresOutputReference output in outputs.Properties)
        {
            Assert.DoesNotContain(unexpectedValue, output.Name, StringComparison.Ordinal);
            Assert.DoesNotContain(unexpectedValue, output.ValueExpression, StringComparison.Ordinal);
        }
    }

    [Then("the Railway deployment metadata matches the {string} overload")]
    public void ThenTheRailwayDeploymentMetadataMatchesTheOverload(string overload)
    {
        RailwayPostgresDeploymentState state = AspireModelInspector.GetRailwayState(_context.RedisBuilder.Resource);

        switch (overload)
        {
            case "literal database and parameter credentials":
                Assert.Equal("orders-cache", state.DatabaseName.LiteralValue);
                Assert.Equal("railway-account-email", state.AccountEmail.Parameter?.Name);
                Assert.Equal("railway-api-key", state.ApiKey.Parameter?.Name);
                break;

            case "parameter database and parameter credentials":
                Assert.Equal("railway-database-name", state.DatabaseName.Parameter?.Name);
                Assert.Equal("railway-account-email", state.AccountEmail.Parameter?.Name);
                Assert.Equal("railway-api-key", state.ApiKey.Parameter?.Name);
                break;

            case "literal deployment values":
                Assert.Equal("orders-cache", state.DatabaseName.LiteralValue);
                Assert.Equal("owner@example.com", state.AccountEmail.LiteralValue);
                Assert.Equal("management-secret", state.ApiKey.LiteralValue);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(overload), overload, "Unknown PublishToRailway overload.");
        }
    }

    [Then("the fluent API returns the same Redis resource builder")]
    public void ThenTheFluentApiReturnsTheSameRedisResourceBuilder()
    {
        Assert.True(_context.FluentApiReturnedSameBuilder);
    }

    private static void AssertOutputExportMetadata()
    {
        AspireExportAttribute outputsExport = Assert.Single(typeof(RailwayPostgresOutputs).GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.outputs", outputsExport.Id);
        Assert.True(outputsExport.ExposeProperties);
        Assert.False(outputsExport.ExposeMethods);
        Assert.NotNull(typeof(RailwayPostgresOutputs).GetProperty(nameof(RailwayPostgresOutputs.Properties))?.GetCustomAttribute<AspireExportIgnoreAttribute>());
        Assert.NotNull(typeof(RailwayPostgresOutputs).GetMethod(nameof(RailwayPostgresOutputs.IsSecret))?.GetCustomAttribute<AspireExportIgnoreAttribute>());

        AspireExportAttribute referenceExport = Assert.Single(typeof(RailwayPostgresOutputReference).GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.railway.postgres.outputReference", referenceExport.Id);
        Assert.False(referenceExport.ExposeProperties);
        Assert.False(referenceExport.ExposeMethods);
    }

    private static void AssertValueCatalogMetadata()
    {
        AssertValueCatalog(
            RailwayPostgresOwnershipMode.CreateOrAdopt,
            "railwayPostgresOwnershipMode",
            "createOrAdopt");
        AssertValueCatalog(RailwayPostgresOwnershipMode.CreateOnly, "railwayPostgresOwnershipMode", "createOnly");
        AssertValueCatalog(RailwayPostgresOwnershipMode.ExistingOnly, "railwayPostgresOwnershipMode", "existingOnly");
        AssertValueCatalog(RailwayPostgresCloudPlatform.Aws, "railwayPostgresCloudPlatform", "aws");
        AssertValueCatalog(RailwayPostgresCloudPlatform.Gcp, "railwayPostgresCloudPlatform", "gcp");
        AssertValueCatalog(RailwayPostgresPlan.PayAsYouGo, "railwayPostgresPlan", "payAsYouGo");
        AssertValueCatalog(RailwayPostgresPlan.Fixed250Mb, "railwayPostgresPlan", "fixed250Mb");
        AssertValueCatalog(RailwayPostgresRegion.AwsEuWest2, "railwayPostgresRegion", "awsEuWest2");
        AssertValueCatalog(RailwayPostgresRegion.GcpEuropeWest1, "railwayPostgresRegion", "gcpEuropeWest1");
    }

    private static void AssertValueCatalog<TEnum>(TEnum value, string catalogName, string name)
        where TEnum : struct, Enum
    {
        FieldInfo field = typeof(TEnum).GetField(value.ToString())
            ?? throw new InvalidOperationException($"The enum value '{value}' was not found.");
        AspireValueAttribute attribute = Assert.Single(field.GetCustomAttributes<AspireValueAttribute>());

        Assert.Equal(catalogName, attribute.CatalogName);
        Assert.Equal(name, attribute.Name);
    }
}
