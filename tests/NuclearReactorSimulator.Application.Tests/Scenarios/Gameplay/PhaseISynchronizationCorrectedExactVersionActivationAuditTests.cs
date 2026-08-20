using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 activation contract after the validated Hotfix 5 qualification. It registers exact synchronization v3 as the
/// supported sustained version while preserving exact v1/v2 identities, and verifies that the scheduled long journey
/// uses the qualified 10 s stabilization / 20-60 s sustained contract without retuning plant physics or controller gains.
/// </summary>
public sealed class PhaseISynchronizationCorrectedExactVersionActivationAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private const string FrozenQualificationSummarySha256 = "7F99941BBDB38E927514A4F868AEF334013DE629DEF1BF545AE62BC947A92515";
    private const string FrozenQualificationMetricsSha256 = "8BA6AD9381B491C8F6CFEE21F4D59ED31B55A77CFDEFD2959BEC2CE880FCFAB2";
    private const string FrozenQualificationCheckpointsSha256 = "6826A2085E911A8B5740EC2CC544CF84A7FC940C7EF460D2410F71F2266CF81C";

    [Fact]
    public void FrozenHotfix5Qualification_ProvesExactV3IsSafeToActivate()
    {
        AssertFrozenEvidence(
            "I5_HF5_ValidatedSynchronizationCorrectedV3QualificationSummary.txt",
            FrozenQualificationSummarySha256,
            "selected-contract=corrected-only",
            "physical-retuning=False",
            "governor-retuning=False",
            "steam-capacity-retuning=False",
            "stop-grade-retuning=False",
            "stable-gross-violations=0",
            "stable-rotor-violations=0",
            "stable-shaft-violations=0",
            "stable-reverse-admission-violations=0",
            "synchronization-v3-qualification-passes=True",
            "i5-long-journey-v3-activation-unblocked=True");

        AssertFrozenEvidence(
            "I5_HF5_ValidatedSynchronizationCorrectedV3QualificationMetrics.csv",
            FrozenQualificationMetricsSha256,
            "minimum_stable_gross_mwe,4.49895163786615",
            "minimum_stable_shaft_mw,5.091457952202167",
            "minimum_stable_rotor_rpm,2998.7683197564124",
            "maximum_stable_rotor_rpm,3003.8189758791232",
            "qualification_passes,True");

        AssertFrozenEvidence(
            "I5_HF5_ValidatedSynchronizationCorrectedV3QualificationCheckpoints.csv",
            FrozenQualificationCheckpointsSha256,
            "1,10,3.8747397307762608",
            "2,20,4.5367303464842825",
            "6,60,4.6149367436954929");
    }

    [Fact]
    public void SynchronizationExactVersions_PreserveHistoricalV1V2AndExposeV3AsSupportedCurrent()
    {
        var v1 = new GridSynchronizationInitialConditionFactory();
        var v2 = new GridSynchronizationSustainedInitialConditionFactory();
        var v3 = new GridSynchronizationCorrectedInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { v1, v2, v3 });

        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 1), v1.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 2), v2.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 3), v3.Descriptor.Reference);
        Assert.Equal(3, registry.Descriptors.Count);
        Assert.Same(v1, registry.Resolve(v1.Descriptor.Reference));
        Assert.Same(v2, registry.Resolve(v2.Descriptor.Reference));
        Assert.Same(v3, registry.Resolve(v3.Descriptor.Reference));

        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, RuntimeMode(v1));
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, RuntimeMode(v2));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, RuntimeMode(v3));
        Assert.Equal(TimeSpan.FromMilliseconds(10d), FixedStep(v1));
        Assert.Equal(TimeSpan.FromMilliseconds(10d), FixedStep(v2));
        Assert.Equal(TimeSpan.FromMilliseconds(10d), FixedStep(v3));

        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 1), GridSynchronizationLoadProgram.InitialCondition);

        var compositionRoot = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "NuclearReactorSimulator.App",
            "Composition",
            "CompositionRoot.cs"));
        Assert.Contains("new GridSynchronizationInitialConditionFactory()", compositionRoot, StringComparison.Ordinal);
        Assert.Contains("new GridSynchronizationSustainedInitialConditionFactory()", compositionRoot, StringComparison.Ordinal);
        Assert.Contains("new GridSynchronizationCorrectedInitialConditionFactory()", compositionRoot, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(compositionRoot, "new GridSynchronizationCorrectedInitialConditionFactory()"));
    }

    [Fact]
    public void ScheduledLongJourney_UsesExactV3WithExplicitStabilizationAndStrictSustainedWindow()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "GameplayJourneyLongRunningTests.cs"));

        Assert.Contains("new GridSynchronizationCorrectedInitialConditionFactory()", source, StringComparison.Ordinal);
        Assert.Contains("GridSynchronizationCorrectedInitialConditionFactory.Reference", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new GridSynchronizationSustainedInitialConditionFactory(),", source, StringComparison.Ordinal);
        Assert.Contains("if (checkpoint == 1)", source, StringComparison.Ordinal);
        Assert.Contains("2_950d, 3_050d", source, StringComparison.Ordinal);
        Assert.Contains("2_990d, 3_010d", source, StringComparison.Ordinal);
        Assert.Contains("generator.ElectricalOutput.NumericValue ?? 0d) > 4.0d", source, StringComparison.Ordinal);
        Assert.Contains("admissionTrain.AdmissionFlow.NumericValue", source, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseISynchronizationCorrectedExactVersionActivationAudit")]
    public void ValidatedQualification_ActivatesExactV3RegistryAndLongJourneyContract()
    {
        ResetReportDirectory();

        var v1 = new GridSynchronizationInitialConditionFactory();
        var v2 = new GridSynchronizationSustainedInitialConditionFactory();
        var v3 = new GridSynchronizationCorrectedInitialConditionFactory();
        var v1Mode = RuntimeMode(v1);
        var v2Mode = RuntimeMode(v2);
        var v3Mode = RuntimeMode(v3);

        var compositionRoot = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "NuclearReactorSimulator.App",
            "Composition",
            "CompositionRoot.cs"));
        var journeySource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "GameplayJourneyLongRunningTests.cs"));

        var qualificationGreen = FrozenEvidenceContains(
            "I5_HF5_ValidatedSynchronizationCorrectedV3QualificationSummary.txt",
            "synchronization-v3-qualification-passes=True")
            && FrozenEvidenceContains(
                "I5_HF5_ValidatedSynchronizationCorrectedV3QualificationSummary.txt",
                "stable-reverse-admission-violations=0");
        var v3Registered = CountOccurrences(compositionRoot, "new GridSynchronizationCorrectedInitialConditionFactory()") == 1;
        var v2Preserved = v2.Descriptor.Reference.Version == 2 && v2Mode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        var v3Supported = v3.Descriptor.Reference.Version == 3 && v3Mode == HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn;
        var longJourneyAligned = journeySource.Contains("GridSynchronizationCorrectedInitialConditionFactory.Reference", StringComparison.Ordinal)
            && journeySource.Contains("if (checkpoint == 1)", StringComparison.Ordinal)
            && journeySource.Contains("2_950d, 3_050d", StringComparison.Ordinal)
            && journeySource.Contains("2_990d, 3_010d", StringComparison.Ordinal)
            && journeySource.Contains("generator.ElectricalOutput.NumericValue ?? 0d) > 4.0d", StringComparison.Ordinal)
            && journeySource.Contains("admissionTrain.AdmissionFlow.NumericValue", StringComparison.Ordinal);

        var passes = qualificationGreen
            && v1Mode == HydraulicNumericalCouplingMode.ExplicitCommittedState
            && v2Preserved
            && v3Supported
            && v3Registered
            && longJourneyAligned;

        var directory = ReportDirectory();
        File.WriteAllLines(
            Path.Combine(directory, "02-synchronization-exact-version-activation-matrix.csv"),
            new[]
            {
                "version,status,hydraulic_mode,registered_for_loading,long_journey_role",
                $"1,COMPATIBILITY-RETAINED,{v1Mode},True,historical-M7.5",
                $"2,COMPATIBILITY-REFERENCE,{v2Mode},True,frozen-explicit-reference",
                $"3,SUPPORTED-CURRENT,{v3Mode},{v3Registered},scheduled-long-sustained",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "01-i5-synchronization-corrected-v3-activation.summary.txt"),
            new[]
            {
                "=== 01-i5-synchronization-corrected-v3-activation ===",
                "Validated Hotfix 5 evidence authorizes exact pre-synchronization-grid-loading@3 for supported sustained synchronization. Exact @1/@2 identities remain loadable and unchanged; @3 changes only hydraulic numerical ownership to the already-qualified corrected-commit mode.",
                "source-qualification=I.5 REV1 Hotfix 5 VALIDATED; selected-contract=corrected-only; physical-retuning=False; governor-retuning=False; steam-capacity-retuning=False; stop-grade-retuning=False;",
                $"exact-v1=pre-synchronization-grid-loading@1|{v1Mode}; exact-v2=pre-synchronization-grid-loading@2|{v2Mode}; exact-v3=pre-synchronization-grid-loading@3|{v3Mode}; exact-v1-v2-reinterpreted=False;",
                $"v3-registered={v3Registered}; long-journey-uses-v3={longJourneyAligned}; stabilization-checkpoint=10s|gross-positive|shaft>4.5MW|rotor=2950-3050rpm|forward-admission; sustained-window=20-60s|gross>4.0MWe|shaft>4.5MW|rotor=2990-3010rpm|forward-admission;",
                $"synchronization-v3-activation-passes={passes}; gameplay-long-v3-unblocked={passes}; i5-cumulative-rerun-unblocked={passes}; runtime-physics-retuned=False; production-desktop-policy-changed=False;",
                "I.5 activation recommendation: run the unchanged cumulative closure matrix. The gameplay-long gate must now exercise exact synchronization @3 under the qualified stabilization/sustained contract; do not reinterpret or delete @1/@2.",
            },
            Utf8WithoutBom);

        Assert.True(passes, "I.5 synchronization exact-v3 activation contract failed. Inspect artifacts/i5-synchronization-corrected-v3-activation.");
    }

    private static HydraulicNumericalCouplingMode RuntimeMode(IVersionedInitialConditionFactory factory)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        return engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode;
    }

    private static TimeSpan FixedStep(IVersionedInitialConditionFactory factory)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        return engine.FixedDeltaTime;
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while (true)
        {
            var index = text.IndexOf(value, offset, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }
            count++;
            offset = index + value.Length;
        }
    }

    private static void AssertFrozenEvidence(string fileName, string expectedSha256, params string[] expectedTokens)
    {
        var path = Path.Combine(EvidenceDirectory(), fileName);
        Assert.True(File.Exists(path), $"Frozen evidence file is missing: {fileName}");
        var text = File.ReadAllText(path);
        Assert.Equal(expectedSha256, CanonicalTextSha256(text));
        foreach (var token in expectedTokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static bool FrozenEvidenceContains(string fileName, string token)
        => File.ReadAllText(Path.Combine(EvidenceDirectory(), fileName)).Contains(token, StringComparison.Ordinal);

    private static string CanonicalTextSha256(string text)
    {
        var canonical = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-synchronization-corrected-v3-activation");

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
            $"{DateTimeOffset.UtcNow:O} I.5 synchronization corrected-v3 activation audit started{Environment.NewLine}",
            Utf8WithoutBom);
    }
}
