namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Application-level operator-command increments. They define how one UI intent changes controller setpoints; they are not
/// simulation timesteps, physical integration constants or hidden control laws.
/// </summary>
public sealed record ControlRoomRuntimeCommandPolicy
{
    public ControlRoomRuntimeCommandPolicy(
        double turbineSpeedSetpointIncrementRpm,
        double generatorLoadSetpointIncrementWatts)
    {
        if (!double.IsFinite(turbineSpeedSetpointIncrementRpm) || turbineSpeedSetpointIncrementRpm <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(turbineSpeedSetpointIncrementRpm));
        }

        if (!double.IsFinite(generatorLoadSetpointIncrementWatts) || generatorLoadSetpointIncrementWatts <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorLoadSetpointIncrementWatts));
        }

        TurbineSpeedSetpointIncrementRpm = turbineSpeedSetpointIncrementRpm;
        GeneratorLoadSetpointIncrementWatts = generatorLoadSetpointIncrementWatts;
    }

    public static ControlRoomRuntimeCommandPolicy Default { get; } = new(10d, 5_000_000d);

    public double TurbineSpeedSetpointIncrementRpm { get; }

    public double GeneratorLoadSetpointIncrementWatts { get; }
}
