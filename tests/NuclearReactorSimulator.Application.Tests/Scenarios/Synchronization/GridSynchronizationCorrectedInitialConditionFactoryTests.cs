using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization;

public sealed class GridSynchronizationCorrectedInitialConditionFactoryTests
{
    [Fact]
    public void Version3_PreservesVersion2ControlAndGridContractWhileChangingOnlyHydraulicNumericalMode()
    {
        var version2 = new GridSynchronizationSustainedInitialConditionFactory();
        var version3 = new GridSynchronizationCorrectedInitialConditionFactory();

        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 2), version2.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("pre-synchronization-grid-loading", 3), version3.Descriptor.Reference);

        var version2Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(version2.CreateRuntimeEngine());
        var version3Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(version3.CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), version2Engine.FixedDeltaTime);
        Assert.Equal(version2Engine.FixedDeltaTime, version3Engine.FixedDeltaTime);

        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            version2Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            version3Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);

        var version2Turbine = version2Engine.PersistentInputs.TurbineSecondaryInputs.Definition;
        var version3Turbine = version3Engine.PersistentInputs.TurbineSecondaryInputs.Definition;
        var version2Controller = version2Turbine.ActuatorSystem.ControlSystem.GetController("speed-control");
        var version3Controller = version3Turbine.ActuatorSystem.ControlSystem.GetController("speed-control");

        Assert.Equal(version2Controller.Algorithm, version3Controller.Algorithm);
        Assert.Equal(version2Controller.ProportionalGain, version3Controller.ProportionalGain, 12);
        Assert.Equal(version2Controller.IntegralGainPerSecond, version3Controller.IntegralGainPerSecond, 12);
        Assert.Equal(version2Controller.DerivativeGainSeconds, version3Controller.DerivativeGainSeconds, 12);
        Assert.Equal(version2Controller.OutputRange.Minimum, version3Controller.OutputRange.Minimum, 12);
        Assert.Equal(version2Controller.OutputRange.Maximum, version3Controller.OutputRange.Maximum, 12);

        var version2Generator = Assert.Single(version2Engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var version3Generator = Assert.Single(version3Engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var version2Grid = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(version2Generator.GridCoupling);
        var version3Grid = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(version3Generator.GridCoupling);

        Assert.Equal(version2Generator.MaximumElectricalPower.Megawatts, version3Generator.MaximumElectricalPower.Megawatts, 12);
        Assert.Equal(version2Grid.MaximumSynchronizingCorrectionPower.Megawatts, version3Grid.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(version2Grid.FrequencyDampingPowerAtOneHertzSlip.Megawatts, version3Grid.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
        Assert.Equal(version2Grid.PowerFlowMode, version3Grid.PowerFlowMode);
    }
}
