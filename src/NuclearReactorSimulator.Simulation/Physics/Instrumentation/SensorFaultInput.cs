using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Explicit deterministic sensor-fault input. Scenario ownership is intentionally deferred to M8.</summary>
public sealed record SensorFaultInput
{
    public SensorFaultInput(string channelId, SensorFaultMode mode, double biasEngineeringUnits = 0d)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Sensor-fault channel id cannot be empty or whitespace.", nameof(channelId));
        }

        if (!double.IsFinite(biasEngineeringUnits))
        {
            throw new ArgumentOutOfRangeException(nameof(biasEngineeringUnits), biasEngineeringUnits, "Sensor bias must be finite.");
        }

        if (mode != SensorFaultMode.Bias && biasEngineeringUnits != 0d)
        {
            throw new ArgumentException("A non-zero sensor bias is only valid for Bias fault mode.", nameof(biasEngineeringUnits));
        }

        ChannelId = channelId.Trim();
        Mode = mode;
        BiasEngineeringUnits = biasEngineeringUnits;
    }

    public string ChannelId { get; }

    public SensorFaultMode Mode { get; }

    public double BiasEngineeringUnits { get; }

    public static SensorFaultInput Healthy(string channelId) => new(channelId, SensorFaultMode.None);
}
