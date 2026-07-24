using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization;

public sealed class GridSynchronizationSustainedInitialConditionFactoryTests
{
    [Fact]
    public void Version2_CreatesFinitePreSynchronizationStateWithoutMutatingHistoricalVersion1Identity()
    {
        var legacy = new GridSynchronizationInitialConditionFactory();
        var current = new GridSynchronizationSustainedInitialConditionFactory();

        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 1), legacy.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 2), current.Descriptor.Reference);

        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(current.CreateRuntimeEngine());
        var currentDrumInventory = currentEngine.CurrentState.PlantState.PlantState.GetFluidNode("drum");
        Assert.Equal(FluidPhase.SaturatedMixture, currentDrumInventory.Phase);
        Assert.NotNull(currentDrumInventory.VaporQuality);
        Assert.InRange(
            currentEngine.CurrentState.MeasuredSignals.GetSignal("level").EngineeringValue ?? double.NaN,
            0.49d,
            0.51d);
        var currentGovernorDroop = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineGovernorDroopDefinition>(
            currentEngine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        Assert.Equal(150d, currentGovernorDroop.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
        var currentDrum = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        var currentSteamSource = Assert.IsType<SteamDrumSteamSourceDefinition>(currentDrum.SteamSource);
        Assert.Equal(100d, currentSteamSource.HydraulicResistance.PascalSecondsSquaredPerKilogramSquared, 12);
        var stageDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.IsType<TurbineThermodynamicWorkDefinition>(stageDefinition.ThermodynamicWork);
        Assert.Equal(TurbineAdmissionPhasePolicy.VaporMassFractionLimited, stageDefinition.AdmissionPhasePolicy);
        var generatorDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var gridCoupling = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(
            generatorDefinition.GridCoupling);
        Assert.Equal(10d, gridCoupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(10d, gridCoupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
        var condenserDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.Equal(20d, condenserDefinition.MaximumCondensationMassFlowRate.KilogramsPerSecond, 12);
        var coolingDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.CoolingBoundaries);
        Assert.Equal(40d, coolingDefinition.MaximumInstalledHeatRejectionPower.GetValueOrDefault().Megawatts, 12);
        Assert.Equal(
            CondenserCondensateEnergyMode.SaturatedLiquidAtSteamSpacePressure,
            condenserDefinition.CondensateEnergyMode);
        var coolingBoundary = Assert.Single(
            currentEngine.PersistentInputs
                .PlantInputs
                .GeneratorGridInputs
                .CondensateFeedwaterInputs
                .CondenserInputs
                .CoolingBoundaryInputs);
        Assert.Equal(40d, coolingBoundary.AvailableHeatRejectionPower.Megawatts, 12);

        var snapshot = new ControlRoomRuntimeCoordinator(currentEngine).Current;
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var steamLine = Assert.Single(snapshot.TurbineSecondary.SteamLines);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
        var admission = Assert.Single(snapshot.TurbineSecondary.AdmissionTrains);

        Assert.False(generator.BreakerClosed);
        Assert.True(generator.SynchronizationConditionsSatisfied);
        Assert.True((steamLine.MassFlow.NumericValue ?? 0d) > 10d);
        Assert.True((steamLine.PressureDifference.NumericValue ?? 0d) > 0d);
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_980d, 3_020d);
        Assert.True(double.IsFinite(admission.TurbineInletPressure.NumericValue ?? double.NaN));
        Assert.True(double.IsFinite(admission.TurbineInletTemperature.NumericValue ?? double.NaN));
        Assert.InRange(admission.ControlValvePosition.NumericValue ?? double.NaN, 25d, 35d);
        Assert.False(snapshot.AnyTripActive);
    }

    [Fact]
    public void Version2_CloseLoadThenRunRemainsThermodynamicallyResolvableForOneSimulatedSecond()
    {
        var engine = new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine();
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var initialGenerator = Assert.Single(initial.Electrical.Generators);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        var paralleled = engine.Step(ControlRoomRunState.Running);
        var paralleledGenerator = Assert.Single(paralleled.Electrical.Generators);
        Assert.True(paralleledGenerator.BreakerClosed);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            paralleledGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        var snapshot = engine.Step(ControlRoomRunState.Running);

        for (var step = 0; step < 100; step++)
        {
            snapshot = engine.Step(ControlRoomRunState.Running);
        }

        var generator = Assert.Single(snapshot.Electrical.Generators);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);

        Assert.True(generator.BreakerClosed);
        Assert.True((generator.RequestedElectricalPower.NumericValue ?? 0d) > 4.5d);
        Assert.False(snapshot.AnyTripActive);
        Assert.True(double.IsFinite(rotor.Speed.NumericValue ?? double.NaN));
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_900d, 3_100d);
    }
}
