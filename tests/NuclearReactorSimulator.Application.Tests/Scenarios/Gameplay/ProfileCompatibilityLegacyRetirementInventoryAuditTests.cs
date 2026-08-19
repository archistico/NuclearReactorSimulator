using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Application.Scenarios.Xenon;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-I.1 closes the first Phase-I compatibility debt without changing runtime behavior. It inventories every
/// exact-version initial-condition factory registered by the desktop composition, distinguishes current/default/opt-in
/// profiles from compatibility-retained historical identities, and separates production-qualified numerical modes from
/// audit-only Phase-H modes. Exact-version compatibility is retained; no historical save/replay identity is reinterpreted.
/// </summary>
public sealed class ProfileCompatibilityLegacyRetirementInventoryAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private const string FrozenH30SummarySha256 = "DEB0D1D694E099198C572255D7EDEB4C5BAEDF6320EE2DF1158447C5BFAFFB26";
    private const string FrozenH30MetricsSha256 = "03F17D33023739DAD2622FB641572AC61534B8937AB2F036472E3508EE438D23";

    [Fact]
    public void FrozenH30Evidence_ProvesPhaseHClosedAndPhaseIUnblocked()
    {
        AssertFrozenEvidence(
            "H30_ValidatedPhaseHClosureSummary.txt",
            FrozenH30SummarySha256,
            "phase-h-production-policy-decision=OPT-IN ONLY",
            "phase-h-closure-evidence-chain-passes=True",
            "h30-audit-passes=True",
            "phase-h-closed=True",
            "phase-i-unblocked=True");
        AssertFrozenEvidence(
            "H30_ValidatedPhaseHClosureMetrics.csv",
            FrozenH30MetricsSha256,
            "closure_decision,OPT-IN ONLY",
            "authoritative_default_policy,ExplicitCommittedState",
            "qualified_opt_in_policy,H29FourNodeCorrectedCommitCandidate",
            "phase_h_closed,True",
            "phase_i_unblocked,True");
    }

    [Fact]
    public void ExactVersionInventory_EnumeratesSupportedCompatibilityWithoutDeletingReplayIdentities()
    {
        var profiles = ProfileCases();
        var registry = new VersionedInitialConditionRegistry(profiles.Select(static profile => profile.Factory));

        Assert.Equal(12, profiles.Count);
        Assert.Equal(12, registry.Descriptors.Count);
        Assert.Equal(9, profiles.Select(static p => p.Reference.InitialConditionId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(12, profiles.Select(static p => p.Reference).Distinct().Count());
        Assert.DoesNotContain(profiles, static p => p.RetirementAction == "DELETE-NOW");

        foreach (var profile in profiles)
        {
            Assert.Equal(profile.Reference, profile.Factory.Descriptor.Reference);
            Assert.Same(profile.Factory, registry.Resolve(profile.Reference));
        }

        AssertProfile(profiles, "integrated-operations-desktop-stable", 1, "COMPATIBILITY-RETAINED", "RETAIN-EXACT-VERSION");
        AssertProfile(profiles, "integrated-operations-desktop-stable", 2, "AUTHORITATIVE-DEFAULT", "RETAIN");
        AssertProfile(profiles, "integrated-operations-desktop-stable", 3, "QUALIFIED-OPT-IN", "RETAIN");
        AssertProfile(profiles, "pre-synchronization-grid-loading", 1, "COMPATIBILITY-RETAINED", "RETAIN-EXACT-VERSION");
        AssertProfile(profiles, "pre-synchronization-grid-loading", 2, "SUPPORTED-CURRENT", "RETAIN");

        Assert.Equal(
            DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            DesktopHydraulicProductionPolicySelector.Resolve(
                DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy).InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            DesktopHydraulicProductionPolicySelector.Resolve(
                DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy).InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationInitialConditionFactory.Reference,
            DesktopHydraulicProductionPolicySelector.Resolve(
                DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
                explicitKillRequested: true).InitialCondition);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ProfileCompatibilityLegacyRetirementInventoryAudit")]
    public void RegisteredProfiles_ResolveExactRuntimeModesAndProduceRetirementInventory()
    {
        ResetReportDirectory();

        var profiles = ProfileCases();
        var rows = new List<string>
        {
            "initial_condition_id,version,classification,expected_hydraulic_mode,observed_hydraulic_mode,retirement_action,fixed_step_ms",
        };

        foreach (var profile in profiles)
        {
            var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(profile.Factory.CreateRuntimeEngine());
            var observedMode = engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode;
            Assert.Equal(profile.ExpectedHydraulicMode, observedMode);
            Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
            rows.Add(string.Join(",",
                profile.Reference.InitialConditionId,
                profile.Reference.Version,
                profile.Classification,
                profile.ExpectedHydraulicMode,
                observedMode,
                profile.RetirementAction,
                engine.FixedDeltaTime.TotalMilliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState);
        var optInDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState,
            RuntimeMode(DesktopHydraulicProductionPolicySelector.CreateFactory(defaultDecision)));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            RuntimeMode(DesktopHydraulicProductionPolicySelector.CreateFactory(optInDecision)));
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState,
            RuntimeMode(DesktopHydraulicProductionPolicySelector.CreateFactory(killDecision)));

        var directory = ReportDirectory();
        File.WriteAllLines(Path.Combine(directory, "02-profile-compatibility-matrix.csv"), rows, Utf8WithoutBom);
        File.WriteAllLines(
            Path.Combine(directory, "03-numerical-mode-retirement-inventory.csv"),
            new[]
            {
                "mode,phase_i_classification,production_selectable,retirement_action",
                "ExplicitCommittedState,AUTHORITATIVE-PRODUCTION,True,RETAIN",
                "DeterministicHybridSemiImplicit,AUDIT-ONLY-HISTORICAL,False,RETIRE-AFTER-AUDIT-CONSOLIDATION",
                "FourNodeBranchContinuityShadowIntegrated,AUDIT-ONLY-HISTORICAL,False,RETIRE-AFTER-AUDIT-CONSOLIDATION",
                "FourNodeBranchContinuityCorrectedCommitOptIn,QUALIFIED-OPT-IN,True,RETAIN",
            },
            Utf8WithoutBom);

        var compatibilityRetained = profiles.Count(static p => p.Classification == "COMPATIBILITY-RETAINED");
        var deleteNow = profiles.Count(static p => p.RetirementAction == "DELETE-NOW");
        var passes = profiles.Count == 12
            && profiles.Select(static p => p.Reference).Distinct().Count() == 12
            && compatibilityRetained == 2
            && deleteNow == 0
            && defaultDecision.InitialCondition.Version == 2
            && optInDecision.InitialCondition.Version == 3
            && killDecision.InitialCondition.Version == 2;
        Assert.True(passes);

        var summary = new[]
        {
            "=== 01-current-v2-phase-i-profile-compatibility-legacy-retirement-inventory ===",
            "I.1 begins Phase I after validated H.30 closure. It inventories exact-version initial-condition compatibility and numerical-mode lifecycle without changing plant physics, numerical mathematics, the H.30 OPT-IN ONLY production decision, save/replay identity or the 10 ms fixed step.",
            $"registered-profile-versions={profiles.Count}; unique-profile-ids={profiles.Select(static p => p.Reference.InitialConditionId).Distinct(StringComparer.Ordinal).Count()}; compatibility-retained-exact-versions={compatibilityRetained}; delete-now-profile-versions={deleteNow};",
            "authoritative-default=integrated-operations-desktop-stable@2|ExplicitCommittedState; qualified-opt-in=integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn; explicit-kill=integrated-operations-desktop-stable@2|ExplicitCommittedState;",
            "compatibility-retained=integrated-operations-desktop-stable@1|pre-synchronization-grid-loading@1; compatibility-policy=retain exact identities for scenario/save/replay loading; no version reinterpretation;",
            "numerical-mode-lifecycle=ExplicitCommittedState:retain-authoritative|DeterministicHybridSemiImplicit:audit-only-retirement-candidate|FourNodeBranchContinuityShadowIntegrated:audit-only-retirement-candidate|FourNodeBranchContinuityCorrectedCommitOptIn:retain-qualified-opt-in;",
            "audit-only-retirement-rule=do-not-delete H.5/H.21 numerical modes until Phase-I audit consolidation no longer requires executable historical audit seams;",
            "H30-phase-h-closed=True; H30-phase-i-unblocked=True; phase-h-production-policy-decision=OPT-IN ONLY; production-fixed-step=10.000 ms; runtime-behavior-changed=False;",
            $"profile-compatibility-inventory-passes={passes}; i1-audit-passes={passes}; phase-i-compatibility-baseline-established={passes};",
            "I.1 recommendation: retain every exact-version profile identity now; retire only unselected historical numerical audit modes after audit consolidation proves they are no longer required as executable provenance. Proceed next to Phase-I audit consolidation/CI baseline work, not to M10.9.5 yet.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-profile-compatibility-legacy-retirement-inventory.summary.txt"), summary, Utf8WithoutBom);
    }

    private static IReadOnlyList<ProfileCase> ProfileCases()
        => new ProfileCase[]
        {
            new(new ColdShutdownInitialConditionFactory(), new InitialConditionReference("cold-shutdown-pre-start", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new FirstCriticalityInitialConditionFactory(), new InitialConditionReference("pre-criticality-source-range", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new HeatUpTurbineStartupInitialConditionFactory(), new InitialConditionReference("low-power-steam-raising", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new GridSynchronizationInitialConditionFactory(), new InitialConditionReference("pre-synchronization-grid-loading", 1), "COMPATIBILITY-RETAINED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN-EXACT-VERSION"),
            new(new GridSynchronizationSustainedInitialConditionFactory(), new InitialConditionReference("pre-synchronization-grid-loading", 2), "SUPPORTED-CURRENT", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new PowerManoeuvringInitialConditionFactory(), new InitialConditionReference("stable-low-load-parallel-operation", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new DesktopIntegratedOperationsInitialConditionFactory(), new InitialConditionReference("integrated-operations-desktop-stable", 1), "COMPATIBILITY-RETAINED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN-EXACT-VERSION"),
            new(new DesktopSustainedGenerationInitialConditionFactory(), new InitialConditionReference("integrated-operations-desktop-stable", 2), "AUTHORITATIVE-DEFAULT", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(), new InitialConditionReference("integrated-operations-desktop-stable", 3), "QUALIFIED-OPT-IN", HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, "RETAIN"),
            new(new SecondaryTransientInitialConditionFactory(), new InitialConditionReference("secondary-transient-ready", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new XenonRestartInitialConditionFactory(), new InitialConditionReference("post-shutdown-xenon-restart-window", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
            new(new LowPowerXenonInitialConditionFactory(), new InitialConditionReference("poisoned-low-power-operation", 1), "SUPPORTED", HydraulicNumericalCouplingMode.ExplicitCommittedState, "RETAIN"),
        };

    private static HydraulicNumericalCouplingMode RuntimeMode(IVersionedInitialConditionFactory factory)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        return engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode;
    }

    private static void AssertProfile(
        IReadOnlyList<ProfileCase> profiles,
        string id,
        int version,
        string classification,
        string retirementAction)
    {
        var profile = Assert.Single(profiles, p => p.Reference == new InitialConditionReference(id, version));
        Assert.Equal(classification, profile.Classification);
        Assert.Equal(retirementAction, profile.RetirementAction);
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i1-profile-compatibility-legacy-retirement-inventory");

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
            $"{DateTimeOffset.UtcNow:O} I.1 profile compatibility / legacy retirement inventory started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private sealed record ProfileCase(
        IVersionedInitialConditionFactory Factory,
        InitialConditionReference Reference,
        string Classification,
        HydraulicNumericalCouplingMode ExpectedHydraulicMode,
        string RetirementAction);
}
