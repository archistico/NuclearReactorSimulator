using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
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
        var legacyDrum = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        Assert.Equal(SteamDrumLiquidRecirculationMode.LegacyReturnSplit, legacyDrum.LiquidRecirculationMode);
        var legacyCondenser = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.False(legacyCondenser.OverallHeatTransferConductance.HasValue);
        var legacyGeneratorDefinition = Assert.Single(legacyEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        Assert.Null(legacyGeneratorDefinition.GridCoupling);

        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(current.CreateRuntimeEngine());
        var stageDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.True(stageDefinition.ExpansionResistance.HasValue);
        Assert.Equal(
            21_400d,
            stageDefinition.ExpansionResistance.GetValueOrDefault().PascalSecondsSquaredPerKilogramSquared);
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
        var currentGeneratorDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var gridCoupling = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(
            currentGeneratorDefinition.GridCoupling);
        Assert.Equal(10d, gridCoupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(10d, gridCoupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);

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

    private static double AdmissionTrainInventoryKilograms(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var plant = engine.CurrentState.PlantState.PlantState;
        return plant.GetFluidNode("stop-out").Mass.Kilograms
            + plant.GetFluidNode("control-out").Mass.Kilograms
            + plant.GetFluidNode("turbine-inlet").Mass.Kilograms;
    }
}
