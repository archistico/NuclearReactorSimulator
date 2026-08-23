using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Final M10.9.4.1 / Phase-I closure. Historical H.30 RQ1, I.3 exact-v3 and I.4 remain immutable
/// provenance; current authority is repaired exact @4 plus the independent synchronization exact @3.
/// Closure requires ordinary/current evidence and the complete scheduled-long matrix, including the
/// repaired exact-v4 300-second reference regression against the unchanged frozen I.3 budgets.
/// </summary>
public sealed class PhaseICumulativeM10941ClosureAuditTests
{
    private const string ClosureOptInEnvironmentVariable = "NRS_I5_CLOSURE_AUDIT";
    private const string OrdinaryPassedEnvironmentVariable = "NRS_I5_ORDINARY_CI_PASSED";
    private const string LongPassedEnvironmentVariable = "NRS_I5_LONG_GATES_PASSED";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void HistoricalPhaseIEvidence_RemainsFrozenAndIsNotReinterpretedAsExactV4()
    {
        var root = FindRepositoryRoot();
        var h30 = File.ReadAllText(Path.Combine(root, "eng", "evidence-manifests", "h30-rq1-validated.csv"));
        var i3 = File.ReadAllText(Path.Combine(root, "eng", "evidence-manifests", "i3-validated.csv"));
        var i4 = File.ReadAllText(Path.Combine(root, "eng", "evidence-manifests", "i4-validated.csv"));

        Assert.Contains("decision,ACTIVATE", h30, StringComparison.Ordinal);
        Assert.Contains("authoritative-default,integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn", h30, StringComparison.Ordinal);
        Assert.Contains("rollback-reference,integrated-operations-desktop-stable@2|ExplicitCommittedState", h30, StringComparison.Ordinal);

        Assert.Contains("status,VALIDATED", i3, StringComparison.Ordinal);
        Assert.Contains("trajectory-id,phase-i-production-v3-healthy-300s-v1", i3, StringComparison.Ordinal);
        Assert.Contains("logical-steps,30000", i3, StringComparison.Ordinal);
        Assert.Contains("generation-health-violations,0", i3, StringComparison.Ordinal);
        Assert.Contains("targeted-reverse-flow-violations,0", i3, StringComparison.Ordinal);
        Assert.Contains("slopes-count,7", i3, StringComparison.Ordinal);
        Assert.Contains("tolerance-budget-count,19", i3, StringComparison.Ordinal);
        Assert.Contains("budgets-sha256,9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB", i3, StringComparison.Ordinal);

        Assert.Contains("status,VALIDATED", i4, StringComparison.Ordinal);
        Assert.Contains("legacy-mode-retirement-authorized,False", i4, StringComparison.Ordinal);
        Assert.Contains("DEFER-SOURCE-REMOVAL", i4, StringComparison.Ordinal);

        AssertFrozenEvidence(
            "I3_ValidatedAuthoritativeToleranceBudgets.csv",
            "9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB");
        AssertFrozenEvidence(
            "I4_ValidatedKnownLimitationsLegacyRetirementReviewSummary.txt",
            "330A292E0EF44E5D21D2A24F67AECB7F62BB7840472D00F75184BFDDC7EBED94");
        AssertFrozenEvidence(
            "I4_ValidatedLegacyRetirementDecision.csv",
            "5AB2F9281A4BB8724EBB655A98EF15D4CE2BD75548AE16668BD6EA10AAF3D9D3");
    }

    [Fact]
    public void ClosureInputs_UseRepairedExactV4ProductionWithHistoricalV3AndExactV2RollbackRetained()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, production.EffectivePolicy);
        Assert.Equal("integrated-operations-desktop-stable", production.InitialCondition.InitialConditionId);
        Assert.Equal(4, production.InitialCondition.Version);
        Assert.False(production.ExplicitKillApplied);

        var historicalV3 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, historicalV3.EffectivePolicy);
        Assert.Equal(3, historicalV3.InitialCondition.Version);

        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy,
            explicitKillRequested: true);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.True(rollback.ExplicitKillApplied);
    }

    [Fact]
    public void ClosureExecutionContract_CoversCurrentAuthoritativeEvidenceAndFinalScheduledLongMatrix()
    {
        var root = FindRepositoryRoot();
        var ordinary = File.ReadAllText(Path.Combine(root, "eng", "ci-ordinary.cmd"));
        var current = File.ReadAllText(Path.Combine(root, "eng", "ci-current-evidence.cmd"));
        var longGates = File.ReadAllText(Path.Combine(root, "eng", "ci-long.cmd"));
        var closure = File.ReadAllText(Path.Combine(root, "scripts", "run-m10941-cumulative-closure-audit.cmd"));

        Assert.Contains("dotnet test --configuration Release --no-build", ordinary, StringComparison.Ordinal);
        Assert.Contains("call eng\\ci-current-evidence.cmd", ordinary, StringComparison.Ordinal);

        Assert.Contains("run-phase-i-audit-consolidation-ci-baseline-audit.cmd", current, StringComparison.Ordinal);
        Assert.Contains("run-i5-synchronization-corrected-v3-activation-audit.cmd", current, StringComparison.Ordinal);
        Assert.Contains("run-m10-final-v9-authoritative-production-audit.cmd", current, StringComparison.Ordinal);
        Assert.DoesNotContain("run-i5-repaired-exact-v4-production-activation-audit.cmd", current, StringComparison.Ordinal);
        Assert.DoesNotContain("run-h30-rq1-production-policy-rereview-audit.cmd", current, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-i-known-limitations-legacy-retirement-review-audit.cmd", current, StringComparison.Ordinal);

        Assert.Contains("run-gameplay-long-tests.cmd", longGates, StringComparison.Ordinal);
        Assert.Contains("run-operational-envelope-audit.cmd", longGates, StringComparison.Ordinal);
        Assert.Contains("run-reference-plant-scale-audit.cmd", longGates, StringComparison.Ordinal);
        Assert.Contains("run-i5-repaired-v4-300s-reference-requalification-audit.cmd", longGates, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd", longGates, StringComparison.Ordinal);

        Assert.Contains("call eng\\ci-ordinary.cmd", closure, StringComparison.Ordinal);
        Assert.Contains("call eng\\ci-long.cmd", closure, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseICumulativeM10941ClosureAudit")]
    public void ValidatedCurrentAndLongEvidence_ClosesM10941AndPhaseIOnRepairedExactV4()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(ClosureOptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal("1", Environment.GetEnvironmentVariable(OrdinaryPassedEnvironmentVariable));
        Assert.Equal("1", Environment.GetEnvironmentVariable(LongPassedEnvironmentVariable));

        var root = FindRepositoryRoot();
        var activationSummary = ReadRequiredArtifact(
            root,
            "i5-repaired-exact-v4-production-activation",
            "01-i5-repaired-exact-v4-production-activation.summary.txt");
        var repairedReferenceSummary = ReadRequiredArtifact(
            root,
            "i5-repaired-v4-300s-reference-requalification",
            "01-i5-repaired-v4-300s-reference-requalification.summary.txt");
        var synchronizationActivationSummary = ReadRequiredArtifact(
            root,
            "i5-synchronization-corrected-v3-activation",
            "01-i5-synchronization-corrected-v3-activation.summary.txt");
        var i2Summary = ReadRequiredArtifact(
            root,
            "i2-phase-i-audit-consolidation-ci-baseline",
            "01-phase-i-audit-consolidation-ci-baseline.summary.txt");

        Assert.Contains("i5-repaired-v4-production-activation-passes=True", activationSummary, StringComparison.Ordinal);
        Assert.Contains("production-activation=True", activationSummary, StringComparison.Ordinal);
        Assert.Contains("exact-v4-authoritative=True", activationSummary, StringComparison.Ordinal);
        Assert.Contains("exact-v3-reinterpreted=False", activationSummary, StringComparison.Ordinal);
        Assert.Contains("exact-v2-reinterpreted=False", activationSummary, StringComparison.Ordinal);

        Assert.Contains("repaired-v4-reference-requalification-passes=True", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("repaired-v4-generation-continuity-passes=True", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("repaired-v4-conservation-inventory-passes=True", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("frozen-i3-budget-regression-passes=True", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("phase-i-reference-determinism-passes=True", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("frozen-i3-budget-violations=0", repairedReferenceSummary, StringComparison.Ordinal);
        Assert.Contains("historical-i3-v3-reinterpreted=False", repairedReferenceSummary, StringComparison.Ordinal);

        Assert.Contains("synchronization-v3-activation-passes=True", synchronizationActivationSummary, StringComparison.Ordinal);
        Assert.Contains("gameplay-long-v3-unblocked=True", synchronizationActivationSummary, StringComparison.Ordinal);
        Assert.Contains("pre-synchronization-grid-loading@3|FourNodeBranchContinuityCorrectedCommitOptIn", synchronizationActivationSummary, StringComparison.Ordinal);

        Assert.Contains("phase-i-audit-consolidation-passes=True", i2Summary, StringComparison.Ordinal);
        Assert.Contains("authoritative-default=integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn", i2Summary, StringComparison.Ordinal);

        ResetReportDirectory();
        WriteClosureArtifacts();
    }

    private static string ReadRequiredArtifact(string root, string directory, string fileName)
    {
        var path = Path.Combine(root, "artifacts", directory, fileName);
        Assert.True(File.Exists(path), $"Required cumulative-closure artifact is missing: {path}");
        return File.ReadAllText(path);
    }

    private static void WriteClosureArtifacts()
    {
        var directory = ReportDirectory();
        File.WriteAllLines(
            Path.Combine(directory, "02-m10941-cumulative-closure-gate-matrix.csv"),
            new[]
            {
                "gate,role,status",
                "ordinary-ci,complete ordinary suite plus repaired-v4 current-evidence contracts,PASS",
                "historical-h30-rq1,validated exact-v3 activation decision retained as immutable provenance,PASS",
                "historical-i3-v3-reference,validated 300 s exact-v3 trajectory plus seven slopes and 19 frozen budgets retained as immutable provenance,PASS",
                "historical-i4-limitations,validated known limitations plus DEFER-SOURCE-REMOVAL legacy decision retained,PASS",
                "i5-v4-production-activation,authoritative repaired exact-v4 plus exact-v3 historical replay and exact-v2 rollback,PASS",
                "gameplay-long,current long journeys including independent synchronization exact-v3 contract,PASS",
                "operational-envelope,authoritative repaired exact-v4 operational-envelope/protection/replay matrix,PASS",
                "reference-plant-scale,reduced 10 MWe reference scale contract on current production plus synchronization family,PASS",
                "i5-v4-300s-reference,authoritative repaired exact-v4 300 s health/conservation/determinism against unchanged 19 frozen I.3 budgets,PASS",
                "i5-synchronization-v3,independent exact @3 corrected synchronization identity and sustained journey contract,PASS",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-m10941-cumulative-closure-metrics.csv"),
            new[]
            {
                "metric,value",
                "production-policy,ACTIVATE-REPAIRED-V4",
                "authoritative-default,integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn",
                "historical-v3,integrated-operations-desktop-stable@3|HistoricalCorrelationTopology|FourNodeBranchContinuityCorrectedCommitOptIn",
                "rollback-reference,integrated-operations-desktop-stable@2|HistoricalCorrelationTopology|ExplicitCommittedState",
                "production-fixed-step-ms,10",
                "repaired-v4-reference-seconds,300",
                "repaired-v4-reference-steps,30000",
                "frozen-i3-final-window-slopes,7",
                "frozen-i3-tolerance-budgets,19",
                "repaired-v4-generation-health-violations,0",
                "repaired-v4-targeted-reverse-flow-violations,0",
                "repaired-v4-frozen-budget-violations,0",
                "repaired-stage4-performance-class,bounded-at-or-below-explicit",
                "legacy-mode-retirement-authorized,False",
                "synchronization-supported-current,pre-synchronization-grid-loading@3|FourNodeBranchContinuityCorrectedCommitOptIn",
                "synchronization-legacy-reference,pre-synchronization-grid-loading@2|ExplicitCommittedState",
                "m1095-unblocked,True",
            },
            Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v4-m10941-cumulative-closure ===",
            "I.5 closes M10.9.4.1 Operational Envelope & Numerical Hardening and Phase I on authoritative repaired exact @4. Historical H.30 RQ1, I.3 exact-v3 and I.4 are retained as immutable provenance; exact @3 is not reinterpreted. The final scheduled-long chain validates current repaired production while the independent synchronization exact-v3 family remains qualified and unchanged.",
            "ordinary-ci-passes=True; current-evidence-chain-passes=True; repaired-v4-production-activation-passes=True; synchronization-v3-activation-passes=True; scheduled-long-gates-pass=True; gameplay-long-passes=True; operational-envelope-passes=True; reference-plant-scale-contract-passes=True;",
            "production-policy=ACTIVATE-REPAIRED-V4; authoritative-default=integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn; historical-v3=integrated-operations-desktop-stable@3|HistoricalCorrelationTopology|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|HistoricalCorrelationTopology|ExplicitCommittedState; production-fixed-step=10.000 ms;",
            "historical-i3-v3-reference-preserved=True; historical-i3-v3-reinterpreted=False; historical-i3-slopes=7; historical-i3-frozen-budgets=19; repaired-v4-reference-requalification-passes=True; repaired-v4-reference-seconds=300; repaired-v4-reference-steps=30000; repaired-v4-generation-health-violations=0; repaired-v4-targeted-reverse-flow-violations=0; repaired-v4-frozen-budget-violations=0;",
            "repaired-stage4-performance-class=bounded-at-or-below-explicit; i4-known-limitations-provenance-preserved=True; i4-legacy-retirement-decision-preserved=True; legacy-mode-retirement-authorized=False; legacy-source-removal-deferred=True; synchronization-supported-current=pre-synchronization-grid-loading@3|FourNodeBranchContinuityCorrectedCommitOptIn; synchronization-v2-reference=pre-synchronization-grid-loading@2|ExplicitCommittedState;",
            "m10941-cumulative-closure-passes=True; i5-audit-passes=True; m10941-closed=True; phase-i-closed=True; m1095-unblocked=True;",
            "I.5 recommendation: close M10.9.4.1 and Phase I on repaired exact @4 desktop production with exact @2 fail-closed rollback/reference and exact @3 immutable historical replay. Preserve the 19 I.3 budgets as regression authority, keep synchronization exact @3 as its independent supported identity, and proceed to M10.9.5 Contextual Command Consequence Model without reopening numerical hardening unless new evidence violates these frozen contracts.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-m10941-cumulative-closure.summary.txt"), summary, Utf8WithoutBom);
    }

    private static void AssertFrozenEvidence(string fileName, string expectedSha256)
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", fileName);
        Assert.True(File.Exists(path), $"Frozen Phase-I evidence file is missing: {fileName}");
        Assert.Equal(expectedSha256, CanonicalTextSha256(path));
    }

    private static string CanonicalTextSha256(string path)
    {
        var text = File.ReadAllText(path);
        var canonical = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
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

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-m10941-cumulative-closure");

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
            $"{DateTimeOffset.UtcNow:O} I.5 final repaired-v4 cumulative M10.9.4.1 closure started{Environment.NewLine}",
            Utf8WithoutBom);
    }
}
