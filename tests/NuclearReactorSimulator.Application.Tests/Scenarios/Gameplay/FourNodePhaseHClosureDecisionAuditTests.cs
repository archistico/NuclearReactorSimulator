using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.30 Phase H closure review. This gate is intentionally evidence-only: it does not rerun the expensive
/// H.24/H.28 trajectories and does not modify H.9/H.20/H.22 or the production selector. It freezes the validated
/// H.19-H.29 chain and derives the conservative end-of-Phase-H production policy from that evidence.
/// </summary>
public sealed class FourNodePhaseHClosureDecisionAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly IReadOnlyDictionary<string, string> FrozenPhaseHFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H19_ValidatedQualificationSummary.txt"] = "80A18863448572B2088DEFFEDCA2146D60B83664FCB2D940B6B54686AE70A5F6",
            ["H20_ValidatedActivationContractSummary.txt"] = "E48D35EBA055300061B3DCA11B5F92744E4C1487741F896EC240D0D63003243F",
            ["H21_ValidatedOrchestratorShadowIntegrationSummary.txt"] = "A4DEA28667A88E58E9F2C2BCB11C8DE2B7D55E2AC1B410883B853C8AD200E0EE",
            ["H22_ValidatedCorrectedCommitSeamSummary.txt"] = "1328E3EC5D22336F2AB8412AE764F0873B0A5721F26C610C12865831A34463D6",
            ["H23_ValidatedCommittedReplayProtectionSummary.txt"] = "933ED5D40C0329D14EBF2F757F87F631118485221B4ED272AF092AEA60E0CB25",
            ["H24_PostH28_ValidatedRequalificationSummary.txt"] = "246BE859B7B59B8A208932E7C07035A5F80DCB2960F32A73891BDDDB669ACB71",
            ["H24_PostH28_ValidatedEvidenceManifest.txt"] = "7B64CB3638E7221619D9F6845921802C91213E07D1C3EFE224C3F78C8550B26B",
            ["H25_ValidatedProtectionTransientMatrixSummary.txt"] = "09112868F26AAD1F007820F27BBED6BF48462FC5E8A61C47E0D786D079090E85",
            ["H26_ValidatedIntegratedRollbackSummary.txt"] = "4DDC10F4F084C392969E26D9C5B5C4203A30F93DFB2F8BABB81D7807DFFBD7EC",
            ["H27_ValidatedOffDesignQualificationSummary.txt"] = "DDEAC9E8987FC7C12483A792067B4134BB1D87BDDD052F5E0D46E9AEAD3107AE",
            ["H28_ValidatedPerformanceCostSoakSummary.txt"] = "C2EC26E3C196CEE32EDB99B67C0C8156704E9D27578E189A97B86D27F357E563",
            ["H29_ValidatedProductionActivationCandidateSummary.txt"] = "D9CAF6C953F2A593C9E9409EF91A379A2C7AB1CB295FE33FA6C6C0AABDD29F5F",
            ["H29_ValidatedProductionActivationCandidateMetrics.csv"] = "9534425D12539A084D837061AF5430C9A1AA151340B99C206CEA4D3A196465C3",
            ["H29_ValidatedEvidenceManifest.txt"] = "F0A8212ACBE7CA0D0E17E4B00AAAF005623720851C0BA1B830398332AC60A02E",
        };

    [Fact]
    public void FrozenPhaseHEvidence_RetainsValidatedH19ThroughH29Chain()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenPhaseHFingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.30 prerequisite evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        AssertEvidenceContains("H19_ValidatedQualificationSummary.txt",
            "four-node-long-horizon-cross-profile-shadow-qualification-passes=True",
            "h19-audit-passes=True",
            "stratified-converged=473/473");
        AssertEvidenceContains("H20_ValidatedActivationContractSummary.txt",
            "activation-contract-passes=True",
            "h20-audit-passes=True",
            "rollback-challenges-pass=8/8");
        AssertEvidenceContains("H21_ValidatedOrchestratorShadowIntegrationSummary.txt",
            "four-node-orchestrator-shadow-integration-passes=True",
            "h21-audit-passes=True");
        AssertEvidenceContains("H22_ValidatedCorrectedCommitSeamSummary.txt",
            "four-node-corrected-candidate-commit-seam-passes=True",
            "h22-audit-passes=True",
            "unsafe-corrected-commits=0");
        AssertEvidenceContains("H23_ValidatedCommittedReplayProtectionSummary.txt",
            "four-node-committed-replay-checkpoint-protection-qualification-passes=True",
            "full-replay-trace-equivalent=True",
            "checkpoint-prefix-and-continuation-equivalent=True");
        AssertEvidenceContains("H24_PostH28_ValidatedRequalificationSummary.txt",
            "post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True",
            "h24-post-h28-requalification-audit-passes=True",
            "corrected-candidates-committed=9626",
            "H20-rollbacks=0");
        AssertEvidenceContains("H25_ValidatedProtectionTransientMatrixSummary.txt",
            "four-node-committed-protection-operational-transient-matrix-passes=True",
            "h25-audit-passes=True");
        AssertEvidenceContains("H26_ValidatedIntegratedRollbackSummary.txt",
            "four-node-integrated-rollback-fail-closed-stress-passes=True",
            "h26-audit-passes=True",
            "explicit-fallback-equivalent=12/12");
        AssertEvidenceContains("H27_ValidatedOffDesignQualificationSummary.txt",
            "four-node-off-design-robustness-qualification-envelope-passes=True",
            "h27-audit-passes=True",
            "unsafe-corrected-commits=0");
        AssertEvidenceContains("H28_ValidatedPerformanceCostSoakSummary.txt",
            "four-node-performance-cost-operational-soak-passes=True",
            "h28-audit-passes=True",
            "corrected-performance-class=bounded-but-costly",
            "median-wall-cost-ratio=4.6214685710690242",
            "p95-wall-cost-ratio=10.684444741413872");
        AssertEvidenceContains("H29_ValidatedProductionActivationCandidateSummary.txt",
            "four-node-production-activation-candidate-passes=True",
            "h29-audit-passes=True",
            "h30-closure-review-unblocked=True",
            "corrected-candidates-committed=400",
            "H20-rollbacks=0",
            "unsafe-corrected-commits=0");
        AssertEvidenceContains("H29_ValidatedEvidenceManifest.txt",
            "full-telemetry-data-rows=1026",
            "full-telemetry-canonical-sha256=F0AD7D802769F1EA3ECD4900C3104C275CE2E6C2970ED5FFD883A3F9F83A3E44",
            "activation-telemetry-fingerprint=BB16A2395682226B6E037901317D70B4A12E8E5C184CFC0E7C4B044643B05D68");
    }

    [Fact]
    public void ClosurePolicyContract_PreservesExplicitV2DefaultAndQualifiedCorrectedV3OptIn()
    {
        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var optInDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, defaultDecision.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(2, defaultDecision.InitialCondition.Version);

        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, optInDecision.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, optInDecision.InitialCondition);
        Assert.Equal(3, optInDecision.InitialCondition.Version);

        Assert.True(killDecision.ExplicitKillApplied);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killDecision.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killDecision.InitialCondition);
        Assert.Equal(2, killDecision.InitialCondition.Version);

        Assert.IsType<DesktopSustainedGenerationInitialConditionFactory>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(defaultDecision));
        Assert.IsType<DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(optInDecision));
        Assert.IsType<DesktopSustainedGenerationInitialConditionFactory>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(killDecision));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodePhaseHClosureDecisionAudit")]
    public void ValidatedEvidence_DerivesOptInOnlyAndClosesPhaseHWithoutRuntimeRetuning()
    {
        ResetReportDirectory();

        var allMandatoryTechnicalEvidenceGreen = MandatoryTechnicalEvidenceIsGreen();
        var h28PerformanceGateGreen = EvidenceContains(
            "H28_ValidatedPerformanceCostSoakSummary.txt",
            "four-node-performance-cost-operational-soak-passes=True")
            && EvidenceContains(
                "H28_ValidatedPerformanceCostSoakSummary.txt",
                "h28-audit-passes=True");
        var h28BoundedButCostly = EvidenceContains(
            "H28_ValidatedPerformanceCostSoakSummary.txt",
            "corrected-performance-class=bounded-but-costly");
        var h29ActivationCandidateGreen = EvidenceContains(
            "H29_ValidatedProductionActivationCandidateSummary.txt",
            "four-node-production-activation-candidate-passes=True");

        Assert.True(allMandatoryTechnicalEvidenceGreen);
        Assert.True(h28PerformanceGateGreen);
        Assert.True(h28BoundedButCostly);
        Assert.True(h29ActivationCandidateGreen);

        var decision = DeriveDecision(
            allMandatoryTechnicalEvidenceGreen,
            h28PerformanceGateGreen,
            h28BoundedButCostly,
            h29ActivationCandidateGreen);
        Assert.Equal(PhaseHClosureDecision.OptInOnly, decision);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var optInDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, defaultDecision.EffectivePolicy);
        Assert.Equal(2, defaultDecision.InitialCondition.Version);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, optInDecision.EffectivePolicy);
        Assert.Equal(3, optInDecision.InitialCondition.Version);
        Assert.True(killDecision.ExplicitKillApplied);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killDecision.EffectivePolicy);
        Assert.Equal(2, killDecision.InitialCondition.Version);

        const bool numericalContractRetuned = false;
        const bool productionSelectorChanged = false;
        const bool h24Rerun = false;
        const bool h28Rerun = false;
        var passes = allMandatoryTechnicalEvidenceGreen
            && h28PerformanceGateGreen
            && h29ActivationCandidateGreen
            && decision == PhaseHClosureDecision.OptInOnly
            && !numericalContractRetuned
            && !productionSelectorChanged
            && !h24Rerun
            && !h28Rerun;
        Assert.True(passes);

        WriteReports(
            decision,
            allMandatoryTechnicalEvidenceGreen,
            h28PerformanceGateGreen,
            h28BoundedButCostly,
            h29ActivationCandidateGreen,
            defaultDecision,
            optInDecision,
            killDecision,
            passes);
    }

    private static PhaseHClosureDecision DeriveDecision(
        bool allMandatoryTechnicalEvidenceGreen,
        bool h28PerformanceGateGreen,
        bool h28BoundedButCostly,
        bool h29ActivationCandidateGreen)
    {
        if (!allMandatoryTechnicalEvidenceGreen || !h28PerformanceGateGreen || !h29ActivationCandidateGreen)
        {
            return PhaseHClosureDecision.RemainExplicit;
        }

        if (h28BoundedButCostly)
        {
            return PhaseHClosureDecision.OptInOnly;
        }

        // H.30 may activate only if the validated cost evidence positively supports default activation.
        // The current H.28 evidence does not make that claim, so any unrecognized performance classification fails closed.
        return PhaseHClosureDecision.RemainExplicit;
    }

    private static bool MandatoryTechnicalEvidenceIsGreen()
        => EvidenceContains("H19_ValidatedQualificationSummary.txt", "h19-audit-passes=True")
            && EvidenceContains("H20_ValidatedActivationContractSummary.txt", "h20-audit-passes=True")
            && EvidenceContains("H21_ValidatedOrchestratorShadowIntegrationSummary.txt", "h21-audit-passes=True")
            && EvidenceContains("H22_ValidatedCorrectedCommitSeamSummary.txt", "h22-audit-passes=True")
            && EvidenceContains("H23_ValidatedCommittedReplayProtectionSummary.txt", "h23-audit-passes=True")
            && EvidenceContains("H24_PostH28_ValidatedRequalificationSummary.txt", "h24-post-h28-requalification-audit-passes=True")
            && EvidenceContains("H25_ValidatedProtectionTransientMatrixSummary.txt", "h25-audit-passes=True")
            && EvidenceContains("H26_ValidatedIntegratedRollbackSummary.txt", "h26-audit-passes=True")
            && EvidenceContains("H27_ValidatedOffDesignQualificationSummary.txt", "h27-audit-passes=True")
            && EvidenceContains("H29_ValidatedProductionActivationCandidateSummary.txt", "h29-audit-passes=True");

    private static void WriteReports(
        PhaseHClosureDecision decision,
        bool allMandatoryTechnicalEvidenceGreen,
        bool h28PerformanceGateGreen,
        bool h28BoundedButCostly,
        bool h29ActivationCandidateGreen,
        DesktopHydraulicProductionPolicyDecision defaultDecision,
        DesktopHydraulicProductionPolicyDecision optInDecision,
        DesktopHydraulicProductionPolicyDecision killDecision,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var decisionText = decision switch
        {
            PhaseHClosureDecision.Activate => "ACTIVATE",
            PhaseHClosureDecision.OptInOnly => "OPT-IN ONLY",
            PhaseHClosureDecision.RemainExplicit => "REMAIN EXPLICIT",
            _ => throw new ArgumentOutOfRangeException(nameof(decision)),
        };

        var summary = new[]
        {
            "=== 01-current-v2-phase-h-closure-production-qualification-decision ===",
            "H.30 closes Phase H by reviewing the frozen validated H.19-H.29 evidence chain. It does not rerun H.24/H.28 and does not change H.9 mathematics, H.20 authority, H.22 ownership, P060/F040, bounded branch-continuity limits, physical coefficients, the 10 ms fixed step or the deployment selector. The closure decision is evidence-derived rather than assumed in advance.",
            FormattableString.Invariant($"validated-evidence-chain=H19|H20|H21|H22|H23|H24-post-H28|H25|H26|H27|H28|H29; mandatory-technical-evidence-green={allMandatoryTechnicalEvidenceGreen}; H28-performance-gate-green={h28PerformanceGateGreen}; H29-production-activation-candidate-green={h29ActivationCandidateGreen};"),
            "H19-qualified-representatives=473/473; H20-rollback-challenges=8/8; H22-corrected-commits=443; H23-corrected-commits=242; H24-post-H28-corrected-commits=9626; H25-corrected-commits=178; H26-explicit-fallback-equivalent=12/12; H27-corrected-commits=529; H29-corrected-commits=400;",
            FormattableString.Invariant($"H28-performance-class={(h28BoundedButCostly ? "bounded-but-costly" : "other")}; H28-median-wall-cost-ratio=4.6214685710690242; H28-p95-wall-cost-ratio=10.684444741413872; H28-median-allocation-ratio=1.1164372201028363; H28-bounds-pass={h28PerformanceGateGreen};"),
            FormattableString.Invariant($"phase-h-production-policy-decision={decisionText}; authoritative-default-policy={defaultDecision.EffectivePolicy}; authoritative-default-initial-condition-version={defaultDecision.InitialCondition.Version}; qualified-opt-in-policy={optInDecision.EffectivePolicy}; qualified-opt-in-initial-condition-version={optInDecision.InitialCondition.Version}; explicit-kill-effective-policy={killDecision.EffectivePolicy}; explicit-kill-initial-condition-version={killDecision.InitialCondition.Version};"),
            "decision-rationale=the corrected path is numerically, operationally, protection/replay, rollback, off-design and long-horizon qualified, but H.28 still classifies its measured runtime cost as bounded-but-costly; therefore the evidence supports qualified opt-in availability but does not justify replacing the cheaper validated explicit default;",
            "H20-contract-replaced=False; H22-commit-seam-replaced=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False; production-fixed-step=10.000 ms; production-selector-changed=False; H24-rerun=False; H28-rerun=False;",
            FormattableString.Invariant($"phase-h-closure-evidence-chain-passes={passes}; h30-audit-passes={passes}; phase-h-closed={passes}; phase-i-unblocked={passes};"),
            "H.30 recommendation: close Phase H as OPT-IN ONLY. Keep exact v2 ExplicitCommittedState authoritative by default and as rollback/reference; retain exact v3 FourNodeBranchContinuityCorrectedCommitOptIn as the qualified opt-in path. Reconsider ACTIVATE only if a later separately scoped optimization/qualification demonstrates a materially better cost class without weakening the numerical contract.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-h-closure-production-qualification-decision.summary.txt"), summary, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "02-phase-h-closure-decision-metrics.csv"),
            new[]
            {
                "metric,value",
                $"mandatory_technical_evidence_green,{allMandatoryTechnicalEvidenceGreen}",
                $"h28_performance_gate_green,{h28PerformanceGateGreen}",
                $"h28_bounded_but_costly,{h28BoundedButCostly}",
                $"h29_activation_candidate_green,{h29ActivationCandidateGreen}",
                $"closure_decision,{decisionText}",
                $"authoritative_default_policy,{defaultDecision.EffectivePolicy}",
                $"authoritative_default_initial_condition_version,{defaultDecision.InitialCondition.Version}",
                $"qualified_opt_in_policy,{optInDecision.EffectivePolicy}",
                $"qualified_opt_in_initial_condition_version,{optInDecision.InitialCondition.Version}",
                $"explicit_kill_policy,{killDecision.EffectivePolicy}",
                $"explicit_kill_initial_condition_version,{killDecision.InitialCondition.Version}",
                $"phase_h_closed,{passes}",
                $"phase_i_unblocked,{passes}",
                $"h30_audit_passes,{passes}",
            },
            Utf8WithoutBom);
    }

    private static void AssertEvidenceContains(string fileName, params string[] expectedTokens)
    {
        var text = File.ReadAllText(Path.Combine(EvidenceDirectory(), fileName));
        foreach (var token in expectedTokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static bool EvidenceContains(string fileName, string token)
        => File.ReadAllText(Path.Combine(EvidenceDirectory(), fileName)).Contains(token, StringComparison.Ordinal);

    private static string EvidenceDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h30-phase-h-closure-production-qualification-decision");

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
            $"{DateTimeOffset.UtcNow:O} H.30 Phase H closure evidence review started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private enum PhaseHClosureDecision
    {
        Activate,
        OptInOnly,
        RemainExplicit,
    }
}
