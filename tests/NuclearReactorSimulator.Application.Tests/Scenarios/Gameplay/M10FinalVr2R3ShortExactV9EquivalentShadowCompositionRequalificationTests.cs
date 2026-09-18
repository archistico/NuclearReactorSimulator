using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 Final VR2 R3. Test-only exact-v9-equivalent shadow composition.
/// The canonical exact-v9 factory remains immutable. Before mode 2 is scored, a test-local mode-1
/// reconstruction must match canonical exact-v9 for every one of 128 running-step fingerprints.
/// The scored shadow changes only the water/steam closure mode from 1 to 2 and then runs the
/// frozen 120 s exact-v9 short health/conservation/ownership envelope.
/// </summary>
public sealed class M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalificationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1";
    private const int BaselineEquivalenceSteps = 128;
    private const int HealthSteps = 12_000;
    private const int DeterminismSteps = 128;
    private const int TrajectoryStride = 100;
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification1")]
    public void R3_Mode2Shadow_PreservesExactV9EquivalentShortHealthOwnershipAndDeterminism()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var baselineRows = VerifyCanonicalExactV9EqualsShadowBaseline();
        WriteBaselineEquivalence(baselineRows);

        Assert.Equal(BaselineEquivalenceSteps, baselineRows.Count);
        Assert.Equal(0, baselineRows.Count(static row => !row.Match));

        var health = RunMode2ShadowHealth();
        WriteHealthTrajectory(health.Trajectory);
        WriteOwnershipConservationSummary(health);

        var repeat = VerifyMode2DeterministicRepeat();
        WriteDeterministicRepeat(repeat);

        Assert.Equal(HealthSteps, health.Steps);
        Assert.Equal(0, health.NonFiniteSteps);
        Assert.Equal(0, health.HealthEnvelopeViolationSteps);
        Assert.Equal(0, health.TripSteps);
        Assert.Equal(0, health.BreakerOpenSteps);
        Assert.Equal(0, health.Telemetry.RollbackSteps);
        Assert.Equal(0, health.Telemetry.FallbackCommitViolations);
        Assert.Equal(0, health.Telemetry.UnsafeCommitViolations);
        Assert.Equal(0, health.Telemetry.UntargetedBranchDisagreementSteps);

        Assert.InRange(health.MinElectricalMegawatts, 4.99d, 5.01d);
        Assert.InRange(health.MaxElectricalMegawatts, 4.99d, 5.01d);
        Assert.InRange(health.MinPrimaryPumpKilogramsPerSecond, 99.9d, 100.1d);
        Assert.InRange(health.MaxPrimaryPumpKilogramsPerSecond, 99.9d, 100.1d);
        Assert.InRange(health.MinDrumLevelFraction, 0.49d, 0.51d);
        Assert.InRange(health.MaxDrumLevelFraction, 0.49d, 0.51d);
        Assert.InRange(health.MinGovernorOutputPercent, 29.27d, 29.30d);
        Assert.InRange(health.MaxGovernorOutputPercent, 29.27d, 29.30d);

        Assert.True(health.MinimumMoistureDrainKilogramsPerSecond > 0d);
        Assert.True(health.MaximumCommandedTransferMismatchKilogramsPerSecond <= 1e-8d);
        Assert.True(health.MaximumStageEnergyOwnershipResidualWatts <= 1e-3d);
        Assert.True(health.MaximumMassClosureResidualKilograms <= 1e-6d);
        Assert.True(health.MaximumFullEnergyClosureResidualJoules <= 1e-2d);
        Assert.True(health.MaximumBalanceMassRateResidualKilogramsPerSecond <= 1e-8d);
        Assert.True(health.MaximumBalancePowerResidualWatts <= 1e-3d);

        Assert.Equal(DeterminismSteps, repeat.Steps);
        Assert.Equal(repeat.RunAFingerprint, repeat.RunBFingerprint);
    }

    private static List<BaselineEquivalenceRow> VerifyCanonicalExactV9EqualsShadowBaseline()
    {
        var canonical = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        var shadow = CreateExactV9EquivalentShadow(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);

        Assert.Equal(RuntimeStep, canonical.FixedDeltaTime);
        Assert.Equal(RuntimeStep, shadow.FixedDeltaTime);

        var rows = new List<BaselineEquivalenceRow>(BaselineEquivalenceSteps);
        for (var step = 1; step <= BaselineEquivalenceSteps; step++)
        {
            var canonicalSnapshot = canonical.Step(ControlRoomRunState.Running);
            var shadowSnapshot = shadow.Step(ControlRoomRunState.Running);
            var canonicalFingerprint = ControlRoomSnapshotFingerprint.Compute(canonicalSnapshot);
            var shadowFingerprint = ControlRoomSnapshotFingerprint.Compute(shadowSnapshot);
            rows.Add(new BaselineEquivalenceRow(
                step,
                canonicalFingerprint,
                shadowFingerprint,
                string.Equals(canonicalFingerprint, shadowFingerprint, StringComparison.Ordinal)));
        }

        return rows;
    }

    private static Mode2HealthResult RunMode2ShadowHealth()
    {
        var engine = CreateExactV9EquivalentShadow(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        Assert.Equal(RuntimeStep, engine.FixedDeltaTime);

        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var trajectory = new List<HealthTrajectoryRow>(HealthSteps / TrajectoryStride + 1);

        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var nonFiniteSteps = 0;
        var healthEnvelopeViolationSteps = 0;

        var minElectrical = double.PositiveInfinity;
        var maxElectrical = double.NegativeInfinity;
        var minPrimaryPump = double.PositiveInfinity;
        var maxPrimaryPump = double.NegativeInfinity;
        var minDrumLevel = double.PositiveInfinity;
        var maxDrumLevel = double.NegativeInfinity;
        var minGovernorOutput = double.PositiveInfinity;
        var maxGovernorOutput = double.NegativeInfinity;
        var minimumMoistureDrain = double.PositiveInfinity;
        var maximumTransferMismatch = 0d;
        var maximumStageOwnershipResidual = 0d;
        var maximumMassClosure = 0d;
        var maximumEnergyClosure = 0d;
        var maximumBalanceMassRate = 0d;
        var maximumBalancePower = 0d;

        for (var step = 1; step <= HealthSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            telemetryProbe.Observe(engine);

            var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var stage = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
            var speed = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary
                .ControlAndActuator.Controllers.GetDiagnostic("speed-control");

            var electrical = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var primaryPump = fullPlant.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
            var drumLevel = drum.LiquidLevelFraction.Fraction;
            var governorOutput = speed.Output;
            var moistureDrain = stage.MoistureDrainMassFlowRate.KilogramsPerSecond;
            var transferMismatch = Math.Abs(
                stage.TotalTransferredMassFlowRate.KilogramsPerSecond
                - stage.CommandedMassFlowRate.KilogramsPerSecond);
            var stageOwnershipResidual = Math.Abs(stage.TurbineEnergyOwnershipResidual.Watts);
            var massClosure = Math.Abs(fullPlant.HeatBalance.MassClosureResidualKilograms);
            var energyClosure = Math.Abs(fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules);
            var balanceMassRate = Math.Abs(
                fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond);
            var balancePower = Math.Abs(
                fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts);

            var finite = double.IsFinite(electrical)
                && double.IsFinite(primaryPump)
                && double.IsFinite(drumLevel)
                && double.IsFinite(governorOutput)
                && double.IsFinite(moistureDrain)
                && double.IsFinite(transferMismatch)
                && double.IsFinite(stageOwnershipResidual)
                && double.IsFinite(massClosure)
                && double.IsFinite(energyClosure)
                && double.IsFinite(balanceMassRate)
                && double.IsFinite(balancePower);

            if (!finite)
            {
                nonFiniteSteps++;
            }

            var inEnvelope = electrical >= 4.99d && electrical <= 5.01d
                && primaryPump >= 99.9d && primaryPump <= 100.1d
                && drumLevel >= 0.49d && drumLevel <= 0.51d
                && governorOutput >= 29.27d && governorOutput <= 29.30d
                && moistureDrain > 0d
                && transferMismatch <= 1e-8d
                && stageOwnershipResidual <= 1e-3d
                && massClosure <= 1e-6d
                && energyClosure <= 1e-2d
                && balanceMassRate <= 1e-8d
                && balancePower <= 1e-3d
                && !snapshot.AnyTripActive
                && generator.BreakerClosed;

            if (!inEnvelope)
            {
                healthEnvelopeViolationSteps++;
            }

            if (snapshot.AnyTripActive)
            {
                tripSteps++;
            }

            if (!generator.BreakerClosed)
            {
                breakerOpenSteps++;
            }

            minElectrical = Math.Min(minElectrical, electrical);
            maxElectrical = Math.Max(maxElectrical, electrical);
            minPrimaryPump = Math.Min(minPrimaryPump, primaryPump);
            maxPrimaryPump = Math.Max(maxPrimaryPump, primaryPump);
            minDrumLevel = Math.Min(minDrumLevel, drumLevel);
            maxDrumLevel = Math.Max(maxDrumLevel, drumLevel);
            minGovernorOutput = Math.Min(minGovernorOutput, governorOutput);
            maxGovernorOutput = Math.Max(maxGovernorOutput, governorOutput);
            minimumMoistureDrain = Math.Min(minimumMoistureDrain, moistureDrain);
            maximumTransferMismatch = Math.Max(maximumTransferMismatch, transferMismatch);
            maximumStageOwnershipResidual = Math.Max(maximumStageOwnershipResidual, stageOwnershipResidual);
            maximumMassClosure = Math.Max(maximumMassClosure, massClosure);
            maximumEnergyClosure = Math.Max(maximumEnergyClosure, energyClosure);
            maximumBalanceMassRate = Math.Max(maximumBalanceMassRate, balanceMassRate);
            maximumBalancePower = Math.Max(maximumBalancePower, balancePower);

            if (step % TrajectoryStride == 0)
            {
                var telemetry = telemetryProbe.Snapshot();
                trajectory.Add(new HealthTrajectoryRow(
                    step,
                    step * 0.01d,
                    electrical,
                    primaryPump,
                    drumLevel,
                    governorOutput,
                    moistureDrain,
                    transferMismatch,
                    stageOwnershipResidual,
                    massClosure,
                    energyClosure,
                    balanceMassRate,
                    balancePower,
                    snapshot.AnyTripActive,
                    generator.BreakerClosed,
                    telemetry.RollbackSteps,
                    telemetry.FallbackCommitViolations,
                    telemetry.UnsafeCommitViolations,
                    telemetry.UntargetedBranchDisagreementSteps,
                    finite,
                    inEnvelope));
            }
        }

        return new Mode2HealthResult(
            HealthSteps,
            telemetryProbe.Snapshot(),
            tripSteps,
            breakerOpenSteps,
            nonFiniteSteps,
            healthEnvelopeViolationSteps,
            minElectrical,
            maxElectrical,
            minPrimaryPump,
            maxPrimaryPump,
            minDrumLevel,
            maxDrumLevel,
            minGovernorOutput,
            maxGovernorOutput,
            minimumMoistureDrain,
            maximumTransferMismatch,
            maximumStageOwnershipResidual,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            trajectory);
    }

    private static DeterminismResult VerifyMode2DeterministicRepeat()
    {
        var runA = Mode2Fingerprint();
        var runB = Mode2Fingerprint();
        return new DeterminismResult(DeterminismSteps, runA, runB);
    }

    private static string Mode2Fingerprint()
    {
        var engine = CreateExactV9EquivalentShadow(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(snapshot)}||"));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateExactV9EquivalentShadow(
        WaterSteamThermodynamicClosureMode closureMode)
    {
        Assert.True(
            closureMode is WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain
                or WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain,
            $"R3 shadow accepts only closure mode 1 or 2; actual={closureMode}.");

        var fluidNodeSeeds = new OperationalFluidNodeSeed[]
        {
            new OperationalFluidNodeSeed.SaturatedMixture("suction", 6.416459281680372d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("pressure", 280.1582998275795d, 0.0002203018767873456d),
            new OperationalFluidNodeSeed.SaturatedMixture("outlet", 6.666459281680372d, 0.21514191259171503d),
            new OperationalFluidNodeSeed.SaturatedMixture("steam", 6.398665756944915d, 0.99978878048067332d),
            new OperationalFluidNodeSeed.SaturatedMixture("header", 6.2474207966935325d, 0.99802224982943177d),
            new OperationalFluidNodeSeed.SaturatedMixture("stop-out", 6.0694855493389648d, 0.99600970115709719d),
            new OperationalFluidNodeSeed.SaturatedMixture("control-out", 3.9941878857133641d, 0.97776527205630726d),
            new OperationalFluidNodeSeed.SaturatedMixture("turbine-inlet", 3.8162526383587956d, 0.97666768842836382d),
            new OperationalFluidNodeSeed.SaturatedMixture("exhaust", 0.008438344971042927d, 0.87290510788436326d),
            new OperationalFluidNodeSeed.SaturatedMixture("hotwell", 0.010808002980612689d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("feedwater-inventory", 47.37848866583073d, 0.000003024302581887423d),
        };

        return Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
                NeutronPopulation.FromRelative(0.3297117650655722d),
                mainCirculationRunning: true,
                initialRodPosition: ControlRodPosition.FromPercentWithdrawn(50d),
                initialPrimaryTemperatureCelsius: 280d,
                turbineStartupLineup: true,
                initialRotorSpeedRpm: 3_000d,
                initialGeneratorBreakerClosed: true,
                initialRequestedElectricalPowerMegawatts: 5d,
                initialCondenserCoolingPowerMegawatts: 40d,
                initialTurbineSpeedSetpointRpm: 3_000d,
                initialControlValvePercentOpen: 29.281329697436618d,
                initialHeaderSteamTemperatureCelsius: 278.5d,
                initialStopOutletSteamTemperatureCelsius: 276.755d,
                initialControlOutletSteamTemperatureCelsius: 249.5d,
                initialTurbineInletSteamTemperatureCelsius: 246.5d,
                primaryCirculationPipeResistancePascalSecondsSquaredPerKilogramSquared: 25d,
                mainCirculationPumpResistancePascalSecondsSquaredPerKilogramSquared: 25d,
                mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: 850d,
                turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
                speedControllerIntegralGainPerSecond: 0.02d,
                speedControllerDerivativeGainSeconds: 0.2d,
                hotwellControllerProportionalGain: -0.01d,
                includeTurbineShaftPowerInstrumentation: true,
                maximumCondenserMassFlowRateKilogramsPerSecond: 20d,
                condenserInstalledHeatRejectionCapacityMegawatts: 40d,
                condenserOverallHeatTransferConductanceMegawattsPerKelvin: 1.225d,
                condenserCoolingWaterTemperatureCelsius: 20d,
                usePressureResolvedCondenserCondensateEnergy: true,
                secondaryPumpResistancePascalSecondsSquaredPerKilogramSquared: 500d,
                initialCondensatePumpPercent: 42.966515369975916d,
                initialFeedwaterPumpPercent: 96.930826801569154d,
                levelControllerIntegralGainPerSecond: 0.001d,
                hotwellControllerIntegralGainPerSecond: -0.000001d,
                exhaustSteamSpaceVolumeCubicMetres: 1_000d,
                pressurizedSteamPathNodeVolumeCubicMetres: 100d,
                turbineExpansionResistancePascalSecondsSquaredPerKilogramSquared: 21_400d,
                useThermodynamicTurbineWork: true,
                turbineStageEfficiencyPercent: 86d,
                generatorMaximumSynchronizingCorrectionPowerMegawatts: 0.5d,
                generatorFrequencyDampingPowerAtOneHertzSlipMegawatts: 2d,
                secondaryPumpsHaveDischargeCheckValves: true,
                includeEnhancedSecondaryProtections: true,
                secondaryValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
                secondaryPumpTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.25d),
                governorFullLoadSpeedReferenceRiseRpm: 1.5d,
                steamDrumLiquidRecirculationMode: SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
                steamDrumSteamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d,
                includeCoreThermalCoupling: true,
                primaryOperationalFlowDisplayLagSeconds: 0.5d,
                initialSteamDrumLiquidLevelFraction: 0.5d,
                useVaporFractionLimitedTurbineAdmission: true,
                turbineRotorRatedSpeedMechanicalLossMegawatts: 0.5d,
                deterministicSeedStepCount: 2,
                turbineStopValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
                generatorMaximumElectricalPowerMegawatts: 10d,
                generatorGridPowerFlowMode: SynchronousGridPowerFlowMode.Bidirectional,
                includeEvidenceDerivedElectricalProtections: true,
                includeMainSteamHeaderRelief: true,
                includeTurbineBypass: true,
                useEnthalpyTransportForPassivePipesAndValves: true,
                useEnthalpyTransportForRemainingNonTurbinePaths: true,
                useEnthalpyTransportForTurbineExpansion: true,
                useHybridSemiImplicitHydraulics: false,
                runtimeStep: RuntimeStep,
                useFourNodeBranchContinuityShadowIntegration: false,
                useFourNodeBranchContinuityCorrectedCommitOptIn: true,
                thermodynamicClosureMode: closureMode,
                initialFuelTemperatureCelsiusOverride: 305.62514906467646d,
                initialStructureTemperatureCelsiusOverride: 289.13956081139787d,
                initialFluidNodeSeeds: fluidNodeSeeds,
                governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled,
                turbineAdmissionPhasePolicyOverride: TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain,
                turbineMoistureDrainNodeId: "hotwell"));
    }

    private static void WriteBaselineEquivalence(IReadOnlyCollection<BaselineEquivalenceRow> rows)
    {
        var lines = new List<string> { "step,canonical_fingerprint,shadow_baseline_fingerprint,match" };
        lines.AddRange(rows.Select(static row => string.Join(
            ',',
            row.Step.ToString(CultureInfo.InvariantCulture),
            row.CanonicalFingerprint,
            row.ShadowFingerprint,
            row.Match ? "true" : "false")));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "02-shadow-baseline-equivalence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteHealthTrajectory(IReadOnlyCollection<HealthTrajectoryRow> rows)
    {
        var lines = new List<string>
        {
            "step,seconds,electrical_mwe,primary_pump_kg_s,drum_level,governor_output_percent,moisture_drain_kg_s,transfer_mismatch_kg_s,stage_energy_ownership_residual_w,mass_closure_kg,full_energy_closure_j,balance_mass_rate_kg_s,balance_power_w,trip_active,breaker_closed,rollbacks,fallback_commit_violations,unsafe_commit_violations,untargeted_branch_disagreements,finite,in_envelope"
        };

        lines.AddRange(rows.Select(static row => string.Join(
            ',',
            row.Step.ToString(CultureInfo.InvariantCulture),
            row.Seconds.ToString("R", CultureInfo.InvariantCulture),
            row.ElectricalMegawatts.ToString("R", CultureInfo.InvariantCulture),
            row.PrimaryPumpKilogramsPerSecond.ToString("R", CultureInfo.InvariantCulture),
            row.DrumLevelFraction.ToString("R", CultureInfo.InvariantCulture),
            row.GovernorOutputPercent.ToString("R", CultureInfo.InvariantCulture),
            row.MoistureDrainKilogramsPerSecond.ToString("R", CultureInfo.InvariantCulture),
            row.TransferMismatchKilogramsPerSecond.ToString("R", CultureInfo.InvariantCulture),
            row.StageEnergyOwnershipResidualWatts.ToString("R", CultureInfo.InvariantCulture),
            row.MassClosureResidualKilograms.ToString("R", CultureInfo.InvariantCulture),
            row.FullEnergyClosureResidualJoules.ToString("R", CultureInfo.InvariantCulture),
            row.BalanceMassRateResidualKilogramsPerSecond.ToString("R", CultureInfo.InvariantCulture),
            row.BalancePowerResidualWatts.ToString("R", CultureInfo.InvariantCulture),
            row.TripActive ? "true" : "false",
            row.BreakerClosed ? "true" : "false",
            row.Rollbacks.ToString(CultureInfo.InvariantCulture),
            row.FallbackCommitViolations.ToString(CultureInfo.InvariantCulture),
            row.UnsafeCommitViolations.ToString(CultureInfo.InvariantCulture),
            row.UntargetedBranchDisagreements.ToString(CultureInfo.InvariantCulture),
            row.Finite ? "true" : "false",
            row.InEnvelope ? "true" : "false")));

        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "03-mode2-shadow-health-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteOwnershipConservationSummary(Mode2HealthResult health)
    {
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "04-ownership-conservation-summary.txt"), new[]
        {
            "status=MODE2-SHADOW-HEALTH-EVIDENCE-WRITTEN",
            $"steps={health.Steps.ToString(CultureInfo.InvariantCulture)}",
            "simulated-seconds=120",
            "fixed-timestep-ms=10",
            $"non-finite-steps={health.NonFiniteSteps.ToString(CultureInfo.InvariantCulture)}",
            $"health-envelope-violation-steps={health.HealthEnvelopeViolationSteps.ToString(CultureInfo.InvariantCulture)}",
            $"trip-steps={health.TripSteps.ToString(CultureInfo.InvariantCulture)}",
            $"breaker-open-steps={health.BreakerOpenSteps.ToString(CultureInfo.InvariantCulture)}",
            $"rollbacks={health.Telemetry.RollbackSteps.ToString(CultureInfo.InvariantCulture)}",
            $"fallback-commit-violations={health.Telemetry.FallbackCommitViolations.ToString(CultureInfo.InvariantCulture)}",
            $"unsafe-commit-violations={health.Telemetry.UnsafeCommitViolations.ToString(CultureInfo.InvariantCulture)}",
            $"untargeted-branch-disagreements={health.Telemetry.UntargetedBranchDisagreementSteps.ToString(CultureInfo.InvariantCulture)}",
            FormattableString.Invariant($"electrical-range-mwe={health.MinElectricalMegawatts:R}..{health.MaxElectricalMegawatts:R}"),
            FormattableString.Invariant($"min-electrical-mwe={health.MinElectricalMegawatts:R}"),
            FormattableString.Invariant($"max-electrical-mwe={health.MaxElectricalMegawatts:R}"),
            FormattableString.Invariant($"primary-pump-range-kg-s={health.MinPrimaryPumpKilogramsPerSecond:R}..{health.MaxPrimaryPumpKilogramsPerSecond:R}"),
            FormattableString.Invariant($"min-primary-pump-kg-s={health.MinPrimaryPumpKilogramsPerSecond:R}"),
            FormattableString.Invariant($"max-primary-pump-kg-s={health.MaxPrimaryPumpKilogramsPerSecond:R}"),
            FormattableString.Invariant($"drum-level-range={health.MinDrumLevelFraction:R}..{health.MaxDrumLevelFraction:R}"),
            FormattableString.Invariant($"min-drum-level={health.MinDrumLevelFraction:R}"),
            FormattableString.Invariant($"max-drum-level={health.MaxDrumLevelFraction:R}"),
            FormattableString.Invariant($"governor-output-range-percent={health.MinGovernorOutputPercent:R}..{health.MaxGovernorOutputPercent:R}"),
            FormattableString.Invariant($"min-governor-output-percent={health.MinGovernorOutputPercent:R}"),
            FormattableString.Invariant($"max-governor-output-percent={health.MaxGovernorOutputPercent:R}"),
            FormattableString.Invariant($"minimum-moisture-drain-kg-s={health.MinimumMoistureDrainKilogramsPerSecond:R}"),
            FormattableString.Invariant($"max-commanded-transfer-mismatch-kg-s={health.MaximumCommandedTransferMismatchKilogramsPerSecond:R}"),
            FormattableString.Invariant($"max-stage-energy-ownership-residual-w={health.MaximumStageEnergyOwnershipResidualWatts:R}"),
            FormattableString.Invariant($"max-network-mass-closure-kg={health.MaximumMassClosureResidualKilograms:R}"),
            FormattableString.Invariant($"max-network-energy-closure-j={health.MaximumFullEnergyClosureResidualJoules:R}"),
            FormattableString.Invariant($"max-network-balance-mass-rate-kg-s={health.MaximumBalanceMassRateResidualKilogramsPerSecond:R}"),
            FormattableString.Invariant($"max-network-balance-power-w={health.MaximumBalancePowerResidualWatts:R}"),
            "canonical-exact-v9-change=False",
            "mode2-default-activation=False",
            "r4-long-materiality-executed=False",
        }, Utf8WithoutBom);
    }

    private static void WriteDeterministicRepeat(DeterminismResult repeat)
    {
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "05-deterministic-repeat.txt"), new[]
        {
            "status=MODE2-SHADOW-DETERMINISTIC-REPEAT-EVIDENCE-WRITTEN",
            $"steps={repeat.Steps.ToString(CultureInfo.InvariantCulture)}",
            $"run-a-fingerprint={repeat.RunAFingerprint}",
            $"run-b-fingerprint={repeat.RunBFingerprint}",
            $"fingerprint-match={(repeat.RunAFingerprint == repeat.RunBFingerprint ? "True" : "False")}",
            "historical-exact-v9-fingerprint-equality-required=False",
        }, Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled R3 qualification runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1");

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
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

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the R3 test output directory.");
    }

    private readonly record struct BaselineEquivalenceRow(
        int Step,
        string CanonicalFingerprint,
        string ShadowFingerprint,
        bool Match);

    private sealed record Mode2HealthResult(
        int Steps,
        FourNodeProductionActivationTelemetrySnapshot Telemetry,
        int TripSteps,
        int BreakerOpenSteps,
        int NonFiniteSteps,
        int HealthEnvelopeViolationSteps,
        double MinElectricalMegawatts,
        double MaxElectricalMegawatts,
        double MinPrimaryPumpKilogramsPerSecond,
        double MaxPrimaryPumpKilogramsPerSecond,
        double MinDrumLevelFraction,
        double MaxDrumLevelFraction,
        double MinGovernorOutputPercent,
        double MaxGovernorOutputPercent,
        double MinimumMoistureDrainKilogramsPerSecond,
        double MaximumCommandedTransferMismatchKilogramsPerSecond,
        double MaximumStageEnergyOwnershipResidualWatts,
        double MaximumMassClosureResidualKilograms,
        double MaximumFullEnergyClosureResidualJoules,
        double MaximumBalanceMassRateResidualKilogramsPerSecond,
        double MaximumBalancePowerResidualWatts,
        IReadOnlyList<HealthTrajectoryRow> Trajectory);

    private readonly record struct HealthTrajectoryRow(
        int Step,
        double Seconds,
        double ElectricalMegawatts,
        double PrimaryPumpKilogramsPerSecond,
        double DrumLevelFraction,
        double GovernorOutputPercent,
        double MoistureDrainKilogramsPerSecond,
        double TransferMismatchKilogramsPerSecond,
        double StageEnergyOwnershipResidualWatts,
        double MassClosureResidualKilograms,
        double FullEnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts,
        bool TripActive,
        bool BreakerClosed,
        long Rollbacks,
        long FallbackCommitViolations,
        long UnsafeCommitViolations,
        long UntargetedBranchDisagreements,
        bool Finite,
        bool InEnvelope);

    private readonly record struct DeterminismResult(
        int Steps,
        string RunAFingerprint,
        string RunBFingerprint);
}
