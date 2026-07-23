using System.Diagnostics;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-A audit-only operating-envelope journeys. These tests intentionally inspect canonical read-only evidence but
/// never alter solver definitions, physical coefficients, seed inventories, protection thresholds or integration behavior.
/// </summary>
public sealed class OperationalEnvelopeExtendedAuditTests
{
    private const int StepsPerSecond = 100;
    private const int StepsPerTenSeconds = 10 * StepsPerSecond;
    private const int SecondsPerCheckpoint = 10;
    private const int ThirtySecondCheckpoints = 3;
    private const int ThreeHundredSecondCheckpoints = 30;

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void DesktopFiveMWePoint_SustainsThreeHundredSecondsWithConservationAndPerformanceEvidence()
    {
        var engine = CreateEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var audit = new OperatingEnvelopeAudit("300 s steady / 5 MWe");
        audit.Observe(engine, coordinator.Current);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var stopwatch = Stopwatch.StartNew();
        for (var checkpoint = 1; checkpoint <= ThreeHundredSecondCheckpoints; checkpoint++)
        {
            for (var second = 1; second <= SecondsPerCheckpoint; second++)
            {
                AdvanceCheckpoint(coordinator, StepsPerSecond);
                audit.Observe(engine, coordinator.Current);
                audit.RequireHealthyParallelPoint(coordinator.Current, checkpoint, second);
            }
        }
        stopwatch.Stop();

        audit.AssertConservationClosed();
        audit.AssertSecondaryPumpsNeverReverse();
        Assert.InRange(stopwatch.Elapsed.TotalSeconds, 0d, 300d);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void GeneratorLoadRaiseThenLower_ReplaysToIdenticalCanonicalAndPresentationEvidence()
    {
        var left = RunLoadStepJourney();
        var right = RunLoadStepJourney();

        Assert.Equal(left.FinalFingerprint, right.FinalFingerprint);
        Assert.Equal(left.Audit, right.Audit);
        Assert.True(left.PeakRequestedElectricalPowerMegawatts > left.InitialRequestedElectricalPowerMegawatts);
        Assert.InRange(
            left.FinalRequestedElectricalPowerMegawatts,
            left.InitialRequestedElectricalPowerMegawatts - 1e-9d,
            left.InitialRequestedElectricalPowerMegawatts + 1e-9d);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void BreakerOpenLoadRejection_ProducesBoundedDiagnosticEvidenceWithoutHiddenRepair()
        => RunLoadRejection(
            "breaker-open",
            static snapshot => new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorBreakerOpen,
                Assert.Single(snapshot.Electrical.Generators).BreakerId,
                ControlRoomCommandTargetKind.Breaker),
            static snapshot => Assert.False(Assert.Single(snapshot.Electrical.Generators).BreakerClosed));

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void GeneratorTripLoadRejection_ProducesBoundedDiagnosticEvidenceWithoutHiddenRepair()
        => RunLoadRejection(
            "generator-trip",
            static _ => new ControlRoomCommand(ControlRoomCommandKind.GeneratorTrip),
            static snapshot => Assert.True(snapshot.GeneratorTripActive));

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void TurbineTripLoadRejection_ProducesBoundedDiagnosticEvidenceWithoutHiddenRepair()
        => RunLoadRejection(
            "turbine-trip",
            static _ => new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip),
            static snapshot => Assert.True(snapshot.TurbineTripActive));

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void CondenserCoolingDegradation_RemainsConservativeAndRaisesNoReverseSecondaryPumpFlow()
    {
        var engine = CreateEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var audit = new OperatingEnvelopeAudit("condenser cooling degradation");
        var coolingTarget = (ISecondaryTransientFaultTarget)engine;
        var initialCondenserPressure = CurrentCondenserPressurePascals(engine);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
        coolingTarget.ActivateCondenserCoolingDegradation("m10941a-cooling-degradation", "cooling", 0.25d);

        for (var checkpoint = 1; checkpoint <= ThirtySecondCheckpoints; checkpoint++)
        {
            AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
            audit.Observe(engine, coordinator.Current);
        }

        audit.AssertConservationClosed();
        audit.AssertSecondaryPumpsNeverReverse();
        Assert.True(audit.MaximumCondenserPressurePascals >= initialCondenserPressure, audit.ToString());
    }

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void CurrentV2SecondaryPumpCheckValves_BlockReverseFlowAtEverySampledLoadRejectionStep()
    {
        var engine = CreateEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var audit = new OperatingEnvelopeAudit("per-step secondary-pump non-return");
        var generator = Assert.Single(coordinator.Current.Electrical.Generators);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerOpen,
            generator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));

        for (var step = 0; step < StepsPerTenSeconds; step++)
        {
            AdvanceCheckpoint(coordinator, 1);
            audit.Observe(engine, coordinator.Current);
        }

        audit.AssertConservationClosed();
        audit.AssertSecondaryPumpsNeverReverse();
    }

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    public void ExtendedLoadStepRecording_CheckpointsAndFullReplayRemainEquivalent()
    {
        var registry = CreateRegistry();
        var factory = new ScenarioSessionFactory(registry);
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        var generator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCheckpoint(session.Coordinator, 4 * StepsPerTenSeconds);
        var first = recorder.CreateCheckpoint("m10941a-40s");

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        AdvanceCheckpoint(session.Coordinator, 4 * StepsPerTenSeconds);
        var second = recorder.CreateCheckpoint("m10941a-80s");

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        AdvanceCheckpoint(session.Coordinator, 4 * StepsPerTenSeconds);
        var third = recorder.CreateCheckpoint("m10941a-120s");

        var recording = recorder.Complete();
        var archive = ScenarioSessionArchive.FromRecording("m10941a-extended-load-step", session.Scenario, recording);
        var runner = new ScenarioFullReplayRunner(factory);
        var replay = runner.ReplayAndVerify(archive);
        var restored = runner.SeekAndVerify(archive, third.CheckpointId);

        Assert.Equal(12_001, recording.Frames.Count);
        Assert.Equal(new[] { first, second, third }, recording.Checkpoints);
        Assert.Equal(recording.FinalLogicalStep, replay.ReplayedRecording.FinalLogicalStep);
        Assert.Equal(
            recording.Frames[^1].SnapshotFingerprint,
            replay.ReplayedRecording.Frames[^1].SnapshotFingerprint);
        Assert.Equal(third.LogicalStep, restored.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(third.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
    }

    private static LoadStepJourneyResult RunLoadStepJourney()
    {
        var engine = CreateEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var audit = new OperatingEnvelopeAudit("load raise/lower");
        var initial = Assert.Single(coordinator.Current.Electrical.Generators);
        var initialRequest = initial.RequestedElectricalPower.NumericValue ?? double.NaN;
        var peakRequest = initialRequest;

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            initial.GeneratorId,
            ControlRoomCommandTargetKind.Generator));

        for (var checkpoint = 1; checkpoint <= ThirtySecondCheckpoints; checkpoint++)
        {
            AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
            audit.Observe(engine, coordinator.Current);
            peakRequest = Math.Max(
                peakRequest,
                Assert.Single(coordinator.Current.Electrical.Generators).RequestedElectricalPower.NumericValue ?? double.NaN);
        }

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            initial.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        for (var checkpoint = 1; checkpoint <= ThirtySecondCheckpoints; checkpoint++)
        {
            AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
            audit.Observe(engine, coordinator.Current);
        }

        audit.AssertConservationClosed();
        audit.AssertSecondaryPumpsNeverReverse();
        var finalRequest = Assert.Single(coordinator.Current.Electrical.Generators).RequestedElectricalPower.NumericValue ?? double.NaN;
        return new LoadStepJourneyResult(
            ControlRoomSnapshotFingerprint.Compute(coordinator.Current),
            audit.CreateSummary(),
            initialRequest,
            peakRequest,
            finalRequest);
    }

    private static void RunLoadRejection(
        string name,
        Func<ControlRoomSnapshot, ControlRoomCommand> createCommand,
        Action<ControlRoomSnapshot> assertImmediateState)
    {
        var engine = CreateEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var audit = new OperatingEnvelopeAudit(name);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
        coordinator.Dispatch(createCommand(coordinator.Current));
        AdvanceCheckpoint(coordinator, 1);
        assertImmediateState(coordinator.Current);
        audit.Observe(engine, coordinator.Current);

        try
        {
            for (var checkpoint = 1; checkpoint <= ThirtySecondCheckpoints; checkpoint++)
            {
                AdvanceCheckpoint(coordinator, StepsPerTenSeconds);
                audit.Observe(engine, coordinator.Current);
            }
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"{name} failed after canonical load rejection. {audit}",
                exception);
        }

        audit.AssertConservationClosed();
        audit.AssertSecondaryPumpsNeverReverse();
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

    private static VersionedInitialConditionRegistry CreateRegistry()
        => new(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        });

    private static void AdvanceCheckpoint(ControlRoomRuntimeCoordinator coordinator, int stepCount)
    {
        var remaining = stepCount;
        while (remaining > 0)
        {
            var requested = Math.Min(remaining, coordinator.ExecutionBudget.MaximumSimulationStepsPerBatch);
            var result = coordinator.AdvanceRunning(requested, publicationStride: requested);
            Assert.Equal(requested, result.ExecutedStepCount);
            remaining -= result.ExecutedStepCount;
        }
    }

    private static double CurrentCondenserPressurePascals(IntegratedAutomaticOperationRuntimeEngine engine)
        => Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.Condenser.Condensers)
            .FinalSteamSpacePressure.Pascals;

    private sealed class OperatingEnvelopeAudit
    {
        private const double MaximumMassClosureResidualKilograms = 1e-6d;
        private const double MaximumEnergyClosureResidualJoules = 1e-2d;
        private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
        private const double MaximumBalancePowerResidualWatts = 1e-3d;

        private readonly string _journey;
        private int _samples;
        private double _maximumMassClosureResidual;
        private double _maximumEnergyClosureResidual;
        private double _maximumBalanceMassRateResidual;
        private double _maximumBalancePowerResidual;
        private double _minimumDrumPressurePascals = double.PositiveInfinity;
        private double _maximumDrumPressurePascals = double.NegativeInfinity;
        private double _minimumDrumLevelFraction = double.PositiveInfinity;
        private double _maximumDrumLevelFraction = double.NegativeInfinity;
        private double _minimumCondenserPressurePascals = double.PositiveInfinity;
        private double _maximumCondenserPressurePascals = double.NegativeInfinity;
        private double _minimumRotorSpeedRpm = double.PositiveInfinity;
        private double _maximumRotorSpeedRpm = double.NegativeInfinity;
        private double _minimumGeneratorFrequencyHertz = double.PositiveInfinity;
        private double _maximumGeneratorFrequencyHertz = double.NegativeInfinity;
        private double _minimumCondensatePumpFlowKilogramsPerSecond = double.PositiveInfinity;
        private double _minimumFeedwaterPumpFlowKilogramsPerSecond = double.PositiveInfinity;
        private double _minimumStageFlowKilogramsPerSecond = double.PositiveInfinity;
        private double _maximumStageFlowKilogramsPerSecond = double.NegativeInfinity;
        private double _minimumActualCondensationFlowKilogramsPerSecond = double.PositiveInfinity;
        private double _maximumActualCondensationFlowKilogramsPerSecond = double.NegativeInfinity;
        private double _minimumThermalCondensationLimitKilogramsPerSecond = double.PositiveInfinity;
        private double _maximumThermalCondensationLimitKilogramsPerSecond = double.NegativeInfinity;
        private double _minimumInventoryCondensationLimitKilogramsPerSecond = double.PositiveInfinity;
        private double _maximumInventoryCondensationLimitKilogramsPerSecond = double.NegativeInfinity;
        private double _maximumCondenserCapacityMegawatts = double.NegativeInfinity;
        private double _maximumCondenserSurfaceLimitMegawatts = double.NegativeInfinity;
        private double _minimumExhaustMassKilograms = double.PositiveInfinity;
        private double _maximumExhaustMassKilograms = double.NegativeInfinity;
        private string _lastProtectionState = "—";
        private string _latchedProtectionFunctions = "—";
        private long _lastLogicalStep;

        public OperatingEnvelopeAudit(string journey)
        {
            _journey = journey;
        }

        public double MaximumCondenserPressurePascals => _maximumCondenserPressurePascals;

        public void Observe(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
        {
            var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
            var fullPlant = protectedControl.FullPlant;
            var heatBalance = fullPlant.HeatBalance;
            var thermofluid = fullPlant.IntegratedCycle.ThermofluidAudit;
            var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
            var rotor = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.Rotors);
            var stage = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
            var generator = Assert.Single(fullPlant.IntegratedCycle.Generators);
            var train = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
            var exhaust = fullPlant.CandidatePlant.GetFluidNode(condenser.SteamSpaceNodeId);

            RequireFinite(
                heatBalance.MassClosureResidualKilograms,
                heatBalance.FullEnergyPathClosureResidualJoules,
                thermofluid.BalanceMassRateResidualKilogramsPerSecond,
                thermofluid.BalancePowerResidualWatts,
                drum.Pressure.Pascals,
                drum.LiquidLevelFraction.Fraction,
                condenser.FinalSteamSpacePressure.Pascals,
                rotor.FinalAngularSpeed.RevolutionsPerMinute,
                generator.FinalElectricalFrequency.Hertz,
                train.CondensatePump.MassFlowRate.KilogramsPerSecond,
                train.FeedwaterPump.MassFlowRate.KilogramsPerSecond,
                stage.EffectiveMassFlowRate.KilogramsPerSecond,
                condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
                condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond,
                condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond,
                condenser.EffectiveHeatRejectionCapacity.Megawatts,
                condenser.SurfaceHeatTransferLimitedPower.Megawatts,
                exhaust.Mass.Kilograms);

            _samples++;
            _lastLogicalStep = presentation.LogicalStep;
            _maximumMassClosureResidual = Math.Max(_maximumMassClosureResidual, Math.Abs(heatBalance.MassClosureResidualKilograms));
            _maximumEnergyClosureResidual = Math.Max(_maximumEnergyClosureResidual, Math.Abs(heatBalance.FullEnergyPathClosureResidualJoules));
            _maximumBalanceMassRateResidual = Math.Max(_maximumBalanceMassRateResidual, Math.Abs(thermofluid.BalanceMassRateResidualKilogramsPerSecond));
            _maximumBalancePowerResidual = Math.Max(_maximumBalancePowerResidual, Math.Abs(thermofluid.BalancePowerResidualWatts));
            _minimumDrumPressurePascals = Math.Min(_minimumDrumPressurePascals, drum.Pressure.Pascals);
            _maximumDrumPressurePascals = Math.Max(_maximumDrumPressurePascals, drum.Pressure.Pascals);
            _minimumDrumLevelFraction = Math.Min(_minimumDrumLevelFraction, drum.LiquidLevelFraction.Fraction);
            _maximumDrumLevelFraction = Math.Max(_maximumDrumLevelFraction, drum.LiquidLevelFraction.Fraction);
            _minimumCondenserPressurePascals = Math.Min(_minimumCondenserPressurePascals, condenser.FinalSteamSpacePressure.Pascals);
            _maximumCondenserPressurePascals = Math.Max(_maximumCondenserPressurePascals, condenser.FinalSteamSpacePressure.Pascals);
            _minimumRotorSpeedRpm = Math.Min(_minimumRotorSpeedRpm, rotor.FinalAngularSpeed.RevolutionsPerMinute);
            _maximumRotorSpeedRpm = Math.Max(_maximumRotorSpeedRpm, rotor.FinalAngularSpeed.RevolutionsPerMinute);
            _minimumGeneratorFrequencyHertz = Math.Min(_minimumGeneratorFrequencyHertz, generator.FinalElectricalFrequency.Hertz);
            _maximumGeneratorFrequencyHertz = Math.Max(_maximumGeneratorFrequencyHertz, generator.FinalElectricalFrequency.Hertz);
            _minimumCondensatePumpFlowKilogramsPerSecond = Math.Min(
                _minimumCondensatePumpFlowKilogramsPerSecond,
                train.CondensatePump.MassFlowRate.KilogramsPerSecond);
            _minimumFeedwaterPumpFlowKilogramsPerSecond = Math.Min(
                _minimumFeedwaterPumpFlowKilogramsPerSecond,
                train.FeedwaterPump.MassFlowRate.KilogramsPerSecond);
            _minimumStageFlowKilogramsPerSecond = Math.Min(_minimumStageFlowKilogramsPerSecond, stage.EffectiveMassFlowRate.KilogramsPerSecond);
            _maximumStageFlowKilogramsPerSecond = Math.Max(_maximumStageFlowKilogramsPerSecond, stage.EffectiveMassFlowRate.KilogramsPerSecond);
            _minimumActualCondensationFlowKilogramsPerSecond = Math.Min(
                _minimumActualCondensationFlowKilogramsPerSecond,
                condenser.ActualCondensationMassFlowRate.KilogramsPerSecond);
            _maximumActualCondensationFlowKilogramsPerSecond = Math.Max(
                _maximumActualCondensationFlowKilogramsPerSecond,
                condenser.ActualCondensationMassFlowRate.KilogramsPerSecond);
            _minimumThermalCondensationLimitKilogramsPerSecond = Math.Min(
                _minimumThermalCondensationLimitKilogramsPerSecond,
                condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond);
            _maximumThermalCondensationLimitKilogramsPerSecond = Math.Max(
                _maximumThermalCondensationLimitKilogramsPerSecond,
                condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond);
            _minimumInventoryCondensationLimitKilogramsPerSecond = Math.Min(
                _minimumInventoryCondensationLimitKilogramsPerSecond,
                condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond);
            _maximumInventoryCondensationLimitKilogramsPerSecond = Math.Max(
                _maximumInventoryCondensationLimitKilogramsPerSecond,
                condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond);
            _maximumCondenserCapacityMegawatts = Math.Max(
                _maximumCondenserCapacityMegawatts,
                condenser.EffectiveHeatRejectionCapacity.Megawatts);
            _maximumCondenserSurfaceLimitMegawatts = Math.Max(
                _maximumCondenserSurfaceLimitMegawatts,
                condenser.SurfaceHeatTransferLimitedPower.Megawatts);
            _minimumExhaustMassKilograms = Math.Min(_minimumExhaustMassKilograms, exhaust.Mass.Kilograms);
            _maximumExhaustMassKilograms = Math.Max(_maximumExhaustMassKilograms, exhaust.Mass.Kilograms);
            _lastProtectionState = protectedControl.Protection.LatchedActions.ToString();
            var latchedFunctions = protectedControl.Protection.Functions.Where(static function => function.IsLatched).ToArray();
            _latchedProtectionFunctions = latchedFunctions.Length == 0
                ? "—"
                : string.Join(", ", latchedFunctions.Select(FormatProtectionFunction));
        }

        public void RequireHealthyParallelPoint(ControlRoomSnapshot snapshot, int checkpoint, int secondWithinCheckpoint)
        {
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
            var elapsedSeconds = ((checkpoint - 1) * SecondsPerCheckpoint) + secondWithinCheckpoint;
            var evidence = $"Checkpoint {checkpoint}/{ThreeHundredSecondCheckpoints}, second {secondWithinCheckpoint}/{SecondsPerCheckpoint} ({elapsedSeconds} s). {this}";
            Assert.False(snapshot.AnyTripActive, evidence);
            Assert.True(generator.BreakerClosed, evidence);
            Assert.True((generator.RequestedElectricalPower.NumericValue ?? 0d) > 4.5d, evidence);
            Assert.True((generator.ElectricalOutput.NumericValue ?? 0d) > 4.0d, evidence);
            Assert.True((rotor.ShaftPower.NumericValue ?? 0d) > 4.5d, evidence);
        }

        public void AssertConservationClosed()
        {
            Assert.True(_samples > 0, "The audit did not capture any canonical sample.");
            Assert.True(_maximumMassClosureResidual <= MaximumMassClosureResidualKilograms, ToString());
            Assert.True(_maximumEnergyClosureResidual <= MaximumEnergyClosureResidualJoules, ToString());
            Assert.True(_maximumBalanceMassRateResidual <= MaximumBalanceMassRateResidualKilogramsPerSecond, ToString());
            Assert.True(_maximumBalancePowerResidual <= MaximumBalancePowerResidualWatts, ToString());
        }

        public void AssertSecondaryPumpsNeverReverse()
        {
            Assert.True(_minimumCondensatePumpFlowKilogramsPerSecond >= -1e-12d, ToString());
            Assert.True(_minimumFeedwaterPumpFlowKilogramsPerSecond >= -1e-12d, ToString());
        }

        public OperatingEnvelopeAuditSummary CreateSummary()
            => new(
                _samples,
                _maximumMassClosureResidual,
                _maximumEnergyClosureResidual,
                _maximumBalanceMassRateResidual,
                _maximumBalancePowerResidual,
                _minimumDrumPressurePascals,
                _maximumDrumPressurePascals,
                _minimumDrumLevelFraction,
                _maximumDrumLevelFraction,
                _minimumCondenserPressurePascals,
                _maximumCondenserPressurePascals,
                _minimumRotorSpeedRpm,
                _maximumRotorSpeedRpm,
                _minimumGeneratorFrequencyHertz,
                _maximumGeneratorFrequencyHertz,
                _minimumCondensatePumpFlowKilogramsPerSecond,
                _minimumFeedwaterPumpFlowKilogramsPerSecond,
                _lastProtectionState,
                _lastLogicalStep);

        public override string ToString()
            => string.Concat(
                FormattableString.Invariant($"{_journey} · step={_lastLogicalStep}; samples={_samples}; "),
                FormattableString.Invariant($"mass={_maximumMassClosureResidual:E3} kg; energy={_maximumEnergyClosureResidual:E3} J; "),
                FormattableString.Invariant($"balance-mass={_maximumBalanceMassRateResidual:E3} kg/s; balance-power={_maximumBalancePowerResidual:E3} W; "),
                FormattableString.Invariant($"drum={_minimumDrumPressurePascals / 1e6:0.###}..{_maximumDrumPressurePascals / 1e6:0.###} MPa/"),
                FormattableString.Invariant($"{100d * _minimumDrumLevelFraction:0.###}..{100d * _maximumDrumLevelFraction:0.###}%; "),
                FormattableString.Invariant($"condenser={_minimumCondenserPressurePascals / 1e3:0.###}..{_maximumCondenserPressurePascals / 1e3:0.###} kPa; "),
                FormattableString.Invariant($"rotor={_minimumRotorSpeedRpm:0.###}..{_maximumRotorSpeedRpm:0.###} rpm; "),
                FormattableString.Invariant($"frequency={_minimumGeneratorFrequencyHertz:0.###}..{_maximumGeneratorFrequencyHertz:0.###} Hz; "),
                FormattableString.Invariant($"minimum-pumps={_minimumCondensatePumpFlowKilogramsPerSecond:0.###}/{_minimumFeedwaterPumpFlowKilogramsPerSecond:0.###} kg/s; "),
                FormattableString.Invariant($"stage={_minimumStageFlowKilogramsPerSecond:0.###}..{_maximumStageFlowKilogramsPerSecond:0.###} kg/s; "),
                FormattableString.Invariant($"condense={_minimumActualCondensationFlowKilogramsPerSecond:0.###}..{_maximumActualCondensationFlowKilogramsPerSecond:0.###} kg/s; "),
                FormattableString.Invariant($"thermal-limit={_minimumThermalCondensationLimitKilogramsPerSecond:0.###}..{_maximumThermalCondensationLimitKilogramsPerSecond:0.###} kg/s; "),
                FormattableString.Invariant($"inventory-limit={_minimumInventoryCondensationLimitKilogramsPerSecond:0.###}..{_maximumInventoryCondensationLimitKilogramsPerSecond:0.###} kg/s; "),
                FormattableString.Invariant($"condenser-capacity={_maximumCondenserCapacityMegawatts:0.###} MW; surface-limit-max={_maximumCondenserSurfaceLimitMegawatts:0.###} MW; "),
                FormattableString.Invariant($"exhaust-mass={_minimumExhaustMassKilograms:0.###}..{_maximumExhaustMassKilograms:0.###} kg; "),
                FormattableString.Invariant($"protection={_lastProtectionState}; functions={_latchedProtectionFunctions}"));

        private static string FormatProtectionFunction(NuclearReactorSimulator.Simulation.Physics.Control.Protection.ProtectionFunctionSnapshot function)
        {
            var measurement = function.Measurement.HasValue
                ? function.Measurement.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                : "—";
            return string.Concat(
                function.FunctionId,
                "[measurement=",
                measurement,
                "; trigger=",
                function.TriggerActive.ToString(),
                "; actions=",
                function.Actions.ToString(),
                "]");
        }

        private static void RequireFinite(params double[] values)
            => Assert.All(values, static value => Assert.True(double.IsFinite(value), $"Non-finite canonical audit value: {value}."));
    }

    private sealed record LoadStepJourneyResult(
        string FinalFingerprint,
        OperatingEnvelopeAuditSummary Audit,
        double InitialRequestedElectricalPowerMegawatts,
        double PeakRequestedElectricalPowerMegawatts,
        double FinalRequestedElectricalPowerMegawatts);

    private sealed record OperatingEnvelopeAuditSummary(
        int Samples,
        double MaximumMassClosureResidualKilograms,
        double MaximumEnergyClosureResidualJoules,
        double MaximumBalanceMassRateResidualKilogramsPerSecond,
        double MaximumBalancePowerResidualWatts,
        double MinimumDrumPressurePascals,
        double MaximumDrumPressurePascals,
        double MinimumDrumLevelFraction,
        double MaximumDrumLevelFraction,
        double MinimumCondenserPressurePascals,
        double MaximumCondenserPressurePascals,
        double MinimumRotorSpeedRpm,
        double MaximumRotorSpeedRpm,
        double MinimumGeneratorFrequencyHertz,
        double MaximumGeneratorFrequencyHertz,
        double MinimumCondensatePumpFlowKilogramsPerSecond,
        double MinimumFeedwaterPumpFlowKilogramsPerSecond,
        string LastProtectionState,
        long LastLogicalStep);
}
