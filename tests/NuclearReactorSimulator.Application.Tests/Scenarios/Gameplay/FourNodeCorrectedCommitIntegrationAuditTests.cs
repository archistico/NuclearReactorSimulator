using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.22 first opt-in corrected-candidate ownership audit. The unchanged H.20 decision remains the
/// eligibility authority; H.22 adds a second fail-closed commit seam and keeps every standard factory explicit.
/// </summary>
public sealed class FourNodeCorrectedCommitIntegrationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 2_000;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH21Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H21_ValidatedOrchestratorShadowIntegrationSummary.txt"] = "A4DEA28667A88E58E9F2C2BCB11C8DE2B7D55E2AC1B410883B853C8AD200E0EE",
            ["H21_ValidatedOrchestratorShadowIntegrationTelemetry.csv"] = "EE2ABC69262EFBFF9FA6B6A82C812FE80C3137871E3C6541CE5DEC75EA9424B2",
            ["H21_ValidatedOrchestratorShadowIntegrationMetrics.csv"] = "02B1E2F51470BA5F1DD254251849AFB9657AEA8D4E19A5F601EC5ED2C22B42CD",
        };

    [Fact]
    public void FrozenH21Evidence_RetainsValidatedTrajectoryTransparentOrchestratorWiring()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH21Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.21 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H21_ValidatedOrchestratorShadowIntegrationSummary.txt"));
        Assert.Contains("explicit-vs-shadow-integrated-presentation-equivalent=2000/2000", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidate-eligible=15/15", summary, StringComparison.Ordinal);
        Assert.Contains("rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=0", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-orchestrator-shadow-integration-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h21-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeCorrectedCommitIntegrationAudit")]
    public void OptInCorrectedCommitRuntime_CommitsOnlyH20QualifiedCandidatesAndFailsClosedOtherwise()
    {
        ResetProgress();
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        var repeatEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));

        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(repeatEngine).Mode);

        var seedTelemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(engine).FourNodeBranchContinuity);
        var repeatSeedTelemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(repeatEngine).FourNodeBranchContinuity);
        Assert.True(seedTelemetry.CorrectedCommitArmEnabled);
        Assert.True(repeatSeedTelemetry.CorrectedCommitArmEnabled);
        Assert.Equal(seedTelemetry.CorrectedCommitAuthorized, seedTelemetry.CorrectedCandidateCommitted);
        Assert.Equal(repeatSeedTelemetry.CorrectedCommitAuthorized, repeatSeedTelemetry.CorrectedCandidateCommitted);
        var seedCommitObserved = seedTelemetry.CorrectedCandidateCommitted;

        var rows = new List<StepTelemetryRow>(IntervalCount);
        var repeatRows = new List<StepTelemetryRow>(IntervalCount);
        var presentationRepeatEquivalent = 0;

        for (var interval = 1; interval <= IntervalCount; interval++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            var repeatPresentation = repeatEngine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected H.22 trip at interval {interval}.");
            Assert.False(repeatPresentation.AnyTripActive, $"Unexpected H.22 repeat trip at interval {interval}.");

            var presentationFingerprint = ControlRoomSnapshotFingerprint.Compute(presentation);
            var repeatPresentationFingerprint = ControlRoomSnapshotFingerprint.Compute(repeatPresentation);
            if (string.Equals(presentationFingerprint, repeatPresentationFingerprint, StringComparison.Ordinal))
            {
                presentationRepeatEquivalent++;
            }
            Assert.Equal(presentationFingerprint, repeatPresentationFingerprint);

            var numerics = CurrentHydraulics(engine);
            var repeatNumerics = CurrentHydraulics(repeatEngine);
            Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, numerics.Mode);
            Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, repeatNumerics.Mode);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            var repeatTelemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(repeatNumerics.FourNodeBranchContinuity);
            Assert.True(telemetry.CorrectedCommitArmEnabled);
            Assert.True(repeatTelemetry.CorrectedCommitArmEnabled);
            Assert.Equal(telemetry.CorrectedCommitAuthorized, telemetry.CorrectedCandidateCommitted);
            Assert.Equal(repeatTelemetry.CorrectedCommitAuthorized, repeatTelemetry.CorrectedCandidateCommitted);
            Assert.Equal(telemetry.CorrectedCandidateCommitted, numerics.UsedSemiImplicitCorrection);
            Assert.Equal(repeatTelemetry.CorrectedCandidateCommitted, repeatNumerics.UsedSemiImplicitCorrection);

            if (!telemetry.TriggerObserved)
            {
                Assert.False(telemetry.CorrectedCandidateCommitted);
                Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, telemetry.Reason);
                Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.NotTriggered, telemetry.CorrectedCommitReason);
            }

            if (telemetry.CorrectedCandidateCommitted)
            {
                Assert.True(telemetry.ShadowCorrectionEvaluated);
                Assert.True(telemetry.ShadowCorrectedCandidateEligible);
                Assert.False(telemetry.RollbackRequired);
                Assert.Equal(FourNodeBranchContinuityProposedAuthority.CorrectedCandidate, telemetry.ProposedAuthority);
                Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, telemetry.Reason);
                Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority, telemetry.CorrectedCommitReason);
                Assert.True(telemetry.ShadowConverged);
                Assert.False(telemetry.ShadowLineSearchExhausted);
                Assert.InRange(telemetry.ShadowMaximumRelativePressureResidual, 0d, 1e-5d);
                Assert.InRange(telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond, 0d, 1e-2d);
                Assert.InRange(telemetry.ShadowMassClosureKilogramsPerSecond, 0d, 1e-8d);
                Assert.InRange(telemetry.ShadowEnergyOwnershipResidualWatts, 0d, 1e-3d);
            }

            var audit = CurrentAudit(engine);
            var repeatAudit = CurrentAudit(repeatEngine);
            Assert.InRange(Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond), 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
            Assert.InRange(Math.Abs(audit.MassClosureResidualKilograms), 0d, MaximumMassClosureResidualKilograms);
            Assert.InRange(Math.Abs(audit.BalancePowerResidualWatts), 0d, MaximumBalancePowerResidualWatts);
            Assert.InRange(Math.Abs(audit.EnergyClosureResidualJoules), 0d, MaximumEnergyClosureResidualJoules);

            rows.Add(ToRow(interval, presentationFingerprint, numerics, telemetry, audit));
            repeatRows.Add(ToRow(interval, repeatPresentationFingerprint, repeatNumerics, repeatTelemetry, repeatAudit));

            if (interval % 250 == 0 || interval == IntervalCount)
            {
                WriteProgress($"corrected-commit-progress interval={interval}/{IntervalCount}");
            }
        }

        var triggered = rows.Count(static row => row.TriggerObserved);
        var eligible = rows.Count(static row => row.ShadowCorrectedCandidateEligible);
        var authorized = rows.Count(static row => row.CorrectedCommitAuthorized);
        var commits = rows.Count(static row => row.CorrectedCandidateCommitted);
        var rollbacks = rows.Count(static row => row.RollbackRequired);
        var untargetedDisagreements = rows.Count(static row => row.UntargetedBranchDisagreementDetected);
        var fallbackIntervals = rows.Count(static row => row.TriggerObserved && !row.ShadowCorrectedCandidateEligible);
        var fallbackCommitViolations = rows.Count(static row => !row.ShadowCorrectedCandidateEligible && row.CorrectedCandidateCommitted);
        var unsafeCommits = rows.Count(static row => row.CorrectedCandidateCommitted
            && (row.RollbackRequired
                || row.UntargetedBranchDisagreementDetected
                || !row.ShadowConverged
                || row.ShadowLineSearchExhausted
                || row.ShadowMaximumRelativePressureResidual > 1e-5d
                || row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond > 1e-2d
                || row.ShadowMassClosureKilogramsPerSecond > 1e-8d
                || row.ShadowEnergyOwnershipResidualWatts > 1e-3d));
        var maximumMassClosure = rows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = rows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = rows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = rows.Max(static row => row.BalancePowerResidualWatts);
        var telemetryFingerprint = Fingerprint(rows);
        var repeatFingerprint = Fingerprint(repeatRows);
        var deterministicRepeat = string.Equals(telemetryFingerprint, repeatFingerprint, StringComparison.Ordinal);

        Assert.True(triggered > 0, "H.22 control run observed no P060/F040 trigger.");
        Assert.True(commits > 0, "H.22 opt-in commit seam never committed a corrected candidate.");
        Assert.Equal(eligible, authorized);
        Assert.Equal(authorized, commits);
        Assert.Equal(0, fallbackCommitViolations);
        Assert.Equal(0, unsafeCommits);
        Assert.Equal(IntervalCount, presentationRepeatEquivalent);
        Assert.True(deterministicRepeat);

        var defaultFactory = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultFactory).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = triggered > 0
            && commits > 0
            && eligible == authorized
            && authorized == commits
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && presentationRepeatEquivalent == IntervalCount
            && deterministicRepeat
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.True(passes);

        WriteReports(
            rows,
            seedCommitObserved,
            triggered,
            eligible,
            authorized,
            commits,
            rollbacks,
            untargetedDisagreements,
            fallbackIntervals,
            fallbackCommitViolations,
            unsafeCommits,
            presentationRepeatEquivalent,
            deterministicRepeat,
            telemetryFingerprint,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            defaultMode,
            passes);
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static StepTelemetryRow ToRow(
        int interval,
        string presentationFingerprint,
        PlantNetworkHydraulicNumericalSnapshot numerics,
        FourNodeBranchContinuityIntegrationTelemetry telemetry,
        PlantNetworkAudit audit)
        => new(
            interval,
            presentationFingerprint,
            telemetry.TriggerObserved,
            numerics.PredictorMaximumFractionalSubcooledPressureChange,
            numerics.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
            telemetry.ShadowCorrectionEvaluated,
            telemetry.ProposedAuthority,
            telemetry.Reason,
            telemetry.RollbackRequired,
            telemetry.ShadowCorrectedCandidateEligible,
            telemetry.CorrectedCommitArmEnabled,
            telemetry.CorrectedCommitAuthorized,
            telemetry.CorrectedCommitReason,
            telemetry.CorrectedCandidateCommitted,
            telemetry.UntargetedBranchDisagreementDetected,
            telemetry.BranchOverrideCount,
            telemetry.PreviousPhaseHoldCount,
            telemetry.HysteresisReleaseCount,
            telemetry.ShadowIterationCount,
            telemetry.ShadowConverged,
            telemetry.ShadowLineSearchExhausted,
            telemetry.ShadowMaximumRelativePressureResidual,
            telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
            telemetry.ShadowMassClosureKilogramsPerSecond,
            telemetry.ShadowEnergyOwnershipResidualWatts,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules),
            Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(audit.BalancePowerResidualWatts));

    private static string Fingerprint(IReadOnlyList<StepTelemetryRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.Interval}:{row.PresentationFingerprint}:{row.TriggerObserved}:{row.PredictorPressureChange:G17}:{row.PredictorFlowChangeKilogramsPerSecond:G17}:{row.ShadowCorrectionEvaluated}:{row.ProposedAuthority}:{row.Reason}:{row.RollbackRequired}:{row.ShadowCorrectedCandidateEligible}:{row.CorrectedCommitArmEnabled}:{row.CorrectedCommitAuthorized}:{row.CorrectedCommitReason}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.ShadowIterationCount}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<StepTelemetryRow> rows,
        bool seedCommitObserved,
        int triggered,
        int eligible,
        int authorized,
        int commits,
        int rollbacks,
        int untargetedDisagreements,
        int fallbackIntervals,
        int fallbackCommitViolations,
        int unsafeCommits,
        int presentationRepeatEquivalent,
        bool deterministicRepeat,
        string telemetryFingerprint,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var summary = new[]
        {
            "=== 01-current-v2-four-node-corrected-candidate-commit-seam ===",
            "H.22 adds the first separately opt-in corrected-candidate ownership seam. The H.20 authority decision is unchanged and remains the eligibility gate; any non-qualified or rollback decision falls through immediately to the historical explicit candidate. Standard current-v2 factories remain ExplicitCommittedState.",
            FormattableString.Invariant($"intervals={rows.Count}; production-fixed-step=10.000 ms; seed-preconditioning-commit-observed={seedCommitObserved}; P060-F040-triggered={triggered}; H20-candidate-eligible={eligible}; H22-commit-authorized={authorized}; corrected-candidates-committed={commits};"),
            FormattableString.Invariant($"H20-rollbacks={rollbacks}; fallback-intervals={fallbackIntervals}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"repeat-presentation-equivalent={presentationRepeatEquivalent}/{rows.Count}; deterministic-repeat={deterministicRepeat}; telemetry-fingerprint={telemetryFingerprint};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H20-contract-replaced=False; H20-production-commit-authorized-property-changed=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-corrected-candidate-commit-seam-passes={passes}; h22-audit-passes={passes};"),
            "H.22 recommendation: treat this as commit-seam evidence only. Keep default current-v2 explicit. Before any production-default activation, qualify the opt-in committed trajectory through deterministic replay, protection interactions, long-horizon/cross-profile operation and off-design robustness while preserving immediate H.20 fallback telemetry.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-four-node-corrected-commit-seam.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "interval,presentation_fingerprint,trigger_observed,predictor_pressure_change,predictor_flow_change_kg_s,shadow_correction_evaluated,h20_proposed_authority,h20_reason,h20_rollback_required,h20_candidate_eligible,h22_commit_arm_enabled,h22_commit_authorized,h22_commit_reason,corrected_candidate_committed,untargeted_branch_disagreement,branch_overrides,previous_phase_holds,hysteresis_releases,shadow_iterations,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w,network_mass_closure_kg,network_energy_closure_j,network_balance_mass_rate_kg_s,network_balance_power_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.Interval},{row.PresentationFingerprint},{row.TriggerObserved},{row.PredictorPressureChange:G17},{row.PredictorFlowChangeKilogramsPerSecond:G17},{row.ShadowCorrectionEvaluated},{row.ProposedAuthority},{row.Reason},{row.RollbackRequired},{row.ShadowCorrectedCandidateEligible},{row.CorrectedCommitArmEnabled},{row.CorrectedCommitAuthorized},{row.CorrectedCommitReason},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount},{row.HysteresisReleaseCount},{row.ShadowIterationCount},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-step-commit-telemetry.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-four-node-corrected-commit-seam-metrics.csv"),
            new[]
            {
                "metric,value",
                FormattableString.Invariant($"intervals,{rows.Count}"),
                FormattableString.Invariant($"triggered,{triggered}"),
                FormattableString.Invariant($"h20_candidate_eligible,{eligible}"),
                FormattableString.Invariant($"h22_commit_authorized,{authorized}"),
                FormattableString.Invariant($"corrected_commits,{commits}"),
                FormattableString.Invariant($"rollbacks,{rollbacks}"),
                FormattableString.Invariant($"fallback_intervals,{fallbackIntervals}"),
                FormattableString.Invariant($"fallback_commit_violations,{fallbackCommitViolations}"),
                FormattableString.Invariant($"unsafe_commits,{unsafeCommits}"),
                FormattableString.Invariant($"untargeted_disagreements,{untargetedDisagreements}"),
                FormattableString.Invariant($"repeat_presentation_equivalent,{presentationRepeatEquivalent}"),
                FormattableString.Invariant($"deterministic_repeat,{deterministicRepeat}"),
                FormattableString.Invariant($"telemetry_fingerprint,{telemetryFingerprint}"),
                FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
                FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
                FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
                FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
                FormattableString.Invariant($"h22_audit_passes,{passes}"),
            },
            Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h22-four-node-corrected-commit-seam");

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
        WriteProgress("H.22 corrected-candidate commit-seam audit started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed record StepTelemetryRow(
        int Interval,
        string PresentationFingerprint,
        bool TriggerObserved,
        double PredictorPressureChange,
        double PredictorFlowChangeKilogramsPerSecond,
        bool ShadowCorrectionEvaluated,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason Reason,
        bool RollbackRequired,
        bool ShadowCorrectedCandidateEligible,
        bool CorrectedCommitArmEnabled,
        bool CorrectedCommitAuthorized,
        FourNodeBranchContinuityCorrectedCommitReason CorrectedCommitReason,
        bool CorrectedCandidateCommitted,
        bool UntargetedBranchDisagreementDetected,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        int ShadowIterationCount,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);
}
