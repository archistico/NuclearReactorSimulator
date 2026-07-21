using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Tests.Physics.Control.TurbineSecondary;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.Integration;

public sealed class IntegratedAutomaticOperationTests
{
    [Fact]
    public void Step_UsesCommittedMeasuredFrameAndPublishesCandidateMeasurementsForNextStep()
    {
        var fixture = CreateFixture();
        var solver = CreateSolver(fixture);

        var result = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));

        Assert.Same(fixture.State.MeasuredSignals, result.Snapshot.CommittedMeasuredSignals);
        Assert.Same(result.InstrumentationStep.Snapshot.MeasuredSignals, result.Snapshot.NextMeasuredSignals);
        Assert.Same(result.Snapshot.NextMeasuredSignals, result.CandidateState.MeasuredSignals);
        Assert.Same(result.ControlledStep.ProtectedStep.FullPlantStep.CandidateState, result.CandidateState.PlantState);

        var expectedPressure = fixture.SignalSources.GetSource("steam-drum/drum-a/pressure")
            .Read(result.Snapshot.Control.ProtectedControl.FullPlant);
        Assert.Equal(
            expectedPressure,
            result.Snapshot.NextMeasuredSignals.GetSignal("pressure").EngineeringValue!.Value,
            8);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedAutomaticOperationState()
    {
        var fixture = CreateFixture();
        var solver = CreateSolver(fixture);

        var left = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var right = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));

        Assert.Equal(
            left.Snapshot.NextMeasuredSignals.GetSignal("pressure").EngineeringValue,
            right.Snapshot.NextMeasuredSignals.GetSignal("pressure").EngineeringValue);
        Assert.Equal(
            left.Snapshot.Control.ProtectedControl.ReactorPrimary.FissionPower.TotalFissionThermalPower,
            right.Snapshot.Control.ProtectedControl.ReactorPrimary.FissionPower.TotalFissionThermalPower);
        Assert.Equal(
            left.Snapshot.Control.ProtectedControl.Protection.LatchedActions,
            right.Snapshot.Control.ProtectedControl.Protection.LatchedActions);
        Assert.Equal(left.CandidateState.AlarmState.NextEventSequence, right.CandidateState.AlarmState.NextEventSequence);
    }

    [Fact]
    public void VerificationRunner_ExecutesNormalAndExplicitProtectionMatrixPhasesDeterministically()
    {
        var leftFixture = CreateFixture();
        var rightFixture = CreateFixture();
        var leftPlan = CreateTwoPhasePlan(leftFixture);
        var rightPlan = CreateTwoPhasePlan(rightFixture);

        var left = new AutomaticOperationVerificationRunner(leftFixture.State, leftFixture.Physical.Physical.ThermodynamicModel, leftFixture.SignalSources)
            .Run(leftPlan);
        var right = new AutomaticOperationVerificationRunner(rightFixture.State, rightFixture.Physical.Physical.ThermodynamicModel, rightFixture.SignalSources)
            .Run(rightPlan);

        Assert.Equal(3, left.TotalStepCount);
        Assert.Equal(3, left.Phases.Count);
        Assert.True(left.Phases[0].ProtectionExpectationSatisfied);
        Assert.True(left.Phases[1].ProtectionExpectationSatisfied);
        Assert.Equal(200_000_000d, left.Phases[1].FinalStep.Snapshot.Control.ProtectedControl.ReactorPrimary.ControlAndActuator.Controllers.GetDiagnostic("power-control").Setpoint);
        Assert.True(left.Phases[2].ProtectionExpectationSatisfied);
        Assert.Equal(ProtectionAction.ReactorScram, left.Phases[2].FinalStep.Snapshot.Control.ProtectedControl.Protection.LatchedActions);
        Assert.True(left.Phases[2].FinalStep.Snapshot.Control.Alarms.GetAlarm("scram-active").ConditionActive);
        Assert.Equal(
            left.FinalState.ReactorPrimaryControlState.ControlRods.GetRod("rod-1").Position,
            right.FinalState.ReactorPrimaryControlState.ControlRods.GetRod("rod-1").Position);
        Assert.Equal(left.MaximumAbsoluteMassClosureResidualKilograms, right.MaximumAbsoluteMassClosureResidualKilograms);
        Assert.Equal(left.MaximumAbsoluteFullEnergyPathClosureResidualJoules, right.MaximumAbsoluteFullEnergyPathClosureResidualJoules);
    }

    [Fact]
    public void VerificationCriteria_AreObservationalAndCanRejectImpossibleTrackingWithoutCorrectingState()
    {
        var fixture = CreateFixture();
        var raw = CreateSolver(fixture).Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var phase = new AutomaticOperationVerificationPhase(
            "impossible-tracking",
            1,
            fixture.Inputs,
            new[] { new AutomaticOperationTrackingTarget("pressure", -1e12d, 0d) });
        var plan = new AutomaticOperationVerificationPlan(
            "reject",
            fixture.State,
            TimeSpan.FromMilliseconds(1d),
            new[] { phase },
            new AutomaticOperationAcceptanceCriteria(1e12d, 1e18d, 10, 10));

        var result = new AutomaticOperationVerificationRunner(fixture.State, fixture.Physical.Physical.ThermodynamicModel, fixture.SignalSources)
            .Run(plan);

        Assert.False(result.GateSatisfied);
        Assert.False(result.Phases[0].TrackingSatisfied);
        Assert.Equal(
            raw.CandidateState.PlantState.PlantState.GetFluidNode("drum").Inventory,
            result.FinalState.PlantState.PlantState.GetFluidNode("drum").Inventory);
    }

    private static AutomaticOperationVerificationPlan CreateTwoPhasePlan(Fixture fixture)
    {
        var normal = new AutomaticOperationVerificationPhase("reference-hold", 1, fixture.Inputs);
        var setpointReactorInputs = new ReactorPrimaryControlInputs(
            fixture.Physical.ReactorDefinition,
            new ControllerInputs(fixture.Physical.ReactorDefinition.ActuatorSystem.ControlSystem, new[]
            {
                new ControllerInput("power-control", ControllerMode.Automatic, 200_000_000d, 0d),
                new ControllerInput("flow-control", ControllerMode.Automatic, 0d, 0d),
            }),
            fixture.Inputs.ReactorPrimaryInputs.NonRodReactivity);
        var setpointInputs = new IntegratedAutomaticOperationInputs(
            fixture.Inputs.PlantInputs,
            setpointReactorInputs,
            fixture.Inputs.TurbineSecondaryInputs,
            fixture.Inputs.ProtectionInputs,
            fixture.Inputs.AlarmInputs,
            fixture.Inputs.InstrumentationInputs);
        var setpointChange = new AutomaticOperationVerificationPhase("power-setpoint-change", 1, setpointInputs);
        var scramInputs = new IntegratedAutomaticOperationInputs(
            fixture.Inputs.PlantInputs,
            fixture.Inputs.ReactorPrimaryInputs,
            fixture.Inputs.TurbineSecondaryInputs,
            new ProtectionSystemInputs(fixture.Protection, manualReactorScram: true),
            fixture.Inputs.AlarmInputs,
            fixture.Inputs.InstrumentationInputs);
        var scram = new AutomaticOperationVerificationPhase(
            "scram-matrix",
            1,
            scramInputs,
            expectedLatchedProtectionActions: ProtectionAction.ReactorScram);
        return new AutomaticOperationVerificationPlan(
            "automatic-operation-matrix",
            fixture.State,
            TimeSpan.FromMilliseconds(1d),
            new[] { normal, setpointChange, scram },
            new AutomaticOperationAcceptanceCriteria(1e12d, 1e18d, 10, 10));
    }

    internal static IntegratedAutomaticOperationSolver CreateSolver(Fixture fixture)
        => new(
            fixture.Physical.ReactorDefinition,
            fixture.Physical.SecondaryDefinition,
            fixture.Protection,
            fixture.Alarms,
            fixture.Physical.Physical.ThermodynamicModel,
            fixture.SignalSources);

    internal static Fixture CreateFixture()
    {
        var physical = TurbineSecondaryControlSolverTests.CreateFixture();
        var protection = new ProtectionSystemDefinition(
            "protection",
            physical.FullPlantDefinition,
            physical.Instrumentation,
            new[]
            {
                new ProtectionFunctionDefinition(
                    "very-high-pressure",
                    "pressure",
                    ProtectionComparison.High,
                    1e11d,
                    9e10d,
                    ProtectionAction.ReactorScram),
            });
        var alarms = new AlarmSystemDefinition(
            "alarms",
            physical.Instrumentation,
            protection,
            new[]
            {
                new AlarmDefinition(
                    "pressure-high",
                    "Pressure high",
                    AlarmSeverity.Warning,
                    AlarmLatchingMode.NonLatching,
                    new MeasuredAlarmConditionDefinition("pressure", AlarmComparison.High, 1e11d)),
                new AlarmDefinition(
                    "scram-active",
                    "SCRAM active",
                    AlarmSeverity.Trip,
                    AlarmLatchingMode.LatchedUntilReset,
                    new ProtectionActionAlarmConditionDefinition(ProtectionAction.ReactorScram)),
            });
        var state = new IntegratedAutomaticOperationState(
            physical.FullPlantDefinition,
            physical.Instrumentation,
            physical.FullPlantState,
            InstrumentationState.CreateUninitialized(physical.Instrumentation),
            physical.Signals,
            physical.ReactorState,
            physical.SecondaryState,
            ProtectionSystemState.CreateInitial(protection),
            AlarmSystemState.CreateInitial(alarms));
        var inputs = new IntegratedAutomaticOperationInputs(
            new IntegratedSecondaryCycleInputs(physical.FullPlantDefinition, physical.Physical.Inputs),
            physical.ReactorInputs,
            physical.SecondaryInputs,
            new ProtectionSystemInputs(protection),
            new AlarmSystemInputs(alarms),
            InstrumentationInputs.Healthy(physical.Instrumentation));
        var signalSources = InstrumentSignalSourceCatalog.CreateFullPlantCatalog(physical.FullPlantDefinition);
        return new Fixture(physical, protection, alarms, state, inputs, signalSources);
    }

    internal sealed record Fixture(
        TurbineSecondaryControlSolverTests.Fixture Physical,
        ProtectionSystemDefinition Protection,
        AlarmSystemDefinition Alarms,
        IntegratedAutomaticOperationState State,
        IntegratedAutomaticOperationInputs Inputs,
        InstrumentSignalSourceCatalog SignalSources);
}
