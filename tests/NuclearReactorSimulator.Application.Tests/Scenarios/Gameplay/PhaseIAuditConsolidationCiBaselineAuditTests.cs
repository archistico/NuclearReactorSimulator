using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.2 consolidates Phase-I audit execution policy around frozen validated evidence and provider-neutral CI entry points.
/// It is observational/test infrastructure only and deliberately does not execute historical H.5/H.21 numerical modes.
/// </summary>
public sealed class PhaseIAuditConsolidationCiBaselineAuditTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void FrozenI1Evidence_ProvesCompatibilityBaselineBeforeAuditConsolidation()
    {
        AssertFrozenEvidence(
            "I1_ValidatedProfileCompatibilityLegacyRetirementInventorySummary.txt",
            "0A69767EC4588123FEB86096E1E5841FE0A9F0738DF7B73CD785213FFA7423E8",
            "profile-compatibility-inventory-passes=True",
            "i1-audit-passes=True",
            "phase-i-compatibility-baseline-established=True",
            "delete-now-profile-versions=0");

        AssertFrozenEvidence(
            "I1_ValidatedProfileCompatibilityMatrix.csv",
            "578E6A378F143EECDAF5737DF8A79B57E85C6C0113F27DE3F2A80CB6FD9D3C22",
            "integrated-operations-desktop-stable,2,AUTHORITATIVE-DEFAULT",
            "integrated-operations-desktop-stable,3,QUALIFIED-OPT-IN");

        AssertFrozenEvidence(
            "I1_ValidatedNumericalModeRetirementInventory.csv",
            "38DE1E724A8B0D3E175C1AA93FE1821A85CA486855203D4B0145543615AA80BD",
            "DeterministicHybridSemiImplicit,AUDIT-ONLY-HISTORICAL,False,RETIRE-AFTER-AUDIT-CONSOLIDATION",
            "FourNodeBranchContinuityShadowIntegrated,AUDIT-ONLY-HISTORICAL,False,RETIRE-AFTER-AUDIT-CONSOLIDATION");
    }

    [Fact]
    public void AuditTierManifest_SeparatesCurrentCiFromHistoricalFrozenProvenance()
    {
        var rows = ReadAuditTierRows();
        Assert.Equal(19, rows.Count);

        AssertTier(rows, "ordinary-suite", "ORDINARY", ordinaryRequired: true, scheduledDefault: false);
        AssertTier(rows, "I2-consolidation", "CURRENT-EVIDENCE", ordinaryRequired: true, scheduledDefault: false);
        AssertTier(rows, "I5-sync-v3-activation", "CURRENT-EVIDENCE", ordinaryRequired: true, scheduledDefault: false);
        AssertTier(rows, "I5-v4-production-activation", "CURRENT-EVIDENCE", ordinaryRequired: true, scheduledDefault: false);
        AssertTier(rows, "gameplay-long", "SCHEDULED-LONG", ordinaryRequired: false, scheduledDefault: true);
        AssertTier(rows, "operational-envelope", "SCHEDULED-LONG", ordinaryRequired: false, scheduledDefault: true);
        AssertTier(rows, "reference-plant-scale", "SCHEDULED-LONG", ordinaryRequired: false, scheduledDefault: true);
        AssertTier(rows, "I5-v4-reference-requalification", "SCHEDULED-LONG", ordinaryRequired: false, scheduledDefault: true);
        AssertTier(rows, "H30-rq1-policy-rereview", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "I3-v3-authoritative-reference", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "I4-known-limitations", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "H30-original", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "I1-compatibility", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "I3-HF4-continuity", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "I3-HF5-corrected-300s", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "H24-post-H28", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "H28-performance", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "H21-shadow-integration", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);
        AssertTier(rows, "H5-hybrid-shadow", "HISTORICAL-FROZEN", ordinaryRequired: false, scheduledDefault: false);

        Assert.DoesNotContain(rows, static row => row.Tier == "HISTORICAL-FROZEN" && (row.OrdinaryRequired || row.ScheduledDefault));
    }

    [Fact]
    public void CiWorkflowContract_UsesGlobalJsonAndKeepsLongGatesOutOfOrdinaryCi()
    {
        var root = FindRepositoryRoot();
        var ordinaryWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ordinary-ci.yml"));
        var longWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "scheduled-long-gates.yml"));
        var ordinaryScript = File.ReadAllText(Path.Combine(root, "eng", "ci-ordinary.cmd"));
        var currentEvidenceScript = File.ReadAllText(Path.Combine(root, "eng", "ci-current-evidence.cmd"));
        var longScript = File.ReadAllText(Path.Combine(root, "eng", "ci-long.cmd"));

        Assert.Contains("actions/checkout@v7", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.Contains("actions/setup-dotnet@v6", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.Contains("global-json-file: global.json", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.Contains("eng\\ci-ordinary.cmd", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("run-gameplay-long-tests.cmd", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("run-four-node-post-h28", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("run-untargeted-disagreement-scan-fast-path-audit.cmd", ordinaryWorkflow, StringComparison.Ordinal);

        Assert.Contains("workflow_dispatch", longWorkflow, StringComparison.Ordinal);
        Assert.Contains("schedule", longWorkflow, StringComparison.Ordinal);
        Assert.Contains("eng\\ci-long.cmd", longWorkflow, StringComparison.Ordinal);

        Assert.Contains("dotnet restore", ordinaryScript, StringComparison.Ordinal);
        Assert.Contains("dotnet build --configuration Release --no-restore", ordinaryScript, StringComparison.Ordinal);
        Assert.Contains("dotnet test --configuration Release --no-build", ordinaryScript, StringComparison.Ordinal);
        Assert.Contains("ci-current-evidence.cmd", ordinaryScript, StringComparison.Ordinal);

        Assert.Contains("run-phase-i-audit-consolidation-ci-baseline-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.Contains("run-i5-synchronization-corrected-v3-activation-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.Contains("run-i5-repaired-exact-v4-production-activation-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-h30-rq1-production-policy-rereview-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-i-known-limitations-legacy-retirement-review-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-h-closure-production-qualification-decision-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-profile-compatibility-legacy-retirement-inventory-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-hybrid-production-integration-tests.cmd", currentEvidenceScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-four-node-orchestrator-shadow-integration-audit.cmd", currentEvidenceScript, StringComparison.Ordinal);

        Assert.Contains("dotnet build --no-restore", longScript, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet build --configuration Release --no-restore", longScript, StringComparison.Ordinal);
        Assert.Contains("run-gameplay-long-tests.cmd", longScript, StringComparison.Ordinal);
        Assert.Contains("run-operational-envelope-audit.cmd", longScript, StringComparison.Ordinal);
        Assert.Contains("run-reference-plant-scale-audit.cmd", longScript, StringComparison.Ordinal);
        Assert.Contains("run-i5-repaired-v4-300s-reference-requalification-audit.cmd", longScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd", longScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-phase-i-corrected-300s-healthy-reference-requalification-audit.cmd", longScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-hybrid-production-integration-tests.cmd", longScript, StringComparison.Ordinal);
        Assert.DoesNotContain("run-four-node-orchestrator-shadow-integration-audit.cmd", longScript, StringComparison.Ordinal);

        var gameplayLongSource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "GameplayJourneyLongRunningTests.cs"));
        var operationalEnvelopeSource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "OperationalEnvelopeExtendedAuditTests.cs"));
        var referenceScaleSource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "ReferencePlantScaleMigrationTests.cs"));
        Assert.Contains("DesktopIntegratedOperationsProductionProgram.Scenario", gameplayLongSource, StringComparison.Ordinal);
        Assert.Contains("DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy", gameplayLongSource, StringComparison.Ordinal);
        Assert.Contains("GridSynchronizationCorrectedInitialConditionFactory", gameplayLongSource, StringComparison.Ordinal);
        Assert.Contains("DesktopIntegratedOperationsProductionProgram.Scenario", operationalEnvelopeSource, StringComparison.Ordinal);
        Assert.Contains("DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy", operationalEnvelopeSource, StringComparison.Ordinal);
        Assert.Contains("DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy", referenceScaleSource, StringComparison.Ordinal);
        Assert.Contains("GridSynchronizationCorrectedInitialConditionFactory", referenceScaleSource, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIAuditConsolidationCiBaselineAudit")]
    public void PhaseIAuditConsolidationCiBaseline_WritesFrozenTieredEngineeringContract()
    {
        ResetReportDirectory();
        var rows = ReadAuditTierRows();

        var ordinaryRequired = rows.Count(static row => row.OrdinaryRequired);
        var scheduledLong = rows.Count(static row => row.ScheduledDefault);
        var historicalFrozen = rows.Count(static row => row.Tier == "HISTORICAL-FROZEN");
        var currentEvidence = rows.Count(static row => row.Tier == "CURRENT-EVIDENCE");
        var legacyModeExecutableDependenciesRemain = rows.Count(static row =>
            (row.AuditId is "H21-shadow-integration" or "H5-hybrid-shadow")
            && row.RetirementDependency == "reviewed-source-retained-not-current-ci");

        var passes = rows.Count == 19
            && ordinaryRequired == 4
            && scheduledLong == 4
            && historicalFrozen == 11
            && currentEvidence == 3
            && legacyModeExecutableDependenciesRemain == 2;
        Assert.True(passes);

        var directory = ReportDirectory();
        File.Copy(Path.Combine(FindRepositoryRoot(), "eng", "phase-i-audit-tiers.csv"), Path.Combine(directory, "02-phase-i-audit-tier-manifest.csv"), overwrite: true);

        var retirementRows = new[]
        {
            "numerical_mode,i2_status,production_selectable,current_ci_dependency,source_dependency_remaining,i2_retirement_authorized",
            "ExplicitCommittedState,RETAIN-ROLLBACK-REFERENCE,True,True,True,False",
            "DeterministicHybridSemiImplicit,HISTORICAL-FROZEN-CANDIDATE,False,False,True,False",
            "FourNodeBranchContinuityShadowIntegrated,HISTORICAL-FROZEN-CANDIDATE,False,False,True,False",
            "FourNodeBranchContinuityCorrectedCommitOptIn,RETAIN-AUTHORITATIVE,True,True,True,False",
        };
        File.WriteAllLines(Path.Combine(directory, "03-legacy-mode-retirement-readiness.csv"), retirementRows, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v4-phase-i-audit-consolidation-ci-baseline ===",
            "I.2 audit/CI contract is realigned for final Phase-I closure: current CI follows repaired exact-v4 desktop production while H.30 RQ1, I.3 exact-v3 and I.4 remain immutable historical evidence. The independent synchronization exact-v3 activation remains current. Plant physics, exact-version persistence semantics and the 10 ms fixed step are not retuned by this CI-contract update.",
            $"audit-contract-entries={rows.Count}; ordinary-required-entries={ordinaryRequired}; current-evidence-entries={currentEvidence}; scheduled-long-entries={scheduledLong}; historical-frozen-entries={historicalFrozen};",
            "ordinary-ci=clean restore|Release build warnings-as-errors|complete ordinary suite|I2 current contract|I5 synchronization-v3 activation|I5 repaired-v4 production activation;",
            "scheduled-long-ci=gameplay-long|operational-envelope|reference-plant-scale|I5-repaired-v4-300s-reference-requalification; H24-post-H28-rerun=False; historical-I3-v3-rerun=False;",
            "historical-frozen-not-ci-required=H30-RQ1|I3-v3-authoritative-reference|I4-known-limitations|H30-original|I1-compatibility|I3-HF4-continuity|I3-HF5-corrected-300s|H24-post-H28|H28-performance|H21-shadow-integration|H5-hybrid-shadow; historical-evidence-preserved=True;",
            $"legacy-h5-h21-current-ci-dependency=False; legacy-h5-h21-source-dependencies-remaining={legacyModeExecutableDependenciesRemain}; legacy-mode-retirement-authorized=False;",
            "authoritative-default=integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn; historical-v3=integrated-operations-desktop-stable@3|HistoricalCorrelationTopology|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|HistoricalCorrelationTopology|ExplicitCommittedState; production-fixed-step=10.000 ms; frozen-I3-budgets-retuned=False;",
            $"phase-i-audit-consolidation-passes={passes}; i2-audit-passes={passes}; phase-i-ci-baseline-established={passes};",
            "I.2 recommendation: use ordinary CI on every push/PR and scheduled/manual current long gates separately. Preserve historical H.5/H.21 executable seams for now; a later retirement milestone may remove them only after their source-level dependencies are explicitly archived or replaced by frozen-evidence contracts.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-audit-consolidation-ci-baseline.summary.txt"), summary, Utf8WithoutBom);
    }

    private static IReadOnlyList<AuditTierRow> ReadAuditTierRows()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "phase-i-audit-tiers.csv");
        Assert.True(File.Exists(path), "Phase-I audit tier manifest is missing.");
        var lines = File.ReadAllLines(path);
        Assert.True(lines.Length >= 2);
        Assert.Equal("audit_id,tier,command,ordinary_required,scheduled_default,execution_role,retirement_dependency", lines[0]);

        return lines.Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line =>
            {
                var fields = line.Split(',');
                Assert.Equal(7, fields.Length);
                return new AuditTierRow(
                    fields[0],
                    fields[1],
                    fields[2],
                    bool.Parse(fields[3]),
                    bool.Parse(fields[4]),
                    fields[5],
                    fields[6]);
            })
            .ToArray();
    }

    private static void AssertTier(
        IReadOnlyList<AuditTierRow> rows,
        string auditId,
        string tier,
        bool ordinaryRequired,
        bool scheduledDefault)
    {
        var row = Assert.Single(rows, candidate => candidate.AuditId == auditId);
        Assert.Equal(tier, row.Tier);
        Assert.Equal(ordinaryRequired, row.OrdinaryRequired);
        Assert.Equal(scheduledDefault, row.ScheduledDefault);
    }

    private static void AssertFrozenEvidence(string fileName, string expectedSha256, params string[] expectedTokens)
    {
        var path = Path.Combine(EvidenceDirectory(), fileName);
        Assert.True(File.Exists(path), $"Frozen Phase-I prerequisite evidence file is missing: {fileName}");
        Assert.Equal(expectedSha256, CanonicalSha256(path));
        var text = File.ReadAllText(path);
        foreach (var token in expectedTokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static string EvidenceDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i2-phase-i-audit-consolidation-ci-baseline");

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
            $"{DateTimeOffset.UtcNow:O} I.2 Phase-I audit consolidation / CI baseline started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private sealed record AuditTierRow(
        string AuditId,
        string Tier,
        string Command,
        bool OrdinaryRequired,
        bool ScheduledDefault,
        string ExecutionRole,
        string RetirementDependency);
}
