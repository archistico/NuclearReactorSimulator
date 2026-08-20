using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 11 repaired-closure requalification stage 1. This does not activate the repaired closure in any
/// registered initial-condition identity. It applies the validated H.29 1,024-interval control pattern to the Hotfix 10
/// evidence seam under explicit and corrected hydraulics, then classifies how much of the H.13-H.19 continuity machinery
/// is still exercised after the vapor seam is made single-root.
/// </summary>
public sealed class PhaseIThermodynamicRepairRequalificationStage1AuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int QualificationIntervals = 1_024;
    private const int DeterminismIntervals = 256;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicRepairRequalificationStage1")]
    public void RepairedClosure_H29ControlPattern_RequalifiesFailClosedSafetyAndClassifiesContinuityActivity()
    {
        ResetReportDirectory();

        var explicitRun = Run("repair-explicit", useFourNodeCorrectedCommit: false, QualificationIntervals);
        var correctedRun = Run("repair-corrected", useFourNodeCorrectedCommit: true, QualificationIntervals);
        var deterministicFirst = Run("repair-corrected-determinism-a", useFourNodeCorrectedCommit: true, DeterminismIntervals);
        var deterministicSecond = Run("repair-corrected-determinism-b", useFourNodeCorrectedCommit: true, DeterminismIntervals);

        var deterministicRepeat = deterministicFirst.Failure is null
            && deterministicSecond.Failure is null
            && string.Equals(Fingerprint(deterministicFirst.Rows), Fingerprint(deterministicSecond.Rows), StringComparison.Ordinal);

        var metrics = Classify(explicitRun, correctedRun, deterministicRepeat);
        WriteArtifacts(explicitRun, correctedRun, metrics);

        Assert.Null(explicitRun.Failure);
        Assert.Null(correctedRun.Failure);
        Assert.Null(deterministicFirst.Failure);
        Assert.Null(deterministicSecond.Failure);
        Assert.Equal(QualificationIntervals + 2, explicitRun.Rows.Count);
        Assert.Equal(explicitRun.Rows.Count, correctedRun.Rows.Count);
        Assert.True(deterministicRepeat);
        Assert.True(metrics.Stage1SafetyPasses);
    }

    private static RunResult Run(string label, bool useFourNodeCorrectedCommit, int intervals)
    {
        var rows = new List<StepRow>(intervals + 2);
        var runtimeStep = 0;

        try
        {
            var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
                    Step,
                    useFourNodeCorrectedCommit));
            var generatorId = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Running).Electrical.Generators).GeneratorId;

            for (var interval = 1; interval <= intervals; interval++)
            {
                if (interval == 257 && intervals >= 257)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    runtimeStep++;
                    Capture(interval, runtimeStep, isActionTransition: true);
                }
                else if (interval == 769 && intervals >= 769)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    runtimeStep++;
                    Capture(interval, runtimeStep, isActionTransition: true);
                }

                runtimeStep++;
                Capture(interval, runtimeStep, isActionTransition: false);
            }

            return new RunResult(label, useFourNodeCorrectedCommit, rows, null);

            void Capture(int interval, int step, bool isActionTransition)
            {
                var presentation = engine.Step(ControlRoomRunState.Running);
                var numerics = CurrentHydraulics(engine);
                var audit = CurrentAudit(engine);
                var generator = Assert.Single(presentation.Electrical.Generators);
                var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
                var fourNode = useFourNodeCorrectedCommit
                    ? Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity)
                    : null;

                rows.Add(new StepRow(
                    interval,
                    step,
                    isActionTransition,
                    ControlRoomSnapshotFingerprint.Compute(presentation),
                    presentation.AnyTripActive,
                    generator.RequestedElectricalPower.NumericValue ?? double.NaN,
                    generator.ElectricalOutput.NumericValue ?? double.NaN,
                    rotor.ShaftPower.NumericValue ?? double.NaN,
                    rotor.Speed.NumericValue ?? double.NaN,
                    Math.Abs(audit.MassClosureResidualKilograms),
                    Math.Abs(audit.EnergyClosureResidualJoules),
                    Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
                    Math.Abs(audit.BalancePowerResidualWatts),
                    fourNode?.TriggerObserved ?? false,
                    fourNode?.ShadowCorrectionEvaluated ?? false,
                    fourNode?.ProposedAuthority ?? FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
                    fourNode?.Reason ?? FourNodeBranchContinuityActivationReason.NotTriggered,
                    fourNode?.RollbackRequired ?? false,
                    fourNode?.ShadowCorrectedCandidateEligible ?? false,
                    fourNode?.CorrectedCommitArmEnabled ?? false,
                    fourNode?.CorrectedCommitAuthorized ?? false,
                    fourNode?.CorrectedCommitReason ?? FourNodeBranchContinuityCorrectedCommitReason.NotTriggered,
                    fourNode?.CorrectedCandidateCommitted ?? false,
                    fourNode?.UntargetedBranchDisagreementDetected ?? false,
                    fourNode?.BranchOverrideCount ?? 0,
                    fourNode?.PreviousPhaseHoldCount ?? 0,
                    fourNode?.HysteresisReleaseCount ?? 0,
                    fourNode?.ShadowIterationCount ?? 0,
                    fourNode?.ShadowConverged ?? false,
                    fourNode?.ShadowLineSearchExhausted ?? false,
                    fourNode?.ShadowMaximumRelativePressureResidual ?? 0d,
                    fourNode?.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond ?? 0d,
                    Math.Abs(fourNode?.ShadowMassClosureKilogramsPerSecond ?? 0d),
                    Math.Abs(fourNode?.ShadowEnergyOwnershipResidualWatts ?? 0d)));

                if (presentation.AnyTripActive)
                {
                    throw new InvalidOperationException(
                        $"Unexpected trip under repaired closure at {label}, interval {interval}, runtime step {step}.");
                }
            }
        }
        catch (Exception exception)
        {
            return new RunResult(label, useFourNodeCorrectedCommit, rows, exception);
        }
    }

    private static Classification Classify(RunResult explicitRun, RunResult correctedRun, bool deterministicRepeat)
    {
        var corrected = correctedRun.Rows;
        var alignedCount = Math.Min(explicitRun.Rows.Count, corrected.Count);
        var aligned = Enumerable.Range(0, alignedCount)
            .Select(index => new ComparisonRow(explicitRun.Rows[index], corrected[index]))
            .ToArray();

        var triggers = corrected.Count(static row => row.TriggerObserved);
        var eligible = corrected.Count(static row => row.CandidateEligible);
        var authorized = corrected.Count(static row => row.CommitAuthorized);
        var commits = corrected.Count(static row => row.CorrectedCommitted);
        var rollbacks = corrected.Count(static row => row.RollbackRequired);
        var untargeted = corrected.Count(static row => row.UntargetedBranchDisagreement);
        var qualifiedCommitViolations = corrected.Count(static row => row.CorrectedCommitted && !CommitIsQualified(row));
        var rollbackCommitViolations = corrected.Count(static row => row.RollbackRequired && row.CorrectedCommitted);
        var nonTriggerCommitViolations = corrected.Count(static row => !row.TriggerObserved && row.CorrectedCommitted);
        var armDisabledSteps = corrected.Count(static row => !row.CommitArmEnabled);
        var branchOverridesTotal = corrected.Sum(static row => (long)row.BranchOverrideCount);
        var previousPhaseHoldsTotal = corrected.Sum(static row => (long)row.PreviousPhaseHoldCount);
        var hysteresisReleasesTotal = corrected.Sum(static row => (long)row.HysteresisReleaseCount);
        var branchOverridesMaximum = corrected.Count == 0 ? 0 : corrected.Max(static row => row.BranchOverrideCount);
        var previousPhaseHoldsMaximum = corrected.Count == 0 ? 0 : corrected.Max(static row => row.PreviousPhaseHoldCount);
        var hysteresisReleasesMaximum = corrected.Count == 0 ? 0 : corrected.Max(static row => row.HysteresisReleaseCount);

        var conservationPasses = explicitRun.Rows.All(ConservationPasses)
            && corrected.All(ConservationPasses);
        var safetyPasses = explicitRun.Failure is null
            && correctedRun.Failure is null
            && explicitRun.Rows.All(static row => !row.AnyTripActive)
            && corrected.All(static row => !row.AnyTripActive)
            && conservationPasses
            && deterministicRepeat
            && armDisabledSteps == 0
            && untargeted == 0
            && qualifiedCommitViolations == 0
            && rollbackCommitViolations == 0
            && nonTriggerCommitViolations == 0;

        var authorityExercised = triggers > 0 && commits > 0;
        var continuityActivityObserved = branchOverridesTotal > 0 || previousPhaseHoldsTotal > 0 || hysteresisReleasesTotal > 0;

        return new Classification(
            triggers,
            eligible,
            authorized,
            commits,
            rollbacks,
            untargeted,
            qualifiedCommitViolations,
            rollbackCommitViolations,
            nonTriggerCommitViolations,
            armDisabledSteps,
            branchOverridesTotal,
            previousPhaseHoldsTotal,
            hysteresisReleasesTotal,
            branchOverridesMaximum,
            previousPhaseHoldsMaximum,
            hysteresisReleasesMaximum,
            explicitRun.Rows.Count == 0 ? double.NaN : explicitRun.Rows.Max(static row => row.MassClosureResidualKilograms),
            explicitRun.Rows.Count == 0 ? double.NaN : explicitRun.Rows.Max(static row => row.EnergyClosureResidualJoules),
            corrected.Count == 0 ? double.NaN : corrected.Max(static row => row.MassClosureResidualKilograms),
            corrected.Count == 0 ? double.NaN : corrected.Max(static row => row.EnergyClosureResidualJoules),
            aligned.Length == 0 ? double.NaN : aligned.Max(static item => Math.Abs(item.Explicit.GrossMegawatts - item.Corrected.GrossMegawatts)),
            aligned.Length == 0 ? double.NaN : aligned.Max(static item => Math.Abs(item.Explicit.ShaftMegawatts - item.Corrected.ShaftMegawatts)),
            aligned.Length == 0 ? double.NaN : aligned.Max(static item => Math.Abs(item.Explicit.RotorRpm - item.Corrected.RotorRpm)),
            deterministicRepeat,
            authorityExercised,
            continuityActivityObserved,
            safetyPasses,
            aligned);
    }

    private static bool ConservationPasses(StepRow row)
        => row.MassClosureResidualKilograms <= MaximumMassClosureResidualKilograms
            && row.EnergyClosureResidualJoules <= MaximumEnergyClosureResidualJoules
            && row.BalanceMassRateResidualKilogramsPerSecond <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && row.BalancePowerResidualWatts <= MaximumBalancePowerResidualWatts;

    private static bool CommitIsQualified(StepRow row)
        => row.CandidateEligible
            && row.CommitAuthorized
            && !row.RollbackRequired
            && !row.UntargetedBranchDisagreement
            && row.ShadowCorrectionEvaluated
            && row.ShadowConverged
            && !row.ShadowLineSearchExhausted
            && row.ShadowMaximumRelativePressureResidual <= 1e-5d
            && row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond <= 1e-2d
            && row.ShadowMassClosureKilogramsPerSecond <= 1e-8d
            && row.ShadowEnergyOwnershipResidualWatts <= 1e-3d
            && row.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
            && row.ActivationReason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
            && row.CommitReason == FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority;

    private static void WriteArtifacts(RunResult explicitRun, RunResult correctedRun, Classification metrics)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var summary = new[]
        {
            "=== 01-i5-thermodynamic-repair-requalification-stage1 ===",
            "scope=validated Hotfix 10 CorrelationConsistentInverseDomain evidence seam only; H.29 1024-interval control pattern under explicit and corrected hydraulics; no registered exact-version or production-selector activation;",
            $"explicit=completed:{explicitRun.Failure is null}; runtime-steps:{explicitRun.Rows.Count}; failure:{explicitRun.Failure?.GetType().Name ?? "NONE"};",
            $"corrected=completed:{correctedRun.Failure is null}; runtime-steps:{correctedRun.Rows.Count}; failure:{correctedRun.Failure?.GetType().Name ?? "NONE"};",
            FormattableString.Invariant($"corrected-triggers={metrics.Triggers}; eligible={metrics.Eligible}; authorized={metrics.Authorized}; commits={metrics.Commits}; rollbacks={metrics.Rollbacks}; untargeted-disagreements={metrics.UntargetedDisagreements}; qualified-commit-violations={metrics.QualifiedCommitViolations}; rollback-commit-violations={metrics.RollbackCommitViolations}; nontrigger-commit-violations={metrics.NonTriggerCommitViolations}; commit-arm-disabled-steps={metrics.CommitArmDisabledSteps};"),
            FormattableString.Invariant($"branch-overrides-total/max={metrics.BranchOverridesTotal}/{metrics.BranchOverridesMaximum}; previous-phase-holds-total/max={metrics.PreviousPhaseHoldsTotal}/{metrics.PreviousPhaseHoldsMaximum}; hysteresis-releases-total/max={metrics.HysteresisReleasesTotal}/{metrics.HysteresisReleasesMaximum}; continuity-machinery-active={metrics.ContinuityActivityObserved};"),
            FormattableString.Invariant($"max-explicit-corrected-delta=gross:{metrics.MaximumGrossDeltaMegawatts:G17} MW; shaft:{metrics.MaximumShaftDeltaMegawatts:G17} MW; rotor:{metrics.MaximumRotorDeltaRpm:G17} rpm;"),
            FormattableString.Invariant($"max-conservation=explicit-mass:{metrics.MaximumExplicitMassClosure:G17} kg; explicit-energy:{metrics.MaximumExplicitEnergyClosure:G17} J; corrected-mass:{metrics.MaximumCorrectedMassClosure:G17} kg; corrected-energy:{metrics.MaximumCorrectedEnergyClosure:G17} J;"),
            $"deterministic-repeat={metrics.DeterministicRepeat}; corrected-authority-exercised={metrics.AuthorityExercised}; stage1-safety-passes={metrics.Stage1SafetyPasses}; production-activation=False;",
            "interpretation=green proves the repaired thermodynamic closure can traverse the validated H.29 control pattern without trips or conservation/fail-closed violations. Trigger/commit and branch-continuity counts are classification evidence, not retuned acceptance floors; they determine whether repaired H.17/H.19 long-horizon continuity qualification must preserve active hysteresis machinery or can retire part of it as topologically obsolete;",
            "next-step=if stage1 is green, use these counts to build the repaired long-horizon/cross-profile H.17-H.19/H.24 requalification. Do not activate a new exact desktop identity yet and do not rerun cumulative Phase-I closure;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-thermodynamic-repair-requalification-stage1.summary.txt"), summary, Utf8WithoutBom);

        var telemetry = new List<string>
        {
            "interval,runtime_step,is_action_transition,presentation_fingerprint,trip_active,requested_mwe,gross_mwe,shaft_mw,rotor_rpm,mass_closure_kg,energy_closure_j,balance_mass_kg_s,balance_power_w,trigger_observed,shadow_evaluated,proposed_authority,activation_reason,rollback_required,candidate_eligible,commit_arm_enabled,commit_authorized,commit_reason,corrected_committed,untargeted_disagreement,branch_overrides,previous_phase_holds,hysteresis_releases,shadow_iterations,shadow_converged,line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w",
        };
        telemetry.AddRange(correctedRun.Rows.Select(static row => FormattableString.Invariant(
            $"{row.Interval},{row.RuntimeStep},{row.IsActionTransition},{row.PresentationFingerprint},{row.AnyTripActive},{row.RequestedMegawatts:G17},{row.GrossMegawatts:G17},{row.ShaftMegawatts:G17},{row.RotorRpm:G17},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17},{row.TriggerObserved},{row.ShadowCorrectionEvaluated},{row.ProposedAuthority},{row.ActivationReason},{row.RollbackRequired},{row.CandidateEligible},{row.CommitArmEnabled},{row.CommitAuthorized},{row.CommitReason},{row.CorrectedCommitted},{row.UntargetedBranchDisagreement},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount},{row.HysteresisReleaseCount},{row.ShadowIterationCount},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-repaired-corrected-step-telemetry.csv"), telemetry, Utf8WithoutBom);

        var comparison = new List<string>
        {
            "interval,runtime_step,is_action_transition,explicit_gross_mwe,corrected_gross_mwe,abs_gross_delta_mwe,explicit_shaft_mw,corrected_shaft_mw,abs_shaft_delta_mw,explicit_rotor_rpm,corrected_rotor_rpm,abs_rotor_delta_rpm",
        };
        comparison.AddRange(metrics.AlignedRows.Select(static item => FormattableString.Invariant(
            $"{item.Explicit.Interval},{item.Explicit.RuntimeStep},{item.Explicit.IsActionTransition},{item.Explicit.GrossMegawatts:G17},{item.Corrected.GrossMegawatts:G17},{Math.Abs(item.Explicit.GrossMegawatts - item.Corrected.GrossMegawatts):G17},{item.Explicit.ShaftMegawatts:G17},{item.Corrected.ShaftMegawatts:G17},{Math.Abs(item.Explicit.ShaftMegawatts - item.Corrected.ShaftMegawatts):G17},{item.Explicit.RotorRpm:G17},{item.Corrected.RotorRpm:G17},{Math.Abs(item.Explicit.RotorRpm - item.Corrected.RotorRpm):G17}")));
        File.WriteAllLines(Path.Combine(directory, "03-repaired-explicit-vs-corrected-comparison.csv"), comparison, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "04-i5-thermodynamic-repair-requalification-stage1-metrics.csv"),
            new[]
            {
                "metric,value",
                $"qualification_intervals,{QualificationIntervals}",
                $"corrected_triggers,{metrics.Triggers}",
                $"corrected_eligible,{metrics.Eligible}",
                $"corrected_authorized,{metrics.Authorized}",
                $"corrected_commits,{metrics.Commits}",
                $"corrected_rollbacks,{metrics.Rollbacks}",
                $"untargeted_disagreements,{metrics.UntargetedDisagreements}",
                $"branch_overrides_total,{metrics.BranchOverridesTotal}",
                $"previous_phase_holds_total,{metrics.PreviousPhaseHoldsTotal}",
                $"hysteresis_releases_total,{metrics.HysteresisReleasesTotal}",
                $"corrected_authority_exercised,{metrics.AuthorityExercised}",
                $"continuity_machinery_active,{metrics.ContinuityActivityObserved}",
                $"deterministic_repeat,{metrics.DeterministicRepeat}",
                $"stage1_safety_passes,{metrics.Stage1SafetyPasses}",
            },
            Utf8WithoutBom);
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static void QueueGeneratorLoad(IControlRoomRuntimeEngine engine, string generatorId, ControlRoomCommandKind kind)
        => engine.QueueOperatorCommand(new ControlRoomCommand(kind, generatorId, ControlRoomCommandTargetKind.Generator));

    private static string Fingerprint(IReadOnlyList<StepRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.Interval}:{row.RuntimeStep}:{row.IsActionTransition}:{row.PresentationFingerprint}:{row.AnyTripActive}:{row.RequestedMegawatts:G17}:{row.GrossMegawatts:G17}:{row.ShaftMegawatts:G17}:{row.RotorRpm:G17}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}:{row.TriggerObserved}:{row.ShadowCorrectionEvaluated}:{row.ProposedAuthority}:{row.ActivationReason}:{row.RollbackRequired}:{row.CandidateEligible}:{row.CommitArmEnabled}:{row.CommitAuthorized}:{row.CommitReason}:{row.CorrectedCommitted}:{row.UntargetedBranchDisagreement}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.ShadowIterationCount}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-thermodynamic-repair-requalification-stage1");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
    }

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
        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed record StepRow(
        int Interval,
        int RuntimeStep,
        bool IsActionTransition,
        string PresentationFingerprint,
        bool AnyTripActive,
        double RequestedMegawatts,
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts,
        bool TriggerObserved,
        bool ShadowCorrectionEvaluated,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        bool RollbackRequired,
        bool CandidateEligible,
        bool CommitArmEnabled,
        bool CommitAuthorized,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        bool CorrectedCommitted,
        bool UntargetedBranchDisagreement,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        int ShadowIterationCount,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts);

    private sealed record RunResult(
        string Label,
        bool UseFourNodeCorrectedCommit,
        IReadOnlyList<StepRow> Rows,
        Exception? Failure);

    private sealed record ComparisonRow(StepRow Explicit, StepRow Corrected);

    private sealed record Classification(
        int Triggers,
        int Eligible,
        int Authorized,
        int Commits,
        int Rollbacks,
        int UntargetedDisagreements,
        int QualifiedCommitViolations,
        int RollbackCommitViolations,
        int NonTriggerCommitViolations,
        int CommitArmDisabledSteps,
        long BranchOverridesTotal,
        long PreviousPhaseHoldsTotal,
        long HysteresisReleasesTotal,
        int BranchOverridesMaximum,
        int PreviousPhaseHoldsMaximum,
        int HysteresisReleasesMaximum,
        double MaximumExplicitMassClosure,
        double MaximumExplicitEnergyClosure,
        double MaximumCorrectedMassClosure,
        double MaximumCorrectedEnergyClosure,
        double MaximumGrossDeltaMegawatts,
        double MaximumShaftDeltaMegawatts,
        double MaximumRotorDeltaRpm,
        bool DeterministicRepeat,
        bool AuthorityExercised,
        bool ContinuityActivityObserved,
        bool Stage1SafetyPasses,
        IReadOnlyList<ComparisonRow> AlignedRows);
}
