using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Historical;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Historical;

public sealed class HistoricalScenarioFrameworkTests
{
    [Fact]
    public void Claim_DocumentedFactRequiresDeclaredEvidenceReference()
    {
        Assert.Throws<ArgumentException>(() => new HistoricalScenarioClaimDefinition(
            "fact-1",
            HistoricalScenarioClaimKind.DocumentedFact,
            "A documented historical statement."));
    }

    [Fact]
    public void Context_RejectsClaimThatReferencesUndeclaredSource()
    {
        var claim = new HistoricalScenarioClaimDefinition(
            "fact-1",
            HistoricalScenarioClaimKind.DocumentedFact,
            "A documented historical statement.",
            new[] { "source-missing" });

        var exception = Assert.Throws<ArgumentException>(() => new HistoricalScenarioContextDefinition(
            "Historical subject",
            "Educational historical inspiration only.",
            Array.Empty<HistoricalSourceReference>(),
            new[] { claim },
            Array.Empty<string>(),
            new[] { "Not an exact historical reconstruction." }));

        Assert.Contains("source-missing", exception.Message);
    }

    [Fact]
    public void Context_RequiresAtLeastOneDocumentedFactAndOneModelCapability()
    {
        var source = new HistoricalSourceReference("source-1", "Synthetic citation");
        var approximation = new HistoricalScenarioClaimDefinition(
            "approximation-1",
            HistoricalScenarioClaimKind.EducationalApproximation,
            "Synthetic approximation.",
            rationale: "Reduced-model boundary.");

        Assert.Throws<ArgumentException>(() => new HistoricalScenarioContextDefinition(
            "Historical subject",
            "Educational inspiration only.",
            new[] { source },
            new[] { approximation },
            new[] { HistoricalModelCapabilityIds.DeterministicFullPlantRuntime },
            new[] { "Not an exact reconstruction." }));

        var fact = new HistoricalScenarioClaimDefinition(
            "fact-1",
            HistoricalScenarioClaimKind.DocumentedFact,
            "Synthetic documented statement.",
            new[] { source.SourceId });

        Assert.Throws<ArgumentException>(() => new HistoricalScenarioContextDefinition(
            "Historical subject",
            "Educational inspiration only.",
            new[] { source },
            new[] { fact },
            Array.Empty<string>(),
            new[] { "Not an exact reconstruction." }));
    }

    [Fact]
    public void Review_FailsClosedWhenDeclaredModelCapabilityIsUnavailable()
    {
        var scenario = CreateHistoricalScenario(
            HistoricalModelCapabilityIds.DeterministicFullPlantRuntime,
            "model.unavailable-capability");

        var review = HistoricalScenarioFidelityReviewer.Review(
            scenario,
            HistoricalModelCapabilityIds.ValidatedThroughM94);

        Assert.False(review.IsApproved);
        Assert.Equal(new[] { "model.unavailable-capability" }, review.MissingCapabilities);
    }

    [Fact]
    public void Review_ApprovesOnlyExplicitRequirementsAgainstExplicitValidatedCapabilitySet()
    {
        var scenario = CreateHistoricalScenario(
            HistoricalModelCapabilityIds.DeterministicFullPlantRuntime,
            HistoricalModelCapabilityIds.VersionedInitialConditions,
            HistoricalModelCapabilityIds.QuasiSpatialCoreFeedback);

        var review = HistoricalScenarioFidelityReviewer.Review(
            scenario,
            HistoricalModelCapabilityIds.ValidatedThroughM94);

        Assert.True(review.IsApproved);
        Assert.Empty(review.MissingCapabilities);
        Assert.Equal("historical-framework-test", review.ScenarioId);
    }

    [Fact]
    public void SessionFactory_BlocksHistoricalScenarioBeforeRuntimeResolutionWhenFidelityReviewFails()
    {
        var scenario = CreateHistoricalScenario("model.unavailable-capability");
        var factory = new ScenarioSessionFactory(
            new VersionedInitialConditionRegistry(Array.Empty<IVersionedInitialConditionFactory>()),
            validatedHistoricalModelCapabilities: HistoricalModelCapabilityIds.ValidatedThroughM94);

        var exception = Assert.Throws<HistoricalScenarioFidelityException>(() => factory.Load(scenario));

        Assert.Equal(scenario.ScenarioId, exception.ScenarioId);
        Assert.Equal(new[] { "model.unavailable-capability" }, exception.MissingCapabilities);
    }

    [Fact]
    public void Review_RejectsOrdinaryScenarioWithoutHistoricalContext()
    {
        var scenario = new ScenarioDefinition(
            "ordinary-training",
            "Ordinary training",
            "Not historical-inspired content.",
            new InitialConditionReference("reference", 1));

        Assert.Throws<InvalidOperationException>(() => HistoricalScenarioFidelityReviewer.Review(
            scenario,
            HistoricalModelCapabilityIds.ValidatedThroughM94));
    }

    [Fact]
    public void Context_PreservesExplicitFactApproximationAssumptionSeparation()
    {
        var context = CreateContext(HistoricalModelCapabilityIds.DeterministicFullPlantRuntime);

        Assert.Collection(
            context.Claims,
            claim => Assert.Equal(HistoricalScenarioClaimKind.DocumentedFact, claim.Kind),
            claim => Assert.Equal(HistoricalScenarioClaimKind.EducationalApproximation, claim.Kind),
            claim => Assert.Equal(HistoricalScenarioClaimKind.SimulatorSpecificAssumption, claim.Kind));
        Assert.Single(context.Sources);
        Assert.Single(context.DeliberateNonClaims);
    }

    private static ScenarioDefinition CreateHistoricalScenario(params string[] requiredCapabilities)
        => new(
            "historical-framework-test",
            "Historical framework test",
            "Synthetic metadata-only test scenario; it makes no claim about a real historical event.",
            new InitialConditionReference("reference", 1),
            historicalContext: CreateContext(requiredCapabilities));

    private static HistoricalScenarioContextDefinition CreateContext(params string[] requiredCapabilities)
    {
        const string sourceId = "source-1";
        return new HistoricalScenarioContextDefinition(
            "Synthetic historical subject",
            "Historical inspiration metadata test only; quantitative reconstruction is not claimed.",
            new[]
            {
                new HistoricalSourceReference(sourceId, "Synthetic test citation", "section 1"),
            },
            new[]
            {
                new HistoricalScenarioClaimDefinition(
                    "fact-1",
                    HistoricalScenarioClaimKind.DocumentedFact,
                    "Synthetic documented statement.",
                    new[] { sourceId }),
                new HistoricalScenarioClaimDefinition(
                    "approximation-1",
                    HistoricalScenarioClaimKind.EducationalApproximation,
                    "Synthetic educational approximation.",
                    rationale: "Represents a reduced model boundary for deterministic training."),
                new HistoricalScenarioClaimDefinition(
                    "assumption-1",
                    HistoricalScenarioClaimKind.SimulatorSpecificAssumption,
                    "Synthetic simulator-specific assumption.",
                    rationale: "Required only to construct the synthetic test scenario."),
            },
            requiredCapabilities,
            new[] { "Not an exact historical reconstruction." });
    }
}
