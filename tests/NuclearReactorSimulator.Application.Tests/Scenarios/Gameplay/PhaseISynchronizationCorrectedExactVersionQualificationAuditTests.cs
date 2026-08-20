using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 synchronization blocker qualification. It freezes the validated loaded-contract diagnostic, preserves exact
/// pre-synchronization-grid-loading@1/@2, and qualifies a new exact @3 that changes only hydraulic numerical ownership
/// from ExplicitCommittedState to FourNodeBranchContinuityCorrectedCommitOptIn. The first 10 seconds after load pickup are
/// treated as a bounded stabilization checkpoint; the unchanged >4 MWe sustained floor and 2990-3010 rpm band apply from
/// 20 through 60 seconds.
/// </summary>
public sealed class PhaseISynchronizationCorrectedExactVersionQualificationAuditTests
{
    private const int TotalSteps = 6_000;
    private const int SampleStrideSteps = 100;
    private const int StabilizationCheckpointStep = 1_000;
    private const int StableWindowStartStep = 2_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private const string FrozenDiagnosticSummarySha256 = "8B17B506AB3B2DFC7C488F9C2A6D4235A591184FE4F3D6E2F628FCD8BC560FBE";
    private const string FrozenDiagnosticMetricsSha256 = "988D351BFA7777703609CBE3E100553929731C2B842029B23A80363D5C51C2FA";

    [Fact]
    public void ValidatedLoadedContractDiagnostic_SelectsCorrectedOnlyWithoutPhysicalRetuning()
    {
        AssertFrozenEvidence(
            "I5_HF4_ValidatedSynchronizationLoadedContractDiagnosticSummary.txt",
            FrozenDiagnosticSummarySha256,
            "qualifying-candidates=1; recommended-candidate=corrected-only",
            "candidate=corrected-only; qualifies=True",
            "stable-gross-violations=0",
            "stable-rotor-violations=0",
            "stable-shaft-violations=0",
            "reverse-admission-steps=0",
            "exact-v2-changed=False");

        AssertFrozenEvidence(
            "I5_HF4_ValidatedSynchronizationLoadedContractCandidateMetrics.csv",
            FrozenDiagnosticMetricsSha256,
            "corrected-only,1000,277,0.5,0.02,0,True",
            "H.30 corrected-commit only with frozen synchronization capacity");

        var manifest = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "evidence-manifests",
            "i5-hf4-synchronization-loaded-contract-validated.csv"));
        Assert.Contains("recommended_candidate,corrected-only", manifest, StringComparison.Ordinal);
        Assert.Contains("corrected_only_qualifies,True", manifest, StringComparison.Ordinal);
        Assert.Contains("corrected_only_reverse_admission_steps,0", manifest, StringComparison.Ordinal);
        Assert.Contains("exact_v2_changed,False", manifest, StringComparison.Ordinal);
        Assert.Contains("runtime_production_changed,False", manifest, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseISynchronizationCorrectedExactVersionQualification")]
    public void ExactVersion3_CorrectedCommit_QualifiesBoundedStabilizationAndStrictSustainedLowLoadJourney()
    {
        ResetReportDirectory();

        var version2 = new GridSynchronizationSustainedInitialConditionFactory();
        var version3 = new GridSynchronizationCorrectedInitialConditionFactory();
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 2), version2.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 3), version3.Descriptor.Reference);

        var version2Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(version2.CreateRuntimeEngine());
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(version3.CreateRuntimeEngine());
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState,
            version2Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);

        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var initialGenerator = Assert.Single(initial.Electrical.Generators);
        Assert.False(initialGenerator.BreakerClosed);
        Assert.True(initialGenerator.SynchronizationConditionsSatisfied);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        var paralleled = engine.Step(ControlRoomRunState.Running);
        var paralleledGenerator = Assert.Single(paralleled.Electrical.Generators);
        Assert.True(paralleledGenerator.BreakerClosed);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            paralleledGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        var loaded = engine.Step(ControlRoomRunState.Running);
        var loadedGenerator = Assert.Single(loaded.Electrical.Generators);
        var initialLoadedElectricalMegawatts = loadedGenerator.ElectricalOutput.NumericValue ?? double.NaN;
        Assert.True(initialLoadedElectricalMegawatts > 4.5d);

        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var requestViolationSteps = 0;
        var shaftViolationSteps = 0;
        var reverseAdmissionSteps = 0;
        var stableGrossViolations = 0;
        var stableRotorViolations = 0;
        var stableShaftViolations = 0;
        var stableReverseAdmissionViolations = 0;
        var minimumStableGrossMegawatts = double.PositiveInfinity;
        var minimumStableShaftMegawatts = double.PositiveInfinity;
        var minimumStableRotorRpm = double.PositiveInfinity;
        var maximumStableRotorRpm = double.NegativeInfinity;
        var minimumStableAdmissionFlow = double.PositiveInfinity;
        SnapshotMetrics? stabilization = null;
        SnapshotMetrics? final = null;
        var checkpoints = new List<string>
        {
            "checkpoint,seconds,gross_mwe,shaft_mw,rotor_rpm,admission_kg_s,trip,breaker_closed",
        };

        for (var step = 1; step <= TotalSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
            var train = Assert.Single(snapshot.TurbineSecondary.AdmissionTrains);

            var request = generator.RequestedElectricalPower.NumericValue ?? double.NaN;
            var gross = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var shaft = rotor.ShaftPower.NumericValue ?? double.NaN;
            var rpm = rotor.Speed.NumericValue ?? double.NaN;
            var admission = train.AdmissionFlow.NumericValue ?? double.NaN;

            Assert.True(double.IsFinite(request));
            Assert.True(double.IsFinite(gross));
            Assert.True(double.IsFinite(shaft));
            Assert.True(double.IsFinite(rpm));
            Assert.True(double.IsFinite(admission));

            if (snapshot.AnyTripActive)
            {
                tripSteps++;
            }
            if (!generator.BreakerClosed)
            {
                breakerOpenSteps++;
            }
            if (!(request > 4.5d))
            {
                requestViolationSteps++;
            }
            if (!(shaft > 4.5d))
            {
                shaftViolationSteps++;
            }
            if (!(admission >= 0d))
            {
                reverseAdmissionSteps++;
            }

            if (step == StabilizationCheckpointStep)
            {
                var stabilizationSample = new SnapshotMetrics(gross, shaft, rpm, admission, snapshot.AnyTripActive, generator.BreakerClosed);
                stabilization = stabilizationSample;
                checkpoints.Add(CheckpointLine(1, 10d, stabilizationSample));
            }

            if (step >= StableWindowStartStep && step % SampleStrideSteps == 0)
            {
                if (!(gross > 4.0d))
                {
                    stableGrossViolations++;
                }
                if (!(rpm >= 2_990d && rpm <= 3_010d))
                {
                    stableRotorViolations++;
                }
                if (!(shaft > 4.5d))
                {
                    stableShaftViolations++;
                }
                if (!(admission >= 0d))
                {
                    stableReverseAdmissionViolations++;
                }

                minimumStableGrossMegawatts = Math.Min(minimumStableGrossMegawatts, gross);
                minimumStableShaftMegawatts = Math.Min(minimumStableShaftMegawatts, shaft);
                minimumStableRotorRpm = Math.Min(minimumStableRotorRpm, rpm);
                maximumStableRotorRpm = Math.Max(maximumStableRotorRpm, rpm);
                minimumStableAdmissionFlow = Math.Min(minimumStableAdmissionFlow, admission);
                var stableSample = new SnapshotMetrics(gross, shaft, rpm, admission, snapshot.AnyTripActive, generator.BreakerClosed);
                final = stableSample;

                if (step % 1_000 == 0)
                {
                    checkpoints.Add(CheckpointLine(step / 1_000, step / 100d, stableSample));
                }
            }
        }

        var stabilizationMetrics = Assert.IsType<SnapshotMetrics>(stabilization);
        var finalMetrics = Assert.IsType<SnapshotMetrics>(final);
        Assert.False(stabilizationMetrics.AnyTrip);
        Assert.True(stabilizationMetrics.BreakerClosed);
        Assert.True(stabilizationMetrics.ShaftMegawatts > 4.5d);
        Assert.True(stabilizationMetrics.AdmissionFlowKilogramsPerSecond >= 0d);
        Assert.InRange(stabilizationMetrics.RotorRpm, 2_950d, 3_050d);

        var passes = initialLoadedElectricalMegawatts > 4.5d
            && tripSteps == 0
            && breakerOpenSteps == 0
            && requestViolationSteps == 0
            && shaftViolationSteps == 0
            && reverseAdmissionSteps == 0
            && stableGrossViolations == 0
            && stableRotorViolations == 0
            && stableShaftViolations == 0
            && stableReverseAdmissionViolations == 0
            && finalMetrics.GrossMegawatts > 4.0d
            && finalMetrics.RotorRpm >= 2_990d
            && finalMetrics.RotorRpm <= 3_010d;

        WriteArtifacts(
            initialLoadedElectricalMegawatts,
            tripSteps,
            breakerOpenSteps,
            requestViolationSteps,
            shaftViolationSteps,
            reverseAdmissionSteps,
            stableGrossViolations,
            stableRotorViolations,
            stableShaftViolations,
            stableReverseAdmissionViolations,
            minimumStableGrossMegawatts,
            minimumStableShaftMegawatts,
            minimumStableRotorRpm,
            maximumStableRotorRpm,
            minimumStableAdmissionFlow,
            stabilizationMetrics,
            finalMetrics,
            checkpoints,
            passes);

        Assert.True(passes,
            "Exact pre-synchronization-grid-loading@3 did not qualify the 10 s bounded stabilization plus strict 20-60 s sustained low-load contract. Inspect artifacts/i5-synchronization-corrected-v3-qualification.");
    }

    private static void WriteArtifacts(
        double initialLoadedElectricalMegawatts,
        int tripSteps,
        int breakerOpenSteps,
        int requestViolationSteps,
        int shaftViolationSteps,
        int reverseAdmissionSteps,
        int stableGrossViolations,
        int stableRotorViolations,
        int stableShaftViolations,
        int stableReverseAdmissionViolations,
        double minimumStableGrossMegawatts,
        double minimumStableShaftMegawatts,
        double minimumStableRotorRpm,
        double maximumStableRotorRpm,
        double minimumStableAdmissionFlow,
        SnapshotMetrics stabilization,
        SnapshotMetrics final,
        IReadOnlyList<string> checkpoints,
        bool passes)
    {
        var directory = ReportDirectory();
        File.WriteAllLines(Path.Combine(directory, "03-v3-checkpoints.csv"), checkpoints, Utf8WithoutBom);
        File.WriteAllLines(
            Path.Combine(directory, "02-v3-qualification-metrics.csv"),
            new[]
            {
                "metric,value",
                $"initial_loaded_mwe,{F(initialLoadedElectricalMegawatts)}",
                $"trip_steps,{tripSteps}",
                $"breaker_open_steps,{breakerOpenSteps}",
                $"request_violation_steps,{requestViolationSteps}",
                $"shaft_violation_steps,{shaftViolationSteps}",
                $"reverse_admission_steps,{reverseAdmissionSteps}",
                $"stable_gross_violations,{stableGrossViolations}",
                $"stable_rotor_violations,{stableRotorViolations}",
                $"stable_shaft_violations,{stableShaftViolations}",
                $"stable_reverse_admission_violations,{stableReverseAdmissionViolations}",
                $"minimum_stable_gross_mwe,{F(minimumStableGrossMegawatts)}",
                $"minimum_stable_shaft_mw,{F(minimumStableShaftMegawatts)}",
                $"minimum_stable_rotor_rpm,{F(minimumStableRotorRpm)}",
                $"maximum_stable_rotor_rpm,{F(maximumStableRotorRpm)}",
                $"minimum_stable_admission_kg_s,{F(minimumStableAdmissionFlow)}",
                $"stabilization_gross_mwe,{F(stabilization.GrossMegawatts)}",
                $"stabilization_rotor_rpm,{F(stabilization.RotorRpm)}",
                $"final_gross_mwe,{F(final.GrossMegawatts)}",
                $"final_shaft_mw,{F(final.ShaftMegawatts)}",
                $"final_rotor_rpm,{F(final.RotorRpm)}",
                $"qualification_passes,{passes}",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "01-i5-synchronization-corrected-v3-qualification.summary.txt"),
            new[]
            {
                "=== 01-i5-synchronization-corrected-v3-qualification ===",
                "I.5 qualifies a new exact synchronization version after the validated loaded-contract diagnostic selected corrected-only as the sole bounded candidate. Exact @1/@2 remain immutable; @3 preserves the @2 physical/control/grid contract and changes only hydraulic numerical ownership to FourNodeBranchContinuityCorrectedCommitOptIn.",
                "source-diagnostic=I.5 REV1 Hotfix 4 VALIDATED-DIAGNOSTIC; qualifying-candidates=1; selected-contract=corrected-only; physical-retuning=False; governor-retuning=False; steam-capacity-retuning=False; stop-grade-retuning=False;",
                "exact-v2=pre-synchronization-grid-loading@2|ExplicitCommittedState; exact-v3=pre-synchronization-grid-loading@3|FourNodeBranchContinuityCorrectedCommitOptIn; exact-v2-reinterpreted=False; production-desktop-policy-changed=False; fixed-step=10.000 ms;",
                $"initial-loaded-mwe={F(initialLoadedElectricalMegawatts)}; stabilization-checkpoint-seconds=10; stabilization-gross-mwe={F(stabilization.GrossMegawatts)}; stabilization-shaft-mw={F(stabilization.ShaftMegawatts)}; stabilization-rotor-rpm={F(stabilization.RotorRpm)}; stabilization-admission-kg-s={F(stabilization.AdmissionFlowKilogramsPerSecond)};",
                $"sustained-window-seconds=20-60; sustained-gross-floor-mwe=4.0; sustained-rotor-band-rpm=2990-3010; minimum-stable-gross-mwe={F(minimumStableGrossMegawatts)}; minimum-stable-shaft-mw={F(minimumStableShaftMegawatts)}; stable-rotor-range-rpm={F(minimumStableRotorRpm)}..{F(maximumStableRotorRpm)}; minimum-stable-admission-kg-s={F(minimumStableAdmissionFlow)};",
                $"trip-steps={tripSteps}; breaker-open-steps={breakerOpenSteps}; request-violation-steps={requestViolationSteps}; shaft-violation-steps={shaftViolationSteps}; reverse-admission-steps={reverseAdmissionSteps}; stable-gross-violations={stableGrossViolations}; stable-rotor-violations={stableRotorViolations}; stable-shaft-violations={stableShaftViolations}; stable-reverse-admission-violations={stableReverseAdmissionViolations};",
                $"final-gross-mwe={F(final.GrossMegawatts)}; final-shaft-mw={F(final.ShaftMegawatts)}; final-rotor-rpm={F(final.RotorRpm)}; synchronization-v3-qualification-passes={passes}; i5-long-journey-v3-activation-unblocked={passes};",
                "recommendation=if validated, register exact @3 as the supported sustained-synchronization version and update the long journey to use the explicit 10 s stabilization / strict 20-60 s sustained contract. Preserve @1/@2 exact identities and do not retune physics or controller gains.",
            },
            Utf8WithoutBom);
    }

    private static string CheckpointLine(int checkpoint, double seconds, SnapshotMetrics metrics)
        => string.Join(",",
            checkpoint,
            F(seconds),
            F(metrics.GrossMegawatts),
            F(metrics.ShaftMegawatts),
            F(metrics.RotorRpm),
            F(metrics.AdmissionFlowKilogramsPerSecond),
            metrics.AnyTrip,
            metrics.BreakerClosed);

    private static void AssertFrozenEvidence(string fileName, string expectedSha256, params string[] expectedTokens)
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", fileName);
        Assert.True(File.Exists(path), $"Frozen evidence file is missing: {fileName}");
        var text = File.ReadAllText(path);
        Assert.Equal(expectedSha256, CanonicalTextSha256(text));
        foreach (var token in expectedTokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static string CanonicalTextSha256(string text)
    {
        var canonical = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-synchronization-corrected-v3-qualification");

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
            $"{DateTimeOffset.UtcNow:O} I.5 synchronization corrected-v3 qualification started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private sealed record SnapshotMetrics(
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm,
        double AdmissionFlowKilogramsPerSecond,
        bool AnyTrip,
        bool BreakerClosed);
}
