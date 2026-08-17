using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class ThermodynamicInverseBranchSelectionAnalyzerTests
{
    private const double SteamMassKilograms = 3322.9485347676582d;
    private const double SteamEnergyJoules = 8238192716.5426521d;
    private const double SteamEnergyProbeJoules = 2059.5481791356628d;
    private const double SteamMassProbeKilograms = 0.00083073713369191454d;

    private const double StopOutMassKilograms = 3165.1742481741885d;
    private const double StopOutEnergyJoules = 7863528392.8413477d;
    private const double StopOutEnergyProbeJoules = 1965.8820982103368d;
    private const double StopOutMassProbeKilograms = 0.00079129356204354711d;

    [Fact]
    public void SteamBoundary_CoarseSaturatedMissAllowsEarlierSuperheatedBranchToShadowBoundaryRoot()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(6362325.9673817037d),
            Temperature.FromKelvins(552.58890484070866d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.98827242641541357d));
        var nominal = new FluidNodeInventory(Mass.FromKilograms(SteamMassKilograms), Energy.FromJoules(SteamEnergyJoules));
        var energyPlus = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var nominalDiagnostic = model.DiagnoseInverseBranchSelection(definition, nominal, previous);
        var perturbedDiagnostic = model.DiagnoseInverseBranchSelection(definition, energyPlus, previous);
        var nominalResolved = model.Resolve(definition, nominal, previous);
        var perturbedResolved = model.Resolve(definition, energyPlus, previous);

        Assert.Equal(nominalResolved.Phase.ToString(), nominalDiagnostic.ProductionSelectedPhase);
        Assert.Equal(perturbedResolved.Phase.ToString(), perturbedDiagnostic.ProductionSelectedPhase);
        Assert.Equal("coarse-saturated", nominalDiagnostic.ProductionSelectedBranch);
        Assert.True(nominalDiagnostic.MultiplePhaseRootsAvailable);
        Assert.Equal("coarse-superheated", perturbedDiagnostic.ProductionSelectedBranch);
        Assert.True(perturbedDiagnostic.MultiplePhaseRootsAvailable);
        Assert.False(perturbedDiagnostic.CoarseSaturatedRootFound);
        Assert.True(perturbedDiagnostic.BoundaryAwareSaturatedRootFound);
        Assert.True(perturbedDiagnostic.CoarseSuperheatedRootFound);
        Assert.True(perturbedDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated);
    }

    [Fact]
    public void StopOutBoundary_ShowsTheSameOverlappingRootSelectionMechanism()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var definition = new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(8601730.4979163781d),
            Temperature.FromKelvins(588.83285718179309d),
            FluidPhase.SuperheatedVapor,
            null);
        var nominal = new FluidNodeInventory(Mass.FromKilograms(StopOutMassKilograms), Energy.FromJoules(StopOutEnergyJoules));
        var energyMinus = new FluidNodeInventory(
            Mass.FromKilograms(StopOutMassKilograms),
            Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules));

        var nominalDiagnostic = model.DiagnoseInverseBranchSelection(definition, nominal, previous);
        var perturbedDiagnostic = model.DiagnoseInverseBranchSelection(definition, energyMinus, previous);
        var nominalResolved = model.Resolve(definition, nominal, previous);
        var perturbedResolved = model.Resolve(definition, energyMinus, previous);

        Assert.Equal(nominalResolved.Phase.ToString(), nominalDiagnostic.ProductionSelectedPhase);
        Assert.Equal(perturbedResolved.Phase.ToString(), perturbedDiagnostic.ProductionSelectedPhase);
        Assert.Equal("coarse-superheated", nominalDiagnostic.ProductionSelectedBranch);
        Assert.False(nominalDiagnostic.CoarseSaturatedRootFound);
        Assert.True(nominalDiagnostic.BoundaryAwareSaturatedRootFound);
        Assert.True(nominalDiagnostic.CoarseSuperheatedRootFound);
        Assert.True(nominalDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated);
        Assert.Equal("coarse-saturated", perturbedDiagnostic.ProductionSelectedBranch);
        Assert.True(perturbedDiagnostic.MultiplePhaseRootsAvailable);
    }

    [Fact]
    public void H11ProbeSet_ClassifiesOverlappingRootsCoarseDetectionAndMissingHysteresis()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var state = CreateTwoNodeState();
        var h11 = new ThermodynamicSwitchingLocalizationReport(new[]
        {
            CreateLocalization(
                "steam",
                FluidPhase.SaturatedMixture.ToString(),
                SteamMassKilograms,
                SteamEnergyJoules,
                SteamEnergyProbeJoules,
                SteamMassProbeKilograms),
            CreateLocalization(
                "stop-out",
                FluidPhase.SuperheatedVapor.ToString(),
                StopOutMassKilograms,
                StopOutEnergyJoules,
                StopOutEnergyProbeJoules,
                StopOutMassProbeKilograms),
        });

        var report = new ThermodynamicInverseBranchSelectionAnalyzer(model, model).Analyze(state, h11);

        Assert.Equal(2, report.NodeCount);
        Assert.Equal(2, report.OverlappingRootNodeCount);
        Assert.Equal(2, report.CoarseDetectionToggleNodeCount);
        Assert.Equal(2, report.LateBoundarySaturatedShadowNodeCount);
        Assert.Equal(0, report.PreviousStateTieBreakNodeCount);
        Assert.All(report.Nodes, static node =>
        {
            Assert.Equal(
                "overlapping-roots+coarse-saturated-detection+fixed-priority-no-hysteresis",
                node.MechanismClassification);
            Assert.False(node.PreviousStateTieBreakObserved);
        });
    }

    private static PlantState CreateTwoNodeState()
    {
        var steamDefinition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var stopDefinition = new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d));
        var plant = new PlantDefinition(
            "h12-branch-selection",
            new[] { steamDefinition, stopDefinition },
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var steam = new FluidNodeState(
            steamDefinition,
            new FluidNodeInventory(Mass.FromKilograms(SteamMassKilograms), Energy.FromJoules(SteamEnergyJoules)),
            new FluidThermodynamicState(
                Pressure.FromPascals(6362325.9673817037d),
                Temperature.FromKelvins(552.58890484070866d),
                FluidPhase.SaturatedMixture,
                VaporQuality.FromFraction(0.98827242641541357d)));
        var stop = new FluidNodeState(
            stopDefinition,
            new FluidNodeInventory(Mass.FromKilograms(StopOutMassKilograms), Energy.FromJoules(StopOutEnergyJoules)),
            new FluidThermodynamicState(
                Pressure.FromPascals(8601730.4979163781d),
                Temperature.FromKelvins(588.83285718179309d),
                FluidPhase.SuperheatedVapor,
                null));

        return new PlantState(
            plant,
            new[] { steam, stop },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private static ThermodynamicSwitchingNodeLocalization CreateLocalization(
        string nodeId,
        string nominalPhase,
        double massKilograms,
        double energyJoules,
        double energyProbeJoules,
        double massProbeKilograms)
        => new(
            nodeId,
            "energy+mass",
            "phase-boundary",
            PhaseBoundaryObserved: true,
            EnvelopeBoundaryObserved: false,
            $"hold-{nominalPhase}",
            energyProbeJoules,
            massProbeKilograms,
            Point("nominal", nominalPhase, massKilograms, energyJoules),
            Point("energy-minus", nominalPhase, massKilograms, energyJoules - energyProbeJoules),
            Point("energy-plus", nominalPhase, massKilograms, energyJoules + energyProbeJoules),
            Point("mass-minus", nominalPhase, massKilograms - massProbeKilograms, energyJoules),
            Point("mass-plus", nominalPhase, massKilograms + massProbeKilograms, energyJoules));

    private static ThermodynamicSwitchingProbePoint Point(
        string label,
        string phase,
        double massKilograms,
        double energyJoules)
        => new(
            label,
            true,
            phase,
            massKilograms,
            energyJoules,
            100d / massKilograms,
            energyJoules / massKilograms,
            1d,
            500d,
            null,
            false,
            0d,
            double.NaN,
            double.NaN,
            double.NaN,
            double.NaN,
            double.NaN);
}
