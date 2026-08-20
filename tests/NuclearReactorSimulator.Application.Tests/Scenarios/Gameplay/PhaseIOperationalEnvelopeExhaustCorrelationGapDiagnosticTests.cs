using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 7 diagnostic for the cumulative operational-envelope blocker observed after Hotfix 6.
/// It reproduces the exact 10 s steady -> 30 s load raise -> 30 s load lower journey once under exact desktop @2
/// and once under authoritative exact desktop @3. The diagnostic changes no production runtime, coefficient, profile,
/// thermodynamic equation, hydraulic target set or acceptance threshold.
/// </summary>
public sealed class PhaseIOperationalEnvelopeExhaustCorrelationGapDiagnosticTests
{
    private const int StepsPerSecond = 100;
    private const int WarmupSteps = 10 * StepsPerSecond;
    private const int LoadSegmentSteps = 30 * StepsPerSecond;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIOperationalEnvelopeExhaustGapDiagnostic")]
    public void ExactV2AndV3_LoadRaiseLower_ClassifyExhaustEnvelopeFailureWithoutRuntimeChanges()
    {
        ResetReportDirectory();

        var explicitV2 = RunJourney(
            "desktop-v2-explicit-control",
            DesktopHydraulicProductionPolicy.ExplicitCommittedState);
        var correctedV3 = RunJourney(
            "desktop-v3-corrected-authoritative",
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate);

        WriteArtifacts(explicitV2, correctedV3);

        Assert.NotNull(explicitV2.Failure);
        var explicitFailure = Assert.IsType<WaterSteamStateOutOfRangeException>(explicitV2.Failure);
        Assert.Equal("exhaust", explicitFailure.NodeId);

        Assert.NotNull(correctedV3.Failure);
        var correctedFailure = Assert.IsType<WaterSteamStateOutOfRangeException>(correctedV3.Failure);
        Assert.Equal("exhaust", correctedFailure.NodeId);

        Assert.True(
            correctedV3.Samples.Count < explicitV2.Samples.Count,
            "Expected authoritative @3 to encounter the shared exhaust correlation-gap family earlier than @2 in the frozen journey.");
    }

    private static JourneyResult RunJourney(string label, DesktopHydraulicProductionPolicy policy)
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(policy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        var samples = new List<TraceSample>(WarmupSteps + (2 * LoadSegmentSteps));
        var progressPath = Path.Combine(ReportDirectory(), "00-progress.txt");
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(initial.Electrical.Generators);

        try
        {
            if (!AdvanceSegment(engine, label, "steady", WarmupSteps, samples, progressPath, out var failure))
            {
                return CreateFailedResult(label, decision, samples, failure!);
            }

            engine.QueueOperatorCommand(new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadRaise,
                generator.GeneratorId,
                ControlRoomCommandTargetKind.Generator));
            if (!AdvanceSegment(engine, label, "load-raise-hold", LoadSegmentSteps, samples, progressPath, out failure))
            {
                return CreateFailedResult(label, decision, samples, failure!);
            }

            engine.QueueOperatorCommand(new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadLower,
                generator.GeneratorId,
                ControlRoomCommandTargetKind.Generator));
            if (!AdvanceSegment(engine, label, "load-lower-hold", LoadSegmentSteps, samples, progressPath, out failure))
            {
                return CreateFailedResult(label, decision, samples, failure!);
            }
        }
        catch (Exception exception) when (exception is not WaterSteamStateOutOfRangeException)
        {
            var context = CaptureFailureContext(engine, "unexpected", 0, exception);
            return CreateFailedResult(label, decision, samples, context);
        }

        return new JourneyResult(
            label,
            decision.InitialCondition.InitialConditionId,
            decision.InitialCondition.Version,
            decision.EffectivePolicy.ToString(),
            samples,
            Completed: true,
            Failure: null,
            FailureContext: null);
    }

    private static bool AdvanceSegment(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string label,
        string segment,
        int stepCount,
        List<TraceSample> samples,
        string progressPath,
        out FailureContext? failure)
    {
        for (var segmentStep = 1; segmentStep <= stepCount; segmentStep++)
        {
            try
            {
                var presentation = engine.Step(ControlRoomRunState.Running);
                samples.Add(CaptureSample(engine, presentation, segment, segmentStep));
            }
            catch (WaterSteamStateOutOfRangeException exception)
            {
                failure = CaptureFailureContext(engine, segment, segmentStep, exception);
                AppendProgress(progressPath,
                    $"{label}: FAILURE segment={segment}; segment-step={segmentStep}; attempted-logical-step={failure.AttemptedLogicalStep}; node={exception.NodeId}; v={F(exception.SpecificVolumeCubicMetresPerKilogram)}; u={F(exception.SpecificInternalEnergyJoulesPerKilogram)}");
                return false;
            }

            if (segmentStep % 500 == 0 || segmentStep == stepCount)
            {
                AppendProgress(progressPath,
                    $"{label}: segment={segment}; completed={segmentStep}/{stepCount}; logical-step={engine.LogicalStep}");
            }
        }

        failure = null;
        return true;
    }

    private static TraceSample CaptureSample(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomSnapshot presentation,
        string segment,
        int segmentStep)
    {
        var plant = engine.CurrentState.PlantState.PlantState;
        var exhaust = plant.GetFluidNode("exhaust");
        var canonical = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
        var condenser = Assert.Single(canonical.Condenser.Condensers);
        var stage = Assert.Single(canonical.TurbineExpansion.StageGroups);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        var numerics = canonical.PrimaryCircuit.HydraulicNumerics;
        var telemetry = numerics.FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;

        return new TraceSample(
            engine.LogicalStep,
            segment,
            segmentStep,
            exhaust.Phase.ToString(),
            exhaust.Mass.Kilograms,
            exhaust.Volume.CubicMetres / exhaust.Mass.Kilograms,
            exhaust.SpecificInternalEnergy.JoulesPerKilogram,
            exhaust.Pressure.Kilopascals,
            exhaust.Temperature.DegreesCelsius,
            exhaust.VaporQuality?.Fraction,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond,
            condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond,
            condenser.SteamEnergyRemovalRate.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            rotor.Speed.NumericValue ?? double.NaN,
            numerics.Mode.ToString(),
            telemetry?.TriggerObserved ?? false,
            telemetry?.ShadowCorrectionEvaluated ?? false,
            telemetry?.CorrectedCommitAuthorized ?? false,
            telemetry?.CorrectedCandidateCommitted ?? false,
            telemetry?.RollbackRequired ?? false,
            telemetry?.UntargetedBranchDisagreementDetected ?? false,
            telemetry?.BranchOverrideCount ?? 0,
            telemetry?.PreviousPhaseHoldCount ?? 0);
    }

    private static FailureContext CaptureFailureContext(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string segment,
        int segmentStep,
        Exception exception)
    {
        var plant = engine.CurrentState.PlantState.PlantState;
        var exhaust = plant.GetFluidNode("exhaust");
        var canonical = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
        var condenser = Assert.Single(canonical.Condenser.Condensers);
        var stage = Assert.Single(canonical.TurbineExpansion.StageGroups);
        var numerics = canonical.PrimaryCircuit.HydraulicNumerics;
        var telemetry = numerics.FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;
        var waterSteam = exception as WaterSteamStateOutOfRangeException;

        return new FailureContext(
            segment,
            segmentStep,
            engine.LogicalStep + 1,
            exception,
            waterSteam?.NodeId ?? string.Empty,
            waterSteam?.SpecificVolumeCubicMetresPerKilogram,
            waterSteam?.SpecificInternalEnergyJoulesPerKilogram,
            exhaust.Phase.ToString(),
            exhaust.Mass.Kilograms,
            exhaust.Volume.CubicMetres / exhaust.Mass.Kilograms,
            exhaust.SpecificInternalEnergy.JoulesPerKilogram,
            exhaust.Pressure.Kilopascals,
            exhaust.Temperature.DegreesCelsius,
            exhaust.VaporQuality?.Fraction,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond,
            condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond,
            condenser.SteamEnergyRemovalRate.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            numerics.Mode.ToString(),
            telemetry?.TriggerObserved ?? false,
            telemetry?.ShadowCorrectionEvaluated ?? false,
            telemetry?.CorrectedCommitAuthorized ?? false,
            telemetry?.CorrectedCandidateCommitted ?? false,
            telemetry?.RollbackRequired ?? false,
            telemetry?.UntargetedBranchDisagreementDetected ?? false,
            telemetry?.BranchOverrideCount ?? 0,
            telemetry?.PreviousPhaseHoldCount ?? 0);
    }

    private static JourneyResult CreateFailedResult(
        string label,
        DesktopHydraulicProductionPolicyDecision decision,
        IReadOnlyList<TraceSample> samples,
        FailureContext failure)
        => new(
            label,
            decision.InitialCondition.InitialConditionId,
            decision.InitialCondition.Version,
            decision.EffectivePolicy.ToString(),
            samples,
            Completed: false,
            Failure: failure.Exception,
            FailureContext: failure);

    private static void WriteArtifacts(JourneyResult explicitV2, JourneyResult correctedV3)
    {
        var results = new[] { explicitV2, correctedV3 };
        var directory = ReportDirectory();

        var matrix = new List<string>
        {
            "label,initial_condition_id,version,policy,completed,successful_steps,failure_type,failure_node,failure_segment,failure_segment_step,attempted_logical_step,candidate_v_m3_kg,candidate_u_j_kg,pre_exhaust_phase,pre_exhaust_mass_kg,pre_exhaust_v_m3_kg,pre_exhaust_u_j_kg,pre_exhaust_kpa,pre_exhaust_c,pre_exhaust_quality,condensation_kg_s,thermal_limit_kg_s,inventory_limit_kg_s,steam_removal_mw,turbine_stage_flow_kg_s,hydraulic_mode,trigger,shadow_evaluated,commit_authorized,committed,rollback,untargeted_disagreement,branch_overrides,previous_phase_holds",
        };
        matrix.AddRange(results.Select(ToOutcomeCsv));
        File.WriteAllLines(Path.Combine(directory, "02-v2-v3-outcome-matrix.csv"), matrix, Utf8WithoutBom);

        var trace = new List<string>
        {
            "label,logical_step,seconds,segment,segment_step,exhaust_phase,exhaust_mass_kg,exhaust_v_m3_kg,exhaust_u_j_kg,exhaust_kpa,exhaust_c,exhaust_quality,condensation_kg_s,thermal_limit_kg_s,inventory_limit_kg_s,steam_removal_mw,turbine_stage_flow_kg_s,request_mwe,gross_mwe,shaft_mw,rotor_rpm,hydraulic_mode,trigger,shadow_evaluated,commit_authorized,committed,rollback,untargeted_disagreement,branch_overrides,previous_phase_holds",
        };
        foreach (var result in results)
        {
            trace.AddRange(result.Samples.Select(sample => ToTraceCsv(result.Label, sample)));
        }
        File.WriteAllLines(Path.Combine(directory, "03-per-step-exhaust-trace.csv"), trace, Utf8WithoutBom);

        var v2Failure = explicitV2.FailureContext;
        var v3Failure = correctedV3.FailureContext;
        var summary = new List<string>
        {
            "=== 01-i5-operational-envelope-exhaust-gap-diagnostic ===",
            "scope=exact reproduction of the OperationalEnvelopeExtendedAudit load raise/lower sequence under desktop @2 and @3; no production/runtime/model/tolerance changes;",
            $"v2-reference={explicitV2.InitialConditionId}@{explicitV2.Version}|{explicitV2.Policy}; completed={explicitV2.Completed}; successful-steps={explicitV2.Samples.Count};",
            v2Failure is null
                ? "v2-failure=NONE;"
                : $"v2-failure={v2Failure.Exception.GetType().Name}; node={v2Failure.NodeId}; segment={v2Failure.Segment}; segment-step={v2Failure.SegmentStep}; attempted-logical-step={v2Failure.AttemptedLogicalStep}; candidate-v-m3-kg={F(v2Failure.CandidateSpecificVolume)}; candidate-u-j-kg={F(v2Failure.CandidateSpecificInternalEnergy)};",
            $"v3-authoritative={correctedV3.InitialConditionId}@{correctedV3.Version}|{correctedV3.Policy}; completed={correctedV3.Completed}; successful-steps={correctedV3.Samples.Count};",
            v3Failure is null
                ? "v3-failure=NONE; diagnostic did not reproduce the reported cumulative blocker;"
                : $"v3-failure={v3Failure.Exception.GetType().Name}; node={v3Failure.NodeId}; segment={v3Failure.Segment}; segment-step={v3Failure.SegmentStep}; attempted-logical-step={v3Failure.AttemptedLogicalStep}; candidate-v-m3-kg={F(v3Failure.CandidateSpecificVolume)}; candidate-u-j-kg={F(v3Failure.CandidateSpecificInternalEnergy)};",
            v3Failure is null
                ? "v3-pre-failure-exhaust=NONE;"
                : $"v3-pre-failure-exhaust=phase:{v3Failure.PreviousExhaustPhase}; mass-kg:{F(v3Failure.PreviousExhaustMassKilograms)}; v-m3-kg:{F(v3Failure.PreviousExhaustSpecificVolume)}; u-j-kg:{F(v3Failure.PreviousExhaustSpecificInternalEnergy)}; p-kpa:{F(v3Failure.PreviousExhaustPressureKilopascals)}; t-c:{F(v3Failure.PreviousExhaustTemperatureCelsius)}; condensation-kg-s:{F(v3Failure.CondensationKilogramsPerSecond)}; turbine-stage-flow-kg-s:{F(v3Failure.TurbineStageFlowKilogramsPerSecond)};",
            v3Failure is null
                ? "v3-pre-failure-corrected-telemetry=NONE;"
                : $"v3-pre-failure-corrected-telemetry=trigger:{v3Failure.TriggerObserved}; shadow-evaluated:{v3Failure.ShadowCorrectionEvaluated}; commit-authorized:{v3Failure.CorrectedCommitAuthorized}; committed:{v3Failure.CorrectedCandidateCommitted}; rollback:{v3Failure.RollbackRequired}; untargeted-disagreement:{v3Failure.UntargetedBranchDisagreementDetected}; branch-overrides:{v3Failure.BranchOverrideCount}; previous-phase-holds:{v3Failure.PreviousPhaseHoldCount};",
            "interpretation=@2 and @3 both reach an exhaust WaterSteamStateOutOfRangeException on the frozen load raise/lower journey; @3 reaches the shared no-root family earlier, so corrected history changes reachability/timing but does not create the underlying thermodynamic correlation gap;",
            "phase-i-status=BLOCKED; gameplay-long-v3-status=separate upstream scheduled-long gate already reached completion when cumulative execution advanced to OperationalEnvelopeAudit; runtime-production-changed=False; thermodynamic-envelope-widened=False; four-node-target-set-changed=False; acceptance-floor-weakened=False;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-operational-envelope-exhaust-gap-diagnostic.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string ToOutcomeCsv(JourneyResult result)
    {
        var f = result.FailureContext;
        return string.Join(",",
            result.Label,
            result.InitialConditionId,
            result.Version,
            result.Policy,
            result.Completed,
            result.Samples.Count,
            f?.Exception.GetType().Name ?? string.Empty,
            f?.NodeId ?? string.Empty,
            f?.Segment ?? string.Empty,
            f?.SegmentStep ?? 0,
            f?.AttemptedLogicalStep ?? 0,
            F(f?.CandidateSpecificVolume),
            F(f?.CandidateSpecificInternalEnergy),
            f?.PreviousExhaustPhase ?? string.Empty,
            F(f?.PreviousExhaustMassKilograms),
            F(f?.PreviousExhaustSpecificVolume),
            F(f?.PreviousExhaustSpecificInternalEnergy),
            F(f?.PreviousExhaustPressureKilopascals),
            F(f?.PreviousExhaustTemperatureCelsius),
            F(f?.PreviousExhaustQuality),
            F(f?.CondensationKilogramsPerSecond),
            F(f?.ThermalLimitKilogramsPerSecond),
            F(f?.InventoryLimitKilogramsPerSecond),
            F(f?.SteamRemovalMegawatts),
            F(f?.TurbineStageFlowKilogramsPerSecond),
            f?.HydraulicMode ?? string.Empty,
            f?.TriggerObserved ?? false,
            f?.ShadowCorrectionEvaluated ?? false,
            f?.CorrectedCommitAuthorized ?? false,
            f?.CorrectedCandidateCommitted ?? false,
            f?.RollbackRequired ?? false,
            f?.UntargetedBranchDisagreementDetected ?? false,
            f?.BranchOverrideCount ?? 0,
            f?.PreviousPhaseHoldCount ?? 0);
    }

    private static string ToTraceCsv(string label, TraceSample sample)
        => string.Join(",",
            label,
            sample.LogicalStep,
            F(sample.LogicalStep / 100d),
            sample.Segment,
            sample.SegmentStep,
            sample.ExhaustPhase,
            F(sample.ExhaustMassKilograms),
            F(sample.ExhaustSpecificVolume),
            F(sample.ExhaustSpecificInternalEnergy),
            F(sample.ExhaustPressureKilopascals),
            F(sample.ExhaustTemperatureCelsius),
            F(sample.ExhaustQuality),
            F(sample.CondensationKilogramsPerSecond),
            F(sample.ThermalLimitKilogramsPerSecond),
            F(sample.InventoryLimitKilogramsPerSecond),
            F(sample.SteamRemovalMegawatts),
            F(sample.TurbineStageFlowKilogramsPerSecond),
            F(sample.RequestMegawatts),
            F(sample.GrossMegawatts),
            F(sample.ShaftMegawatts),
            F(sample.RotorRpm),
            sample.HydraulicMode,
            sample.TriggerObserved,
            sample.ShadowCorrectionEvaluated,
            sample.CorrectedCommitAuthorized,
            sample.CorrectedCandidateCommitted,
            sample.RollbackRequired,
            sample.UntargetedBranchDisagreementDetected,
            sample.BranchOverrideCount,
            sample.PreviousPhaseHoldCount);

    private static string F(double? value)
        => value.HasValue ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-operational-envelope-exhaust-gap-diagnostic");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} I.5 operational-envelope exhaust-gap diagnostic started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static void AppendProgress(string path, string message)
        => File.AppendAllText(
            path,
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed record TraceSample(
        long LogicalStep,
        string Segment,
        int SegmentStep,
        string ExhaustPhase,
        double ExhaustMassKilograms,
        double ExhaustSpecificVolume,
        double ExhaustSpecificInternalEnergy,
        double ExhaustPressureKilopascals,
        double ExhaustTemperatureCelsius,
        double? ExhaustQuality,
        double CondensationKilogramsPerSecond,
        double ThermalLimitKilogramsPerSecond,
        double InventoryLimitKilogramsPerSecond,
        double SteamRemovalMegawatts,
        double TurbineStageFlowKilogramsPerSecond,
        double RequestMegawatts,
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm,
        string HydraulicMode,
        bool TriggerObserved,
        bool ShadowCorrectionEvaluated,
        bool CorrectedCommitAuthorized,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired,
        bool UntargetedBranchDisagreementDetected,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount);

    private sealed record FailureContext(
        string Segment,
        int SegmentStep,
        long AttemptedLogicalStep,
        Exception Exception,
        string NodeId,
        double? CandidateSpecificVolume,
        double? CandidateSpecificInternalEnergy,
        string PreviousExhaustPhase,
        double PreviousExhaustMassKilograms,
        double PreviousExhaustSpecificVolume,
        double PreviousExhaustSpecificInternalEnergy,
        double PreviousExhaustPressureKilopascals,
        double PreviousExhaustTemperatureCelsius,
        double? PreviousExhaustQuality,
        double CondensationKilogramsPerSecond,
        double ThermalLimitKilogramsPerSecond,
        double InventoryLimitKilogramsPerSecond,
        double SteamRemovalMegawatts,
        double TurbineStageFlowKilogramsPerSecond,
        string HydraulicMode,
        bool TriggerObserved,
        bool ShadowCorrectionEvaluated,
        bool CorrectedCommitAuthorized,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired,
        bool UntargetedBranchDisagreementDetected,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount);

    private sealed record JourneyResult(
        string Label,
        string InitialConditionId,
        int Version,
        string Policy,
        IReadOnlyList<TraceSample> Samples,
        bool Completed,
        Exception? Failure,
        FailureContext? FailureContext);
}
