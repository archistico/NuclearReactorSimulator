using System.Text.Json.Serialization;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>UI-safe canonical rod or rod-group command target.</summary>
public sealed record ReactorRodTargetPresentationSnapshot(
    string TargetId,
    ControlRoomCommandTargetKind TargetKind)
{
    public string Label => TargetKind == ControlRoomCommandTargetKind.ControlRodGroup
        ? $"GROUP · {TargetId}"
        : $"ROD · {TargetId}";

    /// <summary>
    /// Presentation-only effective motion for the selected canonical target. Group targets report MIXED when members differ.
    /// This diagnostic is excluded from fingerprint-v1 serialization.
    /// </summary>
    [JsonIgnore]
    public string EffectiveMotion { get; init; } = "UNAVAILABLE";
}
