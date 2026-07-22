using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class LossOfCoolantScenarioTests
{
    [Fact]
    public void PressureDrivenBreak_RemovesMassAndCarriedInternalEnergyThroughCanonicalNetworkBoundary()
    {
        var baseline = CreateEngine();
        var faulted = CreateEngine();
        ((ILossOfCoolantFaultTarget)faulted).ActivatePressureDrivenBreak(
            "break",
            "pressure",
            MassFlowRate.FromKilogramsPerSecond(1d),
            Pressure.StandardAtmosphere,
            PressureDifference.FromMegapascals(1d),
            maximumInventoryFractionPerStep: 0.0005d);

        baseline.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var baselineNode = baseline.CurrentState.PlantState.PlantState.GetFluidNode("pressure");
        var faultedNode = faulted.CurrentState.PlantState.PlantState.GetFluidNode("pressure");
        Assert.True(faultedNode.Mass < baselineNode.Mass);
        Assert.True(faultedNode.InternalEnergy < baselineNode.InternalEnergy);
    }

    [Fact]
    public void PressureDrivenBreak_DepressurizesRelativeToIdenticalNoBreakStep()
    {
        var baseline = CreateEngine();
        var faulted = CreateEngine();
        ((ILossOfCoolantFaultTarget)faulted).ActivatePressureDrivenBreak(
            "break",
            "pressure",
            MassFlowRate.FromKilogramsPerSecond(1d),
            Pressure.StandardAtmosphere,
            PressureDifference.FromMegapascals(1d),
            maximumInventoryFractionPerStep: 0.0005d);

        baseline.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var baselinePressure = baseline.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Pressure;
        var faultedPressure = faulted.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Pressure;
        Assert.True(faultedPressure < baselinePressure);
    }

    [Fact]
    public void PressureDrivenBreak_WithNoPositiveDrivingPressure_RemovesNoAdditionalInventory()
    {
        var baseline = CreateEngine();
        var faulted = CreateEngine();
        ((ILossOfCoolantFaultTarget)faulted).ActivatePressureDrivenBreak(
            "break",
            "pressure",
            MassFlowRate.FromKilogramsPerSecond(10d),
            Pressure.FromMegapascals(10d),
            PressureDifference.FromMegapascals(1d),
            maximumInventoryFractionPerStep: 0.001d);

        baseline.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var baselineMass = baseline.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Mass.Kilograms;
        var faultedMass = faulted.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Mass.Kilograms;
        Assert.Equal(baselineMass, faultedMass, 10);
    }

    [Fact]
    public void PressureDrivenBreak_IsBoundedByDeclaredMaximumInventoryFractionPerStep()
    {
        const double maximumFraction = 0.0001d;
        var baseline = CreateEngine();
        var faulted = CreateEngine();
        var initialMass = faulted.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Mass.Kilograms;
        ((ILossOfCoolantFaultTarget)faulted).ActivatePressureDrivenBreak(
            "break",
            "pressure",
            MassFlowRate.FromKilogramsPerSecond(1_000_000d),
            Pressure.Vacuum,
            PressureDifference.FromKilopascals(1d),
            maximumFraction);

        baseline.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var baselineMass = baseline.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Mass.Kilograms;
        var faultedMass = faulted.CurrentState.PlantState.PlantState.GetFluidNode("pressure").Mass.Kilograms;
        var additionalLoss = baselineMass - faultedMass;
        Assert.True(additionalLoss > 0d);
        Assert.True(additionalLoss <= (initialMass * maximumFraction) + 1e-9d);
    }

    [Fact]
    public void BuiltInScenarioPack_BindsAllM85ScenariosWithoutCustomRegistry()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);

        foreach (var scenario in LossOfCoolantScenarioPack.All)
        {
            var session = factory.Load(scenario);
            Assert.Equal(scenario.Faults.Count, session.Coordinator.Current.Faults.PendingCount);
        }
    }

    [Fact]
    public void UnknownBreakTarget_FailsClosedAtActivation()
    {
        var engine = CreateEngine();
        var target = (ILossOfCoolantFaultTarget)engine;

        Assert.Throws<KeyNotFoundException>(() => target.ActivatePressureDrivenBreak(
            "bad",
            "missing-node",
            MassFlowRate.FromKilogramsPerSecond(1d),
            Pressure.StandardAtmosphere,
            PressureDifference.FromMegapascals(1d),
            maximumInventoryFractionPerStep: 0.0005d));
    }

    [Fact]
    public void FixedLeakAndPressureDrivenBreak_CannotSilentlyStackOnSameNode()
    {
        var engine = CreateEngine();
        var hydraulic = (IHydraulicComponentFaultTarget)engine;
        var loca = (ILossOfCoolantFaultTarget)engine;
        hydraulic.ActivateLeak("fixed-leak", "pressure", MassFlowRate.FromKilogramsPerSecond(0.1d));

        Assert.Throws<InvalidOperationException>(() => loca.ActivatePressureDrivenBreak(
            "break",
            "pressure",
            MassFlowRate.FromKilogramsPerSecond(1d),
            Pressure.StandardAtmosphere,
            PressureDifference.FromMegapascals(1d),
            maximumInventoryFractionPerStep: 0.0005d));
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
}
