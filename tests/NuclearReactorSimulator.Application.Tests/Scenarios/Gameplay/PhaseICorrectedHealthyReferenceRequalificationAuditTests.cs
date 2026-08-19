using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.3 Hotfix 5 pre-H.30 requalification. Runs exact v3 corrected-commit for 300 simulated seconds and verifies
/// every 10 ms step for generation health and targeted-train flow direction while sampling conservation/inventory
/// every second. Diagnostic-only: H.30 remains OPT-IN ONLY until a separate policy re-review is validated.
/// </summary>
public sealed class PhaseICorrectedHealthyReferenceRequalificationAuditTests
{
    private const string OptInEnvironmentVariable = "NRS_I3_CORRECTED_300S_AUDIT";
    private const int StepsPerSecond = 100;
    private const int ReferenceSeconds = 300;
    private const int ReferenceSteps = ReferenceSeconds * StepsPerSecond;
    private const int FinalWindowSeconds = 60;
    private const int DeterminismSteps = 256;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly IReadOnlyDictionary<string, string> FrozenValidatedClassifierFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"] = "AA40086BFEF88352EB4F0D1227F56D9F240BEDE2D4FB5A934711E1A557696A72",
            ["02-v2-v3-ten-millisecond-trace.csv"] = "8FEA343B6DA0A02179E77A02A18925EE901B9F7F6D2EBBB4D564D3F56213C57F",
            ["03-generation-drop-comparison.csv"] = "699444879577332C27B0BB1D691AEA2FF6D2C5E738EBDFE86F27B84C7DAC2796",
            ["04-drop-episodes.csv"] = "8B15C549B109E58C14A0E5BCB889689AE176E6BDA8F4D74EA367FD5F70FA1EAA",
        };

    [Fact]
    public void FrozenValidatedHotfix4Classifier_ProvesCorrectedSuppressionBeforeThreeHundredSecondRun()
    {
        foreach (var expected in FrozenValidatedClassifierFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing validated I.3 Hotfix 4 evidence: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"));
        Assert.Contains("explicit-drops=338", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-targeted-reverse-flow-steps=338", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-drops=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-targeted-reverse-flow-steps=0", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-committed=1791", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-only-branch-discontinuity-classified=True", summary, StringComparison.Ordinal);
        Assert.Contains("i3-hotfix4-comparison-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void CorrectedThreeHundredSecondContract_IsExactV3AndPolicyNeutral()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "phase-i-corrected-300s-reference-requalification-contract.csv");
        Assert.True(File.Exists(path));
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Equal("trajectory_id,schema_version,exact_initial_condition,production_policy,simulated_seconds,logical_steps,step_health_resolution_ms,reference_sample_stride_steps,final_window_seconds,reference_role,policy_effect", lines[0]);
        Assert.Equal("phase-i-desktop-v3-corrected-healthy-300s-rq1,1,integrated-operations-desktop-stable@3,FourNodeBranchContinuityCorrectedCommitOptIn,300,30000,10,100,60,H30-POLICY-REREVIEW-PREREQUISITE,NONE-DIAGNOSTIC-ONLY", lines[1]);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseICorrectedHealthyReferenceRequalificationAudit")]
    public void ExactV3Corrected_ThreeHundredSeconds_RemainsHealthyContinuousConservativeAndDeterministic()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();
        Assert.Equal("integrated-operations-desktop-stable", DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference.InitialConditionId);
        Assert.Equal(3, DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference.Version);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory().CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var samples = new List<ReferenceSample>(ReferenceSeconds + 1)
        {
            CaptureReferenceSample(engine, coordinator.Current),
        };
        var healthViolations = new List<StepObservation>();
        var reverseFlowViolations = new List<StepObservation>();
        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;

        for (var step = 1; step <= ReferenceSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            telemetryProbe.Observe(engine);
            var observation = CaptureStepObservation(engine, presentation);
            AssertFinite(observation);

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(observation.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(observation.EnergyClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(observation.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(observation.BalancePowerResidualWatts));

            if (!IsHealthy(observation))
            {
                healthViolations.Add(observation);
            }
            if (HasTargetedReverseFlow(observation))
            {
                reverseFlowViolations.Add(observation);
            }

            if (step % StepsPerSecond == 0)
            {
                samples.Add(CaptureReferenceSample(engine, presentation));
            }
            if (step % 3000 == 0)
            {
                File.AppendAllText(
                    Path.Combine(ReportDirectory(), "00-progress.txt"),
                    $"{DateTimeOffset.UtcNow:O} simulated-seconds={step / StepsPerSecond}; logical-step={step}{Environment.NewLine}",
                    Utf8WithoutBom);
            }
        }

        Assert.Equal(ReferenceSeconds + 1, samples.Count);
        Assert.Equal(ReferenceSteps, samples[^1].LogicalStep);

        var finalWindow = samples.Where(static sample => sample.SimulatedSeconds >= ReferenceSeconds - FinalWindowSeconds).ToArray();
        Assert.Equal(FinalWindowSeconds + 1, finalWindow.Length);
        var slopes = BuildInventorySlopes(finalWindow);
        Assert.Equal(7, slopes.Count);
        Assert.All(slopes, static slope => Assert.True(double.IsFinite(slope.SlopePerSecond)));

        var telemetry = telemetryProbe.Snapshot();
        var deterministicFingerprintA = DeterminismFingerprint();
        var deterministicFingerprintB = DeterminismFingerprint();
        var deterministicRepeat = string.Equals(deterministicFingerprintA, deterministicFingerprintB, StringComparison.Ordinal);
        var trajectoryFingerprint = ComputeTrajectoryFingerprint(samples);

        var conservationPasses = maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts
            && slopes.All(static slope => double.IsFinite(slope.SlopePerSecond));
        var telemetryPasses = telemetry.ObservedSteps == ReferenceSteps
            && telemetry.FourNodeTelemetrySteps == ReferenceSteps
            && telemetry.TriggeredSteps > 0
            && telemetry.CandidateEligibleSteps == telemetry.TriggeredSteps
            && telemetry.CommitAuthorizedSteps == telemetry.TriggeredSteps
            && telemetry.CorrectedCommittedSteps == telemetry.TriggeredSteps
            && telemetry.RollbackSteps == 0
            && telemetry.ExplicitFallbackSteps == 0
            && telemetry.FallbackCommitViolations == 0
            && telemetry.UnsafeCommitViolations == 0
            && telemetry.UntargetedBranchDisagreementSteps == 0;
        var passes = healthViolations.Count == 0
            && reverseFlowViolations.Count == 0
            && conservationPasses
            && telemetryPasses
            && deterministicRepeat;

        WriteArtifacts(
            samples,
            slopes,
            healthViolations,
            reverseFlowViolations,
            telemetry,
            maxMassClosure,
            maxEnergyClosure,
            maxBalanceMassRate,
            maxBalancePower,
            deterministicFingerprintA,
            deterministicFingerprintB,
            deterministicRepeat,
            trajectoryFingerprint,
            passes);

        Assert.True(
            passes,
            FormattableString.Invariant(
                $"I.3 Hotfix 5 corrected 300 s requalification failed. health-violations={healthViolations.Count}; targeted-reverse-flow={reverseFlowViolations.Count}; commits={telemetry.CorrectedCommittedSteps}; rollbacks={telemetry.RollbackSteps}; fallbacks={telemetry.ExplicitFallbackSteps}; unsafe={telemetry.UnsafeCommitViolations}; untargeted={telemetry.UntargetedBranchDisagreementSteps}; deterministic={deterministicRepeat}; max-closure={maxMassClosure:G17}/{maxEnergyClosure:G17}; max-balance={maxBalanceMassRate:G17}/{maxBalancePower:G17}."));
    }

    private static StepObservation CaptureStepObservation(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        return new StepObservation(
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
            fullPlant.HeatBalance.MassClosureResidualKilograms,
            fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts);
    }

    private static ReferenceSample CaptureReferenceSample(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var admissionTrain = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
        var condensateTrain = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
        var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
        var steamLine = Assert.Single(turbine.MainSteamNetwork.SteamLines);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        var exhaust = plant.GetFluidNode(condenser.SteamSpaceNodeId);
        var hotwell = plant.GetFluidNode(condenser.HotwellNodeId);
        var feedwater = plant.GetFluidNode(condensateTrain.FeedwaterInventoryNodeId);
        var drumInventory = plant.GetFluidNode(drum.InventoryNodeId);
        var header = plant.GetFluidNode(steamLine.HeaderNodeId);
        return new ReferenceSample(
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            turbine.TotalShaftPower.Megawatts,
            turbine.TotalSteamMassFlowRate.KilogramsPerSecond,
            admissionTrain.StopValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.ControlValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            rotor.Speed.NumericValue ?? double.NaN,
            condenser.FinalSteamSpacePressure.Kilopascals,
            drum.LiquidLevelFraction.Fraction,
            plant.FluidNodes.Sum(static node => node.Mass.Kilograms),
            plant.FluidNodes.Sum(static node => node.InternalEnergy.Joules),
            exhaust.Mass.Kilograms,
            hotwell.Mass.Kilograms,
            feedwater.Mass.Kilograms,
            drumInventory.Mass.Kilograms,
            header.Mass.Kilograms);
    }

    private static bool IsHealthy(StepObservation observation)
        => !observation.AnyTrip
            && observation.GeneratorBreakerClosed
            && observation.RequestedElectricalPowerMegawatts > 4.5d
            && observation.GrossElectricalPowerMegawatts > 4.0d
            && observation.RotorShaftPowerMegawatts > 4.5d
            && observation.CanonicalShaftPowerMegawatts > 4.5d;

    private static bool HasTargetedReverseFlow(StepObservation observation)
        => observation.StopFlowKilogramsPerSecond < 0d
            || observation.ControlFlowKilogramsPerSecond < 0d
            || observation.AdmissionFlowKilogramsPerSecond < 0d;

    private static void AssertFinite(StepObservation observation)
    {
        foreach (var value in new[]
        {
            observation.SimulatedSeconds,
            observation.RequestedElectricalPowerMegawatts,
            observation.GrossElectricalPowerMegawatts,
            observation.RotorShaftPowerMegawatts,
            observation.CanonicalShaftPowerMegawatts,
            observation.StageFlowKilogramsPerSecond,
            observation.StopFlowKilogramsPerSecond,
            observation.ControlFlowKilogramsPerSecond,
            observation.AdmissionFlowKilogramsPerSecond,
            observation.MassClosureResidualKilograms,
            observation.EnergyClosureResidualJoules,
            observation.BalanceMassRateResidualKilogramsPerSecond,
            observation.BalancePowerResidualWatts,
        })
        {
            Assert.True(double.IsFinite(value), $"Non-finite corrected 300 s observation at logical step {observation.LogicalStep}.");
        }
    }

    private static IReadOnlyList<InventorySlope> BuildInventorySlopes(IReadOnlyList<ReferenceSample> window)
        => new[]
        {
            BuildSlope("total-fluid-mass", "kg/s", window, static sample => sample.TotalFluidMassKilograms),
            BuildSlope("total-fluid-internal-energy", "W", window, static sample => sample.TotalFluidInternalEnergyJoules),
            BuildSlope("exhaust-mass", "kg/s", window, static sample => sample.ExhaustMassKilograms),
            BuildSlope("hotwell-mass", "kg/s", window, static sample => sample.HotwellMassKilograms),
            BuildSlope("feedwater-inventory-mass", "kg/s", window, static sample => sample.FeedwaterInventoryMassKilograms),
            BuildSlope("drum-inventory-mass", "kg/s", window, static sample => sample.DrumInventoryMassKilograms),
            BuildSlope("main-steam-header-mass", "kg/s", window, static sample => sample.MainSteamHeaderMassKilograms),
        };

    private static InventorySlope BuildSlope(string metricId, string unit, IReadOnlyList<ReferenceSample> window, Func<ReferenceSample, double> selector)
    {
        var meanTime = window.Average(static sample => sample.SimulatedSeconds);
        var meanValue = window.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var sample in window)
        {
            var dx = sample.SimulatedSeconds - meanTime;
            numerator += dx * (selector(sample) - meanValue);
            denominator += dx * dx;
        }
        return new InventorySlope(metricId, unit, meanValue, denominator > 0d ? numerator / denominator : double.NaN);
    }

    private static string DeterminismFingerprint()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory().CreateRuntimeEngine());
        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            var telemetry = CurrentHydraulics(engine).FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(presentation)}:{telemetry?.TriggerObserved}:{telemetry?.ProposedAuthority}:{telemetry?.Reason}:{telemetry?.RollbackRequired}:{telemetry?.CorrectedCommitAuthorized}:{telemetry?.CorrectedCandidateCommitted}:{telemetry?.CorrectedCommitReason}:{telemetry?.ShadowIterationCount}||"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string ComputeTrajectoryFingerprint(IReadOnlyList<ReferenceSample> samples)
    {
        var text = string.Join("\n", samples.Select(static sample => FormattableString.Invariant(
            $"{sample.LogicalStep}|{sample.PresentationFingerprint}|{sample.AnyTrip}|{sample.GeneratorBreakerClosed}|{sample.RequestedElectricalPowerMegawatts:G17}|{sample.GrossElectricalPowerMegawatts:G17}|{sample.RotorShaftPowerMegawatts:G17}|{sample.CanonicalShaftPowerMegawatts:G17}|{sample.TotalSteamFlowKilogramsPerSecond:G17}|{sample.StopFlowKilogramsPerSecond:G17}|{sample.ControlFlowKilogramsPerSecond:G17}|{sample.AdmissionFlowKilogramsPerSecond:G17}|{sample.RotorSpeedRpm:G17}|{sample.CondenserPressureKilopascals:G17}|{sample.DrumLevelFraction:G17}|{sample.TotalFluidMassKilograms:G17}|{sample.TotalFluidInternalEnergyJoules:G17}|{sample.ExhaustMassKilograms:G17}|{sample.HotwellMassKilograms:G17}|{sample.FeedwaterInventoryMassKilograms:G17}|{sample.DrumInventoryMassKilograms:G17}|{sample.MainSteamHeaderMassKilograms:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void WriteArtifacts(
        IReadOnlyList<ReferenceSample> samples,
        IReadOnlyList<InventorySlope> slopes,
        IReadOnlyList<StepObservation> healthViolations,
        IReadOnlyList<StepObservation> reverseFlowViolations,
        FourNodeProductionActivationTelemetrySnapshot telemetry,
        double maxMassClosure,
        double maxEnergyClosure,
        double maxBalanceMassRate,
        double maxBalancePower,
        string determinismA,
        string determinismB,
        bool deterministicRepeat,
        string trajectoryFingerprint,
        bool passes)
    {
        var directory = ReportDirectory();
        File.Copy(Path.Combine(FindRepositoryRoot(), "eng", "phase-i-corrected-300s-reference-requalification-contract.csv"), Path.Combine(directory, "02-corrected-300s-reference-contract.csv"), overwrite: true);

        var sampleLines = new List<string>
        {
            "logical_step,simulated_seconds,presentation_fingerprint,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,total_steam_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,rotor_rpm,condenser_kpa,drum_level_fraction,total_fluid_mass_kg,total_fluid_internal_energy_j,exhaust_mass_kg,hotwell_mass_kg,feedwater_mass_kg,drum_inventory_mass_kg,header_mass_kg",
        };
        sampleLines.AddRange(samples.Select(FormatReferenceSample));
        File.WriteAllLines(Path.Combine(directory, "03-corrected-reference-trajectory-samples.csv"), sampleLines, Utf8WithoutBom);

        var slopeLines = new List<string> { "metric_id,unit,final_window_mean,linear_slope_per_second" };
        slopeLines.AddRange(slopes.Select(static slope => FormattableString.Invariant($"{slope.MetricId},{slope.Unit},{slope.MeanValue:G17},{slope.SlopePerSecond:G17}")));
        File.WriteAllLines(Path.Combine(directory, "04-corrected-final-window-slopes.csv"), slopeLines, Utf8WithoutBom);

        WriteStepObservations(Path.Combine(directory, "05-corrected-step-health-violations.csv"), healthViolations);
        WriteStepObservations(Path.Combine(directory, "06-corrected-targeted-reverse-flow-violations.csv"), reverseFlowViolations);

        var telemetryLines = new[]
        {
            "metric,value",
            $"observed_steps,{telemetry.ObservedSteps}",
            $"four_node_steps,{telemetry.FourNodeTelemetrySteps}",
            $"triggered,{telemetry.TriggeredSteps}",
            $"eligible,{telemetry.CandidateEligibleSteps}",
            $"authorized,{telemetry.CommitAuthorizedSteps}",
            $"committed,{telemetry.CorrectedCommittedSteps}",
            $"fallbacks,{telemetry.ExplicitFallbackSteps}",
            $"rollbacks,{telemetry.RollbackSteps}",
            $"fallback_commit_violations,{telemetry.FallbackCommitViolations}",
            $"unsafe_commits,{telemetry.UnsafeCommitViolations}",
            $"untargeted_disagreements,{telemetry.UntargetedBranchDisagreementSteps}",
        };
        File.WriteAllLines(Path.Combine(directory, "07-corrected-production-telemetry.csv"), telemetryLines, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "08-determinism-control.csv"), new[]
        {
            "control_steps,fingerprint_a,fingerprint_b,repeat",
            $"{DeterminismSteps},{determinismA},{determinismB},{deterministicRepeat}",
        }, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v3-phase-i-corrected-300s-healthy-reference-requalification ===",
            "I.3 Hotfix 5 runs the already-qualified exact v3 FourNodeBranchContinuityCorrectedCommitOptIn path for the full 300-second healthy reference horizon. Every 10 ms step is checked for generation health and reverse flow across stop/control/admission; one-second samples provide conservation/inventory and final-window slope evidence. This gate is diagnostic/requalification-only and does not change the H.30 OPT-IN ONLY deployment policy or freeze I.3 tolerance budgets.",
            $"trajectory-id=phase-i-desktop-v3-corrected-healthy-300s-rq1; exact-initial-condition=integrated-operations-desktop-stable@3; production-policy=FourNodeBranchContinuityCorrectedCommitOptIn; simulated-seconds={ReferenceSeconds}; logical-steps={ReferenceSteps}; step-health-resolution-ms=10; reference-samples={samples.Count}; final-window-seconds={FinalWindowSeconds};",
            $"generation-health-violations={healthViolations.Count}; targeted-reverse-flow-violations={reverseFlowViolations.Count}; trip-reference-samples={samples.Count(static sample => sample.AnyTrip)}; trajectory-fingerprint={trajectoryFingerprint}; final-presentation-fingerprint={samples[^1].PresentationFingerprint};",
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17}; inventory-slope-observations={slopes.Count};"),
            $"corrected-triggered={telemetry.TriggeredSteps}; corrected-eligible={telemetry.CandidateEligibleSteps}; corrected-authorized={telemetry.CommitAuthorizedSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; corrected-rollbacks={telemetry.RollbackSteps}; corrected-fallbacks={telemetry.ExplicitFallbackSteps}; corrected-fallback-commit-violations={telemetry.FallbackCommitViolations}; corrected-unsafe={telemetry.UnsafeCommitViolations}; corrected-untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};",
            $"determinism-control-steps={DeterminismSteps}; deterministic-repeat={deterministicRepeat}; deterministic-fingerprint={determinismA};",
            "authoritative-default-before-rereview=integrated-operations-desktop-stable@2|ExplicitCommittedState; qualified-opt-in=integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn; phase-h-production-policy-decision-before-rereview=OPT-IN ONLY; production-fixed-step=10.000 ms; runtime-behavior-changed=False; i3-reference-budgets-frozen=False;",
            $"corrected-300s-generation-health-passes={healthViolations.Count == 0}; corrected-300s-targeted-train-continuity-passes={reverseFlowViolations.Count == 0}; corrected-300s-conservation-inventory-passes={maxMassClosure <= MaximumMassClosureResidualKilograms && maxEnergyClosure <= MaximumEnergyClosureResidualJoules && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond && maxBalancePower <= MaximumBalancePowerResidualWatts}; corrected-300s-deterministic-repeat={deterministicRepeat}; i3-hotfix5-corrected-reference-requalification-passes={passes}; h30-policy-rereview-unblocked={passes};",
            passes
                ? "I.3 Hotfix 5 recommendation: corrected v3 remains continuously healthy and directionally stable across the full 300 s reference horizon with fail-closed telemetry clean. Freeze this result as pre-H.30 re-review evidence, keep I.3 budgets unfrozen, and proceed to a separate H.30 Production Policy Re-review using H.28 cost evidence plus the new explicit-vs-corrected continuity evidence."
                : "I.3 Hotfix 5 recommendation: keep H.30 OPT-IN ONLY and I.3 red. Do not freeze budgets or change production policy until the corrected 300 s failure is localized.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt"), summary, Utf8WithoutBom);
    }

    private static void WriteStepObservations(string path, IReadOnlyList<StepObservation> observations)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,stage_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        lines.AddRange(observations.Select(static item => FormattableString.Invariant(
            $"{item.LogicalStep},{item.SimulatedSeconds:G17},{item.AnyTrip},{item.GeneratorBreakerClosed},{item.RequestedElectricalPowerMegawatts:G17},{item.GrossElectricalPowerMegawatts:G17},{item.RotorShaftPowerMegawatts:G17},{item.CanonicalShaftPowerMegawatts:G17},{item.StageFlowKilogramsPerSecond:G17},{item.StopFlowKilogramsPerSecond:G17},{item.ControlFlowKilogramsPerSecond:G17},{item.AdmissionFlowKilogramsPerSecond:G17},{item.MassClosureResidualKilograms:G17},{item.EnergyClosureResidualJoules:G17},{item.BalanceMassRateResidualKilogramsPerSecond:G17},{item.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(path, lines, Utf8WithoutBom);
    }

    private static string FormatReferenceSample(ReferenceSample sample)
        => string.Join(",", new[]
        {
            sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
            sample.SimulatedSeconds.ToString("G17", CultureInfo.InvariantCulture),
            sample.PresentationFingerprint,
            sample.AnyTrip.ToString(),
            sample.GeneratorBreakerClosed.ToString(),
            sample.RequestedElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.GrossElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.RotorShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.CanonicalShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalSteamFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.StopFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.ControlFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.RotorSpeedRpm.ToString("G17", CultureInfo.InvariantCulture),
            sample.CondenserPressureKilopascals.ToString("G17", CultureInfo.InvariantCulture),
            sample.DrumLevelFraction.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalFluidMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalFluidInternalEnergyJoules.ToString("G17", CultureInfo.InvariantCulture),
            sample.ExhaustMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.HotwellMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.FeedwaterInventoryMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.DrumInventoryMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.MainSteamHeaderMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
        });

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence", "I3_HF5_PreH30");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i3-hotfix5-corrected-300s-healthy-reference-requalification");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} I.3 Hotfix 5 corrected 300 s requalification started{Environment.NewLine}", Utf8WithoutBom);
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

    private sealed record StepObservation(
        long LogicalStep,
        double SimulatedSeconds,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double CanonicalShaftPowerMegawatts,
        double StageFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);

    private sealed record ReferenceSample(
        long LogicalStep,
        double SimulatedSeconds,
        string PresentationFingerprint,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double CanonicalShaftPowerMegawatts,
        double TotalSteamFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double RotorSpeedRpm,
        double CondenserPressureKilopascals,
        double DrumLevelFraction,
        double TotalFluidMassKilograms,
        double TotalFluidInternalEnergyJoules,
        double ExhaustMassKilograms,
        double HotwellMassKilograms,
        double FeedwaterInventoryMassKilograms,
        double DrumInventoryMassKilograms,
        double MainSteamHeaderMassKilograms);

    private sealed record InventorySlope(string MetricId, string Unit, double MeanValue, double SlopePerSecond);
}
