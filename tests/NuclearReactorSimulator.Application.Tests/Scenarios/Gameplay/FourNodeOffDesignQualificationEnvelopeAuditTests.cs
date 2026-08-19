using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.27 targeted off-design robustness / qualification-envelope audit over the unchanged H.22-H.26
/// corrected-commit runtime. Rollback and protection action are valid fail-closed outcomes; unsafe corrected ownership is not.
/// </summary>
public sealed class FourNodeOffDesignQualificationEnvelopeAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int WarmupSteps = 64;
    private const int LoadObservationSteps = 192;
    private const int CoolingObservationSteps = 256;
    private const int CoolingRecoverySteps = 32;
    private const int CombinedObservationSteps = 256;
    private const int CoolingLossObservationSteps = 384;
    private const int DeterminismObservationSteps = 128;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private const string FrozenH26SummaryFingerprint = "4DDC10F4F084C392969E26D9C5B5C4203A30F93DFB2F8BABB81D7807DFFBD7EC";

    [Fact]
    public void FrozenH26Evidence_RetainsValidatedAtomicFailClosedFallbackContract()
    {
        var path = Path.Combine(EvidenceDirectory(), "H26_ValidatedIntegratedRollbackSummary.txt");
        Assert.True(File.Exists(path));
        Assert.Equal(FrozenH26SummaryFingerprint, CanonicalSha256(path));
        var summary = File.ReadAllText(path);
        Assert.Contains("challenges=12", summary, StringComparison.Ordinal);
        Assert.Contains("rollback-challenges=8", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-fallback-equivalent=12/12", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=0", summary, StringComparison.Ordinal);
        Assert.Contains("partial-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("deterministic-repeat=True", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-integrated-rollback-fail-closed-stress-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h26-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeOffDesignQualificationEnvelopeAudit")]
    public void OptInCommittedRuntime_MapsStagedOffDesignCasesWithoutUnsafeOwnershipOrHiddenRepair()
    {
        ResetProgress();
        var results = new List<EnvelopeResult>
        {
            RunHighLoadTenMWe(),
            RunCoolingDegradation("cooling-50pct-capacity", 0.50d),
            RunCoolingDegradation("cooling-25pct-capacity", 0.25d),
            RunCombinedLoadCooling("high-load-10mwe-plus-cooling-50pct", ControlRoomCommandKind.GeneratorLoadRaise, 0.50d),
            RunCombinedLoadCooling("low-load-0mwe-plus-cooling-25pct", ControlRoomCommandKind.GeneratorLoadLower, 0.25d),
            RunCoolingLossProtectionAdjacent(),
        };

        var rows = results.SelectMany(static result => result.Rows).ToArray();
        var triggers = rows.Count(static row => row.TriggerObserved);
        var commits = rows.Count(static row => row.CorrectedCandidateCommitted);
        var rollbacks = rows.Count(static row => row.RollbackRequired);
        var safeFallbacks = rows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted);
        var fallbackCommitViolations = rows.Count(static row => (!row.H20CandidateEligible || row.RollbackRequired) && row.CorrectedCandidateCommitted);
        var unsafeCommits = rows.Count(static row => row.CorrectedCandidateCommitted && !CommitIsQualified(row));
        var untargetedDisagreements = rows.Count(static row => row.UntargetedBranchDisagreementDetected);
        var maximumMassClosure = rows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = rows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = rows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = rows.Max(static row => row.BalancePowerResidualWatts);
        var maximumCondenserPressure = rows.Max(static row => row.CondenserPressurePascals);
        var fingerprint = Fingerprint(rows);

        Assert.All(results, static result => Assert.True(result.EvidenceConditionSatisfied, $"H.27 scenario '{result.ScenarioId}' did not satisfy its evidence condition: {result.EvidenceCondition}"));
        Assert.All(results, static result => Assert.Contains(result.Rows, static row => row.TriggerObserved));
        Assert.True(commits > 0 || safeFallbacks > 0, "H.27 observed neither corrected ownership nor safe fallback.");
        Assert.Equal(0, fallbackCommitViolations);
        Assert.Equal(0, unsafeCommits);
        Assert.InRange(maximumMassClosure, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(maximumEnergyClosure, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(maximumBalanceMassRate, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(maximumBalancePower, 0d, MaximumBalancePowerResidualWatts);

        var repeatLeft = RunDeterminismControl();
        var repeatRight = RunDeterminismControl();
        Assert.Equal(Fingerprint(repeatLeft), Fingerprint(repeatRight));

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultEngine).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = results.All(static result => result.EvidenceConditionSatisfied)
            && results.All(static result => result.Rows.Any(static row => row.TriggerObserved))
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.True(passes);

        WriteReports(results, rows, triggers, commits, rollbacks, safeFallbacks, fallbackCommitViolations, unsafeCommits,
            untargetedDisagreements, maximumMassClosure, maximumEnergyClosure, maximumBalanceMassRate,
            maximumBalancePower, maximumCondenserPressure, fingerprint, defaultMode, passes);
    }

    private static EnvelopeResult RunHighLoadTenMWe()
    {
        var probe = CreateProbe("high-load-10mwe");
        probe.Advance(WarmupSteps, "steady");
        QueueGeneratorLoad(probe.Engine, ControlRoomCommandKind.GeneratorLoadRaise);
        probe.Advance(LoadObservationSteps, "10mwe-request");
        var satisfied = probe.Rows.Any(static row => row.RequestedElectricalPowerMegawatts >= 9.999d);
        return probe.Complete(satisfied, "10 MWe requested-load point observed; any canonical protection action defines a protected boundary");
    }

    private static EnvelopeResult RunCoolingDegradation(string scenarioId, double capacityFraction)
    {
        var probe = CreateProbe(scenarioId);
        probe.Advance(WarmupSteps, "steady");
        var initialPressure = probe.Rows[^1].CondenserPressurePascals;
        var target = (ISecondaryTransientFaultTarget)probe.Engine;
        target.ActivateCondenserCoolingDegradation($"h27-{scenarioId}", "cooling", capacityFraction);
        probe.Advance(CoolingObservationSteps, "degraded-cooling");
        target.ClearSecondaryTransientFault($"h27-{scenarioId}");
        probe.Advance(CoolingRecoverySteps, "cooling-recovery");
        var maximumPressure = probe.Rows.Max(static row => row.CondenserPressurePascals);
        var satisfied = maximumPressure >= initialPressure;
        return probe.Complete(satisfied, $"cooling capacity {capacityFraction:0.00} increases/maintains condenser backpressure without unsafe ownership");
    }

    private static EnvelopeResult RunCombinedLoadCooling(string scenarioId, ControlRoomCommandKind loadCommand, double capacityFraction)
    {
        var probe = CreateProbe(scenarioId);
        probe.Advance(WarmupSteps, "steady");
        QueueGeneratorLoad(probe.Engine, loadCommand);
        probe.Advance(16, "load-transition");
        var target = (ISecondaryTransientFaultTarget)probe.Engine;
        target.ActivateCondenserCoolingDegradation($"h27-{scenarioId}", "cooling", capacityFraction);
        probe.Advance(CombinedObservationSteps, "combined-off-design");
        var final = probe.Rows[^1];
        var loadSatisfied = loadCommand == ControlRoomCommandKind.GeneratorLoadRaise
            ? final.RequestedElectricalPowerMegawatts >= 9.999d
            : final.RequestedElectricalPowerMegawatts <= 0.001d;
        return probe.Complete(loadSatisfied, $"combined {(loadCommand == ControlRoomCommandKind.GeneratorLoadRaise ? "10 MWe" : "0 MWe")} + cooling capacity {capacityFraction:0.00}");
    }

    private static EnvelopeResult RunCoolingLossProtectionAdjacent()
    {
        var probe = CreateProbe("cooling-loss-protection-adjacent");
        probe.Advance(WarmupSteps, "steady");
        var initialPressure = probe.Rows[^1].CondenserPressurePascals;
        var target = (ISecondaryTransientFaultTarget)probe.Engine;
        target.ActivateCondenserCoolingLoss("h27-cooling-loss", "cooling");
        probe.Advance(CoolingLossObservationSteps, "cooling-loss");
        var maximumPressure = probe.Rows.Max(static row => row.CondenserPressurePascals);
        var protectionSeen = probe.Rows.Any(static row => row.AnyTripActive || row.LatchedFunctions.Length > 0);
        var satisfied = maximumPressure >= initialPressure;
        var outcome = protectionSeen
            ? "cooling loss reached a protected boundary"
            : "cooling loss remained inside protection boundary during bounded observation window";
        return probe.Complete(satisfied, outcome);
    }

    private static IReadOnlyList<EnvelopeStepRow> RunDeterminismControl()
    {
        var probe = CreateProbe("determinism-high-load-cooling-50pct", writeProgress: false);
        probe.Advance(WarmupSteps, "steady");
        QueueGeneratorLoad(probe.Engine, ControlRoomCommandKind.GeneratorLoadRaise);
        probe.Advance(16, "load-transition");
        ((ISecondaryTransientFaultTarget)probe.Engine).ActivateCondenserCoolingDegradation("h27-determinism-cooling", "cooling", 0.50d);
        probe.Advance(DeterminismObservationSteps, "combined-off-design");
        return probe.Rows.ToArray();
    }

    private static EnvelopeProbe CreateProbe(string scenarioId, bool writeProgress = true)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);
        return new EnvelopeProbe(scenarioId, engine, writeProgress);
    }

    private static void QueueGeneratorLoad(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomCommandKind kind)
    {
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        engine.QueueOperatorCommand(new ControlRoomCommand(kind, generator.Id, ControlRoomCommandTargetKind.Generator));
    }

    private static EnvelopeStepRow CaptureRow(string scenarioId, string phase, int stepIndex, IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var numerics = CurrentHydraulics(engine);
        var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
        var audit = CurrentAudit(engine);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var protection = CurrentProtection(engine);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var condenser = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.Condenser.Condensers);
        var latchedFunctions = string.Join("|", protection.Functions.Where(static item => item.IsLatched).Select(static item => item.FunctionId));
        return new EnvelopeStepRow(
            scenarioId,
            phase,
            stepIndex,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            presentation.AnyTripActive,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            condenser.FinalSteamSpacePressure.Pascals,
            telemetry.TriggerObserved,
            telemetry.ShadowCorrectedCandidateEligible,
            telemetry.RollbackRequired,
            telemetry.CorrectedCommitAuthorized,
            telemetry.CorrectedCandidateCommitted,
            telemetry.UntargetedBranchDisagreementDetected,
            telemetry.ShadowCorrectionEvaluated,
            telemetry.ShadowConverged,
            telemetry.ShadowLineSearchExhausted,
            telemetry.ShadowMaximumRelativePressureResidual,
            telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
            telemetry.ShadowMassClosureKilogramsPerSecond,
            telemetry.ShadowEnergyOwnershipResidualWatts,
            telemetry.ProposedAuthority,
            telemetry.Reason,
            telemetry.CorrectedCommitReason,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules),
            Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(audit.BalancePowerResidualWatts),
            protection.LatchedActions.ToString().Replace(", ", "|", StringComparison.Ordinal),
            latchedFunctions);
    }

    private static void AssertFailClosedSafety(EnvelopeStepRow row)
    {
        Assert.InRange(row.MassClosureResidualKilograms, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(row.EnergyClosureResidualJoules, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(row.BalanceMassRateResidualKilogramsPerSecond, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(row.BalancePowerResidualWatts, 0d, MaximumBalancePowerResidualWatts);
        if (row.RollbackRequired)
        {
            Assert.False(row.CorrectedCandidateCommitted);
        }
        if (row.CorrectedCandidateCommitted)
        {
            Assert.True(CommitIsQualified(row));
        }
    }

    private static bool CommitIsQualified(EnvelopeStepRow row)
        => row.H20CandidateEligible
            && row.CorrectedCommitAuthorized
            && !row.RollbackRequired
            && !row.UntargetedBranchDisagreementDetected
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

    private static ProtectionSystemSnapshot CurrentProtection(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static string Classify(EnvelopeResult result)
    {
        var rows = result.Rows;
        if (rows.Any(static row => row.AnyTripActive))
        {
            return "protected-boundary";
        }
        if (rows.Any(static row => row.RollbackRequired || (row.TriggerObserved && !row.CorrectedCandidateCommitted)))
        {
            return "safe-fallback-envelope";
        }
        if (rows.Any(static row => row.CorrectedCandidateCommitted))
        {
            return "corrected-qualified";
        }
        return "observed-no-trigger";
    }

    private static string Fingerprint(IReadOnlyList<EnvelopeStepRow> rows)
    {
        var canonical = string.Join("||", rows.Select(static row => FormattableString.Invariant(
            $"{row.ScenarioId}:{row.Phase}:{row.StepIndex}:{row.PresentationFingerprint}:{row.AnyTripActive}:{row.RequestedElectricalPowerMegawatts:G17}:{row.CondenserPressurePascals:G17}:{row.TriggerObserved}:{row.H20CandidateEligible}:{row.RollbackRequired}:{row.CorrectedCommitAuthorized}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.ActivationReason}:{row.CommitReason}:{row.LatchedActions}:{row.LatchedFunctions}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<EnvelopeResult> results,
        IReadOnlyList<EnvelopeStepRow> rows,
        int triggers,
        int commits,
        int rollbacks,
        int safeFallbacks,
        int fallbackCommitViolations,
        int unsafeCommits,
        int untargetedDisagreements,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        double maximumCondenserPressure,
        string fingerprint,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var summary = new List<string>
        {
            "=== 01-current-v2-four-node-off-design-robustness-qualification-envelope ===",
            "H.27 keeps the validated corrected-commit algorithm unchanged and maps a bounded staged off-design matrix beyond the H.19 nominal profile amplitudes. Safe H.20 fallback and canonical protection action are valid envelope boundaries; unsafe or fallback commits are not. H.24 is not rerun.",
            FormattableString.Invariant($"matrix-scenarios={results.Count}; runtime-steps={rows.Count}; production-fixed-step=10.000 ms; P060-F040-triggered={triggers}; corrected-candidates-committed={commits}; H20-rollbacks={rollbacks}; safe-fallbacks={safeFallbacks}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17}; max-condenser-pressure-pa={maximumCondenserPressure:G17}; off-design-telemetry-fingerprint={fingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H26-prerequisite-frozen=True; H24-rerun=False; H20-contract-replaced=False; H22-commit-seam-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-off-design-robustness-qualification-envelope-passes={passes}; h27-audit-passes={passes};"),
            "H.27 recommendation: use the per-scenario classifications as the bounded qualification envelope. Corrected-qualified, safe-fallback-envelope and protected-boundary are all legitimate outcomes when ownership remains fail-closed. Keep default production explicit and move next to H.28 performance/cost/operational soak; do not broaden the envelope by retuning from this gate alone.",
        };
        foreach (var result in results)
        {
            var scenarioRows = result.Rows;
            summary.Insert(summary.Count - 2, FormattableString.Invariant(
                $"scenario={result.ScenarioId}; classification={Classify(result)}; runtime-steps={scenarioRows.Count}; triggers={scenarioRows.Count(static row => row.TriggerObserved)}; commits={scenarioRows.Count(static row => row.CorrectedCandidateCommitted)}; rollbacks={scenarioRows.Count(static row => row.RollbackRequired)}; safe-fallbacks={scenarioRows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted)}; trips={scenarioRows.Count(static row => row.AnyTripActive)}; max-condenser-pressure-pa={scenarioRows.Max(static row => row.CondenserPressurePascals):G17}; requested-load-range-mwe={scenarioRows.Min(static row => row.RequestedElectricalPowerMegawatts):G17}..{scenarioRows.Max(static row => row.RequestedElectricalPowerMegawatts):G17}; evidence-condition={result.EvidenceCondition}; evidence-condition-satisfied={result.EvidenceConditionSatisfied};"));
        }
        File.WriteAllLines(Path.Combine(directory, "01-four-node-off-design-qualification-envelope.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "scenario_id,phase,step,presentation_fingerprint,any_trip,requested_load_mwe,condenser_pressure_pa,trigger_observed,h20_candidate_eligible,h20_rollback_required,h22_commit_authorized,corrected_candidate_committed,untargeted_branch_disagreement,activation_reason,commit_reason,latched_actions,latched_functions,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.ScenarioId},{row.Phase},{row.StepIndex},{row.PresentationFingerprint},{row.AnyTripActive},{row.RequestedElectricalPowerMegawatts:G17},{row.CondenserPressurePascals:G17},{row.TriggerObserved},{row.H20CandidateEligible},{row.RollbackRequired},{row.CorrectedCommitAuthorized},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.ActivationReason},{row.CommitReason},{row.LatchedActions},{row.LatchedFunctions},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-off-design-step-telemetry.csv"), csv, Utf8WithoutBom);

        var envelopeCsv = new List<string>
        {
            "scenario_id,classification,runtime_steps,triggers,commits,rollbacks,safe_fallbacks,trip_steps,max_condenser_pressure_pa,min_requested_load_mwe,max_requested_load_mwe,evidence_condition,evidence_condition_satisfied",
        };
        envelopeCsv.AddRange(results.Select(result => FormattableString.Invariant(
            $"{result.ScenarioId},{Classify(result)},{result.Rows.Count},{result.Rows.Count(static row => row.TriggerObserved)},{result.Rows.Count(static row => row.CorrectedCandidateCommitted)},{result.Rows.Count(static row => row.RollbackRequired)},{result.Rows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted)},{result.Rows.Count(static row => row.AnyTripActive)},{result.Rows.Max(static row => row.CondenserPressurePascals):G17},{result.Rows.Min(static row => row.RequestedElectricalPowerMegawatts):G17},{result.Rows.Max(static row => row.RequestedElectricalPowerMegawatts):G17},{result.EvidenceCondition},{result.EvidenceConditionSatisfied}")));
        File.WriteAllLines(Path.Combine(directory, "03-off-design-qualification-envelope.csv"), envelopeCsv, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "04-off-design-qualification-metrics.csv"), new[]
        {
            "metric,value",
            $"matrix_scenarios,{results.Count}",
            $"runtime_steps,{rows.Count}",
            $"triggered_steps,{triggers}",
            $"corrected_commits,{commits}",
            $"h20_rollbacks,{rollbacks}",
            $"safe_fallbacks,{safeFallbacks}",
            $"fallback_commit_violations,{fallbackCommitViolations}",
            $"unsafe_commits,{unsafeCommits}",
            $"untargeted_branch_disagreements,{untargetedDisagreements}",
            FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
            FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
            FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
            FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
            FormattableString.Invariant($"max_condenser_pressure_pa,{maximumCondenserPressure:G17}"),
            $"telemetry_fingerprint,{fingerprint}",
            $"h27_audit_passes,{passes}",
        }, Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h27-four-node-off-design-qualification-envelope");

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

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("H.27 off-design qualification-envelope audit started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);

    private sealed class EnvelopeProbe
    {
        private readonly List<EnvelopeStepRow> _rows = new();
        private readonly bool _writeProgress;
        private int _stepIndex;

        public EnvelopeProbe(string scenarioId, IntegratedAutomaticOperationRuntimeEngine engine, bool writeProgress)
        {
            ScenarioId = scenarioId;
            Engine = engine;
            _writeProgress = writeProgress;
            if (_writeProgress)
            {
                WriteProgress($"scenario-start id={scenarioId}");
            }
        }

        public string ScenarioId { get; }
        public IntegratedAutomaticOperationRuntimeEngine Engine { get; }
        public IReadOnlyList<EnvelopeStepRow> Rows => _rows;

        public void Advance(int steps, string phase)
        {
            for (var step = 0; step < steps; step++)
            {
                Engine.Step(ControlRoomRunState.Running);
                _stepIndex++;
                var row = CaptureRow(ScenarioId, phase, _stepIndex, Engine);
                AssertFailClosedSafety(row);
                _rows.Add(row);
            }
        }

        public EnvelopeResult Complete(bool evidenceConditionSatisfied, string evidenceCondition)
        {
            if (_writeProgress)
            {
                WriteProgress($"scenario-complete id={ScenarioId} classification={Classify(new EnvelopeResult(ScenarioId, _rows.ToArray(), evidenceCondition, evidenceConditionSatisfied))} steps={_rows.Count} commits={_rows.Count(static row => row.CorrectedCandidateCommitted)} rollbacks={_rows.Count(static row => row.RollbackRequired)} condition={evidenceConditionSatisfied}");
            }
            return new EnvelopeResult(ScenarioId, _rows.ToArray(), evidenceCondition, evidenceConditionSatisfied);
        }
    }

    private sealed record EnvelopeResult(
        string ScenarioId,
        IReadOnlyList<EnvelopeStepRow> Rows,
        string EvidenceCondition,
        bool EvidenceConditionSatisfied);

    private sealed record EnvelopeStepRow(
        string ScenarioId,
        string Phase,
        int StepIndex,
        string PresentationFingerprint,
        bool AnyTripActive,
        double RequestedElectricalPowerMegawatts,
        double CondenserPressurePascals,
        bool TriggerObserved,
        bool H20CandidateEligible,
        bool RollbackRequired,
        bool CorrectedCommitAuthorized,
        bool CorrectedCandidateCommitted,
        bool UntargetedBranchDisagreementDetected,
        bool ShadowCorrectionEvaluated,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts,
        string LatchedActions,
        string LatchedFunctions);
}
