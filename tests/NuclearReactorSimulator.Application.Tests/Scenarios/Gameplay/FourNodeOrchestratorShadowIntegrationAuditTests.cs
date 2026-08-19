using System.Globalization;
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
/// M10.9.4.1-H.21 opt-in orchestrator wiring audit. The H.19-qualified correction and H.20 supervisor execute
/// as an integrated sidecar, while the returned production candidate remains exactly the explicit predictor.
/// </summary>
public sealed class FourNodeOrchestratorShadowIntegrationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 2_000;
    private const int ExpectedH16ControlTriggers = 15;

    private static readonly IReadOnlyDictionary<string, string> FrozenH20Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H20_ValidatedActivationContractSummary.txt"] = "E48D35EBA055300061B3DCA11B5F92744E4C1487741F896EC240D0D63003243F",
            ["H20_ValidatedAuthorityDecisions.csv"] = "015FA83D29AE61F7B11A13B96EDBBF59D20D4C222C854E1F7BB4F699E34852EC",
            ["H20_ValidatedRollbackChallenges.csv"] = "73AE55B691002AF140BDE41C9275E02B787279A3CB5D08451AE357F029AB0739",
            ["H20_ValidatedActivationContractMetrics.csv"] = "75A54C40AD83A1C79C47474D214AED1379456A8D881C3C92A1811BF11A0C2585",
        };

    [Fact]
    public void FrozenH20Evidence_RetainsValidatedFailClosedActivationContract()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH20Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.20 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H20_ValidatedActivationContractSummary.txt"));
        Assert.Contains("frozen-H19-qualified-representatives=473", summary, StringComparison.Ordinal);
        Assert.Contains("default-explicit-decisions=473/473", summary, StringComparison.Ordinal);
        Assert.Contains("qualified-triggered-candidate-eligible=473/473", summary, StringComparison.Ordinal);
        Assert.Contains("rollback-challenges-pass=8/8", summary, StringComparison.Ordinal);
        Assert.Contains("production-commit-authorized=0", summary, StringComparison.Ordinal);
        Assert.Contains("activation-contract-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h20-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeOrchestratorShadowIntegrationAudit")]
    public void OptInShadowIntegratedRuntime_PreservesExplicitTrajectoryAndReportsEveryTriggerWithoutCommit()
    {
        ResetProgress();
        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var integratedEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeShadowIntegrationEvidenceRuntimeEngine(Step));
        var repeatEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeShadowIntegrationEvidenceRuntimeEngine(Step));

        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            CurrentHydraulics(explicitEngine).Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated,
            CurrentHydraulics(integratedEngine).Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated,
            CurrentHydraulics(repeatEngine).Mode);

        var rows = new List<StepTelemetryRow>(IntervalCount);
        var repeatRows = new List<StepTelemetryRow>(IntervalCount);
        var presentationEquivalent = 0;
        var integratedRepeatEquivalent = 0;

        for (var interval = 1; interval <= IntervalCount; interval++)
        {
            var explicitPresentation = explicitEngine.Step(ControlRoomRunState.Running);
            var integratedPresentation = integratedEngine.Step(ControlRoomRunState.Running);
            var repeatPresentation = repeatEngine.Step(ControlRoomRunState.Running);
            Assert.False(explicitPresentation.AnyTripActive, $"Unexpected explicit trip at H.21 interval {interval}.");
            Assert.False(integratedPresentation.AnyTripActive, $"Unexpected shadow-integrated trip at H.21 interval {interval}.");
            Assert.False(repeatPresentation.AnyTripActive, $"Unexpected repeat shadow-integrated trip at H.21 interval {interval}.");

            var explicitFingerprint = ControlRoomSnapshotFingerprint.Compute(explicitPresentation);
            var integratedFingerprint = ControlRoomSnapshotFingerprint.Compute(integratedPresentation);
            var repeatPresentationFingerprint = ControlRoomSnapshotFingerprint.Compute(repeatPresentation);
            if (string.Equals(explicitFingerprint, integratedFingerprint, StringComparison.Ordinal))
            {
                presentationEquivalent++;
            }
            if (string.Equals(integratedFingerprint, repeatPresentationFingerprint, StringComparison.Ordinal))
            {
                integratedRepeatEquivalent++;
            }

            Assert.Equal(explicitFingerprint, integratedFingerprint);
            Assert.Equal(integratedFingerprint, repeatPresentationFingerprint);

            var numerics = CurrentHydraulics(integratedEngine);
            var repeatNumerics = CurrentHydraulics(repeatEngine);
            Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated, numerics.Mode);
            Assert.False(numerics.UsedSemiImplicitCorrection);
            Assert.True(numerics.Converged);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            var repeatTelemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(repeatNumerics.FourNodeBranchContinuity);
            Assert.False(telemetry.CorrectedCandidateCommitted);
            Assert.False(repeatTelemetry.CorrectedCandidateCommitted);
            if (telemetry.TriggerObserved)
            {
                Assert.True(telemetry.ShadowCorrectionEvaluated);
            }
            else
            {
                Assert.False(telemetry.ShadowCorrectionEvaluated);
                Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, telemetry.Reason);
                Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, telemetry.ProposedAuthority);
            }

            rows.Add(ToRow(interval, numerics, telemetry));
            repeatRows.Add(ToRow(interval, repeatNumerics, repeatTelemetry));

            if (interval % 250 == 0 || interval == IntervalCount)
            {
                WriteProgress($"lockstep-progress interval={interval}/{IntervalCount}");
            }
        }

        var triggered = rows.Where(static row => row.TriggerObserved).ToArray();
        var candidateEligible = triggered.Count(static row => row.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate);
        var rollbacks = triggered.Count(static row => row.RollbackRequired);
        var commits = rows.Count(static row => row.CorrectedCandidateCommitted);
        var untargetedDisagreements = rows.Count(static row => row.UntargetedBranchDisagreementDetected);
        var branchOverrides = rows.Sum(static row => row.BranchOverrideCount);
        var previousPhaseHolds = rows.Sum(static row => row.PreviousPhaseHoldCount);
        var hysteresisReleases = rows.Sum(static row => row.HysteresisReleaseCount);
        var telemetryFingerprint = Fingerprint(rows);
        var repeatFingerprint = Fingerprint(repeatRows);
        var deterministicRepeat = string.Equals(telemetryFingerprint, repeatFingerprint, StringComparison.Ordinal);

        Assert.Equal(ExpectedH16ControlTriggers, triggered.Length);
        Assert.Equal(triggered.Length, candidateEligible);
        Assert.Equal(0, rollbacks);
        Assert.Equal(0, commits);
        Assert.Equal(0, untargetedDisagreements);
        Assert.Equal(IntervalCount, presentationEquivalent);
        Assert.Equal(IntervalCount, integratedRepeatEquivalent);
        Assert.True(deterministicRepeat);
        Assert.All(triggered, static row =>
        {
            Assert.True(row.ShadowConverged);
            Assert.False(row.ShadowLineSearchExhausted);
            Assert.InRange(row.ShadowMaximumRelativePressureResidual, 0d, 1e-5d);
            Assert.InRange(row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond, 0d, 1e-2d);
            Assert.InRange(row.ShadowMassClosureKilogramsPerSecond, 0d, 1e-8d);
            Assert.InRange(row.ShadowEnergyOwnershipResidualWatts, 0d, 1e-3d);
            Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, row.Reason);
        });

        var defaultFactory = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultFactory).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = triggered.Length == ExpectedH16ControlTriggers
            && candidateEligible == ExpectedH16ControlTriggers
            && rollbacks == 0
            && commits == 0
            && untargetedDisagreements == 0
            && presentationEquivalent == IntervalCount
            && integratedRepeatEquivalent == IntervalCount
            && deterministicRepeat
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.True(passes);

        WriteReports(
            rows,
            presentationEquivalent,
            integratedRepeatEquivalent,
            candidateEligible,
            rollbacks,
            commits,
            untargetedDisagreements,
            branchOverrides,
            previousPhaseHolds,
            hysteresisReleases,
            deterministicRepeat,
            telemetryFingerprint,
            defaultMode,
            passes);
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static StepTelemetryRow ToRow(
        int interval,
        PlantNetworkHydraulicNumericalSnapshot numerics,
        FourNodeBranchContinuityIntegrationTelemetry telemetry)
        => new(
            interval,
            telemetry.TriggerObserved,
            numerics.PredictorMaximumFractionalSubcooledPressureChange,
            numerics.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
            telemetry.ShadowCorrectionEvaluated,
            telemetry.ProposedAuthority,
            telemetry.Reason,
            telemetry.RollbackRequired,
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
            telemetry.ShadowEnergyOwnershipResidualWatts);

    private static string Fingerprint(IReadOnlyList<StepTelemetryRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.Interval}:{row.TriggerObserved}:{row.PredictorPressureChange:G17}:{row.PredictorFlowChangeKilogramsPerSecond:G17}:{row.ShadowCorrectionEvaluated}:{row.ProposedAuthority}:{row.Reason}:{row.RollbackRequired}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.ShadowIterationCount}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<StepTelemetryRow> rows,
        int presentationEquivalent,
        int integratedRepeatEquivalent,
        int candidateEligible,
        int rollbacks,
        int commits,
        int untargetedDisagreements,
        int branchOverrides,
        int previousPhaseHolds,
        int hysteresisReleases,
        bool deterministicRepeat,
        string telemetryFingerprint,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var triggered = rows.Count(static row => row.TriggerObserved);
        var summary = new[]
        {
            "=== 01-current-v2-four-node-orchestrator-shadow-integration ===",
            "H.21 wires the H.19-qualified four-node H.9 correction and the validated H.20 fail-closed authority supervisor into PlantNetworkOrchestrator as an explicit opt-in sidecar. The production candidate remains the explicit predictor on every interval; H.21 cannot commit corrected candidates.",
            FormattableString.Invariant($"intervals={rows.Count}; production-fixed-step=10.000 ms; explicit-vs-shadow-integrated-presentation-equivalent={presentationEquivalent}/{rows.Count}; shadow-integrated-repeat-equivalent={integratedRepeatEquivalent}/{rows.Count};"),
            FormattableString.Invariant($"P060-F040-triggered={triggered}; expected-H16-control-triggers={ExpectedH16ControlTriggers}; corrected-candidate-eligible={candidateEligible}/{triggered}; rollbacks={rollbacks}; corrected-candidates-committed={commits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"branch-overrides={branchOverrides}; previous-phase-holds={previousPhaseHolds}; hysteresis-releases={hysteresisReleases}; deterministic-repeat={deterministicRepeat}; telemetry-fingerprint={telemetryFingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; standard-factory-shadow-integration-active=False; opt-in-shadow-integration-mode=FourNodeBranchContinuityShadowIntegrated; H20-contract-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; production-corrected-commit-authorized=False;"),
            FormattableString.Invariant($"four-node-orchestrator-shadow-integration-passes={passes}; h21-audit-passes={passes};"),
            "H.21 recommendation: if the full H.19 regression remains green and this integrated sidecar stays trajectory-transparent with zero corrected commits, the next milestone may introduce a separately opt-in corrected-candidate commit seam guarded by the unchanged H.20 authority decision, immediate explicit fallback and the same telemetry. Default current-v2 must remain explicit until that commit path passes replay, protection, long-running and off-design gates.",
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-four-node-orchestrator-shadow-integration.summary.txt"),
            summary,
            Utf8WithoutBom);

        var csv = new List<string>
        {
            "interval,trigger_observed,predictor_pressure_change,predictor_flow_change_kg_s,shadow_correction_evaluated,proposed_authority,reason,rollback_required,corrected_candidate_committed,untargeted_branch_disagreement,branch_overrides,previous_phase_holds,hysteresis_releases,shadow_iterations,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.Interval},{row.TriggerObserved},{row.PredictorPressureChange:G17},{row.PredictorFlowChangeKilogramsPerSecond:G17},{row.ShadowCorrectionEvaluated},{row.ProposedAuthority},{row.Reason},{row.RollbackRequired},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount},{row.HysteresisReleaseCount},{row.ShadowIterationCount},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-step-telemetry.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-four-node-orchestrator-shadow-integration-metrics.csv"),
            new[]
            {
                "metric,value",
                FormattableString.Invariant($"intervals,{rows.Count}"),
                FormattableString.Invariant($"presentation_equivalent,{presentationEquivalent}"),
                FormattableString.Invariant($"repeat_equivalent,{integratedRepeatEquivalent}"),
                FormattableString.Invariant($"triggered,{triggered}"),
                FormattableString.Invariant($"candidate_eligible,{candidateEligible}"),
                FormattableString.Invariant($"rollbacks,{rollbacks}"),
                FormattableString.Invariant($"corrected_commits,{commits}"),
                FormattableString.Invariant($"untargeted_disagreements,{untargetedDisagreements}"),
                FormattableString.Invariant($"branch_overrides,{branchOverrides}"),
                FormattableString.Invariant($"previous_phase_holds,{previousPhaseHolds}"),
                FormattableString.Invariant($"hysteresis_releases,{hysteresisReleases}"),
                FormattableString.Invariant($"deterministic_repeat,{deterministicRepeat}"),
                FormattableString.Invariant($"telemetry_fingerprint,{telemetryFingerprint}"),
                FormattableString.Invariant($"h21_audit_passes,{passes}"),
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h21-four-node-orchestrator-shadow-integration");

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
        WriteProgress("H.21 orchestrator shadow-integration audit started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed record StepTelemetryRow(
        int Interval,
        bool TriggerObserved,
        double PredictorPressureChange,
        double PredictorFlowChangeKilogramsPerSecond,
        bool ShadowCorrectionEvaluated,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason Reason,
        bool RollbackRequired,
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
        double ShadowEnergyOwnershipResidualWatts);
}
