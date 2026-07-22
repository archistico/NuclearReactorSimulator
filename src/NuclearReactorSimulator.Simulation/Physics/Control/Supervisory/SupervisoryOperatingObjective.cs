using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;

/// <summary>
/// Immutable high-level supervisory objective. Targets are controller setpoints only; they are never direct physical-state assignments.
/// </summary>
public sealed record SupervisoryOperatingObjective
{
    private SupervisoryOperatingObjective(
        SupervisoryOperatingObjectiveKind kind,
        double? reactorPowerSetpointWatts,
        double? turbineSpeedSetpointRpm)
    {
        Kind = kind;
        ReactorPowerSetpointWatts = reactorPowerSetpointWatts;
        TurbineSpeedSetpointRpm = turbineSpeedSetpointRpm;
    }

    public SupervisoryOperatingObjectiveKind Kind { get; }
    public double? ReactorPowerSetpointWatts { get; }
    public double? TurbineSpeedSetpointRpm { get; }

    public static SupervisoryOperatingObjective HoldReactorPower(double watts)
    {
        ValidateNonNegativeFinite(watts, nameof(watts));
        return new SupervisoryOperatingObjective(SupervisoryOperatingObjectiveKind.HoldReactorPower, watts, null);
    }

    public static SupervisoryOperatingObjective HoldTurbineSpeed(double rpm)
    {
        ValidateNonNegativeFinite(rpm, nameof(rpm));
        return new SupervisoryOperatingObjective(SupervisoryOperatingObjectiveKind.HoldTurbineSpeed, null, rpm);
    }

    public static SupervisoryOperatingObjective HoldOperatingPoint(double reactorPowerWatts, double turbineSpeedRpm)
    {
        ValidateNonNegativeFinite(reactorPowerWatts, nameof(reactorPowerWatts));
        ValidateNonNegativeFinite(turbineSpeedRpm, nameof(turbineSpeedRpm));
        return new SupervisoryOperatingObjective(
            SupervisoryOperatingObjectiveKind.HoldOperatingPoint,
            reactorPowerWatts,
            turbineSpeedRpm);
    }

    private static void ValidateNonNegativeFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Supervisory objective targets must be finite and non-negative.");
        }
    }
}
