using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Test-only REV1 runtime evidence capture authorized by Diagnostic 3 Deep Review & REV1 Planning 1.
/// It reconstructs exactly the same raw -> seed-step1 10 ms scenario as the reviewed Diagnostic 3,
/// records production mass/energy bookkeeping, reconstructs the mode-2 inverse path from the exact
/// frozen conserved inventories, decomposes the steam-drum liquid transport term as u + p/rho, and
/// emits the minimal input consumed later by the independent Simulation.Tests IAPWS-IF97 comparator.
/// It does not select or implement a production repair.
/// </summary>
public sealed class M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1")]
    public void SeedStep1_SuctionEnergyTransport_EmitsRuntimeEvidenceForIndependentCounterfactual()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var frozenRaw = ReadFrozenNodeRows("01-raw-checkpoint-node-comparison.csv");
        var frozenStep1 = ReadFrozenNodeRows("02-seed-step1-node-comparison.csv");
        var mode1 = CreateCheckpointEngine(referenceConsistentCandidate: false);
        var candidate = CreateCheckpointEngine(referenceConsistentCandidate: true);

        var mode1Row = CaptureBalance(
            "mode1",
            mode1,
            frozenRaw["suction"],
            frozenStep1["suction"],
            candidateSide: false);
        var candidateRow = CaptureBalance(
            "candidate-mode2",
            candidate,
            frozenRaw["suction"],
            frozenStep1["suction"],
            candidateSide: true);
        WriteBalanceRows([mode1Row, candidateRow]);

        var pathRows = CaptureMode2ResolverPaths(
            frozenRaw["suction"],
            frozenStep1["suction"],
            candidate);
        WriteResolverPathRows(pathRows);

        var transportRow = CaptureForwardTransportDecomposition(candidate, frozenRaw["drum"]);
        WriteForwardTransportRows([transportRow]);

        var counterfactualInput = new CounterfactualInputRow(
            transportRow.DrumTemperatureKelvins,
            transportRow.DrumPressurePascals,
            candidateRow.RawSuctionAdvectedSpecificEnergyJoulesPerKilogram,
            transportRow.DrumLiquidAdvectedSpecificEnergyJoulesPerKilogram,
            transportRow.DrumLiquidSpecificInternalEnergyJoulesPerKilogram,
            transportRow.ProductionForwardLiquidDensityKilogramsPerCubicMetre,
            candidateRow.RecirculationFlowKilogramsPerSecond,
            candidateRow.PumpFlowKilogramsPerSecond,
            candidateRow.ObservedEnergyRateWatts);
        WriteReferenceCounterfactualInputRows([counterfactualInput]);
    }

    private static BalanceRow CaptureBalance(
        string label,
        IntegratedAutomaticOperationRuntimeEngine engine,
        IReadOnlyDictionary<string, string> raw,
        IReadOnlyDictionary<string, string> frozenStep1,
        bool candidateSide)
    {
        var prefix = candidateSide ? "candidate" : "mode1";
        var rawMass = D(raw[$"{prefix}_mass_kg"]);
        var rawEnergy = D(raw[$"{prefix}_energy_j"]);
        var rawPressure = D(raw[$"{prefix}_pressure_pa"]);
        var frozenStepMass = D(frozenStep1[$"{prefix}_mass_kg"]);
        var frozenStepEnergy = D(frozenStep1[$"{prefix}_energy_j"]);

        var control = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = control.FullPlant;
        var primary = fullPlant.IntegratedCycle.PrimaryCircuit;
        var post = fullPlant.CandidatePlant.GetFluidNode("suction");

        var loop = primary.MainCirculation.GetLoop("loop");
        var pump = loop.GetPump("pump");
        var drum = primary.SteamDrums.GetDrum("drum-a");
        var plantDefinition = engine.CurrentState.PlantState.PlantState.Definition;
        var suctionDefinition = plantDefinition.GetFluidNode("suction");
        var pumpDefinition = plantDefinition.GetPump("pump");
        var rawDensity = Density.FromKilogramsPerCubicMetre(rawMass / suctionDefinition.Volume.CubicMetres);
        var rawSpecificInternalEnergy = SpecificEnergy.FromJoulesPerKilogram(rawEnergy / rawMass);
        var rawSelectedSpecificEnergy = FluidEnergyTransport.ResolveSelectedSpecificEnergy(
            pumpDefinition.Pipe.EnergyTransportMode,
            rawSpecificInternalEnergy,
            Pressure.FromPascals(rawPressure),
            rawDensity);

        var predictedMassRate = drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond
            - pump.MassFlowRate.KilogramsPerSecond;
        var predictedEnergyRate = drum.LiquidEnergyRate.Watts
            - (rawSelectedSpecificEnergy.JoulesPerKilogram * pump.MassFlowRate.KilogramsPerSecond);
        var observedMassRate = (post.Mass.Kilograms - rawMass) / RuntimeStep.TotalSeconds;
        var observedEnergyRate = (post.InternalEnergy.Joules - rawEnergy) / RuntimeStep.TotalSeconds;

        return new BalanceRow(
            label,
            pumpDefinition.Pipe.EnergyTransportMode.ToString(),
            rawMass,
            frozenStepMass,
            post.Mass.Kilograms,
            post.Mass.Kilograms - frozenStepMass,
            observedMassRate,
            pump.MassFlowRate.KilogramsPerSecond,
            drum.RequestedLiquidRecirculationMassFlowRate.KilogramsPerSecond,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.LiquidRecirculationInventoryLimited,
            predictedMassRate,
            predictedMassRate - observedMassRate,
            rawEnergy,
            frozenStepEnergy,
            post.InternalEnergy.Joules,
            post.InternalEnergy.Joules - frozenStepEnergy,
            observedEnergyRate,
            rawSelectedSpecificEnergy.JoulesPerKilogram,
            drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram,
            rawSelectedSpecificEnergy.JoulesPerKilogram - drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram,
            drum.LiquidEnergyRate.Watts,
            predictedEnergyRate,
            predictedEnergyRate - observedEnergyRate,
            post.Phase.ToString(),
            post.VaporQuality?.Fraction);
    }

    private static ResolverPathRow[] CaptureMode2ResolverPaths(
        IReadOnlyDictionary<string, string> raw,
        IReadOnlyDictionary<string, string> step1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var volume = candidate.CurrentState.PlantState.PlantState.Definition
            .GetFluidNode("suction")
            .Volume
            .CubicMetres;
        var resolver = new ReferenceConsistentTabulatedInverseResolver();
        return
        [
            ResolvePath("raw", raw, "candidate", volume, resolver),
            ResolvePath("seed-step1", step1, "candidate", volume, resolver),
        ];
    }

    private static ResolverPathRow ResolvePath(
        string checkpoint,
        IReadOnlyDictionary<string, string> row,
        string prefix,
        double volume,
        ReferenceConsistentTabulatedInverseResolver resolver)
    {
        var mass = D(row[$"{prefix}_mass_kg"]);
        var energy = D(row[$"{prefix}_energy_j"]);
        var specificVolume = volume / mass;
        var specificEnergy = energy / mass;
        var resolvedOk = resolver.TryResolveWithPath(
            specificVolume,
            specificEnergy,
            out var resolved,
            out var path);

        return resolvedOk
            ? new ResolverPathRow(
                checkpoint,
                mass,
                energy,
                specificVolume,
                specificEnergy,
                true,
                resolved.Phase.ToString(),
                resolved.VaporQuality,
                path.ToString(),
                "RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY")
            : new ResolverPathRow(
                checkpoint,
                mass,
                energy,
                specificVolume,
                specificEnergy,
                false,
                "UNRESOLVED",
                null,
                "UNRESOLVED",
                "RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY");
    }

    private static ForwardTransportRow CaptureForwardTransportDecomposition(
        IntegratedAutomaticOperationRuntimeEngine candidate,
        IReadOnlyDictionary<string, string> rawDrum)
    {
        var rawTemperatureKelvins = D(rawDrum["candidate_temperature_k"]);
        var rawPressurePascals = D(rawDrum["candidate_pressure_pa"]);
        var temperature = Temperature.FromKelvins(rawTemperatureKelvins);
        var mode1 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var mode2 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var left = mode1.GetSaturationProperties(temperature);
        var right = mode2.GetSaturationProperties(temperature);
        var drum = candidate.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.GetDrum("drum-a");

        var pressureBitsEqual = SameBits(left.Pressure.Pascals, right.Pressure.Pascals);
        var liquidDensityBitsEqual = SameBits(
            left.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            right.SaturatedLiquidDensity.KilogramsPerCubicMetre);
        var vaporDensityBitsEqual = SameBits(
            left.SaturatedVaporDensity.KilogramsPerCubicMetre,
            right.SaturatedVaporDensity.KilogramsPerCubicMetre);
        var liquidEnergyBitsEqual = SameBits(
            left.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            right.SaturatedLiquidInternalEnergy.JoulesPerKilogram);
        var vaporEnergyBitsEqual = SameBits(
            left.SaturatedVaporInternalEnergy.JoulesPerKilogram,
            right.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var allBitsEqual = pressureBitsEqual
            && liquidDensityBitsEqual
            && vaporDensityBitsEqual
            && liquidEnergyBitsEqual
            && vaporEnergyBitsEqual;

        var productionDensity = right.SaturatedLiquidDensity.KilogramsPerCubicMetre;
        var calculatedFlowWork = drum.Pressure.Pascals / productionDensity;
        var calculatedEnthalpy = drum.LiquidSpecificInternalEnergy.JoulesPerKilogram + calculatedFlowWork;
        var calculatedRate = drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram
            * drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond;

        return new ForwardTransportRow(
            rawTemperatureKelvins,
            drum.Temperature.Kelvins,
            drum.Temperature.Kelvins - rawTemperatureKelvins,
            rawPressurePascals,
            drum.Pressure.Pascals,
            drum.Pressure.Pascals - rawPressurePascals,
            drum.Phase.ToString(),
            drum.EnergyTransportMode.ToString(),
            left.Pressure.Pascals,
            right.Pressure.Pascals,
            left.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            right.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            left.SaturatedVaporDensity.KilogramsPerCubicMetre,
            right.SaturatedVaporDensity.KilogramsPerCubicMetre,
            left.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            right.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            left.SaturatedVaporInternalEnergy.JoulesPerKilogram,
            right.SaturatedVaporInternalEnergy.JoulesPerKilogram,
            pressureBitsEqual,
            liquidDensityBitsEqual,
            vaporDensityBitsEqual,
            liquidEnergyBitsEqual,
            vaporEnergyBitsEqual,
            allBitsEqual,
            productionDensity,
            drum.LiquidSpecificInternalEnergy.JoulesPerKilogram,
            drum.LiquidSpecificFlowWork.JoulesPerKilogram,
            calculatedFlowWork,
            drum.LiquidSpecificFlowWork.JoulesPerKilogram - calculatedFlowWork,
            drum.LiquidSpecificEnthalpy.JoulesPerKilogram,
            calculatedEnthalpy,
            drum.LiquidSpecificEnthalpy.JoulesPerKilogram - calculatedEnthalpy,
            drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram,
            drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram - calculatedEnthalpy,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.LiquidEnergyRate.Watts,
            calculatedRate,
            drum.LiquidEnergyRate.Watts - calculatedRate);
    }

    private static bool SameBits(double left, double right)
        => BitConverter.DoubleToInt64Bits(left) == BitConverter.DoubleToInt64Bits(right);

    private static Dictionary<string, IReadOnlyDictionary<string, string>> ReadFrozenNodeRows(string fileName)
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "eng", "frozen-evidence", "ordinary",
            "M10FinalVR2_R3_SeedIntegration_TwoSeedStepPreconditioningDivergenceDiagnostic2_ReturnedArtifacts",
            fileName);
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.True(lines.Length >= 2, $"CSV is empty: {path}");
        var headers = lines[0].Split(',');
        var nodeIndex = Array.IndexOf(headers, "node");
        Assert.True(nodeIndex >= 0, $"Missing node column in {path}");
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            var values = line.Split(',');
            Assert.Equal(headers.Length, values.Length);
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++) row.Add(headers[i], values[i]);
            result.Add(values[nodeIndex], row);
        }
        return result;
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateCheckpointEngine(bool referenceConsistentCandidate)
    {
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
                deterministicSeedStepCount: 1,
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

    private static void WriteBalanceRows(IReadOnlyCollection<BalanceRow> rows)
    {
        var lines = new List<string>
        {
            "case,energy_transport_mode,raw_mass_kg,frozen_step1_mass_kg,runtime_step1_mass_kg,checkpoint_mass_delta_kg,observed_mass_rate_kg_s,pump_flow_kg_s,requested_recirculation_flow_kg_s,recirculation_flow_kg_s,recirculation_inventory_limited,predicted_mass_rate_kg_s,predicted_mass_residual_kg_s,raw_energy_j,frozen_step1_energy_j,runtime_step1_energy_j,checkpoint_energy_delta_j,observed_energy_rate_w,raw_suction_advected_specific_energy_j_kg,recirculation_advected_specific_energy_j_kg,suction_minus_recirculation_specific_energy_j_kg,recirculation_energy_rate_w,predicted_energy_rate_w,predicted_energy_residual_w,step1_phase,step1_quality"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.Case,
            row.EnergyTransportMode,
            F(row.RawMassKilograms),
            F(row.FrozenStep1MassKilograms),
            F(row.RuntimeStep1MassKilograms),
            F(row.CheckpointMassDeltaKilograms),
            F(row.ObservedMassRateKilogramsPerSecond),
            F(row.PumpFlowKilogramsPerSecond),
            F(row.RequestedRecirculationFlowKilogramsPerSecond),
            F(row.RecirculationFlowKilogramsPerSecond),
            row.RecirculationInventoryLimited ? "true" : "false",
            F(row.PredictedMassRateKilogramsPerSecond),
            F(row.PredictedMassResidualKilogramsPerSecond),
            F(row.RawEnergyJoules),
            F(row.FrozenStep1EnergyJoules),
            F(row.RuntimeStep1EnergyJoules),
            F(row.CheckpointEnergyDeltaJoules),
            F(row.ObservedEnergyRateWatts),
            F(row.RawSuctionAdvectedSpecificEnergyJoulesPerKilogram),
            F(row.RecirculationAdvectedSpecificEnergyJoulesPerKilogram),
            F(row.SuctionMinusRecirculationSpecificEnergyJoulesPerKilogram),
            F(row.RecirculationEnergyRateWatts),
            F(row.PredictedEnergyRateWatts),
            F(row.PredictedEnergyResidualWatts),
            row.Step1Phase,
            Optional(row.Step1Quality))));
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "01-step1-suction-energy-balance.csv"),
            lines,
            Utf8WithoutBom);
    }

    private static void WriteResolverPathRows(IReadOnlyCollection<ResolverPathRow> rows)
    {
        var lines = new List<string>
        {
            "checkpoint,mass_kg,energy_j,specific_volume_m3_kg,specific_energy_j_kg,resolved,resolved_phase,resolved_quality,resolution_path,evidence_semantics"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.Checkpoint,
            F(row.MassKilograms),
            F(row.EnergyJoules),
            F(row.SpecificVolumeCubicMetresPerKilogram),
            F(row.SpecificEnergyJoulesPerKilogram),
            row.Resolved ? "true" : "false",
            row.ResolvedPhase,
            Optional(row.ResolvedQuality),
            row.ResolutionPath,
            row.EvidenceSemantics)));
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "02-mode2-suction-inverse-path.csv"),
            lines,
            Utf8WithoutBom);
    }

    private static void WriteForwardTransportRows(IReadOnlyCollection<ForwardTransportRow> rows)
    {
        var lines = new List<string>
        {
            "frozen_raw_drum_temperature_k,runtime_drum_temperature_k,drum_temperature_delta_k,frozen_raw_drum_pressure_pa,runtime_drum_pressure_pa,drum_pressure_delta_pa,drum_phase,energy_transport_mode,mode1_saturation_pressure_pa,mode2_saturation_pressure_pa,mode1_liquid_density_kg_m3,mode2_liquid_density_kg_m3,mode1_vapor_density_kg_m3,mode2_vapor_density_kg_m3,mode1_liquid_internal_energy_j_kg,mode2_liquid_internal_energy_j_kg,mode1_vapor_internal_energy_j_kg,mode2_vapor_internal_energy_j_kg,pressure_bits_equal,liquid_density_bits_equal,vapor_density_bits_equal,liquid_internal_energy_bits_equal,vapor_internal_energy_bits_equal,forward_properties_all_bits_equal,production_forward_liquid_density_kg_m3,drum_liquid_internal_energy_j_kg,drum_liquid_specific_flow_work_j_kg,calculated_p_over_rho_j_kg,flow_work_identity_residual_j_kg,drum_liquid_specific_enthalpy_j_kg,calculated_u_plus_p_over_rho_j_kg,enthalpy_identity_residual_j_kg,drum_liquid_advected_specific_energy_j_kg,selected_transport_identity_residual_j_kg,recirculation_flow_kg_s,drum_liquid_energy_rate_w,calculated_liquid_energy_rate_w,liquid_energy_rate_identity_residual_w"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            F(row.FrozenRawDrumTemperatureKelvins),
            F(row.DrumTemperatureKelvins),
            F(row.DrumTemperatureDeltaKelvins),
            F(row.FrozenRawDrumPressurePascals),
            F(row.DrumPressurePascals),
            F(row.DrumPressureDeltaPascals),
            row.DrumPhase,
            row.EnergyTransportMode,
            F(row.Mode1SaturationPressurePascals),
            F(row.Mode2SaturationPressurePascals),
            F(row.Mode1LiquidDensityKilogramsPerCubicMetre),
            F(row.Mode2LiquidDensityKilogramsPerCubicMetre),
            F(row.Mode1VaporDensityKilogramsPerCubicMetre),
            F(row.Mode2VaporDensityKilogramsPerCubicMetre),
            F(row.Mode1LiquidInternalEnergyJoulesPerKilogram),
            F(row.Mode2LiquidInternalEnergyJoulesPerKilogram),
            F(row.Mode1VaporInternalEnergyJoulesPerKilogram),
            F(row.Mode2VaporInternalEnergyJoulesPerKilogram),
            row.PressureBitsEqual ? "true" : "false",
            row.LiquidDensityBitsEqual ? "true" : "false",
            row.VaporDensityBitsEqual ? "true" : "false",
            row.LiquidInternalEnergyBitsEqual ? "true" : "false",
            row.VaporInternalEnergyBitsEqual ? "true" : "false",
            row.ForwardPropertiesAllBitsEqual ? "true" : "false",
            F(row.ProductionForwardLiquidDensityKilogramsPerCubicMetre),
            F(row.DrumLiquidSpecificInternalEnergyJoulesPerKilogram),
            F(row.DrumLiquidSpecificFlowWorkJoulesPerKilogram),
            F(row.CalculatedPressureOverDensityJoulesPerKilogram),
            F(row.FlowWorkIdentityResidualJoulesPerKilogram),
            F(row.DrumLiquidSpecificEnthalpyJoulesPerKilogram),
            F(row.CalculatedInternalEnergyPlusFlowWorkJoulesPerKilogram),
            F(row.EnthalpyIdentityResidualJoulesPerKilogram),
            F(row.DrumLiquidAdvectedSpecificEnergyJoulesPerKilogram),
            F(row.SelectedTransportIdentityResidualJoulesPerKilogram),
            F(row.RecirculationFlowKilogramsPerSecond),
            F(row.DrumLiquidEnergyRateWatts),
            F(row.CalculatedLiquidEnergyRateWatts),
            F(row.LiquidEnergyRateIdentityResidualWatts))));
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "03-forward-transport-decomposition.csv"),
            lines,
            Utf8WithoutBom);
    }

    private static void WriteReferenceCounterfactualInputRows(IReadOnlyCollection<CounterfactualInputRow> rows)
    {
        var lines = new List<string>
        {
            "drum_temperature_k,drum_pressure_pa,mode2_raw_suction_transport_j_kg,production_drum_liquid_transport_j_kg,production_drum_liquid_internal_energy_j_kg,production_drum_liquid_density_kg_m3,recirculation_flow_kg_s,pump_flow_kg_s,actual_candidate_observed_energy_rate_w"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            F(row.DrumTemperatureKelvins),
            F(row.DrumPressurePascals),
            F(row.Mode2RawSuctionTransportJoulesPerKilogram),
            F(row.ProductionDrumLiquidTransportJoulesPerKilogram),
            F(row.ProductionDrumLiquidInternalEnergyJoulesPerKilogram),
            F(row.ProductionDrumLiquidDensityKilogramsPerCubicMetre),
            F(row.RecirculationFlowKilogramsPerSecond),
            F(row.PumpFlowKilogramsPerSecond),
            F(row.ActualCandidateObservedEnergyRateWatts))));
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "04-reference-counterfactual-input.csv"),
            lines,
            Utf8WithoutBom);
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1");

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
            "1",
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled Diagnostic 3 REV1 runner.");
        }
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

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln.");
    }

    private static double D(string value)
        => double.Parse(value, CultureInfo.InvariantCulture);

    private static string F(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Optional(double? value)
        => value.HasValue ? F(value.Value) : "null";

    private sealed record BalanceRow(
        string Case,
        string EnergyTransportMode,
        double RawMassKilograms,
        double FrozenStep1MassKilograms,
        double RuntimeStep1MassKilograms,
        double CheckpointMassDeltaKilograms,
        double ObservedMassRateKilogramsPerSecond,
        double PumpFlowKilogramsPerSecond,
        double RequestedRecirculationFlowKilogramsPerSecond,
        double RecirculationFlowKilogramsPerSecond,
        bool RecirculationInventoryLimited,
        double PredictedMassRateKilogramsPerSecond,
        double PredictedMassResidualKilogramsPerSecond,
        double RawEnergyJoules,
        double FrozenStep1EnergyJoules,
        double RuntimeStep1EnergyJoules,
        double CheckpointEnergyDeltaJoules,
        double ObservedEnergyRateWatts,
        double RawSuctionAdvectedSpecificEnergyJoulesPerKilogram,
        double RecirculationAdvectedSpecificEnergyJoulesPerKilogram,
        double SuctionMinusRecirculationSpecificEnergyJoulesPerKilogram,
        double RecirculationEnergyRateWatts,
        double PredictedEnergyRateWatts,
        double PredictedEnergyResidualWatts,
        string Step1Phase,
        double? Step1Quality);

    private sealed record ResolverPathRow(
        string Checkpoint,
        double MassKilograms,
        double EnergyJoules,
        double SpecificVolumeCubicMetresPerKilogram,
        double SpecificEnergyJoulesPerKilogram,
        bool Resolved,
        string ResolvedPhase,
        double? ResolvedQuality,
        string ResolutionPath,
        string EvidenceSemantics);

    private sealed record ForwardTransportRow(
        double FrozenRawDrumTemperatureKelvins,
        double DrumTemperatureKelvins,
        double DrumTemperatureDeltaKelvins,
        double FrozenRawDrumPressurePascals,
        double DrumPressurePascals,
        double DrumPressureDeltaPascals,
        string DrumPhase,
        string EnergyTransportMode,
        double Mode1SaturationPressurePascals,
        double Mode2SaturationPressurePascals,
        double Mode1LiquidDensityKilogramsPerCubicMetre,
        double Mode2LiquidDensityKilogramsPerCubicMetre,
        double Mode1VaporDensityKilogramsPerCubicMetre,
        double Mode2VaporDensityKilogramsPerCubicMetre,
        double Mode1LiquidInternalEnergyJoulesPerKilogram,
        double Mode2LiquidInternalEnergyJoulesPerKilogram,
        double Mode1VaporInternalEnergyJoulesPerKilogram,
        double Mode2VaporInternalEnergyJoulesPerKilogram,
        bool PressureBitsEqual,
        bool LiquidDensityBitsEqual,
        bool VaporDensityBitsEqual,
        bool LiquidInternalEnergyBitsEqual,
        bool VaporInternalEnergyBitsEqual,
        bool ForwardPropertiesAllBitsEqual,
        double ProductionForwardLiquidDensityKilogramsPerCubicMetre,
        double DrumLiquidSpecificInternalEnergyJoulesPerKilogram,
        double DrumLiquidSpecificFlowWorkJoulesPerKilogram,
        double CalculatedPressureOverDensityJoulesPerKilogram,
        double FlowWorkIdentityResidualJoulesPerKilogram,
        double DrumLiquidSpecificEnthalpyJoulesPerKilogram,
        double CalculatedInternalEnergyPlusFlowWorkJoulesPerKilogram,
        double EnthalpyIdentityResidualJoulesPerKilogram,
        double DrumLiquidAdvectedSpecificEnergyJoulesPerKilogram,
        double SelectedTransportIdentityResidualJoulesPerKilogram,
        double RecirculationFlowKilogramsPerSecond,
        double DrumLiquidEnergyRateWatts,
        double CalculatedLiquidEnergyRateWatts,
        double LiquidEnergyRateIdentityResidualWatts);

    private sealed record CounterfactualInputRow(
        double DrumTemperatureKelvins,
        double DrumPressurePascals,
        double Mode2RawSuctionTransportJoulesPerKilogram,
        double ProductionDrumLiquidTransportJoulesPerKilogram,
        double ProductionDrumLiquidInternalEnergyJoulesPerKilogram,
        double ProductionDrumLiquidDensityKilogramsPerCubicMetre,
        double RecirculationFlowKilogramsPerSecond,
        double PumpFlowKilogramsPerSecond,
        double ActualCandidateObservedEnergyRateWatts);
}
