using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Core.Spatial;

public sealed class QuasiSpatialCoreFeedbackSolverTests
{
    [Fact]
    public void Step_ReducesLocalFeedbackToPowerWeightedGlobalContribution()
    {
        var fixture = CreateFixture(Array.Empty<CoreZoneCouplingDefinition>());

        var result = fixture.Solver.Step(fixture.CoreState, fixture.PlantState, TimeSpan.FromSeconds(1d));

        Assert.Equal(3d, result.Snapshot.PowerWeightedFeedbackReactivity.Pcm, 9);
        Assert.Equal(-10d, result.Snapshot.GetZone("zone-a").LocalFeedbackReactivity.Pcm, 9);
        Assert.Equal(0d, result.Snapshot.GetZone("zone-b").LocalFeedbackReactivity.Pcm, 9);
        Assert.Equal(10d, result.Snapshot.GetZone("zone-c").LocalFeedbackReactivity.Pcm, 9);
    }

    [Fact]
    public void Step_RedistributesPowerShapeWithoutCreatingLocalKineticsOrBreakingClosure()
    {
        var fixture = CreateFixture(Array.Empty<CoreZoneCouplingDefinition>());

        var result = fixture.Solver.Step(fixture.CoreState, fixture.PlantState, TimeSpan.FromSeconds(1d));

        Assert.True(result.CandidateState.GetZone("zone-a").PowerFraction.Fraction < 0.2d);
        Assert.True(result.CandidateState.GetZone("zone-c").PowerFraction.Fraction > 0.5d);
        Assert.Equal(1d, result.CandidateState.Zones.Sum(static zone => zone.PowerFraction.Fraction), 12);
        Assert.Equal(fixture.CoreState.Definition, result.CandidateState.Definition);
    }

    [Fact]
    public void Step_ExplicitCouplingSmoothsShapeDrivingSignal()
    {
        var uncoupled = CreateFixture(Array.Empty<CoreZoneCouplingDefinition>());
        var coupled = CreateFixture(new[]
        {
            new CoreZoneCouplingDefinition("zone-a", "zone-c", CoreZoneCouplingFraction.FromPercent(50d)),
        });

        var uncoupledResult = uncoupled.Solver.Step(uncoupled.CoreState, uncoupled.PlantState, TimeSpan.FromSeconds(1d));
        var coupledResult = coupled.Solver.Step(coupled.CoreState, coupled.PlantState, TimeSpan.FromSeconds(1d));

        var uncoupledShift = Math.Abs(uncoupledResult.CandidateState.GetZone("zone-a").PowerFraction.Fraction - 0.2d);
        var coupledShift = Math.Abs(coupledResult.CandidateState.GetZone("zone-a").PowerFraction.Fraction - 0.2d);

        Assert.True(coupledShift < uncoupledShift);
        Assert.Equal(0d, coupledResult.Snapshot.GetZone("zone-a").CoupledShapeDrivingReactivity.Pcm, 9);
        Assert.Equal(0d, coupledResult.Snapshot.GetZone("zone-c").CoupledShapeDrivingReactivity.Pcm, 9);
    }

    [Fact]
    public void Step_IsDeterministicForSameCommittedStateAndTimestep()
    {
        var fixture = CreateFixture(new[]
        {
            new CoreZoneCouplingDefinition("zone-a", "zone-b", CoreZoneCouplingFraction.FromPercent(15d)),
            new CoreZoneCouplingDefinition("zone-b", "zone-c", CoreZoneCouplingFraction.FromPercent(15d)),
        });

        var first = fixture.Solver.Step(fixture.CoreState, fixture.PlantState, TimeSpan.FromMilliseconds(250d));
        var second = fixture.Solver.Step(fixture.CoreState, fixture.PlantState, TimeSpan.FromMilliseconds(250d));

        Assert.Equal(
            first.CandidateState.Zones.Select(static zone => zone.PowerFraction.Fraction),
            second.CandidateState.Zones.Select(static zone => zone.PowerFraction.Fraction));
        Assert.Equal(first.Snapshot.PowerWeightedFeedbackReactivity, second.Snapshot.PowerWeightedFeedbackReactivity);
        Assert.Equal(first.Snapshot.Zones, second.Snapshot.Zones);
    }

    [Fact]
    public void Step_ZeroSensitivityPreservesCommittedShapeWhileRetainingFeedbackWeighting()
    {
        var fixture = CreateFixture(Array.Empty<CoreZoneCouplingDefinition>(), CorePowerShapeSensitivity.Zero);

        var result = fixture.Solver.Step(fixture.CoreState, fixture.PlantState, TimeSpan.FromSeconds(1d));

        Assert.Equal(3d, result.Snapshot.PowerWeightedFeedbackReactivity.Pcm, 9);
        Assert.Equal(
            fixture.CoreState.Zones.Select(static zone => zone.PowerFraction.Fraction),
            result.CandidateState.Zones.Select(static zone => zone.PowerFraction.Fraction));
    }

    private static Fixture CreateFixture(
        IEnumerable<CoreZoneCouplingDefinition> couplings,
        CorePowerShapeSensitivity? sensitivity = null)
    {
        var suffixes = new[] { "a", "b", "c" };
        var plant = new PlantDefinition(
            "plant",
            suffixes.Select(suffix => new FluidNodeDefinition($"coolant-{suffix}", Volume.FromCubicMetres(10d))).ToArray(),
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            suffixes.SelectMany(suffix => new[]
            {
                new ThermalBodyDefinition($"fuel-{suffix}", HeatCapacity.FromJoulesPerKelvin(1_000_000d)),
                new ThermalBodyDefinition($"structure-{suffix}", HeatCapacity.FromJoulesPerKelvin(2_000_000d)),
            }).ToArray(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        var fluidStates = suffixes.Select(suffix => new FluidNodeState(
            plant.GetFluidNode($"coolant-{suffix}"),
            new FluidNodeInventory(Mass.FromKilograms(8_000d), Energy.FromMegajoules(5_000d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(6.8d),
                Temperature.FromDegreesCelsius(280d),
                FluidPhase.SubcooledLiquid,
                null))).ToArray();
        var fuelTemperatures = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["a"] = 710d,
            ["b"] = 700d,
            ["c"] = 690d,
        };
        var thermalStates = suffixes.SelectMany(suffix => new[]
        {
            ThermalBodyState.FromTemperature(
                plant.GetThermalBody($"fuel-{suffix}"),
                Temperature.FromDegreesCelsius(fuelTemperatures[suffix])),
            ThermalBodyState.FromTemperature(
                plant.GetThermalBody($"structure-{suffix}"),
                Temperature.FromDegreesCelsius(500d)),
        }).ToArray();
        var plantState = new PlantState(
            plant,
            fluidStates,
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            thermalStates,
            Array.Empty<HeatSourceState>());

        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition("zone-a", new CoreZoneCoordinate(0, 0), CoreZonePowerFraction.FromPercent(20d), "fuel-a", "structure-a", "coolant-a"),
                new CoreZoneDefinition("zone-b", new CoreZoneCoordinate(1, 4), CoreZonePowerFraction.FromPercent(30d), "fuel-b", "structure-b", "coolant-b"),
                new CoreZoneDefinition("zone-c", new CoreZoneCoordinate(7, 11), CoreZonePowerFraction.FromPercent(50d), "fuel-c", "structure-c", "coolant-c"),
            });
        var coreState = AggregatedCoreState.CreateNominal(core);
        var definition = new QuasiSpatialCoreFeedbackDefinition(
            "quasi-spatial",
            core,
            new TemperatureReactivityFeedbackDefinition(
                "fuel-temperature",
                ReactivityContributionKind.FuelTemperature,
                Temperature.FromDegreesCelsius(700d),
                TemperatureReactivityCoefficient.FromPcmPerKelvin(-1d)),
            new TemperatureReactivityFeedbackDefinition(
                "coolant-temperature",
                ReactivityContributionKind.CoolantTemperature,
                Temperature.FromDegreesCelsius(280d),
                TemperatureReactivityCoefficient.Zero),
            new VoidReactivityFeedbackDefinition(
                "void",
                VoidFraction.NoVoid,
                VoidReactivityCoefficient.Zero),
            sensitivity ?? CorePowerShapeSensitivity.FromPerPcm(0.02d),
            TimeSpan.FromSeconds(2d),
            couplings);

        return new Fixture(coreState, plantState, new QuasiSpatialCoreFeedbackSolver(definition));
    }

    private sealed record Fixture(
        AggregatedCoreState CoreState,
        PlantState PlantState,
        QuasiSpatialCoreFeedbackSolver Solver);
}
