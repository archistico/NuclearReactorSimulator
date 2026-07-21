using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomComponentCatalogTests
{
    [Fact]
    public void DefaultCatalog_ContainsAllReusableM62ComponentKindsExactlyOnce()
    {
        var components = ControlRoomComponentCatalog.Default;
        var expectedKinds = Enum.GetValues<ControlRoomComponentKind>();

        Assert.Equal(expectedKinds.Length, components.Count);
        Assert.Equal(components.Count, components.Select(static item => item.Kind).Distinct().Count());

        foreach (var kind in expectedKinds)
        {
            Assert.Contains(components, item => item.Kind == kind);
        }
    }

    [Fact]
    public void DefaultCatalog_AssignsInteractiveRulesOnlyToOperatorControls()
    {
        var components = ControlRoomComponentCatalog.Default;

        Assert.All(
            components.Where(static item => item.InteractionMode == ControlRoomInteractionMode.DisplayOnly),
            static item => Assert.Contains("does not alter", item.PointerRule));

        Assert.Contains(
            components,
            static item => item.Kind == ControlRoomComponentKind.ToggleSwitch
                && item.InteractionMode == ControlRoomInteractionMode.Toggle);
        Assert.Contains(
            components,
            static item => item.Kind == ControlRoomComponentKind.Selector
                && item.InteractionMode == ControlRoomInteractionMode.Selection);
        Assert.Contains(
            components,
            static item => item.Kind == ControlRoomComponentKind.PushButton
                && item.InteractionMode == ControlRoomInteractionMode.MomentaryCommand);
    }

    [Theory]
    [InlineData(ControlRoomVisualState.Normal)]
    [InlineData(ControlRoomVisualState.Warning)]
    [InlineData(ControlRoomVisualState.Trip)]
    [InlineData(ControlRoomVisualState.Unavailable)]
    public void VisualState_ProvidesStableFourStatePresentationContract(ControlRoomVisualState state)
    {
        Assert.Contains(state, Enum.GetValues<ControlRoomVisualState>());
    }
}
