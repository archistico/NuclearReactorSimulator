using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class ThermodynamicSwitchingLocalizationAnalyzerTests
{
    [Fact]
    public void PhaseBoundary_IsLocalizedByEnergyAndMassAxis()
    {
        var model = new ThresholdThermodynamicModel(throwAboveThreshold: false);
        var state = CreateState();
        var h10 = CreateH10Report("node");

        var report = new ThermodynamicSwitchingLocalizationAnalyzer(model, model).Analyze(state, h10);
        var node = Assert.Single(report.Nodes);

        Assert.True(node.PhaseBoundaryObserved);
        Assert.False(node.EnvelopeBoundaryObserved);
        Assert.Equal("energy+mass", node.CrossingAxis);
        Assert.Equal("phase-boundary", node.BoundaryClassification);
        Assert.Equal("hold-SubcooledLiquid", node.SuggestedActiveSet);
        Assert.Equal("SubcooledLiquid", node.Nominal.Phase);
        Assert.Equal("SuperheatedVapor", node.EnergyPlus.Phase);
        Assert.Equal("SuperheatedVapor", node.MassMinus.Phase);
        Assert.True(node.Nominal.SaturationReferenceAvailable);
    }

    [Fact]
    public void EnvelopeEdge_IsDistinguishedFromResolvedPhaseChange()
    {
        var model = new ThresholdThermodynamicModel(throwAboveThreshold: true);
        var state = CreateState();
        var h10 = CreateH10Report("node");

        var report = new ThermodynamicSwitchingLocalizationAnalyzer(model, model).Analyze(state, h10);
        var node = Assert.Single(report.Nodes);

        Assert.False(node.PhaseBoundaryObserved);
        Assert.True(node.EnvelopeBoundaryObserved);
        Assert.Equal("energy+mass", node.CrossingAxis);
        Assert.Equal("envelope-edge", node.BoundaryClassification);
        Assert.False(node.EnergyPlus.Resolved);
        Assert.False(node.MassMinus.Resolved);
    }

    [Fact]
    public void OnlyNodesFlaggedByH10_AreLocalized()
    {
        var model = new ThresholdThermodynamicModel(throwAboveThreshold: false);
        var state = CreateState();
        var h10 = new HydraulicMapSmoothnessReport(
            Array.Empty<HydraulicPathSmoothnessProbe>(),
            new[]
            {
                CreateH10Node("other", phaseSwitch: false),
                CreateH10Node("node", phaseSwitch: true),
            });

        var report = new ThermodynamicSwitchingLocalizationAnalyzer(model, model).Analyze(state, h10);

        Assert.Single(report.Nodes, static item => item.NodeId == "node");
    }

    [Fact]
    public void SameInputs_ProduceExactlyRepeatableLocalization()
    {
        var model = new ThresholdThermodynamicModel(throwAboveThreshold: false);
        var state = CreateState();
        var h10 = CreateH10Report("node");
        var analyzer = new ThermodynamicSwitchingLocalizationAnalyzer(model, model);

        var left = analyzer.Analyze(state, h10);
        var right = analyzer.Analyze(state, h10);

        Assert.True(left.Nodes.SequenceEqual(right.Nodes));
    }

    private static PlantState CreateState()
    {
        var definition = new FluidNodeDefinition("node", Volume.FromCubicMetres(1d));
        var plant = new PlantDefinition(
            "h11-localization",
            new[] { definition },
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var node = new FluidNodeState(
            definition,
            new FluidNodeInventory(Mass.FromKilograms(1_000d), Energy.FromMegajoules(500d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(250d),
                FluidPhase.SubcooledLiquid,
                null));

        return new PlantState(
            plant,
            new[] { node },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private static HydraulicMapSmoothnessReport CreateH10Report(string nodeId)
        => new(
            Array.Empty<HydraulicPathSmoothnessProbe>(),
            new[] { CreateH10Node(nodeId, phaseSwitch: true) });

    private static ThermodynamicNodeSmoothnessProbe CreateH10Node(string nodeId, bool phaseSwitch)
        => new(
            nodeId,
            "SubcooledLiquid",
            "SubcooledLiquid",
            phaseSwitch ? "SuperheatedVapor" : "SubcooledLiquid",
            phaseSwitch ? "SuperheatedVapor" : "SubcooledLiquid",
            "SubcooledLiquid",
            EnergyMinusResolved: true,
            EnergyPlusResolved: true,
            MassMinusResolved: true,
            MassPlusResolved: true,
            BasePressurePascals: 5_000_000d,
            EnergyDerivativeScaleGrowth: phaseSwitch ? 4d : 1d,
            MassDerivativeScaleGrowth: phaseSwitch ? 4d : 1d,
            PhaseOrEnvelopeSwitchObserved: phaseSwitch,
            NonSmoothEvidence: phaseSwitch);

    private sealed class ThresholdThermodynamicModel : IFluidThermodynamicModel, IWaterSteamSaturationPropertyProvider
    {
        private readonly bool _throwAboveThreshold;

        public ThresholdThermodynamicModel(bool throwAboveThreshold)
        {
            _throwAboveThreshold = throwAboveThreshold;
        }

        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            var specificEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;
            if (_throwAboveThreshold && specificEnergy > 500_000d)
            {
                throw new WaterSteamStateOutOfRangeException(
                    definition.Id,
                    definition.Volume.CubicMetres / inventory.Mass.Kilograms,
                    specificEnergy);
            }

            var phase = specificEnergy > 500_000d
                ? FluidPhase.SuperheatedVapor
                : FluidPhase.SubcooledLiquid;
            return new FluidThermodynamicState(
                previousState.Pressure,
                previousState.Temperature,
                phase,
                null);
        }

        public WaterSteamSaturationProperties GetSaturationProperties(Temperature temperature)
            => new(
                temperature,
                Pressure.FromMegapascals(4d),
                Density.FromKilogramsPerCubicMetre(800d),
                Density.FromKilogramsPerCubicMetre(20d),
                SpecificEnergy.FromKilojoulesPerKilogram(400d),
                SpecificEnergy.FromKilojoulesPerKilogram(2_000d));

        public WaterSteamSaturationProperties GetSaturationProperties(Pressure pressure)
            => GetSaturationProperties(Temperature.FromDegreesCelsius(250d));
    }
}
