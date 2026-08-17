using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.20 evidence-backed design audit for a fail-closed activation/rollback/telemetry contract.
/// The supervisor is shadow-only and is not wired into PlantNetworkOrchestrator in H.20.
/// </summary>
public sealed class FourNodeActivationRollbackContractAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int ExpectedQualifiedRepresentatives = 473;
    private const double H19MaximumMassClosureKilogramsPerSecond = 0d;
    private const double H19MaximumEnergyOwnershipResidualWatts = 0.000000239d;
    private const string H19RepresentativeEvidenceFingerprint = "7A39ED9FE0E92D34C30197899A8D9F9A3AE6CBAEF7185D6D54627944D66B998F";
    private const string H19MetricsEvidenceFingerprint = "3E2E9AF1207741296DBEC1BAB640C5E0D18434269F9AA931722500CDD6C0BDBC";
    private const string H19SummaryEvidenceFingerprint = "80A18863448572B2088DEFFEDCA2146D60B83664FCB2D940B6B54686AE70A5F6";

    [Fact]
    public void FrozenH19Evidence_RetainsValidatedFourNodeQualificationContract()
    {
        var representatives = LoadFrozenH19Representatives();
        var metrics = LoadFrozenH19Metrics();
        var closure = LoadFrozenH19ClosureEvidence();

        Assert.Equal(H19RepresentativeEvidenceFingerprint, CanonicalEvidenceFingerprint("H19_ValidatedQualifiedRepresentativeResults.csv"));
        Assert.Equal(H19MetricsEvidenceFingerprint, CanonicalEvidenceFingerprint("H19_ValidatedQualificationMetrics.csv"));
        Assert.Equal(H19SummaryEvidenceFingerprint, CanonicalEvidenceFingerprint("H19_ValidatedQualificationSummary.txt"));
        Assert.Equal(ExpectedQualifiedRepresentatives, representatives.Count);
        Assert.All(representatives, static row => Assert.True(row.H19Converged));
        Assert.DoesNotContain(representatives, static row => row.LineSearchExhausted);
        Assert.Equal(30_000, MetricInt(metrics, "production_shadow_steps"));
        Assert.Equal(4, MetricInt(metrics, "profiles"));
        Assert.Equal(3_046, MetricInt(metrics, "census_triggered_events"));
        Assert.Equal(92, MetricInt(metrics, "trigger_episodes"));
        Assert.Equal(ExpectedQualifiedRepresentatives, MetricInt(metrics, "qualified_trigger_samples"));
        Assert.Equal(ExpectedQualifiedRepresentatives, MetricInt(metrics, "converged_qualified_samples"));
        Assert.Equal(0, MetricInt(metrics, "line_search_exhausted"));
        Assert.Equal(245, MetricInt(metrics, "recovered_h17_failures"));
        Assert.Equal(228, MetricInt(metrics, "preserved_h17_successes"));
        Assert.Equal(120, MetricInt(metrics, "recovered_h17_turbine_inlet_mismatch_failures"));
        Assert.Equal(125, MetricInt(metrics, "recovered_h17_no_mismatch_failures"));
        Assert.Equal(120_000, MetricInt(metrics, "committed_phase_state_checks"));
        Assert.Equal(0, MetricInt(metrics, "untargeted_candidate_only_late_shadow_nodes"));
        Assert.Equal(0, MetricInt(metrics, "untargeted_candidate_phase_mismatch_nodes"));
        Assert.True(MetricBool(metrics, "representative_keys_match_frozen_h17"));
        Assert.True(MetricBool(metrics, "cross_profile_stratified_policy_qualifies"));
        Assert.True(MetricBool(metrics, "committed_selection_transparent"));
        Assert.True(MetricBool(metrics, "release_challenges_pass"));
        Assert.True(MetricBool(metrics, "four_node_long_horizon_cross_profile_shadow_qualification_passes"));
        Assert.True(MetricBool(metrics, "h19_audit_passes"));
        Assert.Equal(H19MaximumMassClosureKilogramsPerSecond, closure.MaximumMassClosureKilogramsPerSecond);
        Assert.Equal(H19MaximumEnergyOwnershipResidualWatts, closure.MaximumEnergyOwnershipResidualWatts);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeActivationRollbackContractAudit")]
    public void H19QualifiedEvidence_ProducesDeterministicFailClosedShadowAuthorityAndTypedRollbackTelemetry()
    {
        ResetProgress();
        WriteProgress("load-frozen-h19-evidence");
        var representatives = LoadFrozenH19Representatives();
        var metrics = LoadFrozenH19Metrics();
        var closure = LoadFrozenH19ClosureEvidence();
        var qualificationEvidenceAccepted = QualificationEvidenceAccepted(metrics, representatives, closure);
        Assert.True(qualificationEvidenceAccepted, "Frozen H.19 evidence no longer satisfies the activation-contract prerequisite.");

        var supervisor = new FourNodeBranchContinuityShadowActivationSupervisor();
        var disabledOptions = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly;
        var armedOptions = disabledOptions.WithActivationArmEnabled(true);
        Assert.False(disabledOptions.ActivationArmEnabled);
        Assert.Equal(new[] { "steam", "stop-out", "header", "turbine-inlet" }, disabledOptions.TargetNodeIds);
        Assert.Equal(0.060d, disabledOptions.PredictedPressureChangeTriggerFraction);
        Assert.Equal(40d, disabledOptions.PredictedFlowChangeTriggerKilogramsPerSecond);

        WriteProgress("evaluate-default-disabled-authority");
        var disabledDecisions = representatives
            .Select(row => supervisor.Evaluate(CreateObservation(row, qualificationEvidenceAccepted, closure), disabledOptions))
            .ToArray();
        Assert.Equal(ExpectedQualifiedRepresentatives, disabledDecisions.Length);
        Assert.All(disabledDecisions, static decision =>
        {
            Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, decision.ProposedAuthority);
            Assert.Equal(FourNodeBranchContinuityActivationReason.ActivationArmDisabled, decision.Reason);
            Assert.False(decision.RollbackRequired);
            Assert.False(decision.ProductionCommitAuthorized);
        });

        WriteProgress("evaluate-shadow-armed-authority");
        var armedDecisions = representatives
            .Select(row => supervisor.Evaluate(CreateObservation(row, qualificationEvidenceAccepted, closure), armedOptions))
            .ToArray();
        Assert.Equal(ExpectedQualifiedRepresentatives, armedDecisions.Length);
        Assert.All(armedDecisions, static decision =>
        {
            Assert.Equal(FourNodeBranchContinuityProposedAuthority.CorrectedCandidate, decision.ProposedAuthority);
            Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, decision.Reason);
            Assert.False(decision.RollbackRequired);
            Assert.True(decision.ShadowCorrectedCandidateEligible);
            Assert.False(decision.ProductionCommitAuthorized);
        });

        var armedRepeat = representatives
            .Select(row => supervisor.Evaluate(CreateObservation(row, qualificationEvidenceAccepted, closure), armedOptions))
            .ToArray();
        var decisionFingerprint = DecisionFingerprint(armedDecisions);
        var repeatFingerprint = DecisionFingerprint(armedRepeat);
        var deterministicRepeat = string.Equals(decisionFingerprint, repeatFingerprint, StringComparison.Ordinal);
        Assert.True(deterministicRepeat, "H.20 activation-contract decision stream was not exactly deterministic.");

        WriteProgress("evaluate-rollback-challenges");
        var representative = representatives[0];
        var rollbackChallenges = BuildRollbackChallenges(representative, closure, disabledOptions, qualificationEvidenceAccepted)
            .Select(challenge => new RollbackChallengeResult(
                challenge.Name,
                challenge.ExpectedReason,
                supervisor.Evaluate(challenge.Observation, armedOptions)))
            .ToArray();
        Assert.Equal(8, rollbackChallenges.Length);
        Assert.All(rollbackChallenges, static challenge =>
        {
            Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, challenge.Decision.ProposedAuthority);
            Assert.Equal(challenge.ExpectedReason, challenge.Decision.Reason);
            Assert.True(challenge.Decision.RollbackRequired);
            Assert.False(challenge.Decision.ShadowCorrectedCandidateEligible);
            Assert.False(challenge.Decision.ProductionCommitAuthorized);
        });

        var untriggered = new FourNodeBranchContinuityActivationObservation(
            "synthetic-untriggered",
            triggerObserved: false,
            qualificationEvidenceAccepted: true,
            correctorConverged: true,
            lineSearchExhausted: false,
            relativePressureResidual: 0d,
            absoluteFlowResidualKilogramsPerSecond: 0d,
            massClosureKilogramsPerSecond: 0d,
            energyOwnershipResidualWatts: 0d,
            untargetedBranchDisagreementDetected: false);
        var untriggeredDecision = supervisor.Evaluate(untriggered, armedOptions);
        Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, untriggeredDecision.ProposedAuthority);
        Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, untriggeredDecision.Reason);
        Assert.False(untriggeredDecision.RollbackRequired);
        Assert.False(untriggeredDecision.ProductionCommitAuthorized);

        WriteProgress("verify-production-default-remains-explicit");
        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var currentHydraulicCoupling = currentEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, currentHydraulicCoupling.Mode);

        var activationContractPasses = qualificationEvidenceAccepted
            && disabledDecisions.All(static decision =>
                decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState
                && decision.Reason == FourNodeBranchContinuityActivationReason.ActivationArmDisabled
                && !decision.ProductionCommitAuthorized)
            && armedDecisions.All(static decision =>
                decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
                && decision.Reason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
                && !decision.ProductionCommitAuthorized)
            && rollbackChallenges.All(static challenge =>
                challenge.Decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState
                && challenge.Decision.RollbackRequired
                && !challenge.Decision.ProductionCommitAuthorized)
            && deterministicRepeat
            && currentHydraulicCoupling.Mode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.True(activationContractPasses);

        WriteAuditReports(
            representatives,
            disabledDecisions,
            armedDecisions,
            rollbackChallenges,
            untriggeredDecision,
            qualificationEvidenceAccepted,
            deterministicRepeat,
            decisionFingerprint,
            closure,
            activationContractPasses);
    }

    private static FourNodeBranchContinuityActivationObservation CreateObservation(
        FrozenH19Representative row,
        bool qualificationEvidenceAccepted,
        FrozenClosureEvidence closure)
        => new(
            $"{row.ProfileId}:{row.IntervalIndex}",
            triggerObserved: true,
            qualificationEvidenceAccepted,
            row.H19Converged,
            row.LineSearchExhausted,
            row.PressureResidual,
            row.FlowResidualKilogramsPerSecond,
            closure.MaximumMassClosureKilogramsPerSecond,
            closure.MaximumEnergyOwnershipResidualWatts,
            untargetedBranchDisagreementDetected: false);

    private static IReadOnlyList<RollbackChallenge> BuildRollbackChallenges(
        FrozenH19Representative representative,
        FrozenClosureEvidence closure,
        FourNodeBranchContinuityActivationOptions options,
        bool qualificationEvidenceAccepted)
    {
        FourNodeBranchContinuityActivationObservation Observation(
            bool? evidence = null,
            bool? converged = null,
            bool? lineSearchExhausted = null,
            double? pressureResidual = null,
            double? flowResidual = null,
            double? massClosure = null,
            double? energyOwnership = null,
            bool untargetedBranchDisagreement = false)
            => new(
                $"rollback:{representative.ProfileId}:{representative.IntervalIndex}",
                triggerObserved: true,
                evidence ?? qualificationEvidenceAccepted,
                converged ?? representative.H19Converged,
                lineSearchExhausted ?? representative.LineSearchExhausted,
                pressureResidual ?? representative.PressureResidual,
                flowResidual ?? representative.FlowResidualKilogramsPerSecond,
                massClosure ?? closure.MaximumMassClosureKilogramsPerSecond,
                energyOwnership ?? closure.MaximumEnergyOwnershipResidualWatts,
                untargetedBranchDisagreement);

        return new[]
        {
            new RollbackChallenge(
                "qualification-evidence-unavailable",
                Observation(evidence: false),
                FourNodeBranchContinuityActivationReason.RollbackQualificationEvidenceUnavailable),
            new RollbackChallenge(
                "corrector-nonconvergence",
                Observation(converged: false),
                FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence),
            new RollbackChallenge(
                "line-search-exhausted",
                Observation(lineSearchExhausted: true),
                FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted),
            new RollbackChallenge(
                "pressure-residual-exceeded",
                Observation(pressureResidual: options.MaximumRelativePressureResidual * 1.01d),
                FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded),
            new RollbackChallenge(
                "flow-residual-exceeded",
                Observation(flowResidual: options.MaximumAbsoluteFlowResidualKilogramsPerSecond * 1.01d),
                FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded),
            new RollbackChallenge(
                "mass-closure-exceeded",
                Observation(massClosure: options.MaximumMassClosureKilogramsPerSecond * 1.01d),
                FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded),
            new RollbackChallenge(
                "energy-ownership-exceeded",
                Observation(energyOwnership: options.MaximumEnergyOwnershipResidualWatts * 1.01d),
                FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded),
            new RollbackChallenge(
                "untargeted-branch-disagreement",
                Observation(untargetedBranchDisagreement: true),
                FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement),
        };
    }

    private static bool QualificationEvidenceAccepted(
        IReadOnlyDictionary<string, string> metrics,
        IReadOnlyList<FrozenH19Representative> representatives,
        FrozenClosureEvidence closure)
    {
        var options = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly;
        return CanonicalEvidenceFingerprint("H19_ValidatedQualifiedRepresentativeResults.csv") == H19RepresentativeEvidenceFingerprint
            && CanonicalEvidenceFingerprint("H19_ValidatedQualificationMetrics.csv") == H19MetricsEvidenceFingerprint
            && CanonicalEvidenceFingerprint("H19_ValidatedQualificationSummary.txt") == H19SummaryEvidenceFingerprint
            && representatives.Count == ExpectedQualifiedRepresentatives
            && representatives.All(row => row.H19Converged && !row.LineSearchExhausted)
            && representatives.All(row => row.PressureResidual <= options.MaximumRelativePressureResidual)
            && representatives.All(row => row.FlowResidualKilogramsPerSecond <= options.MaximumAbsoluteFlowResidualKilogramsPerSecond)
            && MetricInt(metrics, "census_triggered_events") == 3_046
            && MetricInt(metrics, "trigger_episodes") == 92
            && MetricInt(metrics, "qualified_trigger_samples") == ExpectedQualifiedRepresentatives
            && MetricInt(metrics, "converged_qualified_samples") == ExpectedQualifiedRepresentatives
            && MetricInt(metrics, "line_search_exhausted") == 0
            && MetricInt(metrics, "untargeted_candidate_only_late_shadow_nodes") == 0
            && MetricInt(metrics, "untargeted_candidate_phase_mismatch_nodes") == 0
            && MetricBool(metrics, "representative_keys_match_frozen_h17")
            && MetricBool(metrics, "cross_profile_stratified_policy_qualifies")
            && MetricBool(metrics, "committed_selection_transparent")
            && MetricBool(metrics, "release_challenges_pass")
            && MetricBool(metrics, "four_node_long_horizon_cross_profile_shadow_qualification_passes")
            && MetricBool(metrics, "h19_audit_passes")
            && closure.MaximumMassClosureKilogramsPerSecond <= options.MaximumMassClosureKilogramsPerSecond
            && closure.MaximumEnergyOwnershipResidualWatts <= options.MaximumEnergyOwnershipResidualWatts;
    }

    private static IReadOnlyList<FrozenH19Representative> LoadFrozenH19Representatives()
    {
        var lines = File.ReadAllLines(EvidencePath("H19_ValidatedQualifiedRepresentativeResults.csv"));
        Assert.Equal(ExpectedQualifiedRepresentatives + 1, lines.Length);
        return lines
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line =>
            {
                var columns = line.Split(',');
                Assert.Equal(14, columns.Length);
                return new FrozenH19Representative(
                    columns[0],
                    int.Parse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture),
                    bool.Parse(columns[4]),
                    bool.Parse(columns[5]),
                    double.Parse(columns[6], NumberStyles.Float, CultureInfo.InvariantCulture),
                    double.Parse(columns[7], NumberStyles.Float, CultureInfo.InvariantCulture));
            })
            .OrderBy(static row => row.ProfileId, StringComparer.Ordinal)
            .ThenBy(static row => row.IntervalIndex)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> LoadFrozenH19Metrics()
        => File.ReadAllLines(EvidencePath("H19_ValidatedQualificationMetrics.csv"))
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line => line.Split(',', 2, StringSplitOptions.None))
            .ToDictionary(static columns => columns[0], static columns => columns[1], StringComparer.Ordinal);

    private static FrozenClosureEvidence LoadFrozenH19ClosureEvidence()
    {
        var summary = File.ReadAllText(EvidencePath("H19_ValidatedQualificationSummary.txt"));
        const string marker = "max-closure/ownership=";
        var markerIndex = summary.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "Frozen H.19 summary no longer contains max-closure/ownership evidence.");
        var start = markerIndex + marker.Length;
        var end = summary.IndexOf(';', start);
        Assert.True(end > start);
        var values = summary[start..end].Split('/');
        Assert.Equal(2, values.Length);
        return new FrozenClosureEvidence(
            double.Parse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    private static string CanonicalEvidenceFingerprint(string fileName)
    {
        var canonicalText = File.ReadAllText(EvidencePath(fileName)).ReplaceLineEndings("\n");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText)));
    }

    private static int MetricInt(IReadOnlyDictionary<string, string> metrics, string key)
        => int.Parse(metrics[key], NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static bool MetricBool(IReadOnlyDictionary<string, string> metrics, string key)
        => bool.Parse(metrics[key]);

    private static string DecisionFingerprint(IEnumerable<FourNodeBranchContinuityActivationDecision> decisions)
    {
        var canonical = string.Join(
            "\n",
            decisions
                .OrderBy(static decision => decision.SampleId, StringComparer.Ordinal)
                .Select(static decision => FormattableString.Invariant(
                    $"{decision.SampleId}|{decision.ProposedAuthority}|{decision.Reason}|{decision.RollbackRequired}|{decision.TriggerObserved}|{decision.ActivationArmEnabled}|{decision.ProductionCommitAuthorized}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteAuditReports(
        IReadOnlyList<FrozenH19Representative> representatives,
        IReadOnlyList<FourNodeBranchContinuityActivationDecision> disabledDecisions,
        IReadOnlyList<FourNodeBranchContinuityActivationDecision> armedDecisions,
        IReadOnlyList<RollbackChallengeResult> rollbackChallenges,
        FourNodeBranchContinuityActivationDecision untriggeredDecision,
        bool qualificationEvidenceAccepted,
        bool deterministicRepeat,
        string decisionFingerprint,
        FrozenClosureEvidence closure,
        bool activationContractPasses)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var maxPressureResidual = representatives.Max(static row => row.PressureResidual);
        var maxFlowResidual = representatives.Max(static row => row.FlowResidualKilogramsPerSecond);
        var rollbackPasses = rollbackChallenges.Count(challenge =>
            challenge.Decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState
            && challenge.Decision.Reason == challenge.ExpectedReason
            && challenge.Decision.RollbackRequired
            && !challenge.Decision.ProductionCommitAuthorized);

        var summary = string.Join(
            Environment.NewLine,
            "================================================================================",
            "M10.9.4.1-H.20 FOUR-NODE ACTIVATION / ROLLBACK / SHADOW TELEMETRY CONTRACT SUMMARY",
            "================================================================================",
            "=== 01-h19-evidence-backed-fail-closed-shadow-activation-contract ===",
            "H.20 freezes the user-validated H.19 473-representative results and defines a deterministic fail-closed shadow authority contract. The new supervisor is not wired into PlantNetworkOrchestrator; production remains ExplicitCommittedState at 10 ms and no corrected candidate can be committed.",
            FormattableString.Invariant($"frozen-H19-qualified-representatives={representatives.Count}; H19-qualification-evidence-accepted={qualificationEvidenceAccepted}; max-qualified-pressure-residual={maxPressureResidual:R}; max-qualified-flow-residual-kg-s={maxFlowResidual:R}; max-H19-closure/ownership={closure.MaximumMassClosureKilogramsPerSecond:R}/{closure.MaximumEnergyOwnershipResidualWatts:R};"),
            FormattableString.Invariant($"activation-default-armed=False; default-explicit-decisions={disabledDecisions.Count(static decision => decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState)}/{disabledDecisions.Count}; default-candidate-eligible=0/{disabledDecisions.Count}; default-production-commit-authorized={disabledDecisions.Count(static decision => decision.ProductionCommitAuthorized)};"),
            FormattableString.Invariant($"shadow-arm-simulation=True; qualified-triggered-candidate-eligible={armedDecisions.Count(static decision => decision.ShadowCorrectedCandidateEligible)}/{armedDecisions.Count}; shadow-arm-rollbacks={armedDecisions.Count(static decision => decision.RollbackRequired)}; production-commit-authorized={armedDecisions.Count(static decision => decision.ProductionCommitAuthorized)}; deterministic-repeat={deterministicRepeat}; decision-fingerprint={decisionFingerprint};"),
            FormattableString.Invariant($"rollback-challenges={rollbackChallenges.Count}; rollback-challenges-pass={rollbackPasses}/{rollbackChallenges.Count}; untriggered-authority={untriggeredDecision.ProposedAuthority}; untriggered-reason={untriggeredDecision.Reason};"),
            FormattableString.Invariant($"activation-contract-passes={activationContractPasses}; h20-audit-passes={activationContractPasses}; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; PlantNetworkOrchestrator-wiring-changed=False; target-node-set-changed-from-H19=False; P060-F040-retuned=False; H9-tolerances-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;"),
            activationContractPasses
                ? "H.20 recommendation: the evidence-backed activation authority contract is deterministic and fail-closed in shadow. Keep default production explicit. A later, separately reviewed opt-in integration candidate may wire this exact contract only if it preserves immediate explicit fallback, typed rollback telemetry, the frozen H.19 qualification prerequisite and the full H.19 long-horizon regression gate."
                : "H.20 recommendation: keep production explicit and repair the activation/rollback contract or its frozen-evidence prerequisite before any integration candidate.",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, "01-four-node-activation-rollback-contract.summary.txt"), summary, Utf8WithoutBom);

        var decisionsCsv = new StringBuilder();
        decisionsCsv.AppendLine("sample_id,default_authority,default_reason,shadow_armed_authority,shadow_armed_reason,rollback_required,production_commit_authorized");
        for (var index = 0; index < representatives.Count; index++)
        {
            var disabled = disabledDecisions[index];
            var armed = armedDecisions[index];
            decisionsCsv.AppendLine(FormattableString.Invariant(
                $"{disabled.SampleId},{disabled.ProposedAuthority},{disabled.Reason},{armed.ProposedAuthority},{armed.Reason},{armed.RollbackRequired},{armed.ProductionCommitAuthorized}"));
        }
        File.WriteAllText(Path.Combine(directory, "02-qualified-representative-authority-decisions.csv"), decisionsCsv.ToString(), Utf8WithoutBom);

        var rollbackCsv = new StringBuilder();
        rollbackCsv.AppendLine("challenge,expected_reason,actual_authority,actual_reason,rollback_required,production_commit_authorized,passes");
        foreach (var challenge in rollbackChallenges)
        {
            var passes = challenge.Decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState
                && challenge.Decision.Reason == challenge.ExpectedReason
                && challenge.Decision.RollbackRequired
                && !challenge.Decision.ProductionCommitAuthorized;
            rollbackCsv.AppendLine(FormattableString.Invariant(
                $"{challenge.Name},{challenge.ExpectedReason},{challenge.Decision.ProposedAuthority},{challenge.Decision.Reason},{challenge.Decision.RollbackRequired},{challenge.Decision.ProductionCommitAuthorized},{passes}"));
        }
        File.WriteAllText(Path.Combine(directory, "03-rollback-challenges.csv"), rollbackCsv.ToString(), Utf8WithoutBom);

        var metricsCsv = string.Join(
            Environment.NewLine,
            "metric,value",
            FormattableString.Invariant($"frozen_h19_qualified_representatives,{representatives.Count}"),
            FormattableString.Invariant($"h19_qualification_evidence_accepted,{qualificationEvidenceAccepted}"),
            FormattableString.Invariant($"default_explicit_decisions,{disabledDecisions.Count(static decision => decision.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState)}"),
            FormattableString.Invariant($"shadow_armed_candidate_eligible,{armedDecisions.Count(static decision => decision.ShadowCorrectedCandidateEligible)}"),
            FormattableString.Invariant($"shadow_armed_rollbacks,{armedDecisions.Count(static decision => decision.RollbackRequired)}"),
            FormattableString.Invariant($"rollback_challenges,{rollbackChallenges.Count}"),
            FormattableString.Invariant($"rollback_challenges_pass,{rollbackPasses}"),
            FormattableString.Invariant($"production_commit_authorized,{armedDecisions.Count(static decision => decision.ProductionCommitAuthorized)}"),
            FormattableString.Invariant($"deterministic_repeat,{deterministicRepeat}"),
            FormattableString.Invariant($"activation_contract_passes,{activationContractPasses}"),
            FormattableString.Invariant($"h20_audit_passes,{activationContractPasses}"),
            string.Empty);
        File.WriteAllText(Path.Combine(directory, "04-four-node-activation-contract-metrics.csv"), metricsCsv, Utf8WithoutBom);
        WriteProgress("audit-complete");
    }

    private static string EvidencePath(string fileName)
        => Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "Evidence",
            fileName);

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h20-four-node-activation-rollback-contract");

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            "H.20 four-node activation/rollback contract audit started." + Environment.NewLine,
            Utf8WithoutBom);
    }

    private static void WriteProgress(string message)
        => File.AppendAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + " " + message + Environment.NewLine,
            Utf8WithoutBom);

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

        throw new InvalidOperationException("Repository root containing NuclearReactorSimulator.sln could not be located.");
    }

    private sealed record FrozenH19Representative(
        string ProfileId,
        int IntervalIndex,
        bool H19Converged,
        bool LineSearchExhausted,
        double PressureResidual,
        double FlowResidualKilogramsPerSecond);

    private sealed record FrozenClosureEvidence(
        double MaximumMassClosureKilogramsPerSecond,
        double MaximumEnergyOwnershipResidualWatts);

    private sealed record RollbackChallenge(
        string Name,
        FourNodeBranchContinuityActivationObservation Observation,
        FourNodeBranchContinuityActivationReason ExpectedReason);

    private sealed record RollbackChallengeResult(
        string Name,
        FourNodeBranchContinuityActivationReason ExpectedReason,
        FourNodeBranchContinuityActivationDecision Decision);
}
