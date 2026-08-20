using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-I.4 reconciles the validated I.3 production-reference drift observations with current known limitations
/// and reviews the remaining H.5/H.21 historical numerical modes for safe retirement. It is evidence/review-only:
/// exact-version identities, production policy, plant physics, numerical mathematics and the 10 ms fixed step are unchanged.
/// </summary>
public sealed class PhaseIKnownLimitationsLegacyRetirementReviewAuditTests
{
    private const string OptInEnvironmentVariable = "NRS_I4_LIMITATIONS_RETIREMENT_AUDIT";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void I3ValidatedManifest_FreezesAuthoritativeReferenceSlopesAndBudgets()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText(Path.Combine(root, "eng", "evidence-manifests", "i3-validated.csv"));

        Assert.Contains("status,VALIDATED", manifest, StringComparison.Ordinal);
        Assert.Contains("authoritative-default,integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn", manifest, StringComparison.Ordinal);
        Assert.Contains("rollback-reference,integrated-operations-desktop-stable@2|ExplicitCommittedState", manifest, StringComparison.Ordinal);
        Assert.Contains("generation-health-violations,0", manifest, StringComparison.Ordinal);
        Assert.Contains("targeted-reverse-flow-violations,0", manifest, StringComparison.Ordinal);
        Assert.Contains("slopes-count,7", manifest, StringComparison.Ordinal);
        Assert.Contains("tolerance-budget-count,19", manifest, StringComparison.Ordinal);
        Assert.Contains("drum-inventory-slope-kg-s,8.2451672984622224", manifest, StringComparison.Ordinal);
        Assert.Contains("main-steam-header-slope-kg-s,-0.35293086123580603", manifest, StringComparison.Ordinal);
        Assert.Contains("total-fluid-energy-slope-w,-2061802.7621648791", manifest, StringComparison.Ordinal);

        AssertFrozenEvidence(
            "I3_ValidatedAuthoritativeReferenceSummary.txt",
            "CA7F21A568CC32C5F7558E7B4F45E2A8C241F02B9CFEBBADB17A618FF8AA57C7");
        AssertFrozenEvidence(
            "I3_ValidatedAuthoritativeReferenceSlopes.csv",
            "9630DB9CE1B6C1889F0F466FBEFBC635949130FE4433C3ACC37689D6746A3BC1");
        AssertFrozenEvidence(
            "I3_ValidatedAuthoritativeToleranceBudgets.csv",
            "9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB");
        AssertFrozenEvidence(
            "I3_ValidatedAuthoritativeDeterminism.csv",
            "3FE67439BA99B746393D12CBB0B2542F025B4A16714E6294F657016F8CC5E858");
    }

    [Fact]
    public void ProductionSelector_RetainsValidatedV3DefaultAndExactV2Rollback()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal("integrated-operations-desktop-stable", production.InitialCondition.InitialConditionId);
        Assert.Equal(3, production.InitialCondition.Version);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, production.EffectivePolicy);
        Assert.False(production.ExplicitKillApplied);

        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);
        Assert.Equal("integrated-operations-desktop-stable", rollback.InitialCondition.InitialConditionId);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.True(rollback.ExplicitKillApplied);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIKnownLimitationsLegacyRetirementReviewAudit")]
    public void CurrentLimitationsAndLegacyModes_ProduceConservativeRetirementDecision()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root, "src");
        var testsRoot = Path.Combine(root, "tests");

        var hybridSourceFiles = FilesContaining(sourceRoot, "DeterministicHybridSemiImplicit");
        var hybridTestFiles = FilesContaining(testsRoot, "DeterministicHybridSemiImplicit");
        var shadowSourceFiles = FilesContaining(sourceRoot, "FourNodeBranchContinuityShadowIntegrated");
        var shadowTestFiles = FilesContaining(testsRoot, "FourNodeBranchContinuityShadowIntegrated");

        Assert.Equal(4, hybridSourceFiles.Count);
        Assert.Equal(4, hybridTestFiles.Count);
        Assert.Equal(4, shadowSourceFiles.Count);
        Assert.Equal(4, shadowTestFiles.Count);

        var tiers = File.ReadAllText(Path.Combine(root, "eng", "phase-i-audit-tiers.csv"));
        Assert.Contains("H21-shadow-integration,HISTORICAL-FROZEN", tiers, StringComparison.Ordinal);
        Assert.Contains("H5-hybrid-shadow,HISTORICAL-FROZEN", tiers, StringComparison.Ordinal);
        Assert.Contains("reviewed-source-retained-not-current-ci", tiers, StringComparison.Ordinal);

        var limitations = File.ReadAllText(Path.Combine(root, "docs", "KNOWN_MODEL_LIMITATIONS.md"));
        Assert.Contains("8.2451672984622224 kg/s", limitations, StringComparison.Ordinal);
        Assert.Contains("-0.35293086123580603 kg/s", limitations, StringComparison.Ordinal);
        Assert.Contains("-2.061802762164879 MW", limitations, StringComparison.Ordinal);
        Assert.Contains("regression baseline, not a claim of asymptotic steady state", limitations, StringComparison.Ordinal);
        Assert.Contains("source-retained historical modes", limitations, StringComparison.Ordinal);

        var retirementContract = File.ReadAllText(Path.Combine(root, "eng", "phase-i-legacy-retirement-review.csv"));
        Assert.Contains("DeterministicHybridSemiImplicit,historical-audit-only,False,False,False,4,4,DEFER-SOURCE-REMOVAL", retirementContract, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityShadowIntegrated,historical-audit-only,False,False,False,4,4,DEFER-SOURCE-REMOVAL", retirementContract, StringComparison.Ordinal);

        var passes = hybridSourceFiles.Count == 4
            && hybridTestFiles.Count == 4
            && shadowSourceFiles.Count == 4
            && shadowTestFiles.Count == 4;
        Assert.True(passes);

        WriteArtifacts(hybridSourceFiles, hybridTestFiles, shadowSourceFiles, shadowTestFiles, passes);
    }

    private static IReadOnlyList<string> FilesContaining(string directory, string token)
        => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
            .Where(static path => path is not "src/NuclearReactorSimulator.Application/ApplicationDescriptor.cs")
            .Where(static path => path is not "tests/NuclearReactorSimulator.Application.Tests/ApplicationDescriptorTests.cs")
            .Where(static path => !path.EndsWith("/PhaseIKnownLimitationsLegacyRetirementReviewAuditTests.cs", StringComparison.Ordinal))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

    private static void WriteArtifacts(
        IReadOnlyList<string> hybridSourceFiles,
        IReadOnlyList<string> hybridTestFiles,
        IReadOnlyList<string> shadowSourceFiles,
        IReadOnlyList<string> shadowTestFiles,
        bool passes)
    {
        var directory = ReportDirectory();
        var dependencyRows = new List<string>
        {
            "mode,dependency_kind,file",
        };
        dependencyRows.AddRange(hybridSourceFiles.Select(static path => $"DeterministicHybridSemiImplicit,source,{path}"));
        dependencyRows.AddRange(hybridTestFiles.Select(static path => $"DeterministicHybridSemiImplicit,test,{path}"));
        dependencyRows.AddRange(shadowSourceFiles.Select(static path => $"FourNodeBranchContinuityShadowIntegrated,source,{path}"));
        dependencyRows.AddRange(shadowTestFiles.Select(static path => $"FourNodeBranchContinuityShadowIntegrated,test,{path}"));
        File.WriteAllLines(Path.Combine(directory, "02-legacy-source-dependencies.csv"), dependencyRows, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-known-limitation-observations.csv"),
            new[]
            {
                "observation_id,value,unit,classification",
                "i3-drum-inventory-final-window-slope,8.2451672984622224,kg/s,FROZEN-REGRESSION-DRIFT",
                "i3-main-steam-header-final-window-slope,-0.35293086123580603,kg/s,FROZEN-REGRESSION-DRIFT",
                "i3-total-fluid-internal-energy-final-window-slope,-2061802.7621648791,W,FROZEN-REGRESSION-DRIFT",
                "h28-corrected-median-wall-cost-ratio,4.6214685710690242,ratio,BOUNDED-BUT-COSTLY",
                "h28-corrected-p95-wall-cost-ratio,10.684444741413872,ratio,BOUNDED-BUT-COSTLY",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "04-legacy-retirement-decision.csv"),
            new[]
            {
                "mode,production_dependency,exact_version_dependency,current_ci_dependency,source_dependency,test_dependency,decision",
                "DeterministicHybridSemiImplicit,False,False,False,True,True,DEFER-SOURCE-REMOVAL",
                "FourNodeBranchContinuityShadowIntegrated,False,False,False,True,True,DEFER-SOURCE-REMOVAL",
            },
            Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v3-phase-i-known-limitations-legacy-retirement-review ===",
            "I.4 reviews current limitations and historical numerical-mode retirement after validated I.3. It does not change runtime physics, numerical mathematics, exact-version persistence, the H.30 RQ1 ACTIVATE policy or the 10 ms fixed step.",
            "i3-authoritative-reference-validated=True; i3-slopes=7; i3-tolerance-budgets=19; generation-health-violations=0; targeted-reverse-flow-violations=0;",
            "known-drift-observations=drum-inventory:+8.2451672984622224kg/s|main-steam-header:-0.35293086123580603kg/s|total-fluid-internal-energy:-2061802.7621648791W; interpretation=regression-baseline-not-asymptotic-steady-state-proof;",
            "legacy-mode=DeterministicHybridSemiImplicit; production-dependency=False; exact-version-dependency=False; current-ci-dependency=False; source-files=4; test-files=4; retirement-decision=DEFER-SOURCE-REMOVAL;",
            "legacy-mode=FourNodeBranchContinuityShadowIntegrated; production-dependency=False; exact-version-dependency=False; current-ci-dependency=False; source-files=4; test-files=4; retirement-decision=DEFER-SOURCE-REMOVAL;",
            "legacy-mode-retirement-authorized=False; exact-version-identities-preserved=True; source-retained-historical-modes=True;",
            "authoritative-default=integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|ExplicitCommittedState; H28-performance-class=bounded-but-costly; production-fixed-step=10.000 ms; runtime-behavior-changed=False;",
            $"phase-i-known-limitations-review-passes={passes}; phase-i-legacy-retirement-review-passes={passes}; i4-audit-passes={passes}; i5-closure-gate-unblocked={passes};",
            "I.4 recommendation: keep the two historical numerical modes source-retained through M10.9.4.1 closure because executable historical seams still compile against them. Do not expose them as production choices. Proceed to I.5 cumulative closure; perform physical source removal only as a separately scoped maintenance change after historical executable tests are archived or replaced.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-known-limitations-legacy-retirement-review.summary.txt"), summary, Utf8WithoutBom);
    }

    private static void AssertFrozenEvidence(string fileName, string expectedSha256)
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", fileName);
        Assert.True(File.Exists(path), $"Frozen I.3 evidence file is missing: {fileName}");
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i4-phase-i-known-limitations-legacy-retirement-review");

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
            $"{DateTimeOffset.UtcNow:O} I.4 known-limitations / legacy-retirement review started{Environment.NewLine}",
            Utf8WithoutBom);
    }
}
