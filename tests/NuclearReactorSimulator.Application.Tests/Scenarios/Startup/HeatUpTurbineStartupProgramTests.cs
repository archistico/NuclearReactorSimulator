using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Startup;

public sealed class HeatUpTurbineStartupProgramTests
{
    [Fact]
    public void Scenario_PinsExactM74InitialConditionAndDefersSynchronizationAndLoading()
    {
        var scenario = HeatUpTurbineStartupProgram.Scenario;

        Assert.Equal("low-power-steam-raising", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Contains(ControlRoomCommandKind.TurbineSpeedRaise, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.TurbineSpeedLower, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodHold, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorBreakerOpen, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorLoadRaise, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorLoadLower, scenario.AllowedOperatorActions);
    }

    [Fact]
    public void Guidance_IsOrderedAndStopsAtM75SynchronizationBoundary()
    {
        var guidance = HeatUpTurbineStartupProgram.Guidance;

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, guidance.Steps.Select(static step => step.Sequence));
        Assert.Equal(
            ControlRoomCommandKind.TurbineSpeedRaise,
            guidance.Steps.Single(static step => step.StepId == "roll-turbine").SuggestedOperatorAction);

        var handoff = guidance.Steps.Single(static step => step.StepId == "m75-handoff");
        Assert.Contains("near-sync-speed", handoff.RequiredCheckIds);
        Assert.Contains("breakers-open", handoff.RequiredCheckIds);
        Assert.Contains("generator-unloaded", handoff.RequiredCheckIds);
    }
}
