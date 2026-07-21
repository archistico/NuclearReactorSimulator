using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Operations;

public sealed class PowerManoeuvringNormalShutdownProgramTests
{
    [Fact]
    public void Scenario_PinsExactM76InitialConditionAndEnablesManoeuvringAndNormalShutdownCommands()
    {
        var scenario = PowerManoeuvringNormalShutdownProgram.Scenario;

        Assert.Equal("stable-low-load-parallel-operation", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Contains(ControlRoomCommandKind.GeneratorLoadRaise, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorLoadLower, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorBreakerOpen, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodInsert, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.TurbineSpeedLower, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
    }

    [Fact]
    public void Guidance_IsOrderedFromLowLoadManoeuvringThroughPostShutdownCooling()
    {
        var guidance = PowerManoeuvringNormalShutdownProgram.Guidance;

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, guidance.Steps.Select(static step => step.Sequence));
        Assert.Equal(ControlRoomCommandKind.GeneratorLoadRaise,
            guidance.Steps.Single(static step => step.StepId == "raise-load").SuggestedOperatorAction);
        Assert.Equal(ControlRoomCommandKind.GeneratorBreakerOpen,
            guidance.Steps.Single(static step => step.StepId == "disconnect").SuggestedOperatorAction);
        Assert.Equal(ControlRoomCommandKind.ControlRodInsert,
            guidance.Steps.Single(static step => step.StepId == "shutdown-reactor").SuggestedOperatorAction);
        Assert.Contains("post-shutdown-cooling", guidance.Steps.Single(static step => step.StepId == "post-shutdown").RequiredCheckIds);
    }
}
