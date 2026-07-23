using System.Text.Json.Serialization;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only scalar value used by control-room workspaces.</summary>
public sealed record ControlRoomValueSnapshot(
    string ValueText,
    string Unit,
    double? NumericValue,
    ControlRoomVisualState State)
{
    /// <summary>
    /// M10.9.2 presentation metadata. JsonIgnore preserves fingerprint-v1/replay compatibility because these fields are
    /// derivable display semantics rather than authoritative plant state.
    /// </summary>
    [JsonIgnore]
    public ControlRoomInstrumentScaleSnapshot? InstrumentScale { get; init; }

    [JsonIgnore]
    public ControlRoomInstrumentProvenance Provenance { get; init; } = ControlRoomInstrumentProvenance.Unspecified;

    [JsonIgnore]
    public ControlRoomInstrumentQuality Quality { get; init; } = ControlRoomInstrumentQuality.Good;

    [JsonIgnore]
    public string ProvenanceText => Provenance switch
    {
        ControlRoomInstrumentProvenance.Measured => "MEASURED",
        ControlRoomInstrumentProvenance.Model => "MODEL",
        ControlRoomInstrumentProvenance.Annunciator => "ANNUNCIATOR",
        _ => "SOURCE —",
    };

    [JsonIgnore]
    public string QualityText => Quality switch
    {
        ControlRoomInstrumentQuality.Good => "QUALITY GOOD",
        ControlRoomInstrumentQuality.Suspect => "QUALITY SUSPECT",
        _ => "UNAVAILABLE",
    };

    [JsonIgnore]
    public bool IsBelowScale => NumericValue.HasValue
        && InstrumentScale is not null
        && NumericValue.Value < InstrumentScale.Minimum;

    [JsonIgnore]
    public bool IsAboveScale => NumericValue.HasValue
        && InstrumentScale is not null
        && NumericValue.Value > InstrumentScale.Maximum;

    [JsonIgnore]
    public bool IsOffScale => IsBelowScale || IsAboveScale;

    [JsonIgnore]
    public string ScaleStatusText => InstrumentScale is null
        ? "SCALE —"
        : IsBelowScale
            ? $"< {InstrumentScale.Minimum:0.###} {Unit}".TrimEnd()
            : IsAboveScale
                ? $"> {InstrumentScale.Maximum:0.###} {Unit}".TrimEnd()
                : "ON SCALE";

    [JsonIgnore]
    public string ScaleSemanticsText
    {
        get
        {
            if (InstrumentScale is null)
            {
                return "RANGES —";
            }

            var parts = new List<string>();
            foreach (var band in InstrumentScale.OperatingBands)
            {
                parts.Add(FormattableString.Invariant(
                    $"{band.Label.ToUpperInvariant()} {band.Minimum:0.###}–{band.Maximum:0.###} {Unit}").TrimEnd());
            }

            if (InstrumentScale.TargetBand is { } target)
            {
                parts.Add(FormattableString.Invariant(
                    $"{target.Label.ToUpperInvariant()} {target.Minimum:0.###}–{target.Maximum:0.###} {Unit}").TrimEnd());
            }

            if (InstrumentScale.Setpoint is { } setpoint)
            {
                parts.Add(FormattableString.Invariant($"SET {setpoint:0.###} {Unit}").TrimEnd());
            }

            foreach (var limit in InstrumentScale.ProtectionLimits)
            {
                var comparison = limit.Direction == ControlRoomLimitDirection.High ? "≥" : "≤";
                parts.Add(FormattableString.Invariant(
                    $"{limit.Label.ToUpperInvariant()} {comparison}{limit.Threshold:0.###} {Unit}").TrimEnd());
            }

            return parts.Count == 0
                ? "NO CANONICAL OPERATING/TARGET/PROTECTION BAND"
                : string.Join(" · ", parts);
        }
    }

    public static ControlRoomValueSnapshot Unavailable(
        string unit = "",
        ControlRoomInstrumentProvenance provenance = ControlRoomInstrumentProvenance.Unspecified)
        => new("—", unit, null, ControlRoomVisualState.Unavailable)
        {
            Provenance = provenance,
            Quality = ControlRoomInstrumentQuality.Unavailable,
        };
}
