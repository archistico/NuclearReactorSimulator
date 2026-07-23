namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Immutable presentation band inside an instrument scale.</summary>
public sealed class ControlRoomInstrumentBandSnapshot
{
    public ControlRoomInstrumentBandSnapshot(
        double minimum,
        double maximum,
        ControlRoomInstrumentBandKind kind,
        string label)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }
        if (!double.IsFinite(maximum) || maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Minimum = minimum;
        Maximum = maximum;
        Kind = kind;
        Label = string.IsNullOrWhiteSpace(label) ? kind.ToString() : label.Trim();
    }

    public double Minimum { get; }
    public double Maximum { get; }
    public ControlRoomInstrumentBandKind Kind { get; }
    public string Label { get; }
}
