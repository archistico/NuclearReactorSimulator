namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>Canonical deterministic P/PI/PID definition over one measured-signal channel.</summary>
public sealed class PidControllerDefinition
{
    public PidControllerDefinition(
        string id,
        string measurementChannelId,
        ControllerAlgorithmKind algorithm,
        double proportionalGain,
        double integralGainPerSecond,
        double derivativeGainSeconds,
        ControllerOutputRange outputRange)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Controller id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Measurement-channel id cannot be empty or whitespace.", nameof(measurementChannelId));
        }

        if (!Enum.IsDefined(typeof(ControllerAlgorithmKind), algorithm))
        {
            throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown controller algorithm.");
        }

        if (!double.IsFinite(proportionalGain) || !double.IsFinite(integralGainPerSecond) || !double.IsFinite(derivativeGainSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(proportionalGain), "Controller gains must be finite.");
        }

        if (algorithm == ControllerAlgorithmKind.Proportional && (integralGainPerSecond != 0d || derivativeGainSeconds != 0d))
        {
            throw new ArgumentException("A proportional-only controller must have zero integral and derivative gains.");
        }

        if (algorithm == ControllerAlgorithmKind.ProportionalIntegral && derivativeGainSeconds != 0d)
        {
            throw new ArgumentException("A PI controller must have zero derivative gain.");
        }

        if (!double.IsFinite(outputRange.Minimum) || !double.IsFinite(outputRange.Maximum) || outputRange.Maximum <= outputRange.Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(outputRange), outputRange, "Controller output range must be valid.");
        }

        Id = id.Trim();
        MeasurementChannelId = measurementChannelId.Trim();
        Algorithm = algorithm;
        ProportionalGain = proportionalGain;
        IntegralGainPerSecond = integralGainPerSecond;
        DerivativeGainSeconds = derivativeGainSeconds;
        OutputRange = outputRange;
    }

    public string Id { get; }
    public string MeasurementChannelId { get; }
    public ControllerAlgorithmKind Algorithm { get; }
    public double ProportionalGain { get; }
    public double IntegralGainPerSecond { get; }
    public double DerivativeGainSeconds { get; }
    public ControllerOutputRange OutputRange { get; }
}
