using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization;

public sealed class GridSynchronizationLoadProgramTests
{
    [Fact]
    public void Scenario_PinsExactM75InitialConditionAndEnablesSynchronizationAndLoadCommands()
    {
        var scenario = GridSynchronizationLoadProgram.Scenario;

        Assert.Equal("pre-synchronization-grid-loading", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Contains(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorBreakerOpen, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorLoadRaise, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorLoadLower, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodWithdraw, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.TurbineSpeedRaise, scenario.AllowedOperatorActions);
    }

    [Fact]
    public void Guidance_IsOrderedFromSynchronizationToM76LowLoadHandoff()
    {
        var guidance = GridSynchronizationLoadProgram.Guidance;

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, guidance.Steps.Select(static step => step.Sequence));
        Assert.Equal(ControlRoomCommandKind.GeneratorBreakerClose,
            guidance.Steps.Single(static step => step.StepId == "close-breaker").SuggestedOperatorAction);
        Assert.Equal(ControlRoomCommandKind.GeneratorLoadRaise,
            guidance.Steps.Single(static step => step.StepId == "take-load").SuggestedOperatorAction);
        Assert.Contains("m76-handoff", guidance.Steps.Single(static step => step.StepId == "m76-handoff").RequiredCheckIds);
    }
}
