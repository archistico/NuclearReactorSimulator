using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopSustainedGenerationInitialConditionFactoryTests
{
    [Fact]
    public void Version2_PreservesLegacyVersion1IdentityAndPublishesGenerationReadyMechanicalSupport()
    {
        var legacy = new DesktopIntegratedOperationsInitialConditionFactory();
        var current = new DesktopSustainedGenerationInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { legacy, current });

        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 1), legacy.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), current.Descriptor.Reference);
        Assert.Same(legacy, registry.Resolve(legacy.Descriptor.Reference));
        Assert.Same(current, registry.Resolve(current.Descriptor.Reference));

        var legacyEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(legacy.CreateRuntimeEngine());
        var legacyStage = Assert.Single(legacyEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.False(legacyStage.ExpansionResistance.HasValue);
        Assert.Null(legacyStage.ThermodynamicWork);
        var legacyDrum = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        Assert.Equal(SteamDrumLiquidRecirculationMode.LegacyReturnSplit, legacyDrum.LiquidRecirculationMode);
        var legacyCondenser = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.False(legacyCondenser.OverallHeatTransferConductance.HasValue);
        var legacyGeneratorDefinition = Assert.Single(legacyEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        Assert.Null(legacyGeneratorDefinition.GridCoupling);
        Assert.False(legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("condensate-pump").HasDischargeCheckValve);
        Assert.False(legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("feedwater-pump").HasDischargeCheckValve);
        Assert.All(
            legacyEngine.CurrentState.TurbineSecondaryControlState.Definition.ActuatorSystem.Actuators
                .Where(static actuator => actuator.TargetKind is
                    NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Valve
                    or NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Pump),
            static actuator => Assert.Null(actuator.TravelRate));
        Assert.Null(legacyEngine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);

        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(current.CreateRuntimeEngine());
        var stageDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.True(stageDefinition.ExpansionResistance.HasValue);
        Assert.Equal(
            21_400d,
            stageDefinition.ExpansionResistance.GetValueOrDefault().PascalSecondsSquaredPerKilogramSquared);
        var thermodynamicWork = Assert.IsType<NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine.TurbineThermodynamicWorkDefinition>(
            stageDefinition.ThermodynamicWork);
        Assert.Equal(2.1d, thermodynamicWork.VaporSpecificHeatAtConstantPressure.KilojoulesPerKilogramKelvin, 12);
        Assert.Equal(1.3d, thermodynamicWork.HeatCapacityRatio, 12);
        Assert.Equal(0.8d, thermodynamicWork.MaximumInletInternalEnergyExtractionFraction, 12);
        var currentDrum = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        Assert.Equal(SteamDrumLiquidRecirculationMode.CirculationDemandBalanced, currentDrum.LiquidRecirculationMode);
        var currentCondenserDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.True(currentCondenserDefinition.OverallHeatTransferConductance.HasValue);
        Assert.Equal(
            1.225d,
            currentCondenserDefinition.OverallHeatTransferConductance.GetValueOrDefault().MegawattsPerKelvin,
            12);
        Assert.Equal(20d, currentCondenserDefinition.MaximumCondensationMassFlowRate.KilogramsPerSecond, 12);
        var currentCoolingBoundary = Assert.Single(
            currentEngine.PersistentInputs
                .PlantInputs
                .GeneratorGridInputs
                .CondensateFeedwaterInputs
                .CondenserInputs
                .CoolingBoundaryInputs);
        Assert.Equal(40d, currentCoolingBoundary.AvailableHeatRejectionPower.Megawatts, 12);
        var currentGeneratorDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var gridCoupling = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(
            currentGeneratorDefinition.GridCoupling);
        Assert.Equal(10d, gridCoupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(10d, gridCoupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
        Assert.True(currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("condensate-pump").HasDischargeCheckValve);
        Assert.True(currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("feedwater-pump").HasDischargeCheckValve);
        var currentActuators = currentEngine.CurrentState.TurbineSecondaryControlState.Definition.ActuatorSystem;
        Assert.Equal(
            0.5d,
            currentActuators.GetActuator("speed-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.5d,
            currentActuators.GetActuator("pressure-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.25d,
            currentActuators.GetActuator("feedwater-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.25d,
            currentActuators.GetActuator("condensate-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        var currentGovernorDroop = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineGovernorDroopDefinition>(
            currentEngine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        Assert.Equal("speed-control", currentGovernorDroop.SpeedControllerId);
        Assert.Equal("generator", currentGovernorDroop.GeneratorId);
        Assert.Equal(150d, currentGovernorDroop.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);

        var coordinator = new ControlRoomRuntimeCoordinator(currentEngine);
        var snapshot = coordinator.Current;
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var steamLine = Assert.Single(snapshot.TurbineSecondary.SteamLines);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
        var stage = Assert.Single(snapshot.TurbineSecondary.StageGroups);
        var condenser = Assert.Single(snapshot.TurbineSecondary.Condensers);
        var feedwater = Assert.Single(snapshot.TurbineSecondary.FeedwaterTrains);

        Assert.True(generator.BreakerClosed);
        Assert.InRange(generator.RequestedElectricalPower.NumericValue ?? double.NaN, 4.9d, 5.1d);
        Assert.True((steamLine.MassFlow.NumericValue ?? 0d) > 10d);
        Assert.True((steamLine.PressureDifference.NumericValue ?? 0d) > 0d);
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_980d, 3_020d);
        Assert.InRange(stage.SteamFlow.NumericValue ?? double.NaN, 12.5d, 30d);
        Assert.True(stage.ThermodynamicWorkModelActive);
        Assert.InRange(stage.AvailableSpecificWork.NumericValue ?? double.NaN, 450d, 500d);
        Assert.InRange(stage.ExtractedSpecificWork.NumericValue ?? double.NaN, 350d, 450d);
        Assert.InRange(rotor.ShaftPower.NumericValue ?? double.NaN, 4.5d, 20d);
        Assert.InRange(snapshot.TurbineSecondary.EffectiveTurbineSteamFlow.NumericValue ?? double.NaN, 12.5d, 30d);
        Assert.True((condenser.CondensationFlow.NumericValue ?? 0d) > 10d);
        Assert.True((feedwater.CondensatePump.MassFlow.NumericValue ?? 0d) > 10d);
        Assert.True((feedwater.FeedwaterPump.MassFlow.NumericValue ?? 0d) > 10d);
    }

    [Fact]
    public void Version2_TurbineExpansionDrainKeepsAdmissionTrainInventoryBounded()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var initialTrainInventoryKilograms = AdmissionTrainInventoryKilograms(engine);
        Assert.True(initialTrainInventoryKilograms > 0d);
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var sampledInventories = new List<double> { initialTrainInventoryKilograms };

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        for (var step = 0; step < 200; step++)
        {
            _ = coordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
            sampledInventories.Add(AdmissionTrainInventoryKilograms(engine));
        }

        var finalTrainInventoryKilograms = sampledInventories[^1];
        Assert.InRange(
            finalTrainInventoryKilograms,
            0.95d * initialTrainInventoryKilograms,
            1.05d * initialTrainInventoryKilograms);

        // The historical min(stop, control, admission) stage law made the combined admission-train inventory
        // mathematically non-decreasing. A pressure-driven turbine drain must break that ratchet: at least one
        // committed step must reduce the train inventory while the 2 s final inventory remains near its initial value.
        var inventoryDeltas = sampledInventories
            .Zip(sampledInventories.Skip(1), static (before, after) => after - before)
            .ToArray();
        Assert.Contains(inventoryDeltas, delta => delta < -1e-9d);

        var admissionTrain = Assert.Single(coordinator.Current.TurbineSecondary.AdmissionTrains);
        var stage = Assert.Single(coordinator.Current.TurbineSecondary.StageGroups);
        var admissionFlow = admissionTrain.AdmissionFlow.NumericValue ?? double.NaN;
        var stageFlow = stage.SteamFlow.NumericValue ?? double.NaN;
        Assert.True(double.IsFinite(admissionFlow));
        Assert.True(double.IsFinite(stageFlow));
        Assert.InRange(admissionFlow, 10d, 30d);
        Assert.InRange(stageFlow, 10d, 30d);
    }


    [Fact]
    public void Version2_EnablesMeaningfulSecondaryProtectionsWhileLegacyVersion1RemainsMinimal()
    {
        var legacyEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopIntegratedOperationsInitialConditionFactory().CreateRuntimeEngine());
        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

        var legacyProtection = legacyEngine.CurrentState.ProtectionState.Definition;
        Assert.Single(legacyProtection.TripFunctions);
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("turbine-overspeed"));
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("condenser-high-backpressure"));
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("generator-overfrequency"));

        var expectedMeasuredChannelIds = currentEngine.CurrentState.InstrumentationDefinition.Channels
            .Select(static channel => channel.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var actualMeasuredChannelIds = currentEngine.CurrentState.MeasuredSignals.Signals
            .Select(static signal => signal.ChannelId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedMeasuredChannelIds, actualMeasuredChannelIds);
        Assert.InRange(
            currentEngine.CurrentState.MeasuredSignals.GetSignal("condenser-pressure").EngineeringValue ?? double.NaN,
            1_000d,
            20_000d);
        var currentGenerator = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var currentRotorState = Assert.Single(currentEngine.CurrentState.PlantState.TurbineState.Rotors);
        var expectedGeneratorFrequencyHertz = currentGenerator.ElectricalFrequencyAt(currentRotorState.AngularSpeed).Hertz;
        Assert.Equal(
            expectedGeneratorFrequencyHertz,
            currentEngine.CurrentState.MeasuredSignals.GetSignal("generator-frequency").EngineeringValue ?? double.NaN,
            9);
        Assert.InRange(expectedGeneratorFrequencyHertz, 49.9d, 50.1d);

        var currentProtection = currentEngine.CurrentState.ProtectionState.Definition;
        Assert.Equal(4, currentProtection.TripFunctions.Count);

        AssertProtection(
            currentProtection.GetTripFunction("turbine-overspeed"),
            "speed",
            ProtectionComparison.High,
            3_300d,
            3_150d,
            ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(
            currentProtection.GetTripFunction("condenser-high-backpressure"),
            "condenser-pressure",
            ProtectionComparison.High,
            30_000d,
            20_000d,
            ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(
            currentProtection.GetTripFunction("generator-overfrequency"),
            "generator-frequency",
            ProtectionComparison.High,
            53d,
            51.5d,
            ProtectionAction.GeneratorTrip);

        Assert.False(new ControlRoomRuntimeCoordinator(currentEngine).Current.AnyTripActive);
    }

    [Fact]
    public void Version2_SecondaryProtectionFunctionsLatchFromMeasuredSignals()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var definition = engine.CurrentState.ProtectionState.Definition;
        var solver = new ProtectionSystemSolver(definition);

        var cases = new[]
        {
            new ProtectionTriggerCase(
                "turbine-overspeed",
                "speed",
                3_350d,
                ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip),
            new ProtectionTriggerCase(
                "condenser-high-backpressure",
                "condenser-pressure",
                35_000d,
                ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip),
            new ProtectionTriggerCase(
                "generator-overfrequency",
                "generator-frequency",
                54d,
                ProtectionAction.GeneratorTrip),
        };

        foreach (var testCase in cases)
        {
            var signals = ReplaceMeasuredSignal(
                engine.CurrentState.MeasuredSignals,
                testCase.ChannelId,
                testCase.EngineeringValue);
            var result = solver.Step(
                signals,
                ProtectionSystemState.CreateInitial(definition),
                new ProtectionSystemInputs(definition));

            Assert.True(result.CandidateState.IsFunctionLatched(testCase.FunctionId));
            Assert.Equal(
                testCase.ExpectedActions,
                result.Snapshot.LatchedActions & testCase.ExpectedActions);
        }
    }

    private static void AssertProtection(
        ProtectionFunctionDefinition definition,
        string channelId,
        ProtectionComparison comparison,
        double tripThreshold,
        double resetThreshold,
        ProtectionAction actions)
    {
        Assert.Equal(channelId, definition.MeasurementChannelId);
        Assert.Equal(comparison, definition.Comparison);
        Assert.Equal(tripThreshold, definition.TripThreshold, 12);
        Assert.Equal(resetThreshold, definition.ResetThreshold, 12);
        Assert.Equal(actions, definition.Actions);
    }

    private static MeasuredSignalFrame ReplaceMeasuredSignal(
        MeasuredSignalFrame source,
        string channelId,
        double engineeringValue)
        => new(
            source.Definition,
            source.Signals.Select(signal => signal.ChannelId == channelId
                ? signal with { EngineeringValue = engineeringValue, ScaledValue = engineeringValue }
                : signal));

    private sealed record ProtectionTriggerCase(
        string FunctionId,
        string ChannelId,
        double EngineeringValue,
        ProtectionAction ExpectedActions);

    private static double AdmissionTrainInventoryKilograms(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var plant = engine.CurrentState.PlantState.PlantState;
        return plant.GetFluidNode("stop-out").Mass.Kilograms
            + plant.GetFluidNode("control-out").Mass.Kilograms
            + plant.GetFluidNode("turbine-inlet").Mass.Kilograms;
    }
}
