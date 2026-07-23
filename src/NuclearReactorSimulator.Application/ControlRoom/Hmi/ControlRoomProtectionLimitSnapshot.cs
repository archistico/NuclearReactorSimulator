namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Presentation-only protection threshold. It does not own or recompute protection logic.</summary>
public sealed class ControlRoomProtectionLimitSnapshot
{
    public ControlRoomProtectionLimitSnapshot(
        double threshold,
        ControlRoomLimitDirection direction,
        string label)
    {
        if (!double.IsFinite(threshold))
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        Threshold = threshold;
        Direction = direction;
        Label = string.IsNullOrWhiteSpace(label) ? "PROTECTION" : label.Trim();
    }

    public double Threshold { get; }
    public ControlRoomLimitDirection Direction { get; }
    public string Label { get; }
}
