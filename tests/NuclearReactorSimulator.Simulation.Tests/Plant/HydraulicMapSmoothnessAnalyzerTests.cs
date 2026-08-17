using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class HydraulicMapSmoothnessAnalyzerTests
{
    [Fact]
    public void EqualPressurePipe_ExposesSquareRootDerivativeScaleGrowth()
    {
        var state = CreatePipeState(5d, 5d);
        var report = new HydraulicMapSmoothnessAnalyzer(new SmoothThermodynamicModel()).Analyze(state);
        var path = Assert.Single(report.HydraulicPaths);

        Assert.Equal("zero", path.BaseBranch);
        Assert.True(path.BranchSwitchObserved);
        Assert.True(path.DerivativeScaleGrowth >= HydraulicMapSmoothnessProbeOptions.H10AuditDefault.DerivativeScaleGrowthThreshold);
        Assert.True(path.NonSmoothEvidence);
    }

    [Fact]
    public void PumpCheckValveBoundary_ExposesBlockedToForwardBranchSwitch()
    {
        var state = CreatePumpState(fromPressureMegapascals: 5d, toPressureMegapascals: 5.4d, hasCheckValve: true);
        var report = new HydraulicMapSmoothnessAnalyzer(new SmoothThermodynamicModel()).Analyze(state);
        var path = Assert.Single(report.HydraulicPaths);

        Assert.Equal("zero", path.BaseBranch);
        Assert.True(path.CoarseMinusBranch == "check-blocked" || path.FineMinusBranch == "check-blocked");
        Assert.True(path.CoarsePlusBranch == "forward" || path.FinePlusBranch == "forward");
        Assert.True(path.BranchSwitchObserved);
        Assert.True(path.NonSmoothEvidence);
    }

    [Fact]
    public void PipeAwayFromZeroPressureDifference_RemainsOnOneBranch()
    {
        var state = CreatePipeState(5.2d, 5d);
        var report = new HydraulicMapSmoothnessAnalyzer(new SmoothThermodynamicModel()).Analyze(state);
        var path = Assert.Single(report.HydraulicPaths);

        Assert.Equal("forward", path.BaseBranch);
        Assert.False(path.BranchSwitchObserved);
        Assert.True(path.DerivativeScaleGrowth < HydraulicMapSmoothnessProbeOptions.H10AuditDefault.DerivativeScaleGrowthThreshold);
    }

    [Fact]
    public void ThermodynamicPhaseBoundary_IsReportedWithoutChangingHydraulicLaw()
    {
        var state = CreatePipeState(5.2d, 5d);
        var report = new HydraulicMapSmoothnessAnalyzer(new PhaseSwitchThermodynamicModel()).Analyze(state);

        Assert.True(report.ThermodynamicPhaseSwitchCount > 0);
        Assert.Contains(report.ThermodynamicNodes, static item => item.NonSmoothEvidence);
    }

    [Fact]
    public void SameState_ProducesExactlyRepeatableSmoothnessEvidence()
    {
        var state = CreatePumpState(fromPressureMegapascals: 5d, toPressureMegapascals: 5.4d, hasCheckValve: true);
        var analyzer = new HydraulicMapSmoothnessAnalyzer(new SmoothThermodynamicModel());

        var left = analyzer.Analyze(state);
        var right = analyzer.Analyze(state);

        Assert.True(left.HydraulicPaths.SequenceEqual(right.HydraulicPaths));
        Assert.True(left.ThermodynamicNodes.SequenceEqual(right.ThermodynamicNodes));
    }

    private static PlantState CreatePipeState(double fromPressureMegapascals, double toPressureMegapascals)
    {
        var fromDefinition = new FluidNodeDefinition("from", Volume.FromCubicMetres(1d));
        var toDefinition = new FluidNodeDefinition("to", Volume.FromCubicMetres(1d));
        var pipe = new PipeDefinition(
            "pipe",
            "from",
            "to",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        var definition = new PlantDefinition(
            "h10-pipe-probe",
            new[] { fromDefinition, toDefinition },
            new[] { pipe },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        return new PlantState(
            definition,
            new[]
            {
                CreateNode(fromDefinition, fromPressureMegapascals, 1_000d),
                CreateNode(toDefinition, toPressureMegapascals, 1_000d),
            },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private static PlantState CreatePumpState(
        double fromPressureMegapascals,
        double toPressureMegapascals,
        bool hasCheckValve)
    {
        var fromDefinition = new FluidNodeDefinition("from", Volume.FromCubicMetres(1d));
        var toDefinition = new FluidNodeDefinition("to", Volume.FromCubicMetres(1d));
        var pump = new PumpDefinition(
            "pump",
            new PipeDefinition(
                "pump-path",
                "from",
                "to",
                QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d)),
            PressureDifference.FromMegapascals(0.4d),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
            PumpEfficiency.FromPercent(80d),
            hasCheckValve);
        var definition = new PlantDefinition(
            "h10-pump-probe",
            new[] { fromDefinition, toDefinition },
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            new[] { pump },
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        return new PlantState(
            definition,
            new[]
            {
                CreateNode(fromDefinition, fromPressureMegapascals, 1_000d),
                CreateNode(toDefinition, toPressureMegapascals, 1_000d),
            },
            Array.Empty<ValveState>(),
            new[] { new PumpState(pump.Id, PumpSpeed.Rated) },
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private static FluidNodeState CreateNode(FluidNodeDefinition definition, double pressureMegapascals, double massKilograms)
        => new(
            definition,
            new FluidNodeInventory(Mass.FromKilograms(massKilograms), Energy.FromMegajoules(massKilograms * 0.5d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(pressureMegapascals),
                Temperature.FromDegreesCelsius(250d),
                FluidPhase.SubcooledLiquid,
                null));

    private sealed class SmoothThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            var pressurePascals = 5_000_000d
                + ((inventory.Mass.Kilograms - 1_000d) * 100_000d)
                + ((inventory.SpecificInternalEnergy.JoulesPerKilogram - 500_000d) * 0.1d);
            return new FluidThermodynamicState(
                Pressure.FromPascals(pressurePascals),
                previousState.Temperature,
                FluidPhase.SubcooledLiquid,
                null);
        }
    }

    private sealed class PhaseSwitchThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            var phase = inventory.SpecificInternalEnergy.JoulesPerKilogram > 500_000d
                ? FluidPhase.SuperheatedVapor
                : FluidPhase.SubcooledLiquid;
            return new FluidThermodynamicState(
                previousState.Pressure,
                previousState.Temperature,
                phase,
                null);
        }
    }
}
