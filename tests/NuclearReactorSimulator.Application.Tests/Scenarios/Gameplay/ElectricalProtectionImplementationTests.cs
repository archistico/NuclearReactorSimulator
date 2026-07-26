using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-E.3.2 evidence-derived electrical protection contract and integration regressions.</summary>
public sealed class ElectricalProtectionImplementationTests
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    [Fact]
    public void DesktopCurrentV2_DeclaresEvidenceDerivedElectricalProtectionSet()
    {
        var engine = CreateDesktopEngine();
        var definition = engine.CurrentState.ProtectionState.Definition;

        AssertFunction(
            definition.GetTripFunction("generator-reverse-power"),
            "generator-output",
            ProtectionComparison.Low,
            -300_000d,
            -100_000d,
            TimeSpan.FromSeconds(2d));
        AssertFunction(
            definition.GetTripFunction("generator-underfrequency"),
            "generator-frequency",
            ProtectionComparison.Low,
            48.8d,
            49.5d,
            TimeSpan.FromSeconds(1d));
        AssertFunction(
            definition.GetTripFunction("generator-loss-of-synchronism"),
            "generator-absolute-frequency-slip",
            ProtectionComparison.High,
            1.5d,
            0.5d,
            TimeSpan.FromSeconds(0.5d));

        foreach (var functionId in new[]
                 {
                     "generator-reverse-power",
                     "generator-underfrequency",
                     "generator-loss-of-synchronism",
                 })
        {
            var function = definition.GetTripFunction(functionId);
            Assert.Equal(ProtectionAction.GeneratorTrip, function.Actions);
            Assert.NotNull(function.Supervision);
            Assert.Equal("generator-breaker-closed", function.Supervision!.MeasurementChannelId);
            Assert.Equal(ProtectionComparison.High, function.Supervision.Comparison);
            Assert.Equal(0.5d, function.Supervision.Threshold, 10);
        }
    }

    [Fact]
    public void GeneratorHmi_PublishesReversePowerAndFrequencyTripMarkers()
    {
        var engine = CreateDesktopEngine();
        var snapshot = new ControlRoomRuntimeCoordinator(engine).Current;
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var powerScale = Assert.IsType<NuclearReactorSimulator.Application.ControlRoom.Hmi.ControlRoomInstrumentScaleSnapshot>(
            generator.ElectricalOutput.InstrumentScale);
        var frequencyScale = Assert.IsType<NuclearReactorSimulator.Application.ControlRoom.Hmi.ControlRoomInstrumentScaleSnapshot>(
            generator.Frequency.InstrumentScale);

        Assert.Contains(powerScale.ProtectionLimits, static limit =>
            limit.Label == "GENERATOR-REVERSE-POWER"
            && Math.Abs(limit.Threshold - (-0.3d)) <= 1e-12d);
        Assert.Contains(frequencyScale.ProtectionLimits, static limit =>
            limit.Label == "GENERATOR-UNDERFREQUENCY"
            && Math.Abs(limit.Threshold - 48.8d) <= 1e-12d);
        Assert.Contains(frequencyScale.ProtectionLimits, static limit =>
            limit.Label == "GENERATOR-OVERFREQUENCY"
            && Math.Abs(limit.Threshold - 53d) <= 1e-12d);
    }

    [Fact]
    public void ReversePowerRelay_RequiresTwoSecondsOfSupervisedPickup()
    {
        var engine = CreateDesktopEngine();
        var definition = engine.CurrentState.ProtectionState.Definition;
        var solver = new ProtectionSystemSolver(definition);
        var state = ProtectionSystemState.CreateInitial(definition);
        var signals = WithElectricalSignals(
            engine.CurrentState.MeasuredSignals,
            breakerClosed: true,
            gridExchangeWatts: -500_000d,
            frequencyHertz: 50d,
            absoluteSlipHertz: 0d);

        for (var step = 0; step < 199; step++)
        {
            state = solver.Step(signals, state, new ProtectionSystemInputs(definition), RuntimeStep).CandidateState;
        }

        Assert.False(state.IsFunctionLatched("generator-reverse-power"));
        var tripped = solver.Step(signals, state, new ProtectionSystemInputs(definition), RuntimeStep);
        Assert.True(tripped.CandidateState.IsFunctionLatched("generator-reverse-power"));
        Assert.True(tripped.Snapshot.GeneratorTripActive);
        Assert.Equal(TimeSpan.FromSeconds(2d),
            tripped.Snapshot.Functions.Single(static item => item.FunctionId == "generator-reverse-power").PickupElapsed);
    }

    [Fact]
    public void UnderfrequencyRelay_IsBlockedWhileGeneratorBreakerIsOpen()
    {
        var engine = CreateDesktopEngine();
        var definition = engine.CurrentState.ProtectionState.Definition;
        var solver = new ProtectionSystemSolver(definition);
        var state = ProtectionSystemState.CreateInitial(definition);
        var disconnectedSignals = WithElectricalSignals(
            engine.CurrentState.MeasuredSignals,
            breakerClosed: false,
            gridExchangeWatts: 0d,
            frequencyHertz: 40d,
            absoluteSlipHertz: 10d);

        for (var step = 0; step < 500; step++)
        {
            state = solver.Step(disconnectedSignals, state, new ProtectionSystemInputs(definition), RuntimeStep).CandidateState;
        }

        Assert.False(state.IsFunctionLatched("generator-underfrequency"));
        Assert.False(state.IsFunctionLatched("generator-loss-of-synchronism"));
    }

    [Fact]
    public void UnderfrequencyAndLossOfSynchronism_UseIndependentEvidenceDerivedPickupWindows()
    {
        var engine = CreateDesktopEngine();
        var definition = engine.CurrentState.ProtectionState.Definition;
        var solver = new ProtectionSystemSolver(definition);

        var underfrequencyState = ProtectionSystemState.CreateInitial(definition);
        var underfrequencySignals = WithElectricalSignals(
            engine.CurrentState.MeasuredSignals,
            breakerClosed: true,
            gridExchangeWatts: 5_000_000d,
            frequencyHertz: 48.5d,
            absoluteSlipHertz: 0d);
        for (var step = 0; step < 100; step++)
        {
            underfrequencyState = solver.Step(
                underfrequencySignals,
                underfrequencyState,
                new ProtectionSystemInputs(definition),
                RuntimeStep).CandidateState;
        }
        Assert.True(underfrequencyState.IsFunctionLatched("generator-underfrequency"));
        Assert.False(underfrequencyState.IsFunctionLatched("generator-loss-of-synchronism"));

        var synchronismState = ProtectionSystemState.CreateInitial(definition);
        var synchronismSignals = WithElectricalSignals(
            engine.CurrentState.MeasuredSignals,
            breakerClosed: true,
            gridExchangeWatts: 5_000_000d,
            frequencyHertz: 50d,
            absoluteSlipHertz: 2d);
        for (var step = 0; step < 50; step++)
        {
            synchronismState = solver.Step(
                synchronismSignals,
                synchronismState,
                new ProtectionSystemInputs(definition),
                RuntimeStep).CandidateState;
        }
        Assert.True(synchronismState.IsFunctionLatched("generator-loss-of-synchronism"));
        Assert.False(synchronismState.IsFunctionLatched("generator-underfrequency"));
    }

    [Fact]
    public void ReversePowerPickupTimer_ReplaysAndRestoresFromInFlightCheckpoint()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        var generator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCoordinator(session.Coordinator, 500);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        AdvanceCoordinator(session.Coordinator, 100);

        var inFlight = recorder.CreateCheckpoint("e32-reverse-power-pickup-in-flight");
        Assert.False(session.Coordinator.Current.GeneratorTripActive);

        AdvanceCoordinator(session.Coordinator, 150);
        Assert.True(session.Coordinator.Current.GeneratorTripActive);
        var expectedFinalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);

        var recording = recorder.Complete();
        var archive = ScenarioSessionArchive.FromRecording("e32-reverse-power-pickup", session.Scenario, recording);
        var runner = new ScenarioFullReplayRunner(factory);
        var replay = runner.ReplayAndVerify(archive);
        var restored = runner.SeekAndVerify(archive, inFlight.CheckpointId);

        Assert.Equal(expectedFinalFingerprint, ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));
        Assert.False(restored.Session.Coordinator.Current.GeneratorTripActive);
        restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceCoordinator(restored.Session.Coordinator, 150);
        Assert.True(restored.Session.Coordinator.Current.GeneratorTripActive);
        Assert.Equal(expectedFinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionImplementationAudit")]
    public void NormalFiveZeroFiveLoadTrajectory_DoesNotTripGeneratorProtection()
    {
        var engine = CreateDesktopEngine();
        var samples = new List<ProtectionAuditSample>();
        AdvanceAndCaptureProtectionClear(engine, samples, "steady-five-mwe", 1_000);
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
        AdvanceAndCaptureProtectionClear(engine, samples, "load-lower-to-zero", 2_000);
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
        AdvanceAndCaptureProtectionClear(engine, samples, "load-raise-to-five", 2_000);

        WriteProtectionAuditReport(
            "01-normal-five-zero-five",
            "Normal breaker-closed 5->0->5 MWe operation must remain below every delayed electrical-protection pickup window.",
            samples);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionImplementationAudit")]
    public void TurbineTripReversePower_PicksUpGeneratorTripAndOpensBreaker()
    {
        var engine = CreateDesktopEngine();
        for (var step = 0; step < 500; step++)
        {
            engine.Step(ControlRoomRunState.Running);
        }

        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
        var samples = new List<ProtectionAuditSample>();

        for (var step = 0; step < 1_000 && !engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection.GeneratorTripActive; step++)
        {
            engine.Step(ControlRoomRunState.Running);
            samples.Add(CaptureProtectionAuditSample(engine, "post-turbine-trip"));
        }

        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var generator = Assert.Single(protectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators);
        Assert.True(protectedControl.Protection.GeneratorTripActive);
        Assert.True(protectedControl.Protection.Functions.Single(
            static item => item.FunctionId == "generator-reverse-power").IsLatched);
        Assert.False(generator.BreakerFinallyClosed);

        WriteProtectionAuditReport(
            "02-turbine-trip-reverse-power-trip",
            "Prime-mover loss with breaker initially closed must accumulate reverse-power pickup, latch generator trip and open the breaker.",
            samples);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionImplementationAudit")]
    public void BreakerOpenTurbineCoastdown_DoesNotBecomeGeneratorTripEligible()
    {
        var engine = CreateDesktopEngine();
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorBreakerOpen);
        for (var step = 0; step < 10; step++)
        {
            engine.Step(ControlRoomRunState.Running);
        }
        Assert.False(Assert.Single(
            engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators).BreakerFinallyClosed);

        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        var samples = new List<ProtectionAuditSample>();
        for (var step = 0; step < 3_000; step++)
        {
            engine.Step(ControlRoomRunState.Running);
            Assert.False(engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection.GeneratorTripActive);
            samples.Add(CaptureProtectionAuditSample(engine, "breaker-open-coastdown"));
        }

        WriteProtectionAuditReport(
            "03-breaker-open-coastdown-supervision",
            "Breaker-open turbine coastdown may cross frequency/slip thresholds but must remain ineligible for generator trip.",
            samples);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateDesktopEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

    private static void AssertFunction(
        ProtectionFunctionDefinition function,
        string measurementChannelId,
        ProtectionComparison comparison,
        double tripThreshold,
        double resetThreshold,
        TimeSpan pickupDelay)
    {
        Assert.Equal(measurementChannelId, function.MeasurementChannelId);
        Assert.Equal(comparison, function.Comparison);
        Assert.Equal(tripThreshold, function.TripThreshold, 10);
        Assert.Equal(resetThreshold, function.ResetThreshold, 10);
        Assert.Equal(pickupDelay, function.PickupDelay);
    }

    private static MeasuredSignalFrame WithElectricalSignals(
        MeasuredSignalFrame source,
        bool breakerClosed,
        double gridExchangeWatts,
        double frequencyHertz,
        double absoluteSlipHertz)
        => new(
            source.Definition,
            source.Signals.Select(signal => signal.ChannelId switch
            {
                "generator-breaker-closed" => signal with
                {
                    EngineeringValue = breakerClosed ? 1d : 0d,
                    ScaledValue = breakerClosed ? 1d : 0d,
                },
                "generator-output" => signal with
                {
                    EngineeringValue = gridExchangeWatts,
                    ScaledValue = gridExchangeWatts,
                },
                "generator-frequency" => signal with
                {
                    EngineeringValue = frequencyHertz,
                    ScaledValue = frequencyHertz,
                },
                "generator-absolute-frequency-slip" => signal with
                {
                    EngineeringValue = absoluteSlipHertz,
                    ScaledValue = absoluteSlipHertz,
                },
                _ => signal,
            }));

    private static void QueueGeneratorCommand(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomCommandKind commandKind)
    {
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var (targetId, targetKind) = commandKind switch
        {
            ControlRoomCommandKind.GeneratorBreakerClose or ControlRoomCommandKind.GeneratorBreakerOpen
                => (generator.BreakerId, ControlRoomCommandTargetKind.Breaker),
            ControlRoomCommandKind.GeneratorLoadRaise or ControlRoomCommandKind.GeneratorLoadLower
                => (generator.Id, ControlRoomCommandTargetKind.Generator),
            _ => throw new ArgumentOutOfRangeException(
                nameof(commandKind),
                commandKind,
                "Only generator load and breaker commands are supported by this test helper."),
        };

        engine.QueueOperatorCommand(new ControlRoomCommand(commandKind, targetId, targetKind));
    }

    private static void AdvanceCoordinator(ControlRoomRuntimeCoordinator coordinator, int stepCount)
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

    private static void AdvanceAndCaptureProtectionClear(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<ProtectionAuditSample> samples,
        string segment,
        int stepCount)
    {
        for (var step = 0; step < stepCount; step++)
        {
            engine.Step(ControlRoomRunState.Running);
            Assert.False(engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection.GeneratorTripActive);
            samples.Add(CaptureProtectionAuditSample(engine, segment));
        }
    }

    private static ProtectionAuditSample CaptureProtectionAuditSample(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string segment)
    {
        var snapshot = engine.LatestCanonicalSnapshot;
        var protectedControl = snapshot.Control.ProtectedControl;
        var generatorGrid = protectedControl.FullPlant.IntegratedCycle.GeneratorGrid;
        var generator = generatorGrid.Generators.Single();
        var functions = protectedControl.Protection.Functions;
        var reverse = functions.Single(static item => item.FunctionId == "generator-reverse-power");
        var underfrequency = functions.Single(static item => item.FunctionId == "generator-underfrequency");
        var synchronism = functions.Single(static item => item.FunctionId == "generator-loss-of-synchronism");
        var slip = Math.Abs(generator.FinalElectricalFrequency.Hertz - generatorGrid.Grid.Frequency.Hertz);

        return new ProtectionAuditSample(
            engine.LogicalStep,
            engine.LogicalStep * RuntimeStep.TotalSeconds,
            segment,
            generator.BreakerFinallyClosed,
            generator.ElectricalOutputPower.Megawatts,
            generator.FinalElectricalFrequency.Hertz,
            slip,
            reverse.PickupElapsed.TotalSeconds,
            underfrequency.PickupElapsed.TotalSeconds,
            synchronism.PickupElapsed.TotalSeconds,
            reverse.IsLatched,
            underfrequency.IsLatched,
            synchronism.IsLatched,
            protectedControl.Protection.GeneratorTripActive);
    }

    private static void WriteProtectionAuditReport(
        string stem,
        string description,
        IReadOnlyList<ProtectionAuditSample> samples)
    {
        Assert.NotEmpty(samples);
        var directory = EnsureProtectionAuditDirectory();
        var csv = new StringBuilder();
        csv.AppendLine("logical_step,seconds,segment,breaker_closed,grid_exchange_mw,frequency_hz,absolute_slip_hz,reverse_pickup_s,underfrequency_pickup_s,synchronism_pickup_s,reverse_latched,underfrequency_latched,synchronism_latched,generator_trip");
        foreach (var sample in samples)
        {
            csv.AppendLine(string.Join(",",
                sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
                sample.Seconds.ToString("0.000", CultureInfo.InvariantCulture),
                EscapeCsv(sample.Segment),
                sample.BreakerClosed,
                sample.GridExchangeMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.FrequencyHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.AbsoluteSlipHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.ReversePickupSeconds.ToString("0.000", CultureInfo.InvariantCulture),
                sample.UnderfrequencyPickupSeconds.ToString("0.000", CultureInfo.InvariantCulture),
                sample.SynchronismPickupSeconds.ToString("0.000", CultureInfo.InvariantCulture),
                sample.ReverseLatched,
                sample.UnderfrequencyLatched,
                sample.SynchronismLatched,
                sample.GeneratorTripActive));
        }

        File.WriteAllText(
            Path.Combine(directory, stem + ".csv"),
            csv.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var firstTrip = samples.FirstOrDefault(static item => item.GeneratorTripActive);
        var summary = new StringBuilder();
        summary.AppendLine("=== " + stem + " ===");
        summary.AppendLine(description);
        summary.AppendLine(FormattableString.Invariant(
            $"samples={samples.Count}; seconds={samples[0].Seconds:0.000}..{samples[^1].Seconds:0.000}; breaker-closed-samples={samples.Count(static item => item.BreakerClosed)}; "));
        summary.AppendLine(FormattableString.Invariant(
            $"grid-exchange={samples.Min(static item => item.GridExchangeMegawatts):0.000000}..{samples.Max(static item => item.GridExchangeMegawatts):0.000000} MWe; frequency={samples.Min(static item => item.FrequencyHertz):0.000000}..{samples.Max(static item => item.FrequencyHertz):0.000000} Hz; max-absolute-slip={samples.Max(static item => item.AbsoluteSlipHertz):0.000000} Hz; "));
        summary.AppendLine(FormattableString.Invariant(
            $"max-pickup: reverse={samples.Max(static item => item.ReversePickupSeconds):0.000}s; underfrequency={samples.Max(static item => item.UnderfrequencyPickupSeconds):0.000}s; synchronism={samples.Max(static item => item.SynchronismPickupSeconds):0.000}s; "));
        summary.AppendLine(firstTrip is null
            ? "generator-trip=none"
            : FormattableString.Invariant(
                $"generator-trip=step {firstTrip.LogicalStep} at {firstTrip.Seconds:0.000}s; reverse-latched={firstTrip.ReverseLatched}; underfrequency-latched={firstTrip.UnderfrequencyLatched}; synchronism-latched={firstTrip.SynchronismLatched}; breaker-closed={firstTrip.BreakerClosed}"));
        summary.AppendLine();

        File.WriteAllText(
            Path.Combine(directory, stem + ".summary.txt"),
            summary.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EnsureProtectionAuditDirectory()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "e3-protection-implementation");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the NuclearReactorSimulator repository root from the test output directory.");
    }

    private static string EscapeCsv(string value)
        => '"' + value.Replace("\"", "\"\"") + '"';

    private sealed record ProtectionAuditSample(
        long LogicalStep,
        double Seconds,
        string Segment,
        bool BreakerClosed,
        double GridExchangeMegawatts,
        double FrequencyHertz,
        double AbsoluteSlipHertz,
        double ReversePickupSeconds,
        double UnderfrequencyPickupSeconds,
        double SynchronismPickupSeconds,
        bool ReverseLatched,
        bool UnderfrequencyLatched,
        bool SynchronismLatched,
        bool GeneratorTripActive);
}
