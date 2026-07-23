using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Long-running, operator-journey acceptance tests. These are intentionally Explicit so the normal fast suite does not
/// pay their runtime cost. Run them deliberately when changing integrated plant balance, turbine/generator/grid behavior,
/// scenario seeds, or before promoting a release candidate.
/// </summary>
public sealed class GameplayJourneyLongRunningTests
{
    private const int StepsPerCheckpoint = 1_000;
    private const int CheckpointCount = 6;

    [Fact(Explicit = true)]
    public void DesktopIntegratedSession_SustainsParallelElectricalExportForSixtySimulatedSeconds()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(DesktopIntegratedOperationsProgram.Scenario);
        var initial = session.Coordinator.Current;
        var initialGenerator = Assert.Single(initial.Electrical.Generators);
        var initialRotor = Assert.Single(initial.TurbineSecondary.Rotors);

        Assert.True(initialGenerator.BreakerClosed);
        Assert.True((initialGenerator.ElectricalOutput.NumericValue ?? 0d) > 0.1d, Diagnostic(initial));
        Assert.True(
            double.IsFinite(initialRotor.ShaftPower.NumericValue ?? double.NaN),
            "Desktop seed must expose a finite MODEL rotor-shaft value even when the measured aggregate channel is temporarily unavailable. " + Diagnostic(initial));

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var journeyEvidence = new List<string>();

        for (var checkpoint = 1; checkpoint <= CheckpointCount; checkpoint++)
        {
            var executedStepCount = AdvanceRunningCooperatively(session.Coordinator, StepsPerCheckpoint);
            Assert.Equal(StepsPerCheckpoint, executedStepCount);

            var current = session.Coordinator.Current;
            var generator = Assert.Single(current.Electrical.Generators);
            var rotor = Assert.Single(current.TurbineSecondary.Rotors);
            journeyEvidence.Add(AtCheckpoint(checkpoint, current));
            var evidence = string.Join(Environment.NewLine, journeyEvidence);

            Assert.False(current.AnyTripActive, evidence);
            Assert.True(generator.BreakerClosed, evidence);
            Assert.True(double.IsFinite(rotor.Speed.NumericValue ?? double.NaN), evidence);
            Assert.True((generator.RequestedElectricalPower.NumericValue ?? 0d) > 4.5d, evidence);
            Assert.True((rotor.ShaftPower.NumericValue ?? 0d) > 4.5d, evidence);
            Assert.True((generator.ElectricalOutput.NumericValue ?? 0d) > 4.0d, evidence);
        }
    }

    [Fact(Explicit = true)]
    public void SynchronizeCloseLoadRaiseJourney_ProducesAndSustainsElectricalExport()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new GridSynchronizationSustainedInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(CreateSustainedSynchronizationScenario());
        var initialGenerator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        Assert.False(initialGenerator.BreakerClosed);
        Assert.True(initialGenerator.SynchronizationConditionsSatisfied);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var paralleled = Assert.Single(session.Coordinator.Current.Electrical.Generators);
        Assert.True(paralleled.BreakerClosed, Diagnostic(session.Coordinator.Current));

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            paralleled.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var loaded = Assert.Single(session.Coordinator.Current.Electrical.Generators);
        Assert.True((loaded.ElectricalOutput.NumericValue ?? 0d) > 4.5d, Diagnostic(session.Coordinator.Current));

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var journeyEvidence = new List<string>();

        for (var checkpoint = 1; checkpoint <= CheckpointCount; checkpoint++)
        {
            var executedStepCount = AdvanceRunningCooperatively(session.Coordinator, StepsPerCheckpoint);
            Assert.Equal(StepsPerCheckpoint, executedStepCount);

            var current = session.Coordinator.Current;
            var generator = Assert.Single(current.Electrical.Generators);
            var rotor = Assert.Single(current.TurbineSecondary.Rotors);
            journeyEvidence.Add(AtCheckpoint(checkpoint, current));
            var evidence = string.Join(Environment.NewLine, journeyEvidence);
            Assert.False(current.AnyTripActive, evidence);
            Assert.True(generator.BreakerClosed, evidence);
            Assert.True((generator.RequestedElectricalPower.NumericValue ?? 0d) > 4.5d, evidence);
            Assert.True((rotor.ShaftPower.NumericValue ?? 0d) > 4.5d, evidence);
            Assert.True((generator.ElectricalOutput.NumericValue ?? 0d) > 4.0d, evidence);
        }
    }


    private static ScenarioDefinition CreateSustainedSynchronizationScenario()
    {
        var source = GridSynchronizationLoadProgram.Scenario;
        return new ScenarioDefinition(
            "pre-synchronization-grid-loading-m1094-long",
            source.Title,
            source.Description,
            GridSynchronizationSustainedInitialConditionFactory.Reference,
            source.Objectives,
            source.AllowedOperatorActions,
            source.Faults,
            source.HistoricalContext);
    }

    private static int AdvanceRunningCooperatively(ControlRoomRuntimeCoordinator coordinator, int stepCount)
    {
        var remaining = stepCount;
        var executed = 0;
        var maximumBatchSize = coordinator.ExecutionBudget.MaximumSimulationStepsPerBatch;

        while (remaining > 0)
        {
            var requestedBatchSize = Math.Min(remaining, maximumBatchSize);
            var result = coordinator.AdvanceRunning(requestedBatchSize, publicationStride: requestedBatchSize);
            executed += result.ExecutedStepCount;
            remaining -= result.ExecutedStepCount;

            if (result.ExecutedStepCount != requestedBatchSize)
            {
                break;
            }
        }

        return executed;
    }

    private static string AtCheckpoint(int checkpoint, ControlRoomSnapshot snapshot)
        => FormattableString.Invariant($"Checkpoint {checkpoint}/{CheckpointCount} · logical step {snapshot.LogicalStep}. {Diagnostic(snapshot)}");

    private static string Diagnostic(ControlRoomSnapshot snapshot)
    {
        var generator = snapshot.Electrical.Generators.FirstOrDefault();
        var rotor = snapshot.TurbineSecondary.Rotors.FirstOrDefault();
        var train = snapshot.TurbineSecondary.AdmissionTrains.FirstOrDefault();
        var condenser = snapshot.TurbineSecondary.Condensers.FirstOrDefault();
        var feedwater = snapshot.TurbineSecondary.FeedwaterTrains.FirstOrDefault();
        var drum = snapshot.PrimaryCircuit.SteamDrums.FirstOrDefault();
        return string.Join(
            " | ",
            ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic(snapshot),
            generator is null ? "GENERATOR —" : FormattableString.Invariant($"BREAKER={generator.BreakerText}; MWe={generator.ElectricalOutput.NumericValue:0.###}; MECH={generator.MechanicalInputPower.NumericValue:0.###}; REQUEST={generator.RequestedElectricalPower.NumericValue:0.###}"),
            rotor is null ? "ROTOR —" : FormattableString.Invariant($"RPM={rotor.Speed.NumericValue:0.###}; SHAFT={rotor.ShaftPower.NumericValue:0.###}"),
            FormattableString.Invariant($"STEAM={snapshot.TurbineSecondary.EffectiveTurbineSteamFlow.NumericValue:0.###} kg/s; MEASURED-SHAFT={snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue:0.###} MW"),
            train is null
                ? "ADMISSION —"
                : FormattableString.Invariant($"ADMISSION={train.AdmissionFlow.NumericValue:0.###} kg/s; STOP={train.StopValvePosition.NumericValue:0.###}%; CONTROL={train.ControlValvePosition.NumericValue:0.###}%; ADMISSION-VALVE={train.AdmissionValvePosition.NumericValue:0.###}%; INLET={train.TurbineInletPressure.NumericValue:0.###} MPa/{train.TurbineInletTemperature.NumericValue:0.###} °C"),
            condenser is null
                ? "CONDENSER —"
                : FormattableString.Invariant($"CONDENSER={condenser.Pressure.NumericValue:0.###} kPa/{condenser.SteamSpaceTemperature.NumericValue:0.###} °C; CONDENSE={condenser.CondensationFlow.NumericValue:0.###} kg/s; QREJ={condenser.HeatRejectionPower.NumericValue:0.###} MW; HOTWELL={condenser.HotwellMass.NumericValue:0.###} kg"),
            feedwater is null
                ? "FEEDWATER —"
                : FormattableString.Invariant($"COND-PUMP={feedwater.CondensatePump.MassFlow.NumericValue:0.###} kg/s; FW-PUMP={feedwater.FeedwaterPump.MassFlow.NumericValue:0.###} kg/s"),
            drum is null
                ? "DRUM —"
                : FormattableString.Invariant($"DRUM={drum.Pressure.NumericValue:0.###} MPa/{drum.Temperature.NumericValue:0.###} °C/{drum.Level.NumericValue:0.###}%; PHASE={drum.Phase}; RETURN={drum.IncomingReturnFlow.NumericValue:0.###} kg/s; STEAM={drum.SteamFlow.NumericValue:0.###} kg/s; RECIRC={drum.RecirculationFlow.NumericValue:0.###} kg/s; FEED={snapshot.PrimaryCircuit.TotalFeedwaterFlow.NumericValue:0.###} kg/s; EXPORT={snapshot.PrimaryCircuit.TotalSteamExportFlow.NumericValue:0.###} kg/s"));
    }
}
