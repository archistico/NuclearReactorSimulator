using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.3 Hotfix 4 diagnostic. Replays the first 100 s of the exact-v2 explicit reference and exact-v3 corrected candidate
/// at 10 ms resolution to classify the shaft-power/steam-admission discontinuities exposed by the I.3 300 s baseline.
/// This audit is observation-only and does not alter physics, solver mathematics, deployment policy or persistence identity.
/// </summary>
public sealed class PhaseIExplicitVsCorrectedBranchDiscontinuityComparisonAuditTests
{
    private const string OptInEnvironmentVariable = "NRS_I3_BRANCH_COMPARISON_AUDIT";
    private const int StepsPerSecond = 100;
    private const int DiagnosticSeconds = 100;
    private const int DiagnosticSteps = DiagnosticSeconds * StepsPerSecond;
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly IReadOnlyDictionary<string, string> FrozenRedDiagnosticFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"] = "267F77EFC061958C66058401BC74815DEBEE1B1E36ADD3CC6C32B5D7061C7392",
            ["06-generation-health-violations.csv"] = "D2BD6DEB86564B8DCF994A083EE8C6894897E9C6B23FBE83120962210BE407DC",
            ["07-shaft-drop-episodes.csv"] = "C3A13E60E9CEF709146661E99E1DCEF14EE3DBA3ADBD68E9F45C59F4C8826798",
        };

    private static readonly IReadOnlyDictionary<string, string> FrozenFailedClassifierFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"] = "5A71965FDBF3BF203B6F9A2BFD321F3588F21FDFE30A33376D521A8AA5535B64",
            ["02-v2-v3-ten-millisecond-trace.csv"] = "8FEA343B6DA0A02179E77A02A18925EE901B9F7F6D2EBBB4D564D3F56213C57F",
            ["03-generation-drop-comparison.csv"] = "699444879577332C27B0BB1D691AEA2FF6D2C5E738EBDFE86F27B84C7DAC2796",
            ["04-drop-episodes.csv"] = "8B15C549B109E58C14A0E5BCB889689AE176E6BDA8F4D74EA367FD5F70FA1EAA",
        };

    [Fact]
    public void FrozenI3RedDiagnostic_RetainsFiveExplicitShaftDropEpisodes()
    {
        var directory = EvidenceDirectory();
        foreach (var expected in FrozenRedDiagnosticFingerprints)
        {
            var path = Path.Combine(directory, expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen I.3 red diagnostic evidence: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(directory, "01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"));
        Assert.Contains("generation-health-violations=5", summary, StringComparison.Ordinal);
        Assert.Contains("shaft-floor-violations=5", summary, StringComparison.Ordinal);
        Assert.Contains("shaft-drop-episodes=5", summary, StringComparison.Ordinal);
        Assert.Contains("first-admission-flow-kg-s=-27.477271591012993", summary, StringComparison.Ordinal);
        Assert.Contains("first-steam-flow-kg-s=0", summary, StringComparison.Ordinal);
        Assert.Contains("first-turbine-inlet-phase=SuperheatedVapor", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenHotfix4FailedClassifier_RetainsTenMillisecondComparisonEvidence()
    {
        var directory = ClassifierFixEvidenceDirectory();
        foreach (var expected in FrozenFailedClassifierFingerprints)
        {
            var path = Path.Combine(directory, expected.Key);
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
                        $"I3_HF4_ClassifierFix1/{expected.Key}"));
            }
        }

        var summary = File.ReadAllText(Path.Combine(directory, "01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"));
        Assert.Contains("explicit-drops=338", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-reverse-admission-steps=330", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-drops=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-reverse-admission-steps=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-committed=1791", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-fallbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-only-branch-discontinuity-classified=False", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseILongAudit")]
    public void ExactV2ExplicitVsV3Corrected_ClassifiesTurbineInletFlowDirectionDiscontinuitiesAtTenMillisecondResolution()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();
        var explicitRun = Run(
            "v2-explicit",
            new DesktopSustainedGenerationInitialConditionFactory(),
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            observeProductionTelemetry: false);
        var correctedRun = Run(
            "v3-corrected",
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(),
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            observeProductionTelemetry: true);

        var explicitDrops = explicitRun.Rows.Where(static row => IsGenerationDrop(row)).ToArray();
        var correctedDrops = correctedRun.Rows.Where(static row => IsGenerationDrop(row)).ToArray();
        var explicitReverseStop = explicitRun.Rows.Where(static row => row.StopFlowKilogramsPerSecond < 0d).ToArray();
        var explicitReverseControl = explicitRun.Rows.Where(static row => row.ControlFlowKilogramsPerSecond < 0d).ToArray();
        var explicitReverseAdmission = explicitRun.Rows.Where(static row => row.AdmissionFlowKilogramsPerSecond < 0d).ToArray();
        var explicitTargetedReverseFlow = explicitRun.Rows.Where(static row => HasTargetedReverseFlow(row)).ToArray();
        var correctedReverseStop = correctedRun.Rows.Where(static row => row.StopFlowKilogramsPerSecond < 0d).ToArray();
        var correctedReverseControl = correctedRun.Rows.Where(static row => row.ControlFlowKilogramsPerSecond < 0d).ToArray();
        var correctedReverseAdmission = correctedRun.Rows.Where(static row => row.AdmissionFlowKilogramsPerSecond < 0d).ToArray();
        var correctedTargetedReverseFlow = correctedRun.Rows.Where(static row => HasTargetedReverseFlow(row)).ToArray();
        var explicitEpisodes = BuildEpisodes(explicitDrops);
        var correctedEpisodes = BuildEpisodes(correctedDrops);

        var comparisonPasses = explicitDrops.Length > 0
            && explicitTargetedReverseFlow.Length == explicitDrops.Length
            && explicitDrops.All(static row => HasTargetedReverseFlow(row))
            && explicitTargetedReverseFlow.All(static row => IsGenerationDrop(row))
            && correctedDrops.Length == 0
            && correctedTargetedReverseFlow.Length == 0
            && correctedRun.Telemetry.CorrectedCommittedSteps > 0
            && correctedRun.Telemetry.RollbackSteps == 0
            && correctedRun.Telemetry.ExplicitFallbackSteps == 0
            && correctedRun.Telemetry.UnsafeCommitViolations == 0
            && correctedRun.Telemetry.UntargetedBranchDisagreementSteps == 0
            && explicitRun.Rows.All(static row => !row.AnyTrip)
            && correctedRun.Rows.All(static row => !row.AnyTrip);

        WriteReports(explicitRun, correctedRun, explicitDrops, correctedDrops, explicitEpisodes, correctedEpisodes, comparisonPasses);

        Assert.True(
            comparisonPasses,
            $"I.3 branch-discontinuity comparison did not isolate the explicit-only failure class. explicit-drops={explicitDrops.Length}; explicit-targeted-reverse={explicitTargetedReverseFlow.Length}; explicit-reverse-stop/control/admission={explicitReverseStop.Length}/{explicitReverseControl.Length}/{explicitReverseAdmission.Length}; corrected-drops={correctedDrops.Length}; corrected-targeted-reverse={correctedTargetedReverseFlow.Length}; corrected-reverse-stop/control/admission={correctedReverseStop.Length}/{correctedReverseControl.Length}/{correctedReverseAdmission.Length}; corrected-commits={correctedRun.Telemetry.CorrectedCommittedSteps}; corrected-rollbacks={correctedRun.Telemetry.RollbackSteps}; corrected-fallbacks={correctedRun.Telemetry.ExplicitFallbackSteps}.");
    }

    private static DiagnosticRun Run(
        string modeId,
        IVersionedInitialConditionFactory factory,
        HydraulicNumericalCouplingMode expectedMode,
        bool observeProductionTelemetry)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(expectedMode, CurrentHydraulics(engine).Mode);

        var probe = observeProductionTelemetry ? new DesktopHydraulicProductionTelemetryProbe() : null;
        var rows = new List<DiagnosticRow>(DiagnosticSteps);
        for (var step = 1; step <= DiagnosticSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            probe?.Observe(engine);
            rows.Add(Capture(modeId, engine, presentation));
        }

        return new DiagnosticRun(
            modeId,
            expectedMode,
            rows,
            probe?.Snapshot() ?? FourNodeProductionActivationTelemetrySnapshot.Empty);
    }

    private static DiagnosticRow Capture(
        string modeId,
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        var numerics = CurrentHydraulics(engine);
        var continuity = numerics.FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;

        return new DiagnosticRow(
            modeId,
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            turbine.TotalShaftPower.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            train.StopValve.MassFlowRate.KilogramsPerSecond,
            train.ControlValve.MassFlowRate.KilogramsPerSecond,
            train.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            plant.GetFluidNode("steam").Pressure.Kilopascals,
            plant.GetFluidNode("header").Pressure.Kilopascals,
            plant.GetFluidNode("stop-out").Pressure.Kilopascals,
            plant.GetFluidNode("control-out").Pressure.Kilopascals,
            plant.GetFluidNode("turbine-inlet").Pressure.Kilopascals,
            train.TurbineInletTemperature.DegreesCelsius,
            train.TurbineInletPhase.ToString(),
            rotor.Speed.NumericValue ?? double.NaN,
            continuity?.TriggerObserved ?? false,
            continuity?.ShadowCorrectedCandidateEligible ?? false,
            continuity?.CorrectedCandidateCommitted ?? false,
            continuity?.RollbackRequired ?? false,
            continuity?.BranchOverrideCount ?? 0,
            continuity?.PreviousPhaseHoldCount ?? 0);
    }

    private static bool IsGenerationDrop(DiagnosticRow row)
        => !row.AnyTrip
            && row.GeneratorBreakerClosed
            && row.RequestedMegawatts > 4.5d
            && row.CanonicalShaftMegawatts <= 4.5d;

    private static bool HasTargetedReverseFlow(DiagnosticRow row)
        => row.StopFlowKilogramsPerSecond < 0d
            || row.ControlFlowKilogramsPerSecond < 0d
            || row.AdmissionFlowKilogramsPerSecond < 0d;

    private static IReadOnlyList<DropEpisode> BuildEpisodes(IReadOnlyList<DiagnosticRow> rows)
    {
        if (rows.Count == 0)
        {
            return Array.Empty<DropEpisode>();
        }

        var episodes = new List<DropEpisode>();
        var start = rows[0];
        var previous = rows[0];
        var count = 1;
        for (var index = 1; index < rows.Count; index++)
        {
            var current = rows[index];
            if (current.LogicalStep == previous.LogicalStep + 1)
            {
                previous = current;
                count++;
                continue;
            }

            episodes.Add(new DropEpisode(start.LogicalStep, previous.LogicalStep, start.SimulatedSeconds, previous.SimulatedSeconds, count));
            start = current;
            previous = current;
            count = 1;
        }
        episodes.Add(new DropEpisode(start.LogicalStep, previous.LogicalStep, start.SimulatedSeconds, previous.SimulatedSeconds, count));
        return episodes;
    }

    private static void WriteReports(
        DiagnosticRun explicitRun,
        DiagnosticRun correctedRun,
        IReadOnlyList<DiagnosticRow> explicitDrops,
        IReadOnlyList<DiagnosticRow> correctedDrops,
        IReadOnlyList<DropEpisode> explicitEpisodes,
        IReadOnlyList<DropEpisode> correctedEpisodes,
        bool passes)
    {
        var directory = ReportDirectory();
        var allRows = explicitRun.Rows.Concat(correctedRun.Rows).ToArray();
        var trace = new List<string>
        {
            "mode,logical_step,simulated_seconds,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,stage_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,steam_kpa,header_kpa,stop_out_kpa,control_out_kpa,turbine_inlet_kpa,turbine_inlet_c,turbine_inlet_phase,rotor_rpm,trigger,eligible,corrected_commit,rollback,branch_overrides,previous_phase_holds",
        };
        trace.AddRange(allRows.Select(FormatRow));
        File.WriteAllLines(Path.Combine(directory, "02-v2-v3-ten-millisecond-trace.csv"), trace, Utf8WithoutBom);

        var drops = new List<string>
        {
            "mode,logical_step,simulated_seconds,request_mwe,gross_mwe,canonical_shaft_mwe,stage_flow_kg_s,admission_flow_kg_s,header_kpa,control_out_kpa,turbine_inlet_kpa,turbine_inlet_phase,trigger,eligible,corrected_commit",
        };
        drops.AddRange(explicitDrops.Concat(correctedDrops).Select(static row => FormattableString.Invariant(
            $"{row.ModeId},{row.LogicalStep},{row.SimulatedSeconds:G17},{row.RequestedMegawatts:G17},{row.GrossMegawatts:G17},{row.CanonicalShaftMegawatts:G17},{row.StageFlowKilogramsPerSecond:G17},{row.AdmissionFlowKilogramsPerSecond:G17},{row.HeaderPressureKilopascals:G17},{row.ControlOutPressureKilopascals:G17},{row.TurbineInletPressureKilopascals:G17},{row.TurbineInletPhase},{row.TriggerObserved},{row.CandidateEligible},{row.CorrectedCommitted}")));
        File.WriteAllLines(Path.Combine(directory, "03-generation-drop-comparison.csv"), drops, Utf8WithoutBom);

        var episodes = new List<string> { "mode,start_step,end_step,start_seconds,end_seconds,duration_steps,duration_ms" };
        episodes.AddRange(explicitEpisodes.Select(static item => FormatEpisode("v2-explicit", item)));
        episodes.AddRange(correctedEpisodes.Select(static item => FormatEpisode("v3-corrected", item)));
        File.WriteAllLines(Path.Combine(directory, "04-drop-episodes.csv"), episodes, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v2-v3-phase-i-targeted-train-branch-discontinuity-comparison ===",
            "I.3 Hotfix 4 Classifier Fix 1 compares exact v2 ExplicitCommittedState and exact v3 FourNodeBranchContinuityCorrectedCommitOptIn over the same first 100 simulated seconds at 10 ms resolution. Classification is based on reverse flow anywhere in the targeted stop/control/admission train, not admission alone. This is diagnostic-only and does not change H.30 policy or runtime mathematics.",
            FormattableString.Invariant($"diagnostic-seconds={DiagnosticSeconds}; steps-per-mode={DiagnosticSteps}; fixed-step=10.000 ms; explicit-drops={explicitDrops.Count}; explicit-drop-episodes={explicitEpisodes.Count}; explicit-reverse-stop-steps={explicitRun.Rows.Count(static row => row.StopFlowKilogramsPerSecond < 0d)}; explicit-reverse-control-steps={explicitRun.Rows.Count(static row => row.ControlFlowKilogramsPerSecond < 0d)}; explicit-reverse-admission-steps={explicitRun.Rows.Count(static row => row.AdmissionFlowKilogramsPerSecond < 0d)}; explicit-targeted-reverse-flow-steps={explicitRun.Rows.Count(static row => HasTargetedReverseFlow(row))}; corrected-drops={correctedDrops.Count}; corrected-drop-episodes={correctedEpisodes.Count}; corrected-reverse-stop-steps={correctedRun.Rows.Count(static row => row.StopFlowKilogramsPerSecond < 0d)}; corrected-reverse-control-steps={correctedRun.Rows.Count(static row => row.ControlFlowKilogramsPerSecond < 0d)}; corrected-reverse-admission-steps={correctedRun.Rows.Count(static row => row.AdmissionFlowKilogramsPerSecond < 0d)}; corrected-targeted-reverse-flow-steps={correctedRun.Rows.Count(static row => HasTargetedReverseFlow(row))};"),
            FormattableString.Invariant($"explicit-drops-with-targeted-reverse-flow={explicitDrops.Count(static row => HasTargetedReverseFlow(row))}/{explicitDrops.Count}; explicit-targeted-reverse-flow-that-are-drops={explicitRun.Rows.Count(static row => HasTargetedReverseFlow(row) && IsGenerationDrop(row))}/{explicitRun.Rows.Count(static row => HasTargetedReverseFlow(row))};"),
            FormattableString.Invariant($"corrected-triggered={correctedRun.Telemetry.TriggeredSteps}; corrected-eligible={correctedRun.Telemetry.CandidateEligibleSteps}; corrected-authorized={correctedRun.Telemetry.CommitAuthorizedSteps}; corrected-committed={correctedRun.Telemetry.CorrectedCommittedSteps}; corrected-rollbacks={correctedRun.Telemetry.RollbackSteps}; corrected-fallbacks={correctedRun.Telemetry.ExplicitFallbackSteps}; corrected-unsafe={correctedRun.Telemetry.UnsafeCommitViolations}; corrected-untargeted-disagreements={correctedRun.Telemetry.UntargetedBranchDisagreementSteps};"),
            $"explicit-only-branch-discontinuity-classified={passes}; i3-hotfix4-comparison-audit-passes={passes};",
            passes
                ? "I.3 Hotfix 4 Classifier Fix 1 recommendation: the 300 s I.3 failure class is reproduced in exact v2 as targeted-train reverse flow (stop/control/admission) coincident one-for-one with generation drops, and is suppressed by the already-qualified exact v3 corrected-commit path. Do not weaken the shaft-health floor. Re-open the H.30 production-policy decision before freezing I.3 reference budgets."
                : "I.3 Hotfix 4 Classifier Fix 1 recommendation: the comparison did not isolate an explicit-only targeted-train reverse-flow failure class. Keep I.3 red and continue root-cause localization before any H.30 policy change or runtime correction.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string FormatRow(DiagnosticRow row)
        => FormattableString.Invariant(
            $"{row.ModeId},{row.LogicalStep},{row.SimulatedSeconds:G17},{row.AnyTrip},{row.GeneratorBreakerClosed},{row.RequestedMegawatts:G17},{row.GrossMegawatts:G17},{row.RotorShaftMegawatts:G17},{row.CanonicalShaftMegawatts:G17},{row.StageFlowKilogramsPerSecond:G17},{row.StopFlowKilogramsPerSecond:G17},{row.ControlFlowKilogramsPerSecond:G17},{row.AdmissionFlowKilogramsPerSecond:G17},{row.SteamPressureKilopascals:G17},{row.HeaderPressureKilopascals:G17},{row.StopOutPressureKilopascals:G17},{row.ControlOutPressureKilopascals:G17},{row.TurbineInletPressureKilopascals:G17},{row.TurbineInletTemperatureCelsius:G17},{row.TurbineInletPhase},{row.RotorRpm:G17},{row.TriggerObserved},{row.CandidateEligible},{row.CorrectedCommitted},{row.RollbackRequired},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount}");

    private static string FormatEpisode(string modeId, DropEpisode item)
        => FormattableString.Invariant(
            $"{modeId},{item.StartStep},{item.EndStep},{item.StartSeconds:G17},{item.EndSeconds:G17},{item.DurationSteps},{item.DurationSteps * 10}");

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", "I3_HF4");

    private static string ClassifierFixEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", "I3_HF4_ClassifierFix1");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i3-hotfix4-explicit-vs-corrected-branch-discontinuity-comparison");

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

    private sealed record DiagnosticRun(
        string ModeId,
        HydraulicNumericalCouplingMode Mode,
        IReadOnlyList<DiagnosticRow> Rows,
        FourNodeProductionActivationTelemetrySnapshot Telemetry);

    private sealed record DiagnosticRow(
        string ModeId,
        long LogicalStep,
        double SimulatedSeconds,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedMegawatts,
        double GrossMegawatts,
        double RotorShaftMegawatts,
        double CanonicalShaftMegawatts,
        double StageFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double SteamPressureKilopascals,
        double HeaderPressureKilopascals,
        double StopOutPressureKilopascals,
        double ControlOutPressureKilopascals,
        double TurbineInletPressureKilopascals,
        double TurbineInletTemperatureCelsius,
        string TurbineInletPhase,
        double RotorRpm,
        bool TriggerObserved,
        bool CandidateEligible,
        bool CorrectedCommitted,
        bool RollbackRequired,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount);

    private sealed record DropEpisode(
        long StartStep,
        long EndStep,
        double StartSeconds,
        double EndSeconds,
        int DurationSteps);
}
