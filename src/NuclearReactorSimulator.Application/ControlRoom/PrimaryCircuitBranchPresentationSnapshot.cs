namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record PrimaryCircuitBranchPresentationSnapshot(
    string FuelChannelGroupId,
    int RepresentedChannelCount,
    ControlRoomValueSnapshot ChannelFlow,
    ControlRoomValueSnapshot ReturnFlow,
    ControlRoomValueSnapshot PerChannelFlow,
    ControlRoomValueSnapshot ChannelPressureDifference,
    string OutletPhase,
    string FlowDirection,
    string VoidText)
{
    public string RepresentedChannelsText => $"Equivalent channels: {RepresentedChannelCount}";

    public string ChannelFlowText => $"Channel {ChannelFlow.ValueText} {ChannelFlow.Unit}".TrimEnd();

    public string ReturnFlowText => $"Return {ReturnFlow.ValueText} {ReturnFlow.Unit}".TrimEnd();

    public string PressureDifferenceText => $"ΔP {ChannelPressureDifference.ValueText} {ChannelPressureDifference.Unit}".TrimEnd();
}
