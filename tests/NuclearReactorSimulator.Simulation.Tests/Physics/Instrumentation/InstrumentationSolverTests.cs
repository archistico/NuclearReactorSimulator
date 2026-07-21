using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Instrumentation;

public sealed class InstrumentationSolverTests
{
    [Fact]
    public void HealthyChannel_ReadsTrueStateIntoSeparateMeasuredFrameAndAppliesScaling()
    {
        var fixture = CreateFullPlantFixture();
        var channel = new InstrumentChannelDefinition(
            "gross-mw",
            "plant/generator/gross-electrical-output",
            "W",
            new SignalRange(-1e12d, 1e12d),
            new LinearSignalScale(-10d, 10d),
            TimeSpan.Zero);
        var definition = new InstrumentationSystemDefinition("instrumentation", new[] { channel });
        var solver = new InstrumentationSolver(definition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition));

        var result = solver.Step(
            fixture.TrueSnapshot,
            InstrumentationState.CreateUninitialized(definition),
            InstrumentationInputs.Healthy(definition),
            TimeSpan.FromMilliseconds(10d));

        var signal = result.Snapshot.MeasuredSignals.GetSignal("gross-mw");
        var diagnostic = result.Snapshot.GetDiagnostic("gross-mw");
        Assert.Equal(fixture.TrueSnapshot.GrossElectricalOutputPower.Watts, signal.EngineeringValue);
        Assert.Equal(fixture.TrueSnapshot.GrossElectricalOutputPower.Watts, diagnostic.TrueEngineeringValue);
        Assert.Equal(SignalValidity.Valid, signal.Validity);
        Assert.Equal(SignalQuality.Good, signal.Quality);
        Assert.NotNull(signal.ScaledValue);
        Assert.DoesNotContain(
            typeof(FullPlantSnapshot),
            typeof(MeasuredSignalFrame).GetProperties().Select(static property => property.PropertyType));
        Assert.DoesNotContain(
            typeof(FullPlantSnapshot),
            typeof(MeasuredSignal).GetProperties().Select(static property => property.PropertyType));
    }

    [Fact]
    public void FirstOrderLag_UsesCommittedInstrumentationStateAndExactDeterministicDiscretization()
    {
        var fixture = CreateFullPlantFixture();
        var channel = new InstrumentChannelDefinition(
            "power",
            "plant/generator/gross-electrical-output",
            "W",
            new SignalRange(-1e12d, 1e12d),
            LinearSignalScale.NormalizedZeroToOne,
            TimeSpan.FromSeconds(2d),
            clampToMeasurementRange: false);
        var definition = new InstrumentationSystemDefinition("instrumentation", new[] { channel });
        var committed = new InstrumentationState(
            definition,
            new[] { new InstrumentationChannelState("power", true, 100d, 100d) });
        var solver = new InstrumentationSolver(definition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition));

        var result = solver.Step(fixture.TrueSnapshot, committed, InstrumentationInputs.Healthy(definition), TimeSpan.FromSeconds(0.5d));

        var truth = fixture.TrueSnapshot.GrossElectricalOutputPower.Watts;
        var alpha = 1d - Math.Exp(-0.5d / 2d);
        var expected = 100d + (alpha * (truth - 100d));
        Assert.Equal(expected, result.CandidateState.GetChannel("power").FilteredEngineeringValue);
        Assert.Equal(expected, result.Snapshot.MeasuredSignals.GetSignal("power").EngineeringValue);
    }

    [Fact]
    public void FaultSeams_AreExplicitDeterministicAndExposeValidityQuality()
    {
        var fixture = CreateFullPlantFixture();
        var channel = new InstrumentChannelDefinition(
            "power",
            "plant/generator/gross-electrical-output",
            "W",
            new SignalRange(-1e12d, 1e12d),
            LinearSignalScale.NormalizedZeroToOne,
            TimeSpan.Zero);
        var definition = new InstrumentationSystemDefinition("instrumentation", new[] { channel });
        var committed = new InstrumentationState(
            definition,
            new[] { new InstrumentationChannelState("power", true, 50d, 123d) });
        var solver = new InstrumentationSolver(definition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition));

        var biased = solver.Step(
            fixture.TrueSnapshot,
            committed,
            new InstrumentationInputs(definition, new[] { new SensorFaultInput("power", SensorFaultMode.Bias, 250d) }),
            TimeSpan.FromMilliseconds(10d));
        Assert.Equal(fixture.TrueSnapshot.GrossElectricalOutputPower.Watts + 250d, biased.Snapshot.MeasuredSignals.GetSignal("power").EngineeringValue);
        Assert.Equal(SignalValidity.Valid, biased.Snapshot.MeasuredSignals.GetSignal("power").Validity);
        Assert.Equal(SignalQuality.Suspect, biased.Snapshot.MeasuredSignals.GetSignal("power").Quality);

        var frozen = solver.Step(
            fixture.TrueSnapshot,
            committed,
            new InstrumentationInputs(definition, new[] { new SensorFaultInput("power", SensorFaultMode.Freeze) }),
            TimeSpan.FromMilliseconds(10d));
        Assert.Equal(123d, frozen.Snapshot.MeasuredSignals.GetSignal("power").EngineeringValue);
        Assert.Equal(SignalValidity.Invalid, frozen.Snapshot.MeasuredSignals.GetSignal("power").Validity);
        Assert.Equal(SignalQuality.Suspect, frozen.Snapshot.MeasuredSignals.GetSignal("power").Quality);

        var unavailable = solver.Step(
            fixture.TrueSnapshot,
            committed,
            new InstrumentationInputs(definition, new[] { new SensorFaultInput("power", SensorFaultMode.Unavailable) }),
            TimeSpan.FromMilliseconds(10d));
        Assert.Null(unavailable.Snapshot.MeasuredSignals.GetSignal("power").EngineeringValue);
        Assert.Null(unavailable.Snapshot.MeasuredSignals.GetSignal("power").ScaledValue);
        Assert.Equal(SignalValidity.Invalid, unavailable.Snapshot.MeasuredSignals.GetSignal("power").Validity);
        Assert.Equal(SignalQuality.Unavailable, unavailable.Snapshot.MeasuredSignals.GetSignal("power").Quality);
    }

    [Fact]
    public void RangeViolation_IsObservableAndClampingDoesNotHideQualityDegradation()
    {
        var fixture = CreateFullPlantFixture();
        var truth = fixture.TrueSnapshot.GrossElectricalOutputPower.Watts;
        var channel = new InstrumentChannelDefinition(
            "narrow",
            "plant/generator/gross-electrical-output",
            "W",
            new SignalRange(truth - 1d, truth + 1d),
            LinearSignalScale.NormalizedZeroToOne,
            TimeSpan.Zero,
            clampToMeasurementRange: true);
        var definition = new InstrumentationSystemDefinition("instrumentation", new[] { channel });
        var solver = new InstrumentationSolver(definition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition));
        var inputs = new InstrumentationInputs(
            definition,
            new[] { new SensorFaultInput("narrow", SensorFaultMode.Bias, 100d) });

        var result = solver.Step(fixture.TrueSnapshot, InstrumentationState.CreateUninitialized(definition), inputs, TimeSpan.FromMilliseconds(1d));
        var signal = result.Snapshot.MeasuredSignals.GetSignal("narrow");

        Assert.Equal(channel.MeasurementRange.Maximum, signal.EngineeringValue);
        Assert.True(signal.OutOfMeasurementRange);
        Assert.Equal(SignalQuality.Suspect, signal.Quality);
    }

    [Fact]
    public void Inputs_RequireExactOneFaultSeamPerChannel()
    {
        var channelA = Channel("A");
        var channelB = Channel("B");
        var definition = new InstrumentationSystemDefinition("instrumentation", new[] { channelA, channelB });

        Assert.Throws<ArgumentException>(() => new InstrumentationInputs(definition, new[] { SensorFaultInput.Healthy("A") }));
        Assert.Throws<ArgumentException>(() => new InstrumentationInputs(definition, new[] { SensorFaultInput.Healthy("A"), SensorFaultInput.Healthy("A") }));
    }

    [Fact]
    public void FullPlantCatalog_ProvidesCanonicalAggregateAndPerComponentSources()
    {
        var fixture = CreateFullPlantFixture();
        var catalog = InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition);
        var drumId = fixture.Definition.PrimaryCircuit.SteamDrumSystem.Drums[0].Id;
        var rotorId = fixture.Definition.TurbineExpansionSystem.Rotors[0].Id;
        var condenserId = fixture.Definition.CondensateFeedwaterSystem.CondenserSystem.Condensers[0].Id;
        var generatorId = fixture.Definition.GeneratorGridSystem.Generators[0].Id;

        Assert.Equal("W", catalog.GetSource("plant/reactor/thermal-power").EngineeringUnitSymbol);
        Assert.Equal("fraction", catalog.GetSource($"steam-drum/{drumId}/level").EngineeringUnitSymbol);
        Assert.Equal("rpm", catalog.GetSource($"turbine-rotor/{rotorId}/speed").EngineeringUnitSymbol);
        Assert.Equal("Pa", catalog.GetSource($"condenser/{condenserId}/pressure").EngineeringUnitSymbol);
        Assert.Equal("Hz", catalog.GetSource($"generator/{generatorId}/frequency").EngineeringUnitSymbol);
    }

    [Fact]
    public void InstrumentedFullPlantSolver_PreservesM47PhysicalOwnershipAndAddsOnlyObservationState()
    {
        var fixture = CreateFullPlantFixture();
        var channel = Channel("power");
        var instrumentationDefinition = new InstrumentationSystemDefinition("instrumentation", new[] { channel });
        var committedState = new InstrumentedFullPlantState(
            fixture.Definition,
            instrumentationDefinition,
            fixture.CommittedState,
            InstrumentationState.CreateUninitialized(instrumentationDefinition));
        var solver = new InstrumentedFullPlantSolver(
            fixture.Definition,
            instrumentationDefinition,
            fixture.ThermodynamicModel,
            InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.Definition));

        var result = solver.Step(
            committedState,
            fixture.Inputs,
            InstrumentationInputs.Healthy(instrumentationDefinition),
            TimeSpan.FromMilliseconds(1d));

        Assert.Same(result.FullPlantStep.CandidateState, result.CandidateState.PlantState);
        Assert.Same(result.InstrumentationStep.CandidateState, result.CandidateState.InstrumentationState);
        Assert.Same(result.FullPlantStep.Snapshot, result.Snapshot.TruePlantState);
        Assert.Same(result.InstrumentationStep.Snapshot.MeasuredSignals, result.Snapshot.MeasuredSignals);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalTrueStateInstrumentationStateInputsAndTimestep()
    {
        var leftFixture = CreateFullPlantFixture();
        var rightFixture = CreateFullPlantFixture();
        var leftDefinition = new InstrumentationSystemDefinition("instrumentation", new[] { Channel("power") });
        var rightDefinition = new InstrumentationSystemDefinition("instrumentation", new[] { Channel("power") });
        var leftSolver = new InstrumentationSolver(leftDefinition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(leftFixture.Definition));
        var rightSolver = new InstrumentationSolver(rightDefinition, InstrumentSignalSourceCatalog.CreateFullPlantCatalog(rightFixture.Definition));
        var leftState = new InstrumentationState(leftDefinition, new[] { new InstrumentationChannelState("power", true, 12d, 12d) });
        var rightState = new InstrumentationState(rightDefinition, new[] { new InstrumentationChannelState("power", true, 12d, 12d) });

        var left = leftSolver.Step(leftFixture.TrueSnapshot, leftState, InstrumentationInputs.Healthy(leftDefinition), TimeSpan.FromMilliseconds(20d));
        var right = rightSolver.Step(rightFixture.TrueSnapshot, rightState, InstrumentationInputs.Healthy(rightDefinition), TimeSpan.FromMilliseconds(20d));

        Assert.Equal(left.CandidateState.GetChannel("power").FilteredEngineeringValue, right.CandidateState.GetChannel("power").FilteredEngineeringValue);
        Assert.Equal(left.Snapshot.MeasuredSignals.GetSignal("power"), right.Snapshot.MeasuredSignals.GetSignal("power"));
    }

    private static InstrumentChannelDefinition Channel(string id)
        => new(
            id,
            "plant/generator/gross-electrical-output",
            "W",
            new SignalRange(-1e12d, 1e12d),
            LinearSignalScale.NormalizedZeroToOne,
            TimeSpan.FromSeconds(1d),
            clampToMeasurementRange: false);

    private static FullPlantFixture CreateFullPlantFixture()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var committedState = new FullPlantState(definition, fixture.PlantState, fixture.TurbineState, fixture.ElectricalState);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var trueSnapshot = new FullPlantSolver(definition, fixture.ThermodynamicModel)
            .Step(committedState, inputs, TimeSpan.FromMilliseconds(1d))
            .Snapshot;
        return new FullPlantFixture(definition, committedState, inputs, trueSnapshot, fixture.ThermodynamicModel);
    }

    private sealed record FullPlantFixture(
        IntegratedSecondaryCycleDefinition Definition,
        FullPlantState CommittedState,
        IntegratedSecondaryCycleInputs Inputs,
        FullPlantSnapshot TrueSnapshot,
        global::NuclearReactorSimulator.Simulation.Physics.Fluids.IFluidThermodynamicModel ThermodynamicModel);
}
