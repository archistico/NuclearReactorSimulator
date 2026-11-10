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
/// Observation only: repaired mode 2 versus canonical mode 1. Storage rates are equilibrium
/// residuals, not independent conservation errors. No seed, controller or acceptance change.
/// </summary>
public sealed class M10FinalVr2R3PostRepairDynamicEquilibriumResidualDiagnostic1Tests
{
    private const string OptIn = "NRS_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_DIAGNOSTIC1";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10);
    private static readonly int[] Checkpoints = [0, 1, 2, 5, 10, 20, 50, 100];
    private static readonly UTF8Encoding Utf8 = new(false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3PostRepairDynamicEquilibriumResidualDiagnostic1")]
    public void RepairedOperatingPoint_CapturesSeedAndRunningResidualsWithoutRetuning()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(OptIn));
        var output = Path.Combine(FindRepositoryRoot(), "artifacts", "r3-dyn-eq-d1");
        Directory.CreateDirectory(output);
        var nodes = new Csv("mode,checkpoint,node,mass_kg,energy_j,pressure_pa,temperature_k,phase,quality,mass_storage_residual_kg_s,energy_storage_residual_w,pressure_step_delta_pa");
        var hydraulics = new Csv("mode,checkpoint,pump_kg_s,channel_kg_s,return_kg_s,recirculation_kg_s,feedwater_kg_s,steam_export_kg_s,pump_boost_pa,pump_net_head_pa,channel_head_pa,return_head_pa,pump_speed_fraction,drum_mass_kg,drum_energy_j,drum_pressure_pa,drum_level");
        var controllers = new Csv("mode,checkpoint,setpoint_rpm,measurement_rpm,error_rpm,integral,output_percent,rotor_initial_rpm,rotor_final_rpm,turbine_torque_nm,effective_load_torque_nm,passive_loss_torque_nm,net_torque_nm,shaft_power_w,electrical_power_w,generator_load_torque_nm,requested_power_w,stage_command_kg_s,stage_transfer_kg_s,control_valve_fraction");
        var balances = new Csv("mode,checkpoint,suction_observed_mass_rate_kg_s,suction_predicted_mass_rate_kg_s,suction_mass_identity_residual_kg_s,suction_observed_energy_rate_w,suction_predicted_energy_rate_w,suction_energy_identity_residual_w,network_mass_closure_kg,network_energy_closure_j,network_mass_rate_residual_kg_s,network_power_residual_w");
        var health = new Csv("mode,step,trip,breaker_closed,rollbacks,finite,electrical_mwe,primary_pump_kg_s,drum_level,governor_percent,in_frozen_envelope");

        foreach (var candidate in new[] { false, true })
        {
            var label = candidate ? "candidate-mode2" : "mode1";
            var raw = ReadRawNodes(candidate);
            foreach (var item in raw.OrderBy(static x => x.Key, StringComparer.Ordinal))
                nodes.Add(label, "raw", item.Key, item.Value.Mass, item.Value.Energy,
                    item.Value.Pressure, item.Value.Temperature, item.Value.Phase, item.Value.Quality, null, null, null);

            // This reconstruction is copied from the accepted preconditioning diagnostic.
            // Exact equality against the actual factory prevents diagnostic configuration drift.
            var seed1 = CreateCheckpointEngine(candidate, 1);
            Capture(label, "seed-step1", seed1, raw, nodes, hydraulics, controllers, balances);
            CaptureInitialHealth(label, "seed-step1", seed1, health);
            var reconstructed = CreateCheckpointEngine(candidate, 2);
            var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(candidate
                ? DesktopSustainedGenerationInitialConditionFactory.CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep)
                : DesktopSustainedGenerationInitialConditionFactory.CreatePostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep));
            AssertReconstructedStep2MatchesFactory(reconstructed, engine);
            Assert.Equal(RuntimeStep, engine.FixedDeltaTime);
            Capture(label, "post-seed", engine, ReadNodes(seed1), nodes, hydraulics, controllers, balances);
            CaptureInitialHealth(label, "post-seed", engine, health);

            var telemetry = new DesktopHydraulicProductionTelemetryProbe();
            for (var step = 1; step <= 100; step++)
            {
                var previous = ReadNodes(engine);
                var view = engine.Step(ControlRoomRunState.Running);
                telemetry.Observe(engine);
                var full = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
                var generator = Assert.Single(full.IntegratedCycle.Generators);
                var pump = full.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
                var drum = Assert.Single(full.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
                var governor = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control").Output;
                var electrical = generator.ElectricalOutputPower.Megawatts;
                var finite = ReadNodes(engine).Values.All(static n => n.Finite)
                    && new[] { electrical, pump, drum.LiquidLevelFraction.Fraction, governor }.All(double.IsFinite);
                var inEnvelope = electrical >= 4.99 && electrical <= 5.01
                    && pump >= 99.9 && pump <= 100.1
                    && drum.LiquidLevelFraction.Fraction >= 0.49 && drum.LiquidLevelFraction.Fraction <= 0.51
                    && governor >= 29.27 && governor <= 29.30 && !view.AnyTripActive && generator.BreakerFinallyClosed;
                health.Add(label, step, view.AnyTripActive, generator.BreakerFinallyClosed,
                    telemetry.Snapshot().RollbackSteps, finite, electrical, pump, drum.LiquidLevelFraction.Fraction, governor, inEnvelope);
                if (Checkpoints.Contains(step))
                    Capture(label, step.ToString(CultureInfo.InvariantCulture), engine, previous, nodes, hydraulics, controllers, balances);
                Assert.True(finite);
                Assert.False(view.AnyTripActive);
                Assert.True(generator.BreakerFinallyClosed);
                Assert.Equal(0L, telemetry.Snapshot().RollbackSteps);
            }
        }

        Assert.Equal(240, nodes.Count);
        Assert.Equal(18, hydraulics.Count);
        Assert.Equal(18, controllers.Count);
        Assert.Equal(18, balances.Count);
        Assert.Equal(204, health.Count);
        nodes.Write(output, "01-node-residuals.csv");
        hydraulics.Write(output, "02-hydraulics-drum.csv");
        controllers.Write(output, "03-controller-rotor.csv");
        balances.Write(output, "04-conservation.csv");
        health.Write(output, "05-health.csv");
    }

    private static void CaptureInitialHealth(string label, string checkpoint, IntegratedAutomaticOperationRuntimeEngine engine, Csv health)
    {
        var view = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var full = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var generator = Assert.Single(full.IntegratedCycle.Generators);
        var pump = full.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
        var drum = Assert.Single(full.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums).LiquidLevelFraction.Fraction;
        var governor = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control").Output;
        var electrical = generator.ElectricalOutputPower.Megawatts;
        var telemetry = new DesktopHydraulicProductionTelemetryProbe();
        telemetry.Observe(engine);
        var finite = ReadNodes(engine).Values.All(static n => n.Finite)
            && new[] { electrical, pump, drum, governor }.All(double.IsFinite);
        health.Add(label, checkpoint, view.AnyTripActive, generator.BreakerFinallyClosed, telemetry.Snapshot().RollbackSteps,
            finite, electrical, pump, drum, governor, electrical >= 4.99 && electrical <= 5.01 && pump >= 99.9 && pump <= 100.1
                && drum >= 0.49 && drum <= 0.51 && governor >= 29.27 && governor <= 29.30 && !view.AnyTripActive && generator.BreakerFinallyClosed);
        Assert.True(finite);
        Assert.False(view.AnyTripActive);
        Assert.True(generator.BreakerFinallyClosed);
        Assert.Equal(0L, telemetry.Snapshot().RollbackSteps);
    }

    private static void Capture(string label, string checkpoint, IntegratedAutomaticOperationRuntimeEngine engine,
        IReadOnlyDictionary<string, Node> previous, Csv nodes, Csv hydraulics, Csv controllers, Csv balances)
    {
        var control = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var full = control.FullPlant;
        var primary = full.IntegratedCycle.PrimaryCircuit;
        var plant = full.CandidatePlant;
        var current = ReadNodes(engine);
        Assert.Equal(previous.Keys.OrderBy(static x => x, StringComparer.Ordinal), current.Keys.OrderBy(static x => x, StringComparer.Ordinal));
        foreach (var item in current.OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            var n = item.Value;
            var before = previous[item.Key];
            nodes.Add(label, checkpoint, item.Key, n.Mass, n.Energy, n.Pressure, n.Temperature, n.Phase, n.Quality,
                (n.Mass - before.Mass) / RuntimeStep.TotalSeconds,
                (n.Energy - before.Energy) / RuntimeStep.TotalSeconds, n.Pressure - before.Pressure);
        }
        var circulation = primary.MainCirculation;
        var pump = circulation.GetLoop("loop").GetPump("pump");
        var drum = Assert.Single(primary.SteamDrums.Drums);
        double P(string id) => plant.GetFluidNode(id).Pressure.Pascals;
        hydraulics.Add(label, checkpoint, circulation.TotalPumpMassFlowRate.KilogramsPerSecond,
            circulation.TotalChannelMassFlowRate.KilogramsPerSecond, circulation.TotalReturnMassFlowRate.KilogramsPerSecond,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond,
            primary.TotalSteamExportMassFlowRate.KilogramsPerSecond, pump.ActivePressureBoost.Pascals,
            P("suction") + pump.ActivePressureBoost.Pascals - P("pressure"), P("pressure") - P("outlet"), P("outlet") - P("drum"),
            pump.EffectiveSpeed.Fraction, drum.InventoryMass.Kilograms, drum.InventoryInternalEnergy.Joules, drum.Pressure.Pascals, drum.LiquidLevelFraction.Fraction);

        var speed = control.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var rotor = Assert.Single(full.IntegratedCycle.TurbineExpansion.Rotors);
        var stage = Assert.Single(full.IntegratedCycle.TurbineExpansion.StageGroups);
        var generator = Assert.Single(full.IntegratedCycle.Generators);
        controllers.Add(label, checkpoint, speed.Setpoint, speed.Measurement, speed.Error, speed.IntegralTerm, speed.Output,
            rotor.InitialAngularSpeed.RevolutionsPerMinute, rotor.FinalAngularSpeed.RevolutionsPerMinute,
            rotor.TurbineTorque.NewtonMetres, rotor.EffectiveExternalLoadTorque.NewtonMetres,
            rotor.PassiveMechanicalLossTorque.NewtonMetres, rotor.NetTorque.NewtonMetres, rotor.ShaftPower.Watts,
            generator.ElectricalOutputPower.Watts, generator.EffectiveElectromagneticTorque.NewtonMetres, generator.RequestedElectricalPower.Watts,
            stage.CommandedMassFlowRate.KilogramsPerSecond, stage.TotalTransferredMassFlowRate.KilogramsPerSecond,
            plant.GetValve("control").Position.Fraction);

        var old = previous["suction"];
        var now = current["suction"];
        var density = Density.FromKilogramsPerCubicMetre(old.Mass / plant.Definition.GetFluidNode("suction").Volume.CubicMetres);
        var transport = FluidEnergyTransport.ResolveSelectedSpecificEnergy(plant.Definition.GetPump("pump").Pipe.EnergyTransportMode,
            SpecificEnergy.FromJoulesPerKilogram(old.Energy / old.Mass), Pressure.FromPascals(old.Pressure), density);
        var predictedMass = drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond - pump.MassFlowRate.KilogramsPerSecond;
        var predictedEnergy = drum.LiquidEnergyRate.Watts - transport.JoulesPerKilogram * pump.MassFlowRate.KilogramsPerSecond;
        var observedMass = (now.Mass - old.Mass) / RuntimeStep.TotalSeconds;
        var observedEnergy = (now.Energy - old.Energy) / RuntimeStep.TotalSeconds;
        var audit = full.IntegratedCycle.ThermofluidAudit;
        balances.Add(label, checkpoint, observedMass, predictedMass, predictedMass - observedMass,
            observedEnergy, predictedEnergy, predictedEnergy - observedEnergy,
            audit.MassClosureResidualKilograms, audit.EnergyClosureResidualJoules,
            audit.BalanceMassRateResidualKilogramsPerSecond, audit.BalancePowerResidualWatts);
    }

    private static Dictionary<string, Node> ReadNodes(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes.ToDictionary(
            static n => n.Id, static n => new Node(n.Mass.Kilograms, n.InternalEnergy.Joules, n.Pressure.Pascals,
                n.Temperature.Kelvins, n.Phase.ToString(), n.VaporQuality?.Fraction), StringComparer.Ordinal);

    private static Dictionary<string, Node> ReadRawNodes(bool candidate)
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary",
            "M10FinalVR2_R3_SeedIntegration_TwoSeedStepPreconditioningDivergenceDiagnostic2_ReturnedArtifacts", "01-raw-checkpoint-node-comparison.csv");
        var lines = File.ReadAllLines(path);
        var header = lines[0].Split(',');
        var prefix = candidate ? "candidate" : "mode1";
        var result = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split(',');
            Assert.Equal(header.Length, fields.Length);
            string S(string key) => fields[Array.IndexOf(header, key)];
            double D(string key) => double.Parse(S(prefix + "_" + key), CultureInfo.InvariantCulture);
            var q = S(prefix + "_quality");
            result.Add(S("node"), new Node(D("mass_kg"), D("energy_j"), D("pressure_pa"), D("temperature_k"),
                S(prefix + "_phase"), q == "null" ? null : double.Parse(q, CultureInfo.InvariantCulture)));
        }
        Assert.Equal(12, result.Count);
        return result;
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
        Assert.Equal(Assert.Single(left.FullPlant.IntegratedCycle.TurbineExpansion.Rotors),
            Assert.Single(right.FullPlant.IntegratedCycle.TurbineExpansion.Rotors));
        Assert.Equal(Assert.Single(left.FullPlant.IntegratedCycle.Generators),
            Assert.Single(right.FullPlant.IntegratedCycle.Generators));
        Assert.Equal(left.FullPlant.CandidatePlant.GetValve("control"), right.FullPlant.CandidatePlant.GetValve("control"));
    }



    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed record Node(double Mass, double Energy, double Pressure, double Temperature, string Phase, double? Quality)
    {
        public bool Finite => new[] { Mass, Energy, Pressure, Temperature, Quality ?? 0 }.All(double.IsFinite);
    }

    private sealed class Csv(string header)
    {
        private readonly List<string> _lines = [header];
        public int Count => _lines.Count - 1;
        public void Add(params object?[] fields)
        {
            Assert.Equal(header.Split(',').Length, fields.Length);
            Assert.All(fields.OfType<double>(), static value => Assert.True(double.IsFinite(value)));
            _lines.Add(string.Join(",", fields.Select(static value => value switch
            {
                null => "null",
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            })));
        }
        public void Write(string directory, string name) => File.WriteAllLines(Path.Combine(directory, name), _lines, Utf8);
    }
}
