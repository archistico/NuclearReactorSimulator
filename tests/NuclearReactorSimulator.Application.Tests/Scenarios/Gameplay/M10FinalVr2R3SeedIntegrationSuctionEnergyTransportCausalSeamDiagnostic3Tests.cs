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
/// Test-only causal-seam diagnostic authorized by returned Two-Seed-Step Diagnostic 2 evidence.
/// It does not retune or repair the candidate. It reconstructs only the first deterministic seed step,
/// reads the production steam-drum recirculation and MCP diagnostics, and closes the suction-node
/// conserved mass/energy balance against the frozen raw and step-1 evidence. It also records the
/// mode-2 inverse resolver path before and after the first step and confirms that the public saturation
/// property provider remains identical across closure modes.
/// </summary>
public sealed class M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3")]
    public void SeedStep1_SuctionEnergyBalance_AttributesPhaseFlipToForwardInverseTransportSeam()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var frozenRaw = ReadFrozenNodeRows("01-raw-checkpoint-node-comparison.csv");
        var frozenStep1 = ReadFrozenNodeRows("02-seed-step1-node-comparison.csv");
        var mode1 = CreateCheckpointEngine(referenceConsistentCandidate: false);
        var candidate = CreateCheckpointEngine(referenceConsistentCandidate: true);

        var mode1Row = CaptureBalance("mode1", mode1, frozenRaw["suction"], frozenStep1["suction"], candidateSide: false);
        var candidateRow = CaptureBalance("candidate-mode2", candidate, frozenRaw["suction"], frozenStep1["suction"], candidateSide: true);
        WriteBalanceRows([mode1Row, candidateRow]);

        var pathRows = CaptureMode2ResolverPaths(frozenRaw["suction"], frozenStep1["suction"], candidate);
        WriteResolverPathRows(pathRows);

        var forwardRow = CaptureForwardSaturationProvider(candidate, frozenRaw["drum"]);
        WriteForwardProviderRows([forwardRow]);

        Assert.Equal(0d, mode1Row.ObservedMassRateKilogramsPerSecond, 9);
        Assert.Equal(0d, candidateRow.ObservedMassRateKilogramsPerSecond, 9);
        Assert.InRange(Math.Abs(mode1Row.ObservedEnergyRateWatts), 0d, 0.01d);
        Assert.InRange(candidateRow.ObservedEnergyRateWatts, -5_217_000d, -5_215_000d);
        Assert.InRange(Math.Abs(candidateRow.PredictedEnergyResidualWatts), 0d, 0.001d);
        Assert.InRange(Math.Abs(candidateRow.PredictedMassResidualKilogramsPerSecond), 0d, 1e-9d);
        Assert.InRange(candidateRow.SuctionMinusRecirculationSpecificEnergyJoulesPerKilogram, 52_100d, 52_250d);
        Assert.Equal(FluidPhase.SubcooledLiquid.ToString(), pathRows[0].ResolvedPhase);
        Assert.Equal(FluidPhase.SaturatedMixture.ToString(), pathRows[1].ResolvedPhase);
        Assert.True(forwardRow.BitwiseIdenticalAcrossModes);
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
        var expectedStepMass = D(frozenStep1[$"{prefix}_mass_kg"]);
        var expectedStepEnergy = D(frozenStep1[$"{prefix}_energy_j"]);

        var control = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = control.FullPlant;
        var primary = fullPlant.IntegratedCycle.PrimaryCircuit;
        var post = fullPlant.CandidatePlant.GetFluidNode("suction");
        Assert.Equal(expectedStepMass, post.Mass.Kilograms);
        Assert.Equal(expectedStepEnergy, post.InternalEnergy.Joules);

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

        Assert.Equal(drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, drum.RequestedLiquidRecirculationMassFlowRate.KilogramsPerSecond);
        Assert.False(drum.LiquidRecirculationInventoryLimited);
        Assert.Equal(pumpDefinition.Pipe.EnergyTransportMode, drum.EnergyTransportMode);

        var predictedMassRate = drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond - pump.MassFlowRate.KilogramsPerSecond;
        var predictedEnergyRate = drum.LiquidEnergyRate.Watts
            - (rawSelectedSpecificEnergy.JoulesPerKilogram * pump.MassFlowRate.KilogramsPerSecond);
        var observedMassRate = (post.Mass.Kilograms - rawMass) / RuntimeStep.TotalSeconds;
        var observedEnergyRate = (post.InternalEnergy.Joules - rawEnergy) / RuntimeStep.TotalSeconds;

        return new BalanceRow(
            label,
            pumpDefinition.Pipe.EnergyTransportMode.ToString(),
            rawMass,
            post.Mass.Kilograms,
            observedMassRate,
            pump.MassFlowRate.KilogramsPerSecond,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            predictedMassRate,
            predictedMassRate - observedMassRate,
            rawEnergy,
            post.InternalEnergy.Joules,
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
        var volume = candidate.CurrentState.PlantState.PlantState.Definition.GetFluidNode("suction").Volume.CubicMetres;
        var resolver = new ReferenceConsistentTabulatedInverseResolver();
        return new[]
        {
            ResolvePath("raw", raw, "candidate", volume, resolver),
            ResolvePath("seed-step1", step1, "candidate", volume, resolver),
        };
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
        Assert.True(resolver.TryResolveWithPath(specificVolume, specificEnergy, out var resolved, out var path));
        return new ResolverPathRow(
            checkpoint,
            mass,
            energy,
            specificVolume,
            specificEnergy,
            resolved.Phase.ToString(),
            resolved.VaporQuality,
            path.ToString());
    }

    private static ForwardProviderRow CaptureForwardSaturationProvider(
        IntegratedAutomaticOperationRuntimeEngine candidate,
        IReadOnlyDictionary<string, string> rawDrum)
    {
        var temperature = Temperature.FromKelvins(D(rawDrum["candidate_temperature_k"]));
        var mode1 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var mode2 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var left = mode1.GetSaturationProperties(temperature);
        var right = mode2.GetSaturationProperties(temperature);
        var drum = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.GetDrum("drum-a");
        var identical = left.Pressure.Pascals.Equals(right.Pressure.Pascals)
            && left.SaturatedLiquidDensity.KilogramsPerCubicMetre.Equals(right.SaturatedLiquidDensity.KilogramsPerCubicMetre)
            && left.SaturatedVaporDensity.KilogramsPerCubicMetre.Equals(right.SaturatedVaporDensity.KilogramsPerCubicMetre)
            && left.SaturatedLiquidInternalEnergy.JoulesPerKilogram.Equals(right.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            && left.SaturatedVaporInternalEnergy.JoulesPerKilogram.Equals(right.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        Assert.Equal(right.SaturatedLiquidInternalEnergy.JoulesPerKilogram, drum.LiquidSpecificInternalEnergy.JoulesPerKilogram);

        return new ForwardProviderRow(
            temperature.Kelvins,
            left.Pressure.Pascals,
            right.Pressure.Pascals,
            left.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            right.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            left.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            right.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            drum.LiquidSpecificInternalEnergy.JoulesPerKilogram,
            drum.LiquidAdvectedSpecificEnergy.JoulesPerKilogram,
            identical);
    }

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
            "case,energy_transport_mode,raw_mass_kg,step1_mass_kg,observed_mass_rate_kg_s,pump_flow_kg_s,recirculation_flow_kg_s,predicted_mass_rate_kg_s,predicted_mass_residual_kg_s,raw_energy_j,step1_energy_j,observed_energy_rate_w,raw_suction_advected_specific_energy_j_kg,recirculation_advected_specific_energy_j_kg,suction_minus_recirculation_specific_energy_j_kg,recirculation_energy_rate_w,predicted_energy_rate_w,predicted_energy_residual_w,step1_phase,step1_quality"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.Case,
            row.EnergyTransportMode,
            F(row.RawMassKilograms),
            F(row.Step1MassKilograms),
            F(row.ObservedMassRateKilogramsPerSecond),
            F(row.PumpFlowKilogramsPerSecond),
            F(row.RecirculationFlowKilogramsPerSecond),
            F(row.PredictedMassRateKilogramsPerSecond),
            F(row.PredictedMassResidualKilogramsPerSecond),
            F(row.RawEnergyJoules),
            F(row.Step1EnergyJoules),
            F(row.ObservedEnergyRateWatts),
            F(row.RawSuctionAdvectedSpecificEnergyJoulesPerKilogram),
            F(row.RecirculationAdvectedSpecificEnergyJoulesPerKilogram),
            F(row.SuctionMinusRecirculationSpecificEnergyJoulesPerKilogram),
            F(row.RecirculationEnergyRateWatts),
            F(row.PredictedEnergyRateWatts),
            F(row.PredictedEnergyResidualWatts),
            row.Step1Phase,
            Optional(row.Step1Quality))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "01-step1-suction-energy-balance.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteResolverPathRows(IReadOnlyCollection<ResolverPathRow> rows)
    {
        var lines = new List<string>
        {
            "checkpoint,mass_kg,energy_j,specific_volume_m3_kg,specific_energy_j_kg,resolved_phase,resolved_quality,resolution_path"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.Checkpoint,
            F(row.MassKilograms),
            F(row.EnergyJoules),
            F(row.SpecificVolumeCubicMetresPerKilogram),
            F(row.SpecificEnergyJoulesPerKilogram),
            row.ResolvedPhase,
            Optional(row.ResolvedQuality),
            row.ResolutionPath)));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "02-mode2-suction-inverse-path.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteForwardProviderRows(IReadOnlyCollection<ForwardProviderRow> rows)
    {
        var lines = new List<string>
        {
            "temperature_k,mode1_saturation_pressure_pa,mode2_saturation_pressure_pa,mode1_liquid_density_kg_m3,mode2_liquid_density_kg_m3,mode1_liquid_internal_energy_j_kg,mode2_liquid_internal_energy_j_kg,drum_liquid_internal_energy_j_kg,drum_liquid_advected_specific_energy_j_kg,bitwise_identical_across_modes"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            F(row.TemperatureKelvins),
            F(row.Mode1SaturationPressurePascals),
            F(row.Mode2SaturationPressurePascals),
            F(row.Mode1LiquidDensityKilogramsPerCubicMetre),
            F(row.Mode2LiquidDensityKilogramsPerCubicMetre),
            F(row.Mode1LiquidInternalEnergyJoulesPerKilogram),
            F(row.Mode2LiquidInternalEnergyJoulesPerKilogram),
            F(row.DrumLiquidInternalEnergyJoulesPerKilogram),
            F(row.DrumLiquidAdvectedSpecificEnergyJoulesPerKilogram),
            row.BitwiseIdenticalAcrossModes ? "true" : "false")));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "03-forward-saturation-provider-seam.csv"), lines, Utf8WithoutBom);
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3");

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 only from the controlled Diagnostic 3 runner.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln.");
    }

    private static double D(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Optional(double? value) => value.HasValue ? F(value.Value) : "null";

    private sealed record BalanceRow(
        string Case,
        string EnergyTransportMode,
        double RawMassKilograms,
        double Step1MassKilograms,
        double ObservedMassRateKilogramsPerSecond,
        double PumpFlowKilogramsPerSecond,
        double RecirculationFlowKilogramsPerSecond,
        double PredictedMassRateKilogramsPerSecond,
        double PredictedMassResidualKilogramsPerSecond,
        double RawEnergyJoules,
        double Step1EnergyJoules,
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
        string ResolvedPhase,
        double? ResolvedQuality,
        string ResolutionPath);

    private sealed record ForwardProviderRow(
        double TemperatureKelvins,
        double Mode1SaturationPressurePascals,
        double Mode2SaturationPressurePascals,
        double Mode1LiquidDensityKilogramsPerCubicMetre,
        double Mode2LiquidDensityKilogramsPerCubicMetre,
        double Mode1LiquidInternalEnergyJoulesPerKilogram,
        double Mode2LiquidInternalEnergyJoulesPerKilogram,
        double DrumLiquidInternalEnergyJoulesPerKilogram,
        double DrumLiquidAdvectedSpecificEnergyJoulesPerKilogram,
        bool BitwiseIdenticalAcrossModes);
}
