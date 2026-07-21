namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Describes how mechanical position maps to normalized hydraulic flow capacity.
/// </summary>
public sealed record ValveCharacteristic
{
    private ValveCharacteristic(ValveCharacteristicKind kind, double rangeability)
    {
        Kind = kind;
        Rangeability = rangeability;
    }

    public ValveCharacteristicKind Kind { get; }

    /// <summary>
    /// Equal-percentage rangeability. It is 1 for characteristics that do not use it.
    /// </summary>
    public double Rangeability { get; }

    public static ValveCharacteristic Linear { get; } = new(ValveCharacteristicKind.Linear, 1d);

    public static ValveCharacteristic QuickOpening { get; } = new(ValveCharacteristicKind.QuickOpening, 1d);

    public static ValveCharacteristic EqualPercentage(double rangeability = 50d)
    {
        if (!double.IsFinite(rangeability) || rangeability <= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(rangeability), rangeability, "Equal-percentage rangeability must be finite and greater than one.");
        }

        return new ValveCharacteristic(ValveCharacteristicKind.EqualPercentage, rangeability);
    }
}
