using System.Globalization;
using System.Reflection;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Evidence-only follow-up after Diagnostic 5 showed that measured reactor thermal readiness does not imply
/// turbine/mechanical readiness at the instant of a load command. This census holds only the first 5.5/6 MWe
/// stage for a long horizon and decomposes steam-path lag, rotor acceleration and infinite-bus coupling so that
/// a 20 s settling timeout is not mistaken for a hard capacity boundary.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic6Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC6";
    private const int StepsPerSecond = 100;
    private const int ReferenceTotalSteps = 1_000;
    private const int PreparationTimeoutSteps = 6_000;
    private const int HoldSteps = 18_000;
    private const int SynchronousWindowSteps = 500;
    private const int TailWindowSteps = 3_000;
    private const double ThermalReadinessToleranceMegawatts = 0.25d;
    private const double SynchronousFrequencyToleranceHertz = 0.01d;
    private const double SynchronousOutputToleranceMegawatts = 0.10d;
    private const double SynchronousNetAccelerationToleranceMegawatts = 0.05d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic6")]
    public void ExactV9_FirstStageLongSettlingSteamPathAndSynchronousRecoveryCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var allRows = new List<ProbeSample>();
        var allEvents = new List<ProbeEvent>();
        var results = new List<ProbeResult>();

        AppendProgress("probe-start=exact-v9-reference-5mwe-step");
        var reference = RunReference("exact-v9-reference-5mwe-step", allRows);
        results.Add(reference);
        AppendProgress(Progress(reference));

        ProbeDefinition[] probes =
        [
            new("exact-v9-long-settle-5p5mwe", 9, 0.5d, 5.5d),
            new("exact-v9-long-settle-6mwe", 9, 1d, 6d),
            new("exact-v4-long-settle-6mwe", 4, 1d, 6d),
        ];

        foreach (var probe in probes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunLongSettlingProbe(probe, allRows, allEvents);
            results.Add(result);
            AppendProgress(Progress(result));
        }

        WriteProbeSummary(results);
        WriteEvents(allEvents);
        WriteTrajectory(allRows);
        WriteDecisionSummary(results);

        Assert.Null(reference.ExceptionType);
        Assert.Equal(636L, reference.FirstTripStep);
        Assert.Equal("generator-loss-of-synchronism", reference.FirstLatchedFunctionId);
        Assert.All(results, static result => Assert.True(result.DiagnosticComplete));
    }

    private static ProbeResult RunReference(string id, ICollection<ProbeSample> allRows)
    {
        var engine = CreateEngine(exactVersion: 9, loadIncrementMegawatts: 5d);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());

        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(presentation.Electrical.Generators).GeneratorId;
        var localRows = new List<ProbeSample>();
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;

        var initial = Capture(id, "reference", null, null, engine);
        localRows.Add(initial);
        allRows.Add(initial);
        allFinite &= initial.AllFinite;

        for (var nextStep = 1; nextStep <= ReferenceTotalSteps; nextStep++)
        {
            try
            {
                if (nextStep == 500)
                {
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(id, "reference", null, null, engine);
                localRows.Add(sample);
                allRows.Add(sample);
                allFinite &= sample.AllFinite;
                CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);
            }
            catch (Exception exception)
            {
                exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
                exceptionMessage = Flatten(exception.Message);
                break;
            }
        }

        return Complete(
            id,
            exactVersion: 9,
            targetLoadMegawatts: 10d,
            loadIncrementMegawatts: 5d,
            localRows,
            allFinite,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            loadCommandStep: 500L,
            firstSynchronousWindowStep: null,
            completionReason: exceptionType is null ? "reference-complete" : "exception",
            exceptionType,
            exceptionMessage);
    }

    private static ProbeResult RunLongSettlingProbe(
        ProbeDefinition probe,
        ICollection<ProbeSample> allRows,
        ICollection<ProbeEvent> allEvents)
    {
        var engine = CreateEngine(probe.ExactVersion, probe.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(presentation.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(presentation.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial generator requested load is unavailable.");
        var initialThermalMegawatts = presentation.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");
        var targetThermalMegawatts = initialThermalMegawatts / initialRequestedMegawatts * probe.TargetLoadMegawatts;

        var localRows = new List<ProbeSample>();
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        long? loadCommandStep = null;
        long? firstSynchronousWindowStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;
        var completionReason = "preparation-timeout";
        var synchronousCount = 0;

        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
        AddEvent(allEvents, probe.Id, engine.LogicalStep, "prepare-start", probe.TargetLoadMegawatts, targetThermalMegawatts, null);

        var initial = Capture(probe.Id, "prepare", probe.TargetLoadMegawatts, targetThermalMegawatts, engine);
        localRows.Add(initial);
        allRows.Add(initial);
        allFinite &= initial.AllFinite;

        try
        {
            for (var preparationStep = 0; preparationStep < PreparationTimeoutSteps; preparationStep++)
            {
                var previous = localRows[^1];
                if (IsThermallyReady(previous, targetThermalMegawatts))
                {
                    loadCommandStep = engine.LogicalStep + 1;
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    AddEvent(allEvents, probe.Id, loadCommandStep.Value, "load-command", probe.TargetLoadMegawatts, targetThermalMegawatts, previous);
                    completionReason = "hold-complete";
                    break;
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, "prepare", probe.TargetLoadMegawatts, targetThermalMegawatts, engine);
                localRows.Add(sample);
                allRows.Add(sample);
                allFinite &= sample.AllFinite;
                CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);

                if (sample.AnyTripActive)
                {
                    completionReason = "trip-during-preparation";
                    AddEvent(allEvents, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                    break;
                }
            }

            if (loadCommandStep.HasValue && firstTripStep is null)
            {
                for (var holdStep = 0; holdStep < HoldSteps; holdStep++)
                {
                    engine.Step(ControlRoomRunState.Running);
                    var sample = Capture(probe.Id, "hold", probe.TargetLoadMegawatts, targetThermalMegawatts, engine);
                    localRows.Add(sample);
                    allRows.Add(sample);
                    allFinite &= sample.AllFinite;
                    CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);

                    if (sample.AnyTripActive)
                    {
                        completionReason = "trip-during-hold";
                        AddEvent(allEvents, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    synchronousCount = IsStrictlySynchronousAtTarget(sample, probe.TargetLoadMegawatts)
                        ? synchronousCount + 1
                        : 0;
                    if (!firstSynchronousWindowStep.HasValue && synchronousCount >= SynchronousWindowSteps)
                    {
                        firstSynchronousWindowStep = sample.LogicalStep;
                        AddEvent(allEvents, probe.Id, sample.LogicalStep, "first-synchronous-window", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                    }
                }

                if (firstTripStep is null)
                {
                    AddEvent(allEvents, probe.Id, engine.LogicalStep, "hold-complete", probe.TargetLoadMegawatts, targetThermalMegawatts, localRows[^1]);
                }
            }
        }
        catch (Exception exception)
        {
            exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            exceptionMessage = Flatten(exception.Message);
            completionReason = "exception";
        }

        return Complete(
            probe.Id,
            probe.ExactVersion,
            probe.TargetLoadMegawatts,
            probe.LoadIncrementMegawatts,
            localRows,
            allFinite,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            loadCommandStep,
            firstSynchronousWindowStep,
            completionReason,
            exceptionType,
            exceptionMessage);
    }

    private static bool IsThermallyReady(ProbeSample sample, double targetThermalMegawatts)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - ThermalReadinessToleranceMegawatts
            && sample.GeneratorFrequencyHertz >= 49.9d
            && sample.GeneratorFrequencyHertz <= 50.1d;

    private static bool IsStrictlySynchronousAtTarget(ProbeSample sample, double targetLoadMegawatts)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && Math.Abs(sample.GeneratorFrequencySlipHertz) <= SynchronousFrequencyToleranceHertz
            && Math.Abs(sample.ElectricalOutputMegawatts - targetLoadMegawatts) <= SynchronousOutputToleranceMegawatts
            && Math.Abs(sample.NetRotorAccelerationPowerMegawatts) <= SynchronousNetAccelerationToleranceMegawatts;

    private static void CaptureTripAndLatch(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ProbeSample sample,
        ref long? firstTripStep,
        ref string? firstLatchedFunctionId,
        ref long? firstLatchedFunctionStep)
    {
        if (sample.AnyTripActive)
        {
            firstTripStep ??= sample.LogicalStep;
        }

        if (firstLatchedFunctionId is not null)
        {
            return;
        }

        var protection = engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;
        var firstLatch = protection.Functions.FirstOrDefault(static function => function.IsLatched);
        if (firstLatch is not null)
        {
            firstLatchedFunctionId = firstLatch.FunctionId;
            firstLatchedFunctionStep = sample.LogicalStep;
        }
    }

    private static ProbeResult Complete(
        string id,
        int exactVersion,
        double targetLoadMegawatts,
        double loadIncrementMegawatts,
        IReadOnlyList<ProbeSample> localRows,
        bool allFinite,
        long? firstTripStep,
        string? firstLatchedFunctionId,
        long? firstLatchedFunctionStep,
        long? loadCommandStep,
        long? firstSynchronousWindowStep,
        string completionReason,
        string? exceptionType,
        string? exceptionMessage)
    {
        var final = localRows.Count == 0 ? null : localRows[^1];
        var holdRows = loadCommandStep.HasValue
            ? localRows.Where(row => row.LogicalStep >= loadCommandStep.Value).ToArray()
            : Array.Empty<ProbeSample>();
        var tailRows = holdRows.Length <= TailWindowSteps
            ? holdRows
            : holdRows[^TailWindowSteps..];

        var tailStrictFraction = tailRows.Length == 0
            ? double.NaN
            : tailRows.Count(row => IsStrictlySynchronousAtTarget(row, targetLoadMegawatts)) / (double)tailRows.Length;
        var cumulativeSlipCycles = holdRows.Sum(static row => row.GeneratorFrequencySlipHertz / StepsPerSecond);
        var signedPhaseWraps = CountSignedPhaseWraps(holdRows);

        return new ProbeResult(
            id,
            exactVersion,
            targetLoadMegawatts,
            loadIncrementMegawatts,
            final?.LogicalStep ?? 0L,
            completionReason,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            loadCommandStep,
            firstSynchronousWindowStep,
            firstSynchronousWindowStep.HasValue,
            tailStrictFraction,
            cumulativeSlipCycles,
            signedPhaseWraps,
            Mean(tailRows, static row => row.GeneratorFrequencyHertz),
            Minimum(tailRows, static row => row.GeneratorFrequencyHertz),
            Maximum(tailRows, static row => row.GeneratorFrequencyHertz),
            Mean(tailRows, static row => row.ElectricalOutputMegawatts),
            Mean(tailRows, static row => row.ReactorThermalMegawatts),
            Mean(tailRows, static row => row.TurbineShaftMegawatts),
            Mean(tailRows, static row => row.PassiveMechanicalLossMegawatts),
            Mean(tailRows, static row => row.GeneratorMechanicalInputMegawatts),
            Mean(tailRows, static row => row.NetRotorAccelerationPowerMegawatts),
            Mean(tailRows, static row => row.DispatchMechanicalAdequacyMegawatts),
            Mean(tailRows, static row => row.TurbineSteamFlowKilogramsPerSecond),
            Mean(tailRows, static row => row.TurbineInletPressureMegapascals),
            Mean(tailRows, static row => row.ControlValvePercentOpen),
            Mean(tailRows, static row => row.PhaseCorrectionPowerMegawatts),
            Mean(tailRows, static row => row.FrequencyCorrectionPowerMegawatts),
            final?.RequestedElectricalMegawatts ?? double.NaN,
            final?.ElectricalOutputMegawatts ?? double.NaN,
            final?.ReactorThermalMegawatts ?? double.NaN,
            final?.TurbineShaftMegawatts ?? double.NaN,
            final?.GeneratorFrequencyHertz ?? double.NaN,
            allFinite,
            exceptionType,
            exceptionMessage);
    }

    private static int CountSignedPhaseWraps(IReadOnlyList<ProbeSample> rows)
    {
        var wraps = 0;
        for (var index = 1; index < rows.Count; index++)
        {
            if (Math.Abs(rows[index].SignedPhaseLeadRadians - rows[index - 1].SignedPhaseLeadRadians) > Math.PI)
            {
                wraps++;
            }
        }

        return wraps;
    }

    private static double Mean(IReadOnlyList<ProbeSample> rows, Func<ProbeSample, double> selector)
        => rows.Count == 0 ? double.NaN : rows.Average(selector);

    private static double Minimum(IReadOnlyList<ProbeSample> rows, Func<ProbeSample, double> selector)
        => rows.Count == 0 ? double.NaN : rows.Min(selector);

    private static double Maximum(IReadOnlyList<ProbeSample> rows, Func<ProbeSample, double> selector)
        => rows.Count == 0 ? double.NaN : rows.Max(selector);

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(int exactVersion, double loadIncrementMegawatts)
    {
        var baseline = exactVersion switch
        {
            9 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine()),
            4 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory().CreateRuntimeEngine()),
            _ => throw new ArgumentOutOfRangeException(nameof(exactVersion), exactVersion, "Unsupported diagnostic exact version."),
        };

        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField(
            "_solver",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Runtime diagnostic clone could not locate the private solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("Runtime diagnostic clone could not read the integrated solver.");
        var commandPolicy = new ControlRoomRuntimeCommandPolicy(
            ControlRoomRuntimeCommandPolicy.Default.TurbineSpeedSetpointIncrementRpm,
            loadIncrementMegawatts * 1_000_000d);

        return new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            baseline.CurrentState,
            baseline.PersistentInputs,
            baseline.LatestCanonicalSnapshot,
            baseline.FixedDeltaTime,
            baseline.LogicalStep,
            commandPolicy);
    }

    private static ProbeSample Capture(
        string probeId,
        string phase,
        double? targetLoadMegawatts,
        double? targetThermalMegawatts,
        IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var grid = cycle.GeneratorGrid.Grid;
        var generatorDefinition = cycle.Definition.GeneratorGridSystem.GetGenerator(generator.GeneratorId);
        var coupling = generatorDefinition.GridCoupling
            ?? throw new InvalidOperationException("Diagnostic requires the canonical synchronous-grid coupling definition.");
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var step = engine.LogicalStep;
        var signedPhaseLeadRadians = SignedShortestPhaseLeadRadians(
            generator.FinalElectricalPhaseAngle.Radians,
            grid.FinalPhaseAngle.Radians);
        var frequencySlipHertz = generator.FinalElectricalFrequency.Hertz - grid.Frequency.Hertz;
        var phaseCorrectionMegawatts = coupling.MaximumSynchronizingCorrectionPower.Megawatts * Math.Sin(signedPhaseLeadRadians);
        var frequencyCorrectionMegawatts = coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts * frequencySlipHertz;
        var requestedMechanicalMegawatts = generator.RequestedElectricalPower.Megawatts / generatorDefinition.Efficiency.Fraction;
        var netRotorAccelerationMegawatts = rotor.ShaftPower.Megawatts
            - rotor.ExternalLoadPower.Megawatts
            - rotor.PassiveMechanicalLossPower.Megawatts;
        var dispatchMechanicalAdequacyMegawatts = rotor.ShaftPower.Megawatts
            - rotor.PassiveMechanicalLossPower.Megawatts
            - requestedMechanicalMegawatts;

        return new ProbeSample(
            probeId,
            step,
            step / (double)StepsPerSecond,
            phase,
            targetLoadMegawatts,
            targetThermalMegawatts,
            presentationGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            presentationGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            presentation.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            generator.MechanicalInputPower.Megawatts,
            rotor.ShaftPower.Megawatts,
            rotor.PassiveMechanicalLossPower.Megawatts,
            netRotorAccelerationMegawatts,
            requestedMechanicalMegawatts,
            dispatchMechanicalAdequacyMegawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.InletPressure.Megapascals,
            stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            train.ControlValve.EffectivePosition.Percent,
            turbine.MainSteamNetwork.TotalReliefMassFlowRate.KilogramsPerSecond,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            frequencySlipHertz,
            signedPhaseLeadRadians,
            phaseCorrectionMegawatts,
            frequencyCorrectionMegawatts,
            generator.CommandedElectromagneticTorque.NewtonMetres,
            generator.EffectiveElectromagneticTorque.NewtonMetres,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
    }

    private static double SignedShortestPhaseLeadRadians(double generatorRadians, double gridRadians)
    {
        var difference = generatorRadians - gridRadians;
        var fullTurn = 2d * Math.PI;
        difference = (difference + Math.PI) % fullTurn;
        if (difference < 0d)
        {
            difference += fullTurn;
        }

        return difference - Math.PI;
    }

    private static void AddEvent(
        ICollection<ProbeEvent> events,
        string probeId,
        long logicalStep,
        string eventKind,
        double targetLoadMegawatts,
        double targetThermalMegawatts,
        ProbeSample? sample)
        => events.Add(new ProbeEvent(
            probeId,
            logicalStep,
            eventKind,
            targetLoadMegawatts,
            targetThermalMegawatts,
            sample?.RequestedElectricalMegawatts,
            sample?.ElectricalOutputMegawatts,
            sample?.ReactorThermalMegawatts,
            sample?.TurbineShaftMegawatts,
            sample?.PassiveMechanicalLossMegawatts,
            sample?.GeneratorFrequencyHertz,
            sample?.GeneratorFrequencySlipHertz,
            sample?.SignedPhaseLeadRadians,
            sample?.NetRotorAccelerationPowerMegawatts,
            sample?.DispatchMechanicalAdequacyMegawatts,
            sample?.TurbineSteamFlowKilogramsPerSecond,
            sample?.ControlValvePercentOpen));

    private static string Progress(ProbeResult result)
        => $"probe-complete={result.Id}|executed={result.ExecutedSteps}|completion={result.CompletionReason}|target={F(result.TargetLoadMegawatts)}|load-command={I(result.LoadCommandStep)}|first-trip={I(result.FirstTripStep)}|first-latch={result.FirstLatchedFunctionId ?? "none"}|first-sync-window={I(result.FirstSynchronousWindowStep)}|tail-sync-fraction={F(result.TailStrictSynchronousFraction)}|tail-freq={F(result.TailMeanFrequencyHertz)}|tail-output={F(result.TailMeanElectricalOutputMegawatts)}|tail-shaft={F(result.TailMeanShaftMegawatts)}|tail-dispatch-adequacy={F(result.TailMeanDispatchMechanicalAdequacyMegawatts)}|phase-wraps={result.SignedPhaseWrapCount}";

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,target_load_mwe,load_increment_mwe,executed_steps,completion_reason,first_trip_step,first_latched_function,first_latched_step,load_command_step,first_synchronous_window_step,ever_synchronous,tail_strict_synchronous_fraction,cumulative_frequency_slip_cycles,signed_phase_wrap_count,tail_mean_frequency_hz,tail_min_frequency_hz,tail_max_frequency_hz,tail_mean_output_mwe,tail_mean_thermal_mw,tail_mean_shaft_mw,tail_mean_passive_loss_mw,tail_mean_external_load_mw,tail_mean_net_acceleration_mw,tail_mean_dispatch_mechanical_adequacy_mw,tail_mean_turbine_flow_kg_s,tail_mean_turbine_inlet_mpa,tail_mean_control_valve_percent,tail_mean_phase_correction_mw,tail_mean_frequency_correction_mw,final_requested_mwe,final_output_mwe,final_thermal_mw,final_shaft_mw,final_frequency_hz,all_finite,exception_type,exception_message"
        };
        lines.AddRange(results.Select(static result => string.Join(",", new[]
        {
            Csv(result.Id),
            result.ExactVersion.ToString(CultureInfo.InvariantCulture),
            F(result.TargetLoadMegawatts),
            F(result.LoadIncrementMegawatts),
            result.ExecutedSteps.ToString(CultureInfo.InvariantCulture),
            Csv(result.CompletionReason),
            I(result.FirstTripStep),
            Csv(result.FirstLatchedFunctionId ?? string.Empty),
            I(result.FirstLatchedFunctionStep),
            I(result.LoadCommandStep),
            I(result.FirstSynchronousWindowStep),
            result.EverSynchronous.ToString(),
            F(result.TailStrictSynchronousFraction),
            F(result.CumulativeFrequencySlipCycles),
            result.SignedPhaseWrapCount.ToString(CultureInfo.InvariantCulture),
            F(result.TailMeanFrequencyHertz),
            F(result.TailMinimumFrequencyHertz),
            F(result.TailMaximumFrequencyHertz),
            F(result.TailMeanElectricalOutputMegawatts),
            F(result.TailMeanThermalMegawatts),
            F(result.TailMeanShaftMegawatts),
            F(result.TailMeanPassiveMechanicalLossMegawatts),
            F(result.TailMeanExternalLoadMegawatts),
            F(result.TailMeanNetRotorAccelerationPowerMegawatts),
            F(result.TailMeanDispatchMechanicalAdequacyMegawatts),
            F(result.TailMeanTurbineSteamFlowKilogramsPerSecond),
            F(result.TailMeanTurbineInletPressureMegapascals),
            F(result.TailMeanControlValvePercentOpen),
            F(result.TailMeanPhaseCorrectionPowerMegawatts),
            F(result.TailMeanFrequencyCorrectionPowerMegawatts),
            F(result.FinalRequestedMegawatts),
            F(result.FinalElectricalOutputMegawatts),
            F(result.FinalThermalMegawatts),
            F(result.FinalShaftMegawatts),
            F(result.FinalFrequencyHertz),
            result.AllFinite.ToString(),
            Csv(result.ExceptionType ?? string.Empty),
            Csv(result.ExceptionMessage ?? string.Empty),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "190-long-settle-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteEvents(IEnumerable<ProbeEvent> events)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,event_kind,target_load_mwe,target_thermal_mw,requested_mwe,output_mwe,thermal_mw,shaft_mw,passive_loss_mw,frequency_hz,frequency_slip_hz,signed_phase_lead_rad,net_acceleration_mw,dispatch_mechanical_adequacy_mw,turbine_flow_kg_s,control_valve_percent"
        };
        lines.AddRange(events.Select(static item => string.Join(",", new[]
        {
            Csv(item.ProbeId),
            item.LogicalStep.ToString(CultureInfo.InvariantCulture),
            Csv(item.EventKind),
            F(item.TargetLoadMegawatts),
            F(item.TargetThermalMegawatts),
            F(item.RequestedMegawatts),
            F(item.OutputMegawatts),
            F(item.ThermalMegawatts),
            F(item.ShaftMegawatts),
            F(item.PassiveLossMegawatts),
            F(item.FrequencyHertz),
            F(item.FrequencySlipHertz),
            F(item.SignedPhaseLeadRadians),
            F(item.NetAccelerationMegawatts),
            F(item.DispatchMechanicalAdequacyMegawatts),
            F(item.TurbineFlowKilogramsPerSecond),
            F(item.ControlValvePercentOpen),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "191-long-settle-events.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,phase,target_load_mwe,target_thermal_mw,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_external_load_mw,turbine_shaft_mw,passive_mechanical_loss_mw,net_rotor_acceleration_power_mw,requested_mechanical_dispatch_mw,dispatch_mechanical_adequacy_mw,turbine_flow_kg_s,turbine_inlet_mpa,effective_specific_work_j_kg,control_valve_percent,relief_flow_kg_s,rotor_rpm,generator_frequency_hz,frequency_slip_hz,signed_phase_lead_rad,phase_correction_power_mw,frequency_correction_power_mw,commanded_em_torque_nm,effective_em_torque_nm,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(",", new[]
        {
            Csv(row.ProbeId),
            row.LogicalStep.ToString(CultureInfo.InvariantCulture),
            F(row.SimulatedSeconds),
            Csv(row.Phase),
            F(row.TargetLoadMegawatts),
            F(row.TargetThermalMegawatts),
            F(row.RequestedElectricalMegawatts),
            F(row.ElectricalOutputMegawatts),
            F(row.ReactorThermalMegawatts),
            F(row.GeneratorMechanicalInputMegawatts),
            F(row.TurbineShaftMegawatts),
            F(row.PassiveMechanicalLossMegawatts),
            F(row.NetRotorAccelerationPowerMegawatts),
            F(row.RequestedMechanicalDispatchMegawatts),
            F(row.DispatchMechanicalAdequacyMegawatts),
            F(row.TurbineSteamFlowKilogramsPerSecond),
            F(row.TurbineInletPressureMegapascals),
            F(row.EffectiveSpecificWorkJoulesPerKilogram),
            F(row.ControlValvePercentOpen),
            F(row.ReliefMassFlowKilogramsPerSecond),
            F(row.RotorRpm),
            F(row.GeneratorFrequencyHertz),
            F(row.GeneratorFrequencySlipHertz),
            F(row.SignedPhaseLeadRadians),
            F(row.PhaseCorrectionPowerMegawatts),
            F(row.FrequencyCorrectionPowerMegawatts),
            F(row.CommandedElectromagneticTorqueNewtonMetres),
            F(row.EffectiveElectromagneticTorqueNewtonMetres),
            row.BreakerClosed.ToString(),
            row.ReactorScram.ToString(),
            row.TurbineTrip.ToString(),
            row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "192-long-settle-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(IReadOnlyCollection<ProbeResult> results)
    {
        var reference = results.Single(static result => result.Id == "exact-v9-reference-5mwe-step");
        var v9Half = results.Single(static result => result.Id == "exact-v9-long-settle-5p5mwe");
        var v9One = results.Single(static result => result.Id == "exact-v9-long-settle-6mwe");
        var v4One = results.Single(static result => result.Id == "exact-v4-long-settle-6mwe");

        var lines = new[]
        {
            "scope=M10 Final replacement-long failure Diagnostic 6; Diagnostic 5 execution PASS established that its 20 s settle timeout occurred without protection and that measured reactor thermal readiness still left turbine shaft/steam flow near the preceding 5 MWe operating point at the load command; this candidate therefore tests long first-stage recovery before authoring a coupled ramp or changing generator-grid semantics; no production src, workload, authority policy, generator-load semantics, protection, exact-v9 or mission @3 changes;",
            $"reference-v9=trip:{I(reference.FirstTripStep)}|latch:{reference.FirstLatchedFunctionId ?? "none"};",
            $"v9-5p5=completion:{v9Half.CompletionReason}|trip:{I(v9Half.FirstTripStep)}|first-sync:{I(v9Half.FirstSynchronousWindowStep)}|tail-sync-fraction:{F(v9Half.TailStrictSynchronousFraction)}|tail-frequency:{F(v9Half.TailMeanFrequencyHertz)}|tail-output:{F(v9Half.TailMeanElectricalOutputMegawatts)}|tail-shaft:{F(v9Half.TailMeanShaftMegawatts)}|tail-dispatch-adequacy:{F(v9Half.TailMeanDispatchMechanicalAdequacyMegawatts)}|phase-wraps:{v9Half.SignedPhaseWrapCount};",
            $"v9-6=completion:{v9One.CompletionReason}|trip:{I(v9One.FirstTripStep)}|first-sync:{I(v9One.FirstSynchronousWindowStep)}|tail-sync-fraction:{F(v9One.TailStrictSynchronousFraction)}|tail-frequency:{F(v9One.TailMeanFrequencyHertz)}|tail-output:{F(v9One.TailMeanElectricalOutputMegawatts)}|tail-shaft:{F(v9One.TailMeanShaftMegawatts)}|tail-dispatch-adequacy:{F(v9One.TailMeanDispatchMechanicalAdequacyMegawatts)}|phase-wraps:{v9One.SignedPhaseWrapCount};",
            $"historical-v4-6=completion:{v4One.CompletionReason}|trip:{I(v4One.FirstTripStep)}|first-sync:{I(v4One.FirstSynchronousWindowStep)}|tail-sync-fraction:{F(v4One.TailStrictSynchronousFraction)}|tail-frequency:{F(v4One.TailMeanFrequencyHertz)}|tail-output:{F(v4One.TailMeanElectricalOutputMegawatts)}|tail-shaft:{F(v4One.TailMeanShaftMegawatts)}|tail-dispatch-adequacy:{F(v4One.TailMeanDispatchMechanicalAdequacyMegawatts)}|phase-wraps:{v4One.SignedPhaseWrapCount};",
            "decision-rule=if exact-v9 5.5 and/or 6 MWe recovers a strict synchronous window and the last 30 s remains predominantly locked, Diagnostic 5's 20 s timeout was a steam-path/rotor settling-time issue rather than a hard capacity boundary; next test a coupled or staged workload with evidence-derived dwell and do not retune protection/coupling. If the long hold remains unlocked while tail dispatch mechanical adequacy is materially negative, localize the limitation to steam-path/energy transfer capacity or target mapping. If dispatch mechanical adequacy closes near zero but frequency continues to slip with repeated phase wraps, investigate synchronous-grid coupling semantics/coefficients before any workload repair. If exact-v4 materially differs from exact-v9 under the same 6 MWe long hold, localize the remaining capacity difference to exact-v9; matching behavior is shared-model evidence. Diagnostic 6 authorizes no production change;",
            "authorization=diagnostic-only; Replacement-Long Execution 1 remains RED; second replacement-long freeze remains unauthorized; ordinary Release gate must pass before focused explicit diagnostic;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "193-long-settle-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 6.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic6");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 6 STARTED" + Environment.NewLine,
            Utf8WithoutBom);
    }

    private static void AppendProgress(string message)
        => File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private static string Flatten(string value)
        => value.Replace('\r', ' ').Replace('\n', ' ');

    private static string F(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("G17", CultureInfo.InvariantCulture)
            : string.Empty;

    private static string F(double value)
        => double.IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string I(long? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Csv(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private sealed record ProbeDefinition(string Id, int ExactVersion, double LoadIncrementMegawatts, double TargetLoadMegawatts);

    private sealed record ProbeResult(
        string Id,
        int ExactVersion,
        double TargetLoadMegawatts,
        double LoadIncrementMegawatts,
        long ExecutedSteps,
        string CompletionReason,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        long? LoadCommandStep,
        long? FirstSynchronousWindowStep,
        bool EverSynchronous,
        double TailStrictSynchronousFraction,
        double CumulativeFrequencySlipCycles,
        int SignedPhaseWrapCount,
        double TailMeanFrequencyHertz,
        double TailMinimumFrequencyHertz,
        double TailMaximumFrequencyHertz,
        double TailMeanElectricalOutputMegawatts,
        double TailMeanThermalMegawatts,
        double TailMeanShaftMegawatts,
        double TailMeanPassiveMechanicalLossMegawatts,
        double TailMeanExternalLoadMegawatts,
        double TailMeanNetRotorAccelerationPowerMegawatts,
        double TailMeanDispatchMechanicalAdequacyMegawatts,
        double TailMeanTurbineSteamFlowKilogramsPerSecond,
        double TailMeanTurbineInletPressureMegapascals,
        double TailMeanControlValvePercentOpen,
        double TailMeanPhaseCorrectionPowerMegawatts,
        double TailMeanFrequencyCorrectionPowerMegawatts,
        double FinalRequestedMegawatts,
        double FinalElectricalOutputMegawatts,
        double FinalThermalMegawatts,
        double FinalShaftMegawatts,
        double FinalFrequencyHertz,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage)
    {
        public bool DiagnosticComplete => AllFinite && ExecutedSteps > 0;
    }

    private sealed record ProbeEvent(
        string ProbeId,
        long LogicalStep,
        string EventKind,
        double TargetLoadMegawatts,
        double TargetThermalMegawatts,
        double? RequestedMegawatts,
        double? OutputMegawatts,
        double? ThermalMegawatts,
        double? ShaftMegawatts,
        double? PassiveLossMegawatts,
        double? FrequencyHertz,
        double? FrequencySlipHertz,
        double? SignedPhaseLeadRadians,
        double? NetAccelerationMegawatts,
        double? DispatchMechanicalAdequacyMegawatts,
        double? TurbineFlowKilogramsPerSecond,
        double? ControlValvePercentOpen);

    private sealed record ProbeSample(
        string ProbeId,
        long LogicalStep,
        double SimulatedSeconds,
        string Phase,
        double? TargetLoadMegawatts,
        double? TargetThermalMegawatts,
        double RequestedElectricalMegawatts,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double TurbineShaftMegawatts,
        double PassiveMechanicalLossMegawatts,
        double NetRotorAccelerationPowerMegawatts,
        double RequestedMechanicalDispatchMegawatts,
        double DispatchMechanicalAdequacyMegawatts,
        double TurbineSteamFlowKilogramsPerSecond,
        double TurbineInletPressureMegapascals,
        double EffectiveSpecificWorkJoulesPerKilogram,
        double ControlValvePercentOpen,
        double ReliefMassFlowKilogramsPerSecond,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorFrequencySlipHertz,
        double SignedPhaseLeadRadians,
        double PhaseCorrectionPowerMegawatts,
        double FrequencyCorrectionPowerMegawatts,
        double CommandedElectromagneticTorqueNewtonMetres,
        double EffectiveElectromagneticTorqueNewtonMetres,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;

        public bool AllFinite => double.IsFinite(SimulatedSeconds)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(ReactorThermalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(TurbineShaftMegawatts)
            && double.IsFinite(PassiveMechanicalLossMegawatts)
            && double.IsFinite(NetRotorAccelerationPowerMegawatts)
            && double.IsFinite(RequestedMechanicalDispatchMegawatts)
            && double.IsFinite(DispatchMechanicalAdequacyMegawatts)
            && double.IsFinite(TurbineSteamFlowKilogramsPerSecond)
            && double.IsFinite(TurbineInletPressureMegapascals)
            && double.IsFinite(EffectiveSpecificWorkJoulesPerKilogram)
            && double.IsFinite(ControlValvePercentOpen)
            && double.IsFinite(ReliefMassFlowKilogramsPerSecond)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorFrequencySlipHertz)
            && double.IsFinite(SignedPhaseLeadRadians)
            && double.IsFinite(PhaseCorrectionPowerMegawatts)
            && double.IsFinite(FrequencyCorrectionPowerMegawatts)
            && double.IsFinite(CommandedElectromagneticTorqueNewtonMetres)
            && double.IsFinite(EffectiveElectromagneticTorqueNewtonMetres);
    }
}
