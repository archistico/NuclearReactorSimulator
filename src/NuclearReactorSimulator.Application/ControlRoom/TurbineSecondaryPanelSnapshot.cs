using System.Text.Json.Serialization;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only M6.5 turbine/secondary-cycle workspace snapshot.</summary>
public sealed record TurbineSecondaryPanelSnapshot(
    IReadOnlyList<MainSteamLinePresentationSnapshot> SteamLines,
    IReadOnlyList<TurbineAdmissionTrainPresentationSnapshot> AdmissionTrains,
    IReadOnlyList<TurbineRotorPresentationSnapshot> Rotors,
    IReadOnlyList<TurbineStageGroupPresentationSnapshot> StageGroups,
    IReadOnlyList<CondenserPresentationSnapshot> Condensers,
    IReadOnlyList<FeedwaterTrainPresentationSnapshot> FeedwaterTrains,
    ControlRoomValueSnapshot TotalSteamFlow,
    ControlRoomValueSnapshot TotalTurbineShaftPower,
    ControlRoomValueSnapshot TotalCondenserHeatRejection,
    bool TurbineTripActive)
{
    public static TurbineSecondaryPanelSnapshot Unavailable { get; } = new(
        Array.Empty<MainSteamLinePresentationSnapshot>(),
        Array.Empty<TurbineAdmissionTrainPresentationSnapshot>(),
        Array.Empty<TurbineRotorPresentationSnapshot>(),
        Array.Empty<TurbineStageGroupPresentationSnapshot>(),
        Array.Empty<CondenserPresentationSnapshot>(),
        Array.Empty<FeedwaterTrainPresentationSnapshot>(),
        ControlRoomValueSnapshot.Unavailable("kg/s"),
        ControlRoomValueSnapshot.Unavailable("MW"),
        ControlRoomValueSnapshot.Unavailable("MW"),
        false);

    public ControlRoomVisualState ProtectionState => TurbineTripActive
        ? ControlRoomVisualState.Trip
        : ControlRoomVisualState.Normal;

    /// <summary>
    /// Current model-derived turbine working-steam flow from effective stage-group admission. Presentation-only so the
    /// historical fingerprint-v1 payload keeps the legacy M4.1 boundary-total field unchanged.
    /// </summary>
    [JsonIgnore]
    public ControlRoomValueSnapshot EffectiveTurbineSteamFlow { get; init; } = TotalSteamFlow;

    public string ProtectionText => TurbineTripActive ? "TURBINE TRIP ACTIVE" : "NO TURBINE TRIP LATCHED";
}
