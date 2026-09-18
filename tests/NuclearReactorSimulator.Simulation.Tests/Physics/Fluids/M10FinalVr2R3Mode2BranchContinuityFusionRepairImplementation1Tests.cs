using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class M10FinalVr2R3Mode2BranchContinuityFusionRepairImplementation1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1";
    private const double FailureSpecificVolume = 0.048580627845180926d;
    private const double FailureSpecificEnergy = 2525533.2846314958d;
    private const double SteamMassKilograms = 3322.9485347676582d;
    private const double SteamEnergyJoules = 8238192716.5426521d;
    private const double SteamEnergyProbeJoules = 2059.5481791356628d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3Mode2BranchContinuityFusionRepairImplementation1")]
    public void Repair_PreservesMode0Mode1Fusion_AndRoutesMode2ThroughNonFusedProductionResolve()
    {
        RequireOptIn();
        Directory.CreateDirectory(ArtifactDirectory());

        var mode0 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        var mode1 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var mode2 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);

        Assert.True(mode0.IsLegacyBranchContinuityFusionEligible);
        Assert.True(mode1.IsLegacyBranchContinuityFusionEligible);
        Assert.False(mode2.IsLegacyBranchContinuityFusionEligible);

        var returnedDefinition = new FluidNodeDefinition("turbine-inlet", Volume.FromCubicMetres(FailureSpecificVolume));
        var returnedInventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(FailureSpecificEnergy));
        var returnedPrevious = new FluidThermodynamicState(
            Pressure.FromMegapascals(3.9d),
            Temperature.FromKelvins(522d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.95d));

        var directMode2 = mode2.Resolve(returnedDefinition, returnedInventory, returnedPrevious);
        var sameInstance = new ThermodynamicBranchContinuityModel(
            mode2,
            mode2,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            new[] { "turbine-inlet" });
        var sameInstanceState = sameInstance.Resolve(returnedDefinition, returnedInventory, returnedPrevious);

        var splitProduction = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var splitDiagnostic = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var split = new ThermodynamicBranchContinuityModel(
            splitProduction,
            splitDiagnostic,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            new[] { "turbine-inlet" });
        var splitState = split.Resolve(returnedDefinition, returnedInventory, returnedPrevious);

        Assert.Equal(directMode2, sameInstanceState);
        Assert.Equal(directMode2, splitState);
        Assert.Equal(split.Decisions.ToArray(), sameInstance.Decisions.ToArray());
        Assert.Equal("production-no-overlap", Assert.Single(sameInstance.Decisions).DecisionKind);

        var mode0Regression = VerifyHistoricalFusionEquivalence(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        var mode1Regression = VerifyHistoricalFusionEquivalence(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);

        WriteReturnedStateEvidence(directMode2, sameInstanceState, splitState, sameInstance);
        WriteHistoricalRegressionEvidence(mode0Regression, mode1Regression);
    }

    private static HistoricalFusionRegression VerifyHistoricalFusionEquivalence(WaterSteamThermodynamicClosureMode mode)
    {
        var optimizedProduction = new SimplifiedWaterSteamThermodynamicModel(mode);
        var nonFusedProduction = new SimplifiedWaterSteamThermodynamicModel(mode);
        var nonFusedProxy = new CombinedProviderProxy(nonFusedProduction);
        var optimized = new ThermodynamicBranchContinuityModel(
            optimizedProduction,
            optimizedProduction,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);
        var nonFused = new ThermodynamicBranchContinuityModel(
            nonFusedProxy,
            nonFusedProxy,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(6362325.9673817037d),
            Temperature.FromKelvins(552.58890484070866d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.98827242641541357d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var optimizedState = optimized.Resolve(definition, inventory, previous);
        var nonFusedState = nonFused.Resolve(definition, inventory, previous);
        var stateEqual = optimizedState == nonFusedState;
        var decisionsEqual = optimized.Decisions.SequenceEqual(nonFused.Decisions);

        Assert.True(optimizedProduction.IsLegacyBranchContinuityFusionEligible);
        Assert.True(stateEqual);
        Assert.True(decisionsEqual);
        return new HistoricalFusionRegression(stateEqual, decisionsEqual);
    }

    private static void WriteReturnedStateEvidence(
        FluidThermodynamicState direct,
        FluidThermodynamicState sameInstance,
        FluidThermodynamicState split,
        ThermodynamicBranchContinuityModel sameInstanceWrapper)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "03-returned-state-repair-regression.txt"),
            new[]
            {
                "status=EVIDENCE-WRITTEN",
                "returned-node=turbine-inlet",
                "mode2-fusion-eligible=False",
                $"direct-phase={direct.Phase}",
                $"same-instance-phase={sameInstance.Phase}",
                $"split-phase={split.Phase}",
                $"same-instance-equals-direct={sameInstance == direct}",
                $"split-equals-direct={split == direct}",
                $"same-instance-equals-split={sameInstance == split}",
                $"same-instance-decision-kind={Assert.Single(sameInstanceWrapper.Decisions).DecisionKind}",
            },
            Utf8WithoutBom);
    }

    private static void WriteHistoricalRegressionEvidence(HistoricalFusionRegression mode0, HistoricalFusionRegression mode1)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "04-historical-fusion-regression.txt"),
            new[]
            {
                "status=EVIDENCE-WRITTEN",
                "mode0-fusion-eligible=True",
                $"mode0-optimized-vs-nonfused-state-equal={mode0.StateEqual}",
                $"mode0-optimized-vs-nonfused-decisions-equal={mode0.DecisionsEqual}",
                "mode1-fusion-eligible=True",
                $"mode1-optimized-vs-nonfused-state-equal={mode1.StateEqual}",
                $"mode1-optimized-vs-nonfused-decisions-equal={mode1.DecisionsEqual}",
                "mode2-fusion-eligible=False",
            },
            Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 only from the controlled repair implementation runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-mode2-branch-continuity-fusion-repair-implementation1");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the repair test output directory.");
    }

    private sealed class CombinedProviderProxy : IFluidThermodynamicModel, IWaterSteamInverseBranchDiagnosticProvider
    {
        private readonly SimplifiedWaterSteamThermodynamicModel _inner;
        public CombinedProviderProxy(SimplifiedWaterSteamThermodynamicModel inner) => _inner = inner;
        public FluidThermodynamicState Resolve(FluidNodeDefinition definition, FluidNodeInventory inventory, FluidThermodynamicState previousState)
            => _inner.Resolve(definition, inventory, previousState);
        public WaterSteamInverseBranchSelectionDiagnostic DiagnoseInverseBranchSelection(FluidNodeDefinition definition, FluidNodeInventory inventory, FluidThermodynamicState previousState)
            => _inner.DiagnoseInverseBranchSelection(definition, inventory, previousState);
    }

    private readonly record struct HistoricalFusionRegression(bool StateEqual, bool DecisionsEqual);
}
