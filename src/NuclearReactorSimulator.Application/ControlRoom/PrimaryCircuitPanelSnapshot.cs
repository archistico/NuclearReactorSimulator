namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only M6.4 primary-circuit mnemonic snapshot.</summary>
public sealed record PrimaryCircuitPanelSnapshot(
    IReadOnlyList<PrimaryCircuitLoopPresentationSnapshot> Loops,
    IReadOnlyList<PrimaryCircuitSteamDrumPresentationSnapshot> SteamDrums,
    IReadOnlyList<PrimaryCircuitValvePresentationSnapshot> Valves,
    ControlRoomValueSnapshot TotalPrimaryMass,
    ControlRoomValueSnapshot TotalFeedwaterFlow,
    ControlRoomValueSnapshot TotalSteamExportFlow)
{
    public static PrimaryCircuitPanelSnapshot Unavailable { get; } = new(
        Array.Empty<PrimaryCircuitLoopPresentationSnapshot>(),
        Array.Empty<PrimaryCircuitSteamDrumPresentationSnapshot>(),
        Array.Empty<PrimaryCircuitValvePresentationSnapshot>(),
        ControlRoomValueSnapshot.Unavailable("kg"),
        ControlRoomValueSnapshot.Unavailable("kg/s"),
        ControlRoomValueSnapshot.Unavailable("kg/s"));

    public IReadOnlyList<PrimaryCircuitPumpPresentationSnapshot> Pumps =>
        Loops.SelectMany(static loop => loop.Pumps).ToArray();
}
