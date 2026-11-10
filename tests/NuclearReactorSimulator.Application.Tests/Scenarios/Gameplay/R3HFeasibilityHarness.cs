using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

internal sealed class R3HFeasibilityHarness
{
    internal static readonly TimeSpan StepTime = TimeSpan.FromMilliseconds(10);
    internal static readonly string Root = FindRoot();
    internal static readonly string Output = Path.Combine(Root, "artifacts", "r3-h-feasibility1");
    internal static readonly int[] Checkpoints = [1, 2, 5, 10, 20, 50, 100];
    private readonly OperationalFluidNodeSeed.ConservedInventory[] _baseline = BaselineSeeds();
    private readonly double[] _volumes;
    private readonly double[] _scales;
    private readonly ReferenceConsistentTabulatedInverseResolver _resolver = new();
    internal readonly IntegratedAutomaticOperationRuntimeEngine Factory;

    internal R3HFeasibilityHarness()
    {
        Factory = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine(StepTime));
        _volumes = _baseline.Select(seed => Factory.CurrentState.PlantState.PlantState.Definition.GetFluidNode(seed.NodeId).Volume.CubicMetres).ToArray();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "eng", "r3-equilibrium-reconstruction-planning1-contract.json")));
        var plan = document.RootElement;
        Assert.Equal(_baseline.Select(static n => n.NodeId), plan.GetProperty("search").GetProperty("variable_order").EnumerateArray().Select(static n => n.GetString()));
        _scales = plan.GetProperty("residual_scales").EnumerateArray().SelectMany(static n => new[]
        {
            double.Parse(n.GetProperty("mass_scale_kg_s").GetString()!, CultureInfo.InvariantCulture),
            double.Parse(n.GetProperty("energy_scale_w").GetString()!, CultureInfo.InvariantCulture)
        }).Append(double.Parse(plan.GetProperty("rotor_torque_scale_nm").GetString()!, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(25, _scales.Length);
        Assert.All(_scales, static scale => Assert.True(double.IsFinite(scale) && scale > 0));
    }

    internal sealed record Node(string Id, double Mass, double Energy, double Pressure, double Temperature, string Phase, double? Quality);
    internal sealed record Observation(string Checkpoint, Node[] Nodes, double[] ScaledResiduals, bool Healthy, bool InEnvelope,
        double ElectricalMwe, double PumpKgS, double DrumLevel, double Governor, double RotorTorque,
        double MassClosure, double EnergyClosure, string StateJson);
    internal sealed record Evaluation(Node[] Raw, Observation[] Observations, IntegratedAutomaticOperationRuntimeEngine Engine)
    {
        internal double[] Residuals => Observations.SelectMany(static o => o.ScaledResiduals).ToArray();
    }

    internal Evaluation Evaluate(double[] coordinates)
    {
        if (coordinates.Length != 24 || !coordinates.All(static x => double.IsFinite(x) && Math.Abs(x) <= 0.02))
            throw new ArgumentOutOfRangeException(nameof(coordinates));
        var seeds = new OperationalFluidNodeSeed.ConservedInventory[12];
        var raw = new Node[12];
        for (var i = 0; i < seeds.Length; i++)
        {
            var initial = _baseline[i];
            var mass = initial.MassKilograms * (1 + coordinates[2 * i]);
            var energy = initial.InternalEnergyJoules * (1 + coordinates[2 * i + 1]);
            if (!(mass > 0) || !double.IsFinite(energy) || !_resolver.TryResolve(_volumes[i] / mass, energy / mass, out var resolved))
                throw new CandidateRejectedException("C4-UNRESOLVED/" + initial.NodeId);
            var state = resolved.ToFluidThermodynamicState();
            seeds[i] = new(initial.NodeId, mass, energy, state.Pressure.Pascals, state.Temperature.Kelvins, state.Phase, state.VaporQuality?.Fraction);
            raw[i] = new(initial.NodeId, mass, energy, state.Pressure.Pascals, state.Temperature.Kelvins, state.Phase.ToString(), state.VaporQuality?.Fraction);
        }
        var first = CreateSeedEngine(seeds, 1);
        var firstObservation = Observe("seed-step1", first, raw);
        var state1 = first.CurrentState;
        var solver = new IntegratedAutomaticOperationSolver(state1.ReactorPrimaryControlState.Definition,
            state1.TurbineSecondaryControlState.Definition, state1.ProtectionState.Definition, state1.AlarmState.Definition,
            new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain),
            InstrumentSignalSourceCatalog.CreateFullPlantCatalog(state1.PlantDefinition));
        var second = solver.Step(state1, first.PersistentInputs, StepTime);
        var engine = new IntegratedAutomaticOperationRuntimeEngine(solver, second.CandidateState, first.PersistentInputs, second.Snapshot, StepTime);
        var postSeed = Observe("post-seed", engine, firstObservation.Nodes);
        engine.Step(ControlRoomRunState.Running);
        var running = Observe("1", engine, postSeed.Nodes);
        return new(raw, [firstObservation, postSeed, running], engine);
    }

    internal R3HFeasibilitySearch.Sample Sample(double[] coordinates)
    {
        try
        {
            var evaluation = Evaluate(coordinates);
            if (evaluation.Observations.Any(static o => !o.Healthy)) return R3HFeasibilitySearch.Sample.Invalid("HEALTH-SENTINEL");
            return new(evaluation.Residuals, "VALID", evaluation.Raw.Select(static n => n.Phase)
                .Concat(evaluation.Observations.SelectMany(static o => o.Nodes.Select(static n => n.Phase))).ToArray());
        }
        catch (CandidateRejectedException ex) { return R3HFeasibilitySearch.Sample.Invalid(ex.Message); }
        catch (WaterSteamStateOutOfRangeException ex) { return R3HFeasibilitySearch.Sample.Invalid("THERMODYNAMIC-DOMAIN/" + ex.Message); }
        catch (ArgumentOutOfRangeException ex) { return R3HFeasibilitySearch.Sample.Invalid("QUANTITY-DOMAIN/" + ex.ParamName); }
        catch (ArithmeticException ex) { return R3HFeasibilitySearch.Sample.Invalid("NUMERICAL-REJECTION/" + ex.Message); }
    }

    internal Observation Observe(string checkpoint, IntegratedAutomaticOperationRuntimeEngine engine, IReadOnlyList<Node> previous)
    {
        var control = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var full = control.FullPlant;
        var plant = full.CandidatePlant;
        var nodes = _baseline.Select(seed =>
        {
            var n = plant.GetFluidNode(seed.NodeId);
            return new Node(n.Id, n.Mass.Kilograms, n.InternalEnergy.Joules, n.Pressure.Pascals, n.Temperature.Kelvins, n.Phase.ToString(), n.VaporQuality?.Fraction);
        }).ToArray();
        var residuals = new double[25];
        for (var i = 0; i < nodes.Length; i++)
        {
            Assert.Equal(nodes[i].Id, previous[i].Id);
            residuals[2 * i] = (nodes[i].Mass - previous[i].Mass) / StepTime.TotalSeconds / _scales[2 * i];
            residuals[2 * i + 1] = (nodes[i].Energy - previous[i].Energy) / StepTime.TotalSeconds / _scales[2 * i + 1];
        }
        var rotor = Assert.Single(full.IntegratedCycle.TurbineExpansion.Rotors);
        residuals[24] = rotor.NetTorque.NewtonMetres / _scales[24];
        var generator = Assert.Single(full.IntegratedCycle.Generators);
        var drum = Assert.Single(full.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
        var pump = full.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
        var governor = control.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control").Output;
        var telemetry = new DesktopHydraulicProductionTelemetryProbe();
        telemetry.Observe(engine);
        var view = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var finite = residuals.All(double.IsFinite) && nodes.All(static n => new[] { n.Mass, n.Energy, n.Pressure, n.Temperature, n.Quality ?? 0 }.All(double.IsFinite));
        var healthy = finite && !view.AnyTripActive && generator.BreakerFinallyClosed && telemetry.Snapshot().RollbackSteps == 0
            && Math.Abs(full.HeatBalance.MassClosureResidualKilograms) <= 1e-6
            && Math.Abs(full.HeatBalance.FullEnergyPathClosureResidualJoules) <= 1e-2;
        var electrical = generator.ElectricalOutputPower.Megawatts;
        var level = drum.LiquidLevelFraction.Fraction;
        var envelope = healthy && electrical >= 4.99 && electrical <= 5.01 && pump >= 99.9 && pump <= 100.1
            && level >= 0.49 && level <= 0.51 && governor >= 29.27 && governor <= 29.30;
        return new(checkpoint, nodes, residuals, healthy, envelope, electrical, pump, level, governor, rotor.NetTorque.NewtonMetres,
            full.HeatBalance.MassClosureResidualKilograms, full.HeatBalance.FullEnergyPathClosureResidualJoules, StateJson(engine));
    }

    internal string BaselineIdentity()
    {
        var evaluation = Evaluate(new double[24]);
        // The direct second preconditioning step must match the unmodified factory exactly.
        var direct = CreateSeedEngine(BaselineSeeds(), 2);
        Assert.Equal(StateJson(Factory), StateJson(direct));
        var solverEvaluation = Evaluate(new double[24]);
        Assert.Equal(evaluation.Observations[1].StateJson, StateJson(Factory));
        Assert.Equal(evaluation.Observations[1].StateJson, solverEvaluation.Observations[1].StateJson);
        Factory.Step(ControlRoomRunState.Running);
        Assert.Equal(evaluation.Observations[2].StateJson, StateJson(Factory));
        Assert.Equal(75, evaluation.Residuals.Length);
        Assert.All(evaluation.Observations, static o => Assert.True(o.Healthy));
        return Hash(evaluation.Observations[1].StateJson);
    }

    internal static string StateJson(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var state = engine.CurrentState;
        var snapshot = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var plant = state.PlantState.PlantState;
        // Project state owners explicitly; definitions are deliberately not serialized.
        return JsonSerializer.Serialize(new
        {
            Fluids = plant.FluidNodes.Select(static n => new { n.Id, Mass = n.Mass.Kilograms, Energy = n.InternalEnergy.Joules, Pressure = n.Pressure.Pascals, Temperature = n.Temperature.Kelvins, n.Phase, Quality = n.VaporQuality?.Fraction }),
            Thermal = plant.ThermalBodies.Select(static n => new { n.Id, Energy = n.StoredThermalEnergy.Joules, Temperature = n.Temperature.Kelvins }),
            plant.Valves, plant.Pumps, plant.HeatSources,
            state.ReactorPrimaryControlState.PointKinetics,
            state.ReactorPrimaryControlState.IodineXenon,
            state.ReactorPrimaryControlState.ControlRods,
            PrimaryMemory = state.ReactorPrimaryControlState.ControlAndActuator.Controllers.Controllers,
            PrimaryActuators = state.ReactorPrimaryControlState.ControlAndActuator.Actuators.Actuators,
            SecondaryMemory = state.TurbineSecondaryControlState.ControlAndActuator.Controllers.Controllers,
            SecondaryActuators = state.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators,
            PrimaryControllers = snapshot.ReactorPrimary.ControlAndActuator.Controllers.Diagnostics,
            SecondaryControllers = snapshot.TurbineSecondary.ControlAndActuator.Controllers.Diagnostics,
            Rotors = snapshot.FullPlant.IntegratedCycle.TurbineExpansion.Rotors,
            Generators = snapshot.FullPlant.IntegratedCycle.Generators,
            FissionWatts = snapshot.FullPlant.IntegratedCycle.PrimaryCircuit.TotalFissionThermalPower.Watts,
            DecayWatts = snapshot.FullPlant.IntegratedCycle.PrimaryCircuit.TotalDecayHeatPower.Watts
        });
    }

    internal static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    internal object InventoryVector(double[] coordinates) => _baseline.Select((seed, index) => new
    {
        node = seed.NodeId,
        mass_kg = seed.MassKilograms * (1 + coordinates[2 * index]),
        internal_energy_j = seed.InternalEnergyJoules * (1 + coordinates[2 * index + 1])
    }).ToArray();
    internal static void Write(string name, object value)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }

    internal static string FindRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "NuclearReactorSimulator.sln"))) return d.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    internal sealed class CandidateRejectedException(string reason) : Exception(reason);

    private static OperationalFluidNodeSeed.ConservedInventory[] BaselineSeeds() => [
new("control-out", 2050.381218833311d, 5265414775.165846d, 3994187.8856824953d, 523.4213085893939d, FluidPhase.SaturatedMixture, 0.9777652720483322d),
                new("drum", 3917.5214804203474d, 5036375894.23722d, 6416459.280654079d, 553.149999989397d, FluidPhase.SaturatedMixture, 0.042322749720366454d),
                new("exhaust", 66.55252025010584d, 142837556.9034473d, 8438.344970819902d, 315.67536613075987d, FluidPhase.SaturatedMixture, 0.8729051078626365d),
                new("feedwater-inventory", 9891.727368706732d, 1962195312.33038d, 17484.899929958774d, 320.5284886658307d, FluidPhase.SubcooledLiquid, null),
                new("header", 3226.6676799875017d, 8341269781.836807d, 6247420.79633744d, 551.3857140089044d, FluidPhase.SaturatedMixture, 0.9980222497643916d),
                new("hotwell", 9891.883976124516d, 1960455817.8974555d, 10808.002981101188d, 320.48565943701715d, FluidPhase.SubcooledLiquid, null),
                new("outlet", 1375.1470283335327d, 2104014157.044389d, 6666459.282132472d, 555.6953255146227d, FluidPhase.SaturatedMixture, 0.21514191260824764d),
                new("pressure", 7507.98659453272d, 9219919054.66319d, 6916459.281680611d, 553.3082998275795d, FluidPhase.SubcooledLiquid, null),
                new("steam", 3306.8269899275992d, 8552155703.008327d, 6398665.756830846d, 552.9659692384483d, FluidPhase.SaturatedMixture, 0.9997887804601804d),
                new("stop-out", 3132.5687588735377d, 8093920564.089228d, 6069485.550324526d, 549.4887377389139d, FluidPhase.SaturatedMixture, 0.9960097013407739d),
                new("suction", 7502.7459746337245d, 9214263779.64126d, 6416459.281680332d, 553.1499999999999d, FluidPhase.SubcooledLiquid, null),
                new("turbine-inlet", 1958.8758929660944d, 5027614200.998343d, 3816252.638393859d, 520.7342832023278d, FluidPhase.SaturatedMixture, 0.9766676884377713d),
    ];
    private static IntegratedAutomaticOperationRuntimeEngine CreateSeedEngine(IReadOnlyCollection<OperationalFluidNodeSeed> fluidNodeSeeds, int deterministicSeedStepCount)
    {
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
                runtimeStep: StepTime,
                useFourNodeBranchContinuityShadowIntegration: false,
                useFourNodeBranchContinuityCorrectedCommitOptIn: true,
                thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain,
                initialFuelTemperatureCelsiusOverride: 305.62514906467646d,
                initialStructureTemperatureCelsiusOverride: 289.13956081139787d,
                initialFluidNodeSeeds: fluidNodeSeeds,
                governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled,
                turbineAdmissionPhasePolicyOverride: TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain,
                turbineMoistureDrainNodeId: "hotwell"));
    }


}
