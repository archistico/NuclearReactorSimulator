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
/// Evidence-only follow-up after Replacement-Long Failure Diagnostic 4 showed that fixed-time smaller/slower
/// electrical ramps and short reactor-support lead times still trip before a stable 10 MWe window. Diagnostic 4's
/// nominal 66 MWth pre-power probe reached only about 37 MWth at the load step, so it did not establish actual
/// thermal readiness. This audit gates each load increment on measured reactor thermal readiness and then requires
/// a protected post-increment settling window before proceeding. It changes no production source or policy.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic5Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC5";
    private const int StepsPerSecond = 100;
    private const int ReferenceTotalSteps = 3_000;
    private const int StagedMaximumSteps = 30_000;
    private const int PreparationTimeoutSteps = 6_000;
    private const int SettlingTimeoutSteps = 2_000;
    private const int StableWindowSteps = 100;
    private const int FinalStableWindowSteps = 500;
    private const double ThermalReadinessToleranceMegawatts = 0.25d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic5")]
    public void ExactV9_ReadinessGatedStagedLoadAndAttainableCapacityCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var allRows = new List<ProbeSample>();
        var allEvents = new List<StageEvent>();
        var results = new List<ProbeResult>();

        AppendProgress("probe-start=exact-v9-reference-5mwe-step");
        var reference = RunReference("exact-v9-reference-5mwe-step", allRows);
        results.Add(reference);
        AppendProgress(Progress(reference));

        ProbeDefinition[] stagedProbes =
        [
            new("exact-v9-readiness-gated-1mwe", 9, 1d),
            new("exact-v9-readiness-gated-0p5mwe", 9, 0.5d),
            new("exact-v4-readiness-gated-1mwe", 4, 1d),
        ];

        foreach (var probe in stagedProbes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunReadinessGatedProbe(probe, allRows, allEvents);
            results.Add(result);
            AppendProgress(Progress(result));
        }

        WriteProbeSummary(results);
        WriteStageEvents(allEvents);
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

        var initial = Capture(id, "reference", null, null, engine, localRows, allRows);
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
                var sample = Capture(id, "reference", null, null, engine, localRows, allRows);
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
            loadIncrementMegawatts: 5d,
            localRows,
            allFinite,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            stagesCompleted: 0,
            stageCount: 1,
            completionReason: exceptionType is null ? "reference-complete" : "exception",
            stableTenMegawatts: false,
            exceptionType,
            exceptionMessage);
    }

    private static ProbeResult RunReadinessGatedProbe(
        ProbeDefinition probe,
        ICollection<ProbeSample> allRows,
        ICollection<StageEvent> allEvents)
    {
        var engine = CreateEngine(probe.ExactVersion, probe.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(presentation.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(presentation.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial generator requested load is unavailable.");
        var initialThermalMegawatts = presentation.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");
        var thermalPerElectricalMegawatt = initialThermalMegawatts / initialRequestedMegawatts;
        var stageCount = checked((int)Math.Round((10d - initialRequestedMegawatts) / probe.LoadIncrementMegawatts, MidpointRounding.AwayFromZero));

        var localRows = new List<ProbeSample>();
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;
        var stagesCompleted = 0;
        var completionReason = "maximum-steps";
        var stableTenMegawatts = false;
        var stageIndex = 0;
        var targetLoadMegawatts = Math.Min(10d, initialRequestedMegawatts + probe.LoadIncrementMegawatts);
        var targetThermalMegawatts = thermalPerElectricalMegawatt * targetLoadMegawatts;
        var phase = "prepare";
        var phaseStartStep = engine.LogicalStep;
        long? loadCommandStep = null;
        var stableCount = 0;

        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
        AddEvent(allEvents, probe.Id, stageIndex + 1, engine.LogicalStep, "prepare-start", targetLoadMegawatts, targetThermalMegawatts, null);

        var initial = Capture(probe.Id, phase, targetLoadMegawatts, targetThermalMegawatts, engine, localRows, allRows);
        allFinite &= initial.AllFinite;

        for (var iteration = 0; iteration < StagedMaximumSteps; iteration++)
        {
            try
            {
                var previous = localRows[^1];

                if (phase == "prepare" && IsThermallyReady(previous, targetThermalMegawatts))
                {
                    loadCommandStep = engine.LogicalStep + 1;
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    phase = "settle";
                    phaseStartStep = engine.LogicalStep;
                    stableCount = 0;
                    AddEvent(allEvents, probe.Id, stageIndex + 1, loadCommandStep.Value, "load-command", targetLoadMegawatts, targetThermalMegawatts, previous);
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, phase, targetLoadMegawatts, targetThermalMegawatts, engine, localRows, allRows);
                allFinite &= sample.AllFinite;
                CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);

                if (sample.AnyTripActive)
                {
                    completionReason = "trip";
                    AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "trip", targetLoadMegawatts, targetThermalMegawatts, sample);
                    break;
                }

                if (phase == "prepare")
                {
                    if (sample.LogicalStep - phaseStartStep >= PreparationTimeoutSteps)
                    {
                        completionReason = "prepare-timeout";
                        AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "prepare-timeout", targetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    continue;
                }

                if (phase == "settle")
                {
                    stableCount = IsStableAtRequestedLoad(sample, targetLoadMegawatts)
                        ? stableCount + 1
                        : 0;

                    if (stableCount >= StableWindowSteps)
                    {
                        stagesCompleted++;
                        AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "stage-stable", targetLoadMegawatts, targetThermalMegawatts, sample);

                        if (targetLoadMegawatts >= 9.999d)
                        {
                            phase = "final-hold";
                            phaseStartStep = sample.LogicalStep;
                            stableCount = 0;
                            AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "final-hold-start", targetLoadMegawatts, targetThermalMegawatts, sample);
                        }
                        else
                        {
                            stageIndex++;
                            targetLoadMegawatts = Math.Min(10d, initialRequestedMegawatts + (probe.LoadIncrementMegawatts * (stageIndex + 1)));
                            targetThermalMegawatts = thermalPerElectricalMegawatt * targetLoadMegawatts;
                            engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
                            phase = "prepare";
                            phaseStartStep = sample.LogicalStep;
                            loadCommandStep = null;
                            stableCount = 0;
                            AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "prepare-start", targetLoadMegawatts, targetThermalMegawatts, sample);
                        }
                    }
                    else if (loadCommandStep.HasValue && sample.LogicalStep - loadCommandStep.Value >= SettlingTimeoutSteps)
                    {
                        completionReason = "settle-timeout";
                        AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "settle-timeout", targetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    continue;
                }

                if (phase == "final-hold")
                {
                    stableCount = IsStableAtRequestedLoad(sample, 10d)
                        ? stableCount + 1
                        : 0;

                    if (stableCount >= FinalStableWindowSteps)
                    {
                        stableTenMegawatts = true;
                        completionReason = "stable-10mwe";
                        AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "stable-10mwe", targetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    if (sample.LogicalStep - phaseStartStep >= SettlingTimeoutSteps)
                    {
                        completionReason = "final-hold-timeout";
                        AddEvent(allEvents, probe.Id, stageIndex + 1, sample.LogicalStep, "final-hold-timeout", targetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
                exceptionMessage = Flatten(exception.Message);
                completionReason = "exception";
                break;
            }
        }

        return Complete(
            probe.Id,
            probe.ExactVersion,
            probe.LoadIncrementMegawatts,
            localRows,
            allFinite,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            stagesCompleted,
            stageCount,
            completionReason,
            stableTenMegawatts,
            exceptionType,
            exceptionMessage);
    }

    private static bool IsThermallyReady(ProbeSample sample, double targetThermalMegawatts)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - ThermalReadinessToleranceMegawatts
            && sample.GeneratorFrequencyHertz >= 49.9d
            && sample.GeneratorFrequencyHertz <= 50.1d;

    private static bool IsStableAtRequestedLoad(ProbeSample sample, double requestedMegawatts)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.RequestedElectricalMegawatts >= requestedMegawatts - 1e-6d
            && sample.RequestedElectricalMegawatts <= requestedMegawatts + 1e-6d
            && sample.ElectricalOutputMegawatts >= requestedMegawatts - 0.5d
            && sample.ElectricalOutputMegawatts <= requestedMegawatts + 0.5d
            && sample.GeneratorFrequencyHertz >= 49.5d
            && sample.GeneratorFrequencyHertz <= 50.5d;

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
        double loadIncrementMegawatts,
        IReadOnlyList<ProbeSample> localRows,
        bool allFinite,
        long? firstTripStep,
        string? firstLatchedFunctionId,
        long? firstLatchedFunctionStep,
        int stagesCompleted,
        int stageCount,
        string completionReason,
        bool stableTenMegawatts,
        string? exceptionType,
        string? exceptionMessage)
    {
        var final = localRows.Count == 0 ? null : localRows[^1];
        var firstTen = localRows.FirstOrDefault(static row => row.RequestedElectricalMegawatts >= 9.999d);
        return new ProbeResult(
            id,
            exactVersion,
            loadIncrementMegawatts,
            localRows.Count == 0 ? 0 : localRows[^1].LogicalStep,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            firstTen?.LogicalStep,
            stagesCompleted,
            stageCount,
            completionReason,
            stableTenMegawatts,
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.ReactorThermalMegawatts),
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.TurbineShaftMegawatts),
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.TurbineSteamFlowKilogramsPerSecond),
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.TurbineInletPressureMegapascals),
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.ReliefMassFlowKilogramsPerSecond),
            localRows.Count == 0 ? double.NaN : localRows.Min(static row => row.GeneratorFrequencyHertz),
            localRows.Count == 0 ? double.NaN : localRows.Max(static row => Math.Abs(row.GeneratorPhaseDifferenceRadians)),
            final?.RequestedElectricalMegawatts ?? double.NaN,
            final?.ElectricalOutputMegawatts ?? double.NaN,
            final?.ReactorThermalMegawatts ?? double.NaN,
            final?.TurbineShaftMegawatts ?? double.NaN,
            allFinite,
            exceptionType,
            exceptionMessage);
    }

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
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<ProbeSample> localRows,
        ICollection<ProbeSample> allRows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var step = engine.LogicalStep;

        var row = new ProbeSample(
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
            stage.ShaftPower.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.InletPressure.Megapascals,
            stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            train.ControlValve.EffectivePosition.Percent,
            turbine.MainSteamNetwork.TotalReliefMassFlowRate.KilogramsPerSecond,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalPhaseDifference.Radians,
            generator.CommandedElectromagneticTorque.NewtonMetres,
            generator.EffectiveElectromagneticTorque.NewtonMetres,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
        localRows.Add(row);
        allRows.Add(row);
        return row;
    }

    private static void AddEvent(
        ICollection<StageEvent> events,
        string probeId,
        int stageNumber,
        long logicalStep,
        string eventKind,
        double targetLoadMegawatts,
        double targetThermalMegawatts,
        ProbeSample? sample)
        => events.Add(new StageEvent(
            probeId,
            stageNumber,
            logicalStep,
            eventKind,
            targetLoadMegawatts,
            targetThermalMegawatts,
            sample?.RequestedElectricalMegawatts,
            sample?.ElectricalOutputMegawatts,
            sample?.ReactorThermalMegawatts,
            sample?.TurbineShaftMegawatts,
            sample?.GeneratorFrequencyHertz,
            sample?.GeneratorPhaseDifferenceRadians,
            sample?.TurbineSteamFlowKilogramsPerSecond,
            sample?.ControlValvePercentOpen,
            sample?.ReliefMassFlowKilogramsPerSecond));

    private static string Progress(ProbeResult result)
        => $"probe-complete={result.Id}|executed={result.ExecutedSteps}|completion={result.CompletionReason}|stages={result.StagesCompleted}/{result.StageCount}|first-trip={I(result.FirstTripStep)}|first-latch={result.FirstLatchedFunctionId ?? "none"}|first-10mwe={I(result.FirstTenMegawattRequestStep)}|stable-10={result.StableTenMegawatts}|max-thermal={F(result.MaximumThermalMegawatts)}|max-shaft={F(result.MaximumShaftMegawatts)}|max-flow={F(result.MaximumTurbineSteamFlowKilogramsPerSecond)}";

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,load_increment_mwe,executed_steps,completion_reason,stages_completed,stage_count,first_trip_step,first_latched_function,first_latched_step,first_10mwe_request_step,stable_10mwe,max_thermal_mw,max_shaft_mw,max_turbine_flow_kg_s,max_turbine_inlet_mpa,max_relief_flow_kg_s,min_frequency_hz,max_abs_phase_rad,final_requested_mwe,final_output_mwe,final_thermal_mw,final_shaft_mw,all_finite,exception_type,exception_message"
        };
        lines.AddRange(results.Select(static result => string.Join(",", new[]
        {
            Csv(result.Id),
            result.ExactVersion.ToString(CultureInfo.InvariantCulture),
            F(result.LoadIncrementMegawatts),
            result.ExecutedSteps.ToString(CultureInfo.InvariantCulture),
            Csv(result.CompletionReason),
            result.StagesCompleted.ToString(CultureInfo.InvariantCulture),
            result.StageCount.ToString(CultureInfo.InvariantCulture),
            I(result.FirstTripStep),
            Csv(result.FirstLatchedFunctionId ?? string.Empty),
            I(result.FirstLatchedFunctionStep),
            I(result.FirstTenMegawattRequestStep),
            result.StableTenMegawatts.ToString(),
            F(result.MaximumThermalMegawatts),
            F(result.MaximumShaftMegawatts),
            F(result.MaximumTurbineSteamFlowKilogramsPerSecond),
            F(result.MaximumTurbineInletPressureMegapascals),
            F(result.MaximumReliefMassFlowKilogramsPerSecond),
            F(result.MinimumFrequencyHertz),
            F(result.MaximumAbsolutePhaseDifferenceRadians),
            F(result.FinalRequestedMegawatts),
            F(result.FinalElectricalOutputMegawatts),
            F(result.FinalThermalMegawatts),
            F(result.FinalShaftMegawatts),
            result.AllFinite.ToString(),
            Csv(result.ExceptionType ?? string.Empty),
            Csv(result.ExceptionMessage ?? string.Empty),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "180-readiness-gated-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteStageEvents(IEnumerable<StageEvent> events)
    {
        var lines = new List<string>
        {
            "probe_id,stage_number,logical_step,event_kind,target_load_mwe,target_thermal_mw,requested_mwe,output_mwe,thermal_mw,shaft_mw,frequency_hz,phase_rad,turbine_flow_kg_s,control_valve_percent,relief_flow_kg_s"
        };
        lines.AddRange(events.Select(static item => string.Join(",", new[]
        {
            Csv(item.ProbeId),
            item.StageNumber.ToString(CultureInfo.InvariantCulture),
            item.LogicalStep.ToString(CultureInfo.InvariantCulture),
            Csv(item.EventKind),
            F(item.TargetLoadMegawatts),
            F(item.TargetThermalMegawatts),
            F(item.RequestedMegawatts),
            F(item.OutputMegawatts),
            F(item.ThermalMegawatts),
            F(item.ShaftMegawatts),
            F(item.FrequencyHertz),
            F(item.PhaseRadians),
            F(item.TurbineFlowKilogramsPerSecond),
            F(item.ControlValvePercentOpen),
            F(item.ReliefFlowKilogramsPerSecond),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "181-readiness-gated-stage-events.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,phase,target_load_mwe,target_thermal_mw,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_mechanical_input_mw,turbine_shaft_mw,turbine_flow_kg_s,turbine_inlet_mpa,effective_specific_work_j_kg,control_valve_percent,relief_flow_kg_s,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,commanded_em_torque_nm,effective_em_torque_nm,breaker_closed,reactor_scram,turbine_trip,generator_trip"
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
            F(row.TurbineSteamFlowKilogramsPerSecond),
            F(row.TurbineInletPressureMegapascals),
            F(row.EffectiveSpecificWorkJoulesPerKilogram),
            F(row.ControlValvePercentOpen),
            F(row.ReliefMassFlowKilogramsPerSecond),
            F(row.RotorRpm),
            F(row.GeneratorFrequencyHertz),
            F(row.GeneratorPhaseDifferenceRadians),
            F(row.CommandedElectromagneticTorqueNewtonMetres),
            F(row.EffectiveElectromagneticTorqueNewtonMetres),
            row.BreakerClosed.ToString(),
            row.ReactorScram.ToString(),
            row.TurbineTrip.ToString(),
            row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "182-readiness-gated-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(IReadOnlyCollection<ProbeResult> results)
    {
        var reference = results.Single(static result => result.Id == "exact-v9-reference-5mwe-step");
        var v9One = results.Single(static result => result.Id == "exact-v9-readiness-gated-1mwe");
        var v9Half = results.Single(static result => result.Id == "exact-v9-readiness-gated-0p5mwe");
        var v4One = results.Single(static result => result.Id == "exact-v4-readiness-gated-1mwe");

        var lines = new[]
        {
            "scope=M10 Final replacement-long failure Diagnostic 5; Diagnostic 4 execution PASS proved fixed-time smaller/slower ramps and short energy-support lead do not establish stable 10 MWe, while its nominal 66 MWth pre-power target reached only about 37 MWth at the actual load step; this candidate therefore tests measured readiness rather than elapsed-time assumptions; no production src, workload, authority policy, generator-load semantics, protection, exact-v9 or mission @3 changes;",
            $"reference-v9=trip:{I(reference.FirstTripStep)}|latch:{reference.FirstLatchedFunctionId ?? "none"};",
            $"v9-readiness-1mwe=completion:{v9One.CompletionReason}|stages:{v9One.StagesCompleted}/{v9One.StageCount}|stable-10:{v9One.StableTenMegawatts}|trip:{I(v9One.FirstTripStep)}|latch:{v9One.FirstLatchedFunctionId ?? "none"}|max-thermal:{F(v9One.MaximumThermalMegawatts)}|max-shaft:{F(v9One.MaximumShaftMegawatts)}|max-flow:{F(v9One.MaximumTurbineSteamFlowKilogramsPerSecond)};",
            $"v9-readiness-0p5mwe=completion:{v9Half.CompletionReason}|stages:{v9Half.StagesCompleted}/{v9Half.StageCount}|stable-10:{v9Half.StableTenMegawatts}|trip:{I(v9Half.FirstTripStep)}|latch:{v9Half.FirstLatchedFunctionId ?? "none"}|max-thermal:{F(v9Half.MaximumThermalMegawatts)}|max-shaft:{F(v9Half.MaximumShaftMegawatts)}|max-flow:{F(v9Half.MaximumTurbineSteamFlowKilogramsPerSecond)};",
            $"historical-v4-readiness-1mwe=completion:{v4One.CompletionReason}|stages:{v4One.StagesCompleted}/{v4One.StageCount}|stable-10:{v4One.StableTenMegawatts}|trip:{I(v4One.FirstTripStep)}|latch:{v4One.FirstLatchedFunctionId ?? "none"}|max-thermal:{F(v4One.MaximumThermalMegawatts)}|max-shaft:{F(v4One.MaximumShaftMegawatts)};",
            "decision-rule=if either exact-v9 readiness-gated schedule reaches a protected stable 10 MWe window, the plant model has attainable high-load capacity and the failed replacement manoeuvre is a workload/procedure timing plus command-granularity qualification gap; author a separate workload/operator-policy repair and then a new freeze, without protection or generator-grid retuning. If readiness is reached for successive stages but a specific electrical increment repeatedly trips despite proportional thermal preparation, localize the first failing stage and use flow/pressure/specific-work/relief evidence to decide steam-path capacity versus electromagnetic coupling. If thermal readiness itself cannot be reached while holding the prior stable load, classify the operating procedure as physically incompatible with the current reduced-order steam-energy inventory and investigate an explicit coupled ramp policy rather than pre-powering. Compare exact-v4 only under the same readiness algorithm; matching failure remains shared semantics/capacity evidence, while a material v4/v9 split localizes exact-v9 capacity. Diagnostic 5 authorizes no production change;",
            "authorization=diagnostic-only; Replacement-Long Execution 1 remains RED; second replacement-long freeze remains unauthorized; run the ordinary Release gate before the focused explicit diagnostic so local validation and GitHub CI exercise the same ordinary configuration;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "183-readiness-gated-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 5.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic5");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 5 STARTED" + Environment.NewLine,
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

    private sealed record ProbeDefinition(string Id, int ExactVersion, double LoadIncrementMegawatts);

    private sealed record ProbeResult(
        string Id,
        int ExactVersion,
        double LoadIncrementMegawatts,
        long ExecutedSteps,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        long? FirstTenMegawattRequestStep,
        int StagesCompleted,
        int StageCount,
        string CompletionReason,
        bool StableTenMegawatts,
        double MaximumThermalMegawatts,
        double MaximumShaftMegawatts,
        double MaximumTurbineSteamFlowKilogramsPerSecond,
        double MaximumTurbineInletPressureMegapascals,
        double MaximumReliefMassFlowKilogramsPerSecond,
        double MinimumFrequencyHertz,
        double MaximumAbsolutePhaseDifferenceRadians,
        double FinalRequestedMegawatts,
        double FinalElectricalOutputMegawatts,
        double FinalThermalMegawatts,
        double FinalShaftMegawatts,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage)
    {
        public bool DiagnosticComplete => AllFinite && ExecutedSteps > 0;
    }

    private sealed record StageEvent(
        string ProbeId,
        int StageNumber,
        long LogicalStep,
        string EventKind,
        double TargetLoadMegawatts,
        double TargetThermalMegawatts,
        double? RequestedMegawatts,
        double? OutputMegawatts,
        double? ThermalMegawatts,
        double? ShaftMegawatts,
        double? FrequencyHertz,
        double? PhaseRadians,
        double? TurbineFlowKilogramsPerSecond,
        double? ControlValvePercentOpen,
        double? ReliefFlowKilogramsPerSecond);

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
        double TurbineSteamFlowKilogramsPerSecond,
        double TurbineInletPressureMegapascals,
        double EffectiveSpecificWorkJoulesPerKilogram,
        double ControlValvePercentOpen,
        double ReliefMassFlowKilogramsPerSecond,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorPhaseDifferenceRadians,
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
            && double.IsFinite(TurbineSteamFlowKilogramsPerSecond)
            && double.IsFinite(TurbineInletPressureMegapascals)
            && double.IsFinite(EffectiveSpecificWorkJoulesPerKilogram)
            && double.IsFinite(ControlValvePercentOpen)
            && double.IsFinite(ReliefMassFlowKilogramsPerSecond)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorPhaseDifferenceRadians)
            && double.IsFinite(CommandedElectromagneticTorqueNewtonMetres)
            && double.IsFinite(EffectiveElectromagneticTorqueNewtonMetres);
    }
}
