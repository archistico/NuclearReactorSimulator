namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomPlantMimicSnapshot(
    IReadOnlyList<ControlRoomPlantMimicElementSnapshot> Elements,
    IReadOnlyList<ControlRoomPlantMimicConnectionSnapshot> Connections,
    string PathSummaryText)
{
    public static ControlRoomPlantMimicSnapshot Empty { get; } = new(
        Array.Empty<ControlRoomPlantMimicElementSnapshot>(),
        Array.Empty<ControlRoomPlantMimicConnectionSnapshot>(),
        "PLANT FLOW PATH UNAVAILABLE");
}
