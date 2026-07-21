using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Criticality;

public sealed class FirstCriticalityLowPowerProgramTests
{
    [Fact]
    public void Scenario_PinsExactSourceRangeInitialConditionAndAllowsOnlyM73OperationalActions()
    {
        var scenario = FirstCriticalityLowPowerProgram.Scenario;

        Assert.Equal("pre-criticality-source-range", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Contains(ControlRoomCommandKind.ControlRodWithdraw, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodHold, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodInsert, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ReactorScram, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.TurbineSpeedRaise, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorLoadRaise, scenario.AllowedOperatorActions);
    }

    [Fact]
    public void Guidance_IsOrderedAndStopsAtLowPowerBeforeSteamAndGridStartup()
    {
        var guidance = FirstCriticalityLowPowerProgram.Guidance;

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, guidance.Steps.Select(static step => step.Sequence));
        Assert.Equal(
            ControlRoomCommandKind.ControlRodWithdraw,
            guidance.Steps.Single(static step => step.StepId == "controlled-approach").SuggestedOperatorAction);
        Assert.Equal(
            ControlRoomCommandKind.ControlRodHold,
            guidance.Steps.Single(static step => step.StepId == "first-criticality").SuggestedOperatorAction);

        var handoff = guidance.Steps.Single(static step => step.StepId == "m74-handoff");
        Assert.Contains("low-power", handoff.RequiredCheckIds);
        Assert.Contains("stable-period", handoff.RequiredCheckIds);
        Assert.Contains("steam-isolated", handoff.RequiredCheckIds);
        Assert.Contains("breakers-open", handoff.RequiredCheckIds);
        Assert.Contains("xenon-boundary", handoff.RequiredCheckIds);
    }
}
