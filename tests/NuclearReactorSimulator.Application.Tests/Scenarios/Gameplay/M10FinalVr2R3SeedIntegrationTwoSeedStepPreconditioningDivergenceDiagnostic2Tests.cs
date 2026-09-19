using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Test-only localization diagnostic authorized by returned Fast-Gate Dynamic Equilibrium Diagnostic 1.
/// It does not repair or retune the seed. It compares the already-frozen raw reference-consistent
/// candidate against canonical mode 1, then reconstructs the exact versioned seed after deterministic
/// preconditioning step 1 and step 2 so the first phase/pressure/head/flow/controller displacement can
/// be assigned to an exact checkpoint. Production source and acceptance envelopes remain untouched.
/// </summary>
public sealed class M10FinalVr2R3SeedIntegrationTwoSeedStepPreconditioningDivergenceDiagnostic2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SEED_PRECONDITIONING_DIAGNOSTIC2";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3SeedIntegrationTwoSeedStepPreconditioningDivergenceDiagnostic2")]
    public void PreconditioningStep1AndStep2_LocalizeFirstMode1Mode2Displacement()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var rawRows = ReadFrozenRawCheckpoint();
        WriteNodeRows("01-raw-checkpoint-node-comparison.csv", rawRows);

        var mode1Step1 = CreateCheckpointEngine(referenceConsistentCandidate: false, deterministicSeedStepCount: 1);
        var candidateStep1 = CreateCheckpointEngine(referenceConsistentCandidate: true, deterministicSeedStepCount: 1);
        var mode1Step2 = CreateCheckpointEngine(referenceConsistentCandidate: false, deterministicSeedStepCount: 2);
        var candidateStep2 = CreateCheckpointEngine(referenceConsistentCandidate: true, deterministicSeedStepCount: 2);

        AssertReconstructedStep2MatchesFactory(
            mode1Step2,
            Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreatePostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep)));
        AssertReconstructedStep2MatchesFactory(
            candidateStep2,
            Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep)));

        var step1Rows = CaptureNodeRows("seed-step1", mode1Step1, candidateStep1);
        var step2Rows = CaptureNodeRows("seed-step2", mode1Step2, candidateStep2);
        WriteNodeRows("02-seed-step1-node-comparison.csv", step1Rows);
        WriteNodeRows("03-seed-step2-node-comparison.csv", step2Rows);

        var headRows = CaptureHeadRows("seed-step1", mode1Step1, candidateStep1)
            .Concat(CaptureHeadRows("seed-step2", mode1Step2, candidateStep2))
            .ToArray();
        WriteHeadRows(headRows);

        var flowRows = CaptureFlowRows("seed-step1", mode1Step1, candidateStep1)
            .Concat(CaptureFlowRows("seed-step2", mode1Step2, candidateStep2))
            .ToArray();
        WriteFlowRows(flowRows);

        var controlRows = new[]
        {
            CaptureControlRow("seed-step1", mode1Step1, candidateStep1),
            CaptureControlRow("seed-step2", mode1Step2, candidateStep2),
        };
        WriteControlRows(controlRows);

        Assert.Equal(12, rawRows.Count);
        Assert.Equal(12, step1Rows.Count);
        Assert.Equal(12, step2Rows.Count);
        Assert.Equal(16, headRows.Length);
        Assert.Equal(24, flowRows.Length);
        Assert.Equal(2, controlRows.Length);
        Assert.All(step1Rows.Concat(step2Rows), static row => Assert.True(row.AllFinite));
        Assert.All(headRows, static row => Assert.True(row.AllFinite));
        Assert.All(flowRows, static row => Assert.True(row.AllFinite));
        Assert.All(controlRows, static row => Assert.True(row.AllFinite));
    }

    private static IReadOnlyList<NodeRow> ReadFrozenRawCheckpoint()
    {
        var root = FindRepositoryRoot();
        var legacy = ReadCsvByKey(Path.Combine(
            root,
            "eng", "frozen-evidence", "ordinary",
            "M10FinalVR2_R3_Requalification2_AuthoredSeedForwardInverseConsistency_Diagnostic2_ReturnedArtifacts",
            "01-raw-authored-seed-closure-comparison.csv"), "node");
        var vector = ReadCsvByKey(Path.Combine(
            root,
            "eng", "frozen-evidence", "ordinary",
            "M10FinalVR2_R3_ReferenceConsistentRawSeed_CandidateConstruction1_ReturnedArtifacts",
            "01-candidate-conserved-inventory-vector.csv"), "node");
        var roundtrip = ReadCsvByKey(Path.Combine(
            root,
            "eng", "frozen-evidence", "ordinary",
            "M10FinalVR2_R3_ReferenceConsistentRawSeed_CandidateConstruction1_ReturnedArtifacts",
            "02-candidate-target-roundtrip.csv"), "node");

        Assert.Equal(12, legacy.Count);
        Assert.Equal(12, vector.Count);
        Assert.Equal(12, roundtrip.Count);
        Assert.Equal(legacy.Keys.OrderBy(static x => x, StringComparer.Ordinal), vector.Keys.OrderBy(static x => x, StringComparer.Ordinal));
        Assert.Equal(legacy.Keys.OrderBy(static x => x, StringComparer.Ordinal), roundtrip.Keys.OrderBy(static x => x, StringComparer.Ordinal));

        return legacy.Keys.OrderBy(static x => x, StringComparer.Ordinal).Select(node =>
        {
            var l = legacy[node];
            var v = vector[node];
            var r = roundtrip[node];
            var mode1Mass = D(l, "mass_kg");
            var candidateMass = D(v, "mass_kg");
            var mode1Energy = D(l, "internal_energy_j");
            var candidateEnergy = D(v, "internal_energy_j");
            var mode1Pressure = D(l, "mode1_pressure_pa");
            var candidatePressure = D(r, "resolved_pressure_pa");
            var mode1Temperature = D(l, "mode1_temperature_k");
            var candidateTemperature = D(r, "resolved_temperature_k");
            var mode1Quality = DN(l, "mode1_quality");
            var candidateQuality = DN(r, "resolved_quality");
            return new NodeRow(
                "raw", node,
                mode1Mass, candidateMass, candidateMass - mode1Mass,
                mode1Energy, candidateEnergy, candidateEnergy - mode1Energy,
                mode1Pressure, candidatePressure, candidatePressure - mode1Pressure,
                mode1Temperature, candidateTemperature, candidateTemperature - mode1Temperature,
                S(l, "mode1_phase"), S(r, "resolved_phase"), mode1Quality, candidateQuality);
        }).ToArray();
    }

    private static IReadOnlyList<NodeRow> CaptureNodeRows(
        string checkpoint,
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var left = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var right = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        Assert.Equal(left.Keys.OrderBy(static x => x, StringComparer.Ordinal), right.Keys.OrderBy(static x => x, StringComparer.Ordinal));

        return left.Keys.OrderBy(static x => x, StringComparer.Ordinal).Select(id =>
        {
            var a = left[id];
            var b = right[id];
            return new NodeRow(
                checkpoint, id,
                a.Mass.Kilograms, b.Mass.Kilograms, b.Mass.Kilograms - a.Mass.Kilograms,
                a.InternalEnergy.Joules, b.InternalEnergy.Joules, b.InternalEnergy.Joules - a.InternalEnergy.Joules,
                a.Pressure.Pascals, b.Pressure.Pascals, b.Pressure.Pascals - a.Pressure.Pascals,
                a.Temperature.Kelvins, b.Temperature.Kelvins, b.Temperature.Kelvins - a.Temperature.Kelvins,
                a.Phase.ToString(), b.Phase.ToString(), a.VaporQuality?.Fraction, b.VaporQuality?.Fraction);
        }).ToArray();
    }

    private static IReadOnlyList<HeadRow> CaptureHeadRows(
        string checkpoint,
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var plant1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant;
        var plant2 = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant;
        var rows = new List<HeadRow>(8);
        Add("main-circulation-pump", Pressure(plant1, "suction") + 1_000_000d - Pressure(plant1, "pressure"), Pressure(plant2, "suction") + 1_000_000d - Pressure(plant2, "pressure"));
        Add("channel", Pressure(plant1, "pressure") - Pressure(plant1, "outlet"), Pressure(plant2, "pressure") - Pressure(plant2, "outlet"));
        Add("return", Pressure(plant1, "outlet") - Pressure(plant1, "drum"), Pressure(plant2, "outlet") - Pressure(plant2, "drum"));
        Add("main-steam-line", Pressure(plant1, "steam") - Pressure(plant1, "header"), Pressure(plant2, "steam") - Pressure(plant2, "header"));
        Add("stop-valve", Pressure(plant1, "header") - Pressure(plant1, "stop-out"), Pressure(plant2, "header") - Pressure(plant2, "stop-out"));
        Add("control-valve", Pressure(plant1, "stop-out") - Pressure(plant1, "control-out"), Pressure(plant2, "stop-out") - Pressure(plant2, "control-out"));
        Add("admission-valve", Pressure(plant1, "control-out") - Pressure(plant1, "turbine-inlet"), Pressure(plant2, "control-out") - Pressure(plant2, "turbine-inlet"));
        Add("turbine-expansion", Pressure(plant1, "turbine-inlet") - Pressure(plant1, "exhaust"), Pressure(plant2, "turbine-inlet") - Pressure(plant2, "exhaust"));
        return rows;

        void Add(string path, double a, double b) => rows.Add(new HeadRow(checkpoint, path, a, b, b - a));
    }

    private static IReadOnlyList<FlowRow> CaptureFlowRows(
        string checkpoint,
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var full1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var full2 = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var cycle1 = full1.IntegratedCycle;
        var cycle2 = full2.IntegratedCycle;
        var primary1 = cycle1.PrimaryCircuit;
        var primary2 = cycle2.PrimaryCircuit;
        var circulation1 = primary1.MainCirculation;
        var circulation2 = primary2.MainCirculation;
        var turbine1 = cycle1.TurbineExpansion;
        var turbine2 = cycle2.TurbineExpansion;
        var stage1 = Assert.Single(turbine1.StageGroups);
        var stage2 = Assert.Single(turbine2.StageGroups);
        var feed1 = Assert.Single(cycle1.CondensateFeedwater.Trains);
        var feed2 = Assert.Single(cycle2.CondensateFeedwater.Trains);
        var rows = new List<FlowRow>(12);

        Add("primary-pump", circulation1.TotalPumpMassFlowRate.KilogramsPerSecond, circulation2.TotalPumpMassFlowRate.KilogramsPerSecond);
        Add("primary-channel", circulation1.TotalChannelMassFlowRate.KilogramsPerSecond, circulation2.TotalChannelMassFlowRate.KilogramsPerSecond);
        Add("primary-return", circulation1.TotalReturnMassFlowRate.KilogramsPerSecond, circulation2.TotalReturnMassFlowRate.KilogramsPerSecond);
        Add("primary-feedwater-boundary", primary1.TotalFeedwaterMassFlowRate.KilogramsPerSecond, primary2.TotalFeedwaterMassFlowRate.KilogramsPerSecond);
        Add("primary-steam-export", primary1.TotalSteamExportMassFlowRate.KilogramsPerSecond, primary2.TotalSteamExportMassFlowRate.KilogramsPerSecond);
        Add("secondary-steam-line", turbine1.MainSteamNetwork.TotalSteamLineMassFlowRate.KilogramsPerSecond, turbine2.MainSteamNetwork.TotalSteamLineMassFlowRate.KilogramsPerSecond);
        Add("secondary-turbine-admission", turbine1.MainSteamNetwork.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond, turbine2.MainSteamNetwork.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond);
        Add("turbine-commanded", stage1.CommandedMassFlowRate.KilogramsPerSecond, stage2.CommandedMassFlowRate.KilogramsPerSecond);
        Add("turbine-transferred", stage1.TotalTransferredMassFlowRate.KilogramsPerSecond, stage2.TotalTransferredMassFlowRate.KilogramsPerSecond);
        Add("turbine-moisture-drain", stage1.MoistureDrainMassFlowRate.KilogramsPerSecond, stage2.MoistureDrainMassFlowRate.KilogramsPerSecond);
        Add("condensate-pump", feed1.CondensatePump.MassFlowRate.KilogramsPerSecond, feed2.CondensatePump.MassFlowRate.KilogramsPerSecond);
        Add("feedwater-pump", feed1.FeedwaterPump.MassFlowRate.KilogramsPerSecond, feed2.FeedwaterPump.MassFlowRate.KilogramsPerSecond);
        return rows;

        void Add(string signal, double a, double b) => rows.Add(new FlowRow(checkpoint, signal, a, b, b - a));
    }

    private static ControlRow CaptureControlRow(
        string checkpoint,
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var protected1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl;
        var protected2 = candidate.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle1 = protected1.FullPlant.IntegratedCycle;
        var cycle2 = protected2.FullPlant.IntegratedCycle;
        var speed1 = protected1.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var speed2 = protected2.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var stage1 = Assert.Single(cycle1.TurbineExpansion.StageGroups);
        var stage2 = Assert.Single(cycle2.TurbineExpansion.StageGroups);
        var gen1 = Assert.Single(cycle1.Generators);
        var gen2 = Assert.Single(cycle2.Generators);
        return new ControlRow(
            checkpoint,
            speed1.Setpoint, speed2.Setpoint,
            speed1.Measurement, speed2.Measurement,
            speed1.Error, speed2.Error,
            speed1.IntegralTerm, speed2.IntegralTerm,
            speed1.Output, speed2.Output,
            stage1.CommandedMassFlowRate.KilogramsPerSecond, stage2.CommandedMassFlowRate.KilogramsPerSecond,
            stage1.TotalTransferredMassFlowRate.KilogramsPerSecond, stage2.TotalTransferredMassFlowRate.KilogramsPerSecond,
            stage1.MoistureDrainMassFlowRate.KilogramsPerSecond, stage2.MoistureDrainMassFlowRate.KilogramsPerSecond,
            stage1.ShaftPower.Watts, stage2.ShaftPower.Watts,
            gen1.ElectricalOutputPower.Megawatts, gen2.ElectricalOutputPower.Megawatts);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateCheckpointEngine(
        bool referenceConsistentCandidate,
        int deterministicSeedStepCount)
    {
        Assert.InRange(deterministicSeedStepCount, 1, 2);

        OperationalFluidNodeSeed[] fluidNodeSeeds = referenceConsistentCandidate
            ?
            [
                new OperationalFluidNodeSeed.ConservedInventory("control-out", 2050.381218833311d, 5265414775.165846d, 3994187.8856824953d, 523.4213085893939d, FluidPhase.SaturatedMixture, 0.9777652720483322d),
                new OperationalFluidNodeSeed.ConservedInventory("drum", 3917.5214804203474d, 5036375894.23722d, 6416459.280654079d, 553.149999989397d, FluidPhase.SaturatedMixture, 0.042322749720366454d),
                new OperationalFluidNodeSeed.ConservedInventory("exhaust", 66.55252025010584d, 142837556.9034473d, 8438.344970819902d, 315.67536613075987d, FluidPhase.SaturatedMixture, 0.8729051078626365d),
                new OperationalFluidNodeSeed.ConservedInventory("feedwater-inventory", 9891.727368706732d, 1962195312.33038d, 17484.899929958774d, 320.5284886658307d, FluidPhase.SubcooledLiquid, null),
                new OperationalFluidNodeSeed.ConservedInventory("header", 3226.6676799875017d, 8341269781.836807d, 6247420.79633744d, 551.3857140089044d, FluidPhase.SaturatedMixture, 0.9980222497643916d),
                new OperationalFluidNodeSeed.ConservedInventory("hotwell", 9891.883976124516d, 1960455817.8974555d, 10808.002981101188d, 320.48565943701715d, FluidPhase.SubcooledLiquid, null),
                new OperationalFluidNodeSeed.ConservedInventory("outlet", 1375.1470283335327d, 2104014157.044389d, 6666459.282132472d, 555.6953255146227d, FluidPhase.SaturatedMixture, 0.21514191260824764d),
                new OperationalFluidNodeSeed.ConservedInventory("pressure", 7507.98659453272d, 9219919054.66319d, 6916459.281680611d, 553.3082998275795d, FluidPhase.SubcooledLiquid, null),
                new OperationalFluidNodeSeed.ConservedInventory("steam", 3306.8269899275992d, 8552155703.008327d, 6398665.756830846d, 552.9659692384483d, FluidPhase.SaturatedMixture, 0.9997887804601804d),
                new OperationalFluidNodeSeed.ConservedInventory("stop-out", 3132.5687588735377d, 8093920564.089228d, 6069485.550324526d, 549.4887377389139d, FluidPhase.SaturatedMixture, 0.9960097013407739d),
                new OperationalFluidNodeSeed.ConservedInventory("suction", 7502.7459746337245d, 9214263779.64126d, 6416459.281680332d, 553.1499999999999d, FluidPhase.SubcooledLiquid, null),
                new OperationalFluidNodeSeed.ConservedInventory("turbine-inlet", 1958.8758929660944d, 5027614200.998343d, 3816252.638393859d, 520.7342832023278d, FluidPhase.SaturatedMixture, 0.9766676884377713d),
            ]
            :
            [
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
            ];

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
                deterministicSeedStepCount: deterministicSeedStepCount,
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
                thermodynamicClosureMode: referenceConsistentCandidate
                    ? WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain
                    : WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
                initialFuelTemperatureCelsiusOverride: 305.62514906467646d,
                initialStructureTemperatureCelsiusOverride: 289.13956081139787d,
                initialFluidNodeSeeds: fluidNodeSeeds,
                governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled,
                turbineAdmissionPhasePolicyOverride: TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain,
                turbineMoistureDrainNodeId: "hotwell"));
    }

    private static void AssertReconstructedStep2MatchesFactory(
        IntegratedAutomaticOperationRuntimeEngine reconstructed,
        IntegratedAutomaticOperationRuntimeEngine factory)
    {
        var left = reconstructed.LatestCanonicalSnapshot.Control.ProtectedControl;
        var right = factory.LatestCanonicalSnapshot.Control.ProtectedControl;
        var leftNodes = left.FullPlant.CandidatePlant.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var rightNodes = right.FullPlant.CandidatePlant.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        Assert.Equal(leftNodes.Keys.OrderBy(static x => x, StringComparer.Ordinal), rightNodes.Keys.OrderBy(static x => x, StringComparer.Ordinal));
        foreach (var id in leftNodes.Keys)
        {
            var a = leftNodes[id];
            var b = rightNodes[id];
            Assert.Equal(a.Mass.Kilograms, b.Mass.Kilograms);
            Assert.Equal(a.InternalEnergy.Joules, b.InternalEnergy.Joules);
            Assert.Equal(a.Pressure.Pascals, b.Pressure.Pascals);
            Assert.Equal(a.Temperature.Kelvins, b.Temperature.Kelvins);
            Assert.Equal(a.Phase, b.Phase);
            Assert.Equal(a.VaporQuality?.Fraction, b.VaporQuality?.Fraction);
        }

        var leftFlow = left.FullPlant.IntegratedCycle.PrimaryCircuit.MainCirculation;
        var rightFlow = right.FullPlant.IntegratedCycle.PrimaryCircuit.MainCirculation;
        Assert.Equal(leftFlow.TotalPumpMassFlowRate.KilogramsPerSecond, rightFlow.TotalPumpMassFlowRate.KilogramsPerSecond);
        Assert.Equal(leftFlow.TotalChannelMassFlowRate.KilogramsPerSecond, rightFlow.TotalChannelMassFlowRate.KilogramsPerSecond);
        Assert.Equal(leftFlow.TotalReturnMassFlowRate.KilogramsPerSecond, rightFlow.TotalReturnMassFlowRate.KilogramsPerSecond);

        var leftSpeed = left.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var rightSpeed = right.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        Assert.Equal(leftSpeed.Setpoint, rightSpeed.Setpoint);
        Assert.Equal(leftSpeed.Measurement, rightSpeed.Measurement);
        Assert.Equal(leftSpeed.Error, rightSpeed.Error);
        Assert.Equal(leftSpeed.IntegralTerm, rightSpeed.IntegralTerm);
        Assert.Equal(leftSpeed.Output, rightSpeed.Output);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadCsvByKey(string path, string keyColumn)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.True(lines.Length >= 2, $"CSV is empty: {path}");
        var headers = lines[0].Split(',');
        var keyIndex = Array.IndexOf(headers, keyColumn);
        Assert.True(keyIndex >= 0, $"Missing key column '{keyColumn}' in {path}");
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            var values = line.Split(',');
            Assert.Equal(headers.Length, values.Length);
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++) row[headers[i]] = values[i];
            result.Add(values[keyIndex], row);
        }
        return result;
    }

    private static double D(IReadOnlyDictionary<string, string> row, string column)
        => double.Parse(S(row, column), CultureInfo.InvariantCulture);
    private static double? DN(IReadOnlyDictionary<string, string> row, string column)
    {
        var value = S(row, column);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            ? null
            : double.Parse(value, CultureInfo.InvariantCulture);
    }
    private static string S(IReadOnlyDictionary<string, string> row, string column)
        => row.TryGetValue(column, out var value) ? value : throw new InvalidDataException($"Missing CSV column '{column}'.");
    private static double Pressure(PlantSnapshot plant, string id) => plant.GetFluidNode(id).Pressure.Pascals;
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string N(double? value) => value?.ToString("R", CultureInfo.InvariantCulture) ?? "null";

    private static void WriteNodeRows(string fileName, IReadOnlyCollection<NodeRow> rows)
    {
        var lines = new List<string>
        {
            "checkpoint,node,mode1_mass_kg,candidate_mass_kg,mass_delta_kg,mode1_energy_j,candidate_energy_j,energy_delta_j,mode1_pressure_pa,candidate_pressure_pa,pressure_delta_pa,mode1_temperature_k,candidate_temperature_k,temperature_delta_k,mode1_phase,candidate_phase,mode1_quality,candidate_quality,phase_match"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.Checkpoint, row.Node,
            F(row.Mode1Mass), F(row.CandidateMass), F(row.MassDelta),
            F(row.Mode1Energy), F(row.CandidateEnergy), F(row.EnergyDelta),
            F(row.Mode1Pressure), F(row.CandidatePressure), F(row.PressureDelta),
            F(row.Mode1Temperature), F(row.CandidateTemperature), F(row.TemperatureDelta),
            row.Mode1Phase, row.CandidatePhase, N(row.Mode1Quality), N(row.CandidateQuality),
            row.PhaseMatch.ToString().ToLowerInvariant())));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), fileName), lines, Utf8WithoutBom);
    }

    private static void WriteHeadRows(IReadOnlyCollection<HeadRow> rows)
    {
        var lines = new List<string> { "checkpoint,path,mode1_head_pa,candidate_head_pa,head_delta_pa" };
        lines.AddRange(rows.Select(row => string.Join(",", row.Checkpoint, row.Path, F(row.Mode1), F(row.Candidate), F(row.Delta))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "04-seed-step-hydraulic-heads.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteFlowRows(IReadOnlyCollection<FlowRow> rows)
    {
        var lines = new List<string> { "checkpoint,signal,mode1_flow_kg_s,candidate_flow_kg_s,flow_delta_kg_s" };
        lines.AddRange(rows.Select(row => string.Join(",", row.Checkpoint, row.Signal, F(row.Mode1), F(row.Candidate), F(row.Delta))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "05-seed-step-flow-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteControlRows(IReadOnlyCollection<ControlRow> rows)
    {
        var lines = new List<string>
        {
            "checkpoint,mode1_speed_setpoint,candidate_speed_setpoint,mode1_speed_measurement,candidate_speed_measurement,mode1_speed_error,candidate_speed_error,mode1_speed_integral,candidate_speed_integral,mode1_governor,candidate_governor,mode1_stage_command_kg_s,candidate_stage_command_kg_s,mode1_stage_transfer_kg_s,candidate_stage_transfer_kg_s,mode1_moisture_drain_kg_s,candidate_moisture_drain_kg_s,mode1_shaft_power_w,candidate_shaft_power_w,mode1_electrical_mwe,candidate_electrical_mwe"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.Checkpoint,
            F(row.Mode1SpeedSetpoint), F(row.CandidateSpeedSetpoint),
            N(row.Mode1SpeedMeasurement), N(row.CandidateSpeedMeasurement),
            F(row.Mode1SpeedError), F(row.CandidateSpeedError),
            F(row.Mode1SpeedIntegral), F(row.CandidateSpeedIntegral),
            F(row.Mode1Governor), F(row.CandidateGovernor),
            F(row.Mode1StageCommand), F(row.CandidateStageCommand),
            F(row.Mode1StageTransfer), F(row.CandidateStageTransfer),
            F(row.Mode1Drain), F(row.CandidateDrain),
            F(row.Mode1ShaftPower), F(row.CandidateShaftPower),
            F(row.Mode1Electrical), F(row.CandidateElectrical))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "06-seed-step-controller-turbine.csv"), lines, Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run this diagnostic.");
        }
    }

    private static string ArtifactDirectory()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        foreach (var file in Directory.EnumerateFiles(path)) File.Delete(file);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed record NodeRow(
        string Checkpoint,
        string Node,
        double Mode1Mass,
        double CandidateMass,
        double MassDelta,
        double Mode1Energy,
        double CandidateEnergy,
        double EnergyDelta,
        double Mode1Pressure,
        double CandidatePressure,
        double PressureDelta,
        double Mode1Temperature,
        double CandidateTemperature,
        double TemperatureDelta,
        string Mode1Phase,
        string CandidatePhase,
        double? Mode1Quality,
        double? CandidateQuality)
    {
        public bool PhaseMatch => string.Equals(Mode1Phase, CandidatePhase, StringComparison.Ordinal);
        public bool AllFinite => new[]
        {
            Mode1Mass, CandidateMass, MassDelta,
            Mode1Energy, CandidateEnergy, EnergyDelta,
            Mode1Pressure, CandidatePressure, PressureDelta,
            Mode1Temperature, CandidateTemperature, TemperatureDelta,
        }.All(double.IsFinite)
            && (!Mode1Quality.HasValue || double.IsFinite(Mode1Quality.Value))
            && (!CandidateQuality.HasValue || double.IsFinite(CandidateQuality.Value));
    }

    private sealed record HeadRow(string Checkpoint, string Path, double Mode1, double Candidate, double Delta)
    {
        public bool AllFinite => double.IsFinite(Mode1) && double.IsFinite(Candidate) && double.IsFinite(Delta);
    }

    private sealed record FlowRow(string Checkpoint, string Signal, double Mode1, double Candidate, double Delta)
    {
        public bool AllFinite => double.IsFinite(Mode1) && double.IsFinite(Candidate) && double.IsFinite(Delta);
    }

    private sealed record ControlRow(
        string Checkpoint,
        double Mode1SpeedSetpoint,
        double CandidateSpeedSetpoint,
        double? Mode1SpeedMeasurement,
        double? CandidateSpeedMeasurement,
        double Mode1SpeedError,
        double CandidateSpeedError,
        double Mode1SpeedIntegral,
        double CandidateSpeedIntegral,
        double Mode1Governor,
        double CandidateGovernor,
        double Mode1StageCommand,
        double CandidateStageCommand,
        double Mode1StageTransfer,
        double CandidateStageTransfer,
        double Mode1Drain,
        double CandidateDrain,
        double Mode1ShaftPower,
        double CandidateShaftPower,
        double Mode1Electrical,
        double CandidateElectrical)
    {
        public bool AllFinite => new[]
        {
            Mode1SpeedSetpoint, CandidateSpeedSetpoint,
            Mode1SpeedError, CandidateSpeedError,
            Mode1SpeedIntegral, CandidateSpeedIntegral,
            Mode1Governor, CandidateGovernor,
            Mode1StageCommand, CandidateStageCommand,
            Mode1StageTransfer, CandidateStageTransfer,
            Mode1Drain, CandidateDrain,
            Mode1ShaftPower, CandidateShaftPower,
            Mode1Electrical, CandidateElectrical,
        }.All(double.IsFinite)
            && (!Mode1SpeedMeasurement.HasValue || double.IsFinite(Mode1SpeedMeasurement.Value))
            && (!CandidateSpeedMeasurement.HasValue || double.IsFinite(CandidateSpeedMeasurement.Value));
    }
}
