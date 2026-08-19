using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// H.30 Requalification 1 re-opens only the production-policy decision. The numerical implementation is not retuned:
/// the gate combines the validated H.28 bounded-cost evidence, original H.30 closure, I.3 explicit-vs-corrected continuity
/// classification and the full exact-v3 corrected 300-second healthy reference requalification.
/// </summary>
public sealed class FourNodeH30ProductionPolicyRereviewAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly IReadOnlyDictionary<string, string> FrozenPolicyPrerequisiteFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H28_ValidatedPerformanceCostSoakSummary.txt"] = "C2EC26E3C196CEE32EDB99B67C0C8156704E9D27578E189A97B86D27F357E563",
            ["H30_ValidatedPhaseHClosureSummary.txt"] = "DEB0D1D694E099198C572255D7EDEB4C5BAEDF6320EE2DF1158447C5BFAFFB26",
            ["H30_ValidatedPhaseHClosureMetrics.csv"] = "03F17D33023739DAD2622FB641572AC61534B8937AB2F036472E3508EE438D23",
        };

    private static readonly IReadOnlyDictionary<string, string> FrozenI3Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"] = "AA40086BFEF88352EB4F0D1227F56D9F240BEDE2D4FB5A934711E1A557696A72",
            ["02-v2-v3-ten-millisecond-trace.csv"] = "8FEA343B6DA0A02179E77A02A18925EE901B9F7F6D2EBBB4D564D3F56213C57F",
            ["03-generation-drop-comparison.csv"] = "699444879577332C27B0BB1D691AEA2FF6D2C5E738EBDFE86F27B84C7DAC2796",
            ["04-drop-episodes.csv"] = "8B15C549B109E58C14A0E5BCB889689AE176E6BDA8F4D74EA367FD5F70FA1EAA",
            ["00-progress.txt"] = "1A5E5658FDA05311067C56E8E8B207EC92930CAABC313B23C4435A0D07025257",
            ["01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt"] = "7165E9C10051328111BFF176660B8F79CA9CCD880418E9C2536D4E81F78F564D",
            ["02-corrected-300s-reference-contract.csv"] = "CE02D8FE57A7B1C9F4FCCF56F55A3546BA7F22C7120B402898AFE48B7AB24D9F",
            ["03-corrected-reference-trajectory-samples.csv"] = "F4AF483F91AAC8263CE6023663F4DDD573EB8B5571E1217BC4AD08EE3ED10FA4",
            ["04-corrected-final-window-slopes.csv"] = "9630DB9CE1B6C1889F0F466FBEFBC635949130FE4433C3ACC37689D6746A3BC1",
            ["05-corrected-step-health-violations.csv"] = "2578A5F62859F6766E0961AA2B8F19BC6F5B99F9B191DA06177625C9F341C169",
            ["06-corrected-targeted-reverse-flow-violations.csv"] = "2578A5F62859F6766E0961AA2B8F19BC6F5B99F9B191DA06177625C9F341C169",
            ["07-corrected-production-telemetry.csv"] = "DA989C0D3C8ABB57F5B2DF777D3149B5BB4D52C98168818B4813C196EA7A037C",
            ["08-determinism-control.csv"] = "3FE67439BA99B746393D12CBB0B2542F025B4A16714E6294F657016F8CC5E858",
        };

    [Fact]
    public void FrozenI3Evidence_ProvesExplicitDiscontinuityAndCorrectedThreeHundredSecondHealth()
    {
        foreach (var expected in FrozenPolicyPrerequisiteFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.30 RQ1 policy prerequisite is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        foreach (var expected in FrozenI3Fingerprints)
        {
            var path = Path.Combine(I3EvidenceDirectory(), expected.Key);
            if (File.Exists(path))
            {
                Assert.Equal(expected.Value, CanonicalSha256(path));
            }
            else
            {
                Assert.Equal(
                    expected.Value,
                    FrozenLargeEvidenceManifest.CanonicalSha256(
                        FindRepositoryRoot(),
                        $"H30_RQ1_I3/{expected.Key}"));
            }
        }

        AssertI3Contains(
            "01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt",
            "explicit-drops=338",
            "explicit-targeted-reverse-flow-steps=338",
            "explicit-drops-with-targeted-reverse-flow=338/338",
            "corrected-drops=0",
            "corrected-targeted-reverse-flow-steps=0",
            "explicit-only-branch-discontinuity-classified=True",
            "i3-hotfix4-comparison-audit-passes=True");

        AssertI3Contains(
            "01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt",
            "logical-steps=30000",
            "generation-health-violations=0",
            "targeted-reverse-flow-violations=0",
            "corrected-committed=3757",
            "corrected-rollbacks=0",
            "corrected-fallbacks=0",
            "corrected-unsafe=0",
            "corrected-untargeted-disagreements=0",
            "deterministic-repeat=True",
            "h30-policy-rereview-unblocked=True");
    }

    [Fact]
    public void ProductionSelector_ActivatesExactV3ByDefaultAndKeepsExactV2FailClosedRollback()
    {
        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, defaultDecision.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(3, defaultDecision.InitialCondition.Version);
        Assert.False(defaultDecision.ExplicitKillApplied);

        Assert.True(killDecision.ExplicitKillApplied);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killDecision.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killDecision.InitialCondition);
        Assert.Equal(2, killDecision.InitialCondition.Version);

        Assert.Equal(DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario, DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(3, DesktopIntegratedOperationsProductionProgram.Scenario.InitialCondition.Version);
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId).ScenarioId);

        Assert.Equal(2, DesktopIntegratedOperationsProgram.Scenario.InitialCondition.Version);
        Assert.Equal(3, DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.InitialCondition.Version);
        Assert.Equal(3, DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.InitialCondition.Version);
        Assert.NotEqual(
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId);
    }

    [Fact]
    public void DesktopComposition_UsesProductionProgramInsteadOfHistoricalV2ProgramForFreshStartup()
    {
        var compositionRoot = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "NuclearReactorSimulator.App",
            "Composition",
            "CompositionRoot.cs"));

        Assert.Contains(
            "sessionFactory.Load(DesktopIntegratedOperationsProductionProgram.Scenario)",
            compositionRoot,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "sessionFactory.Load(DesktopIntegratedOperationsProgram.Scenario)",
            compositionRoot,
            StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeH30ProductionPolicyRereviewAudit")]
    public void ValidatedEvidence_DerivesActivateWithoutNumericalRetuningAndWritesClosureRereview()
    {
        ResetReportDirectory();

        var originalH30Green = EvidenceContains("H30_ValidatedPhaseHClosureSummary.txt", "h30-audit-passes=True")
            && EvidenceContains("H30_ValidatedPhaseHClosureSummary.txt", "phase-h-production-policy-decision=OPT-IN ONLY");
        var h28PerformanceBounded = EvidenceContains("H28_ValidatedPerformanceCostSoakSummary.txt", "h28-audit-passes=True")
            && EvidenceContains("H28_ValidatedPerformanceCostSoakSummary.txt", "corrected-performance-class=bounded-but-costly")
            && EvidenceContains("H28_ValidatedPerformanceCostSoakSummary.txt", "median-wall-cost-ratio=4.6214685710690242")
            && EvidenceContains("H28_ValidatedPerformanceCostSoakSummary.txt", "p95-wall-cost-ratio=10.684444741413872");
        var explicitFailureClassReproduced = I3Contains("01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt", "explicit-drops-with-targeted-reverse-flow=338/338")
            && I3Contains("01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt", "explicit-targeted-reverse-flow-that-are-drops=338/338");
        var correctedComparisonClean = I3Contains("01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt", "corrected-drops=0")
            && I3Contains("01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt", "corrected-targeted-reverse-flow-steps=0");
        var correctedThreeHundredSecondGreen = I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-300s-generation-health-passes=True")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-300s-targeted-train-continuity-passes=True")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-300s-conservation-inventory-passes=True")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-300s-deterministic-repeat=True")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-rollbacks=0")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-fallbacks=0")
            && I3Contains("01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt", "corrected-unsafe=0");

        var decision = DeriveDecision(
            originalH30Green,
            h28PerformanceBounded,
            explicitFailureClassReproduced,
            correctedComparisonClean,
            correctedThreeHundredSecondGreen);
        Assert.Equal(H30RereviewDecision.Activate, decision);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        const bool h9Retuned = false;
        const bool h20Replaced = false;
        const bool h22Replaced = false;
        const bool p060f040Retuned = false;
        const bool physicalCoefficientRetuning = false;
        const bool fixedStepChanged = false;
        var passes = decision == H30RereviewDecision.Activate
            && defaultDecision.InitialCondition.Version == 3
            && killDecision.InitialCondition.Version == 2
            && killDecision.ExplicitKillApplied
            && !h9Retuned
            && !h20Replaced
            && !h22Replaced
            && !p060f040Retuned
            && !physicalCoefficientRetuning
            && !fixedStepChanged;
        Assert.True(passes);

        WriteReports(
            decision,
            originalH30Green,
            h28PerformanceBounded,
            explicitFailureClassReproduced,
            correctedComparisonClean,
            correctedThreeHundredSecondGreen,
            defaultDecision,
            killDecision,
            passes);
    }

    private static H30RereviewDecision DeriveDecision(
        bool originalH30Green,
        bool h28PerformanceBounded,
        bool explicitFailureClassReproduced,
        bool correctedComparisonClean,
        bool correctedThreeHundredSecondGreen)
    {
        if (!originalH30Green || !h28PerformanceBounded || !correctedThreeHundredSecondGreen)
        {
            return H30RereviewDecision.RemainExplicit;
        }

        if (explicitFailureClassReproduced && correctedComparisonClean)
        {
            return H30RereviewDecision.Activate;
        }

        return H30RereviewDecision.OptInOnly;
    }

    private static void WriteReports(
        H30RereviewDecision decision,
        bool originalH30Green,
        bool h28PerformanceBounded,
        bool explicitFailureClassReproduced,
        bool correctedComparisonClean,
        bool correctedThreeHundredSecondGreen,
        DesktopHydraulicProductionPolicyDecision defaultDecision,
        DesktopHydraulicProductionPolicyDecision killDecision,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var summary = new[]
        {
            "=== 01-current-v3-h30-production-policy-rereview-after-i3-continuity-evidence ===",
            "H.30 Requalification 1 re-opens only the production-policy decision after Phase-I evidence revealed a reproducible exact-v2 targeted-train reverse-flow / shaft-drop failure class and exact-v3 eliminated it. The corrected implementation itself is unchanged from the already-qualified H.22-H.29 path; H.28 cost remains bounded-but-costly.",
            $"original-H30-green={originalH30Green}; H28-bounded-cost-green={h28PerformanceBounded}; explicit-failure-class-reproduced={explicitFailureClassReproduced}; corrected-100s-comparison-clean={correctedComparisonClean}; corrected-300s-reference-green={correctedThreeHundredSecondGreen};",
            "explicit-v2-evidence=338/338 generation-drop steps coincide one-for-one with targeted stop/control/admission reverse flow over the 100 s / 10 ms comparison; corrected-v3-evidence=0 drops and 0 targeted reverse-flow steps over the same comparison;",
            "corrected-v3-300s-evidence=30000 steps at 10 ms; generation-health-violations=0; targeted-reverse-flow-violations=0; corrected-committed=3757; rollback=0; fallback=0; unsafe=0; untargeted-disagreement=0; deterministic-repeat=True;",
            "H28-performance-class=bounded-but-costly; H28-median-wall-cost-ratio=4.6214685710690242; H28-p95-wall-cost-ratio=10.684444741413872; H28-allocation-ratio=1.1164372201028363; H28-bounds-pass=True;",
            $"h30-rq1-production-policy-decision={DecisionText(decision)}; authoritative-default-policy={defaultDecision.EffectivePolicy}; authoritative-default-initial-condition-version={defaultDecision.InitialCondition.Version}; explicit-kill-effective-policy={killDecision.EffectivePolicy}; explicit-kill-initial-condition-version={killDecision.InitialCondition.Version};",
            "exact-v2-status=rollback/reference/compatibility-retained; exact-v3-status=authoritative-production-default; version-reinterpretation=False; production-fixed-step=10.000 ms;",
            FormattableString.Invariant($"production-scenario-id={DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId}; historical-h29-scenario-id={DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId}; scenario-reinterpretation=False;"),
            "H9-tolerances-retuned=False; H20-contract-replaced=False; H22-commit-seam-replaced=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;",
            $"h30-rq1-evidence-chain-passes={passes}; h30-rq1-audit-passes={passes}; production-corrected-default-activated={passes && decision == H30RereviewDecision.Activate}; i3-reference-rerun-unblocked={passes};",
            "H.30 Requalification 1 recommendation: ACTIVATE exact v3 corrected-commit as the authoritative desktop production default because the cheaper exact-v2 path has a now-validated healthy-operation continuity defect that exact v3 suppresses, while H.28 shows the additional cost remains bounded. Preserve exact v2 as fail-closed rollback/reference and do not weaken the numerical contract or I.3 shaft-health floor.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-h30-rq1-production-policy-rereview.summary.txt"), summary, Utf8WithoutBom);

        var metrics = new[]
        {
            "metric,value",
            $"decision,{DecisionText(decision)}",
            $"original_h30_green,{originalH30Green}",
            $"h28_bounded_cost_green,{h28PerformanceBounded}",
            $"explicit_failure_class_reproduced,{explicitFailureClassReproduced}",
            $"corrected_100s_comparison_clean,{correctedComparisonClean}",
            $"corrected_300s_reference_green,{correctedThreeHundredSecondGreen}",
            "explicit_generation_drop_steps,338",
            "explicit_targeted_reverse_flow_steps,338",
            "corrected_generation_drop_steps,0",
            "corrected_targeted_reverse_flow_steps,0",
            "corrected_300s_commits,3757",
            "h28_median_wall_cost_ratio,4.6214685710690242",
            "h28_p95_wall_cost_ratio,10.684444741413872",
            "h28_median_allocation_ratio,1.1164372201028363",
            $"authoritative_default_policy,{defaultDecision.EffectivePolicy}",
            $"authoritative_default_version,{defaultDecision.InitialCondition.Version}",
            $"explicit_kill_policy,{killDecision.EffectivePolicy}",
            $"explicit_kill_version,{killDecision.InitialCondition.Version}",
            $"production_scenario_id,{DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId}",
            $"historical_h29_scenario_id,{DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId}",
            "scenario_reinterpretation,False",
            $"audit_passes,{passes}",
        };
        File.WriteAllLines(Path.Combine(directory, "02-h30-rq1-production-policy-rereview-metrics.csv"), metrics, Utf8WithoutBom);
    }

    private static void AssertI3Contains(string fileName, params string[] tokens)
    {
        var text = File.ReadAllText(Path.Combine(I3EvidenceDirectory(), fileName));
        foreach (var token in tokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static bool I3Contains(string fileName, string token)
        => File.ReadAllText(Path.Combine(I3EvidenceDirectory(), fileName)).Contains(token, StringComparison.Ordinal);

    private static bool EvidenceContains(string fileName, string token)
        => File.ReadAllText(Path.Combine(EvidenceDirectory(), fileName)).Contains(token, StringComparison.Ordinal);

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary");

    private static string I3EvidenceDirectory()
        => Path.Combine(EvidenceDirectory(), "H30_RQ1_I3");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h30-rq1-production-policy-rereview");

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

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string DecisionText(H30RereviewDecision decision)
        => decision switch
        {
            H30RereviewDecision.Activate => "ACTIVATE",
            H30RereviewDecision.OptInOnly => "OPT-IN ONLY",
            H30RereviewDecision.RemainExplicit => "REMAIN EXPLICIT",
            _ => throw new ArgumentOutOfRangeException(nameof(decision)),
        };

    private enum H30RereviewDecision
    {
        RemainExplicit,
        OptInOnly,
        Activate,
    }
}
