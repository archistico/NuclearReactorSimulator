namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Scenario/controller target band kept separate from the instrument and normal-operating ranges.</summary>
public sealed class ControlRoomTargetBandSnapshot
{
    public ControlRoomTargetBandSnapshot(double minimum, double maximum, string label)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }
        if (!double.IsFinite(maximum) || maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        Minimum = minimum;
        Maximum = maximum;
        Label = string.IsNullOrWhiteSpace(label) ? "TARGET" : label.Trim();
    }

    public double Minimum { get; }
    public double Maximum { get; }
    public string Label { get; }
}
