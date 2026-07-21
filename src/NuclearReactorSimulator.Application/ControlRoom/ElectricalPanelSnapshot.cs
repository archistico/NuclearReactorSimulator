namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only M6.5 generator/grid workspace snapshot.</summary>
public sealed record ElectricalPanelSnapshot(
    ElectricalGridPresentationSnapshot Grid,
    IReadOnlyList<GeneratorPresentationSnapshot> Generators,
    ControlRoomValueSnapshot GrossElectricalOutput,
    bool GeneratorTripActive)
{
    public static ElectricalPanelSnapshot Unavailable { get; } = new(
        ElectricalGridPresentationSnapshot.Unavailable,
        Array.Empty<GeneratorPresentationSnapshot>(),
        ControlRoomValueSnapshot.Unavailable("MWe"),
        false);

    public ControlRoomVisualState ProtectionState => GeneratorTripActive
        ? ControlRoomVisualState.Trip
        : ControlRoomVisualState.Normal;

    public string ProtectionText => GeneratorTripActive ? "GENERATOR TRIP ACTIVE" : "NO GENERATOR TRIP LATCHED";
}
