using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

/// <summary>Typed Application-layer intent for selecting a bounded M10.6 supervisory objective.</summary>
public sealed record SupervisoryObjectiveRequest
{
    private SupervisoryObjectiveRequest(
        SupervisoryOperatingObjectiveKind kind,
        bool captureCurrentOperatingPoint,
        double? reactorPowerSetpointWatts,
        double? turbineSpeedSetpointRpm)
    {
        Kind = kind;
        CaptureCurrentOperatingPoint = captureCurrentOperatingPoint;
        ReactorPowerSetpointWatts = reactorPowerSetpointWatts;
        TurbineSpeedSetpointRpm = turbineSpeedSetpointRpm;
    }

    public SupervisoryOperatingObjectiveKind Kind { get; }
    public bool CaptureCurrentOperatingPoint { get; }
    public double? ReactorPowerSetpointWatts { get; }
    public double? TurbineSpeedSetpointRpm { get; }

    public static SupervisoryObjectiveRequest HoldCurrentOperatingPoint()
        => new(SupervisoryOperatingObjectiveKind.HoldOperatingPoint, true, null, null);

    public static SupervisoryObjectiveRequest HoldReactorPower(double watts)
    {
        Validate(watts, nameof(watts));
        return new SupervisoryObjectiveRequest(SupervisoryOperatingObjectiveKind.HoldReactorPower, false, watts, null);
    }

    public static SupervisoryObjectiveRequest HoldTurbineSpeed(double rpm)
    {
        Validate(rpm, nameof(rpm));
        return new SupervisoryObjectiveRequest(SupervisoryOperatingObjectiveKind.HoldTurbineSpeed, false, null, rpm);
    }

    private static void Validate(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Supervisory objective requests must be finite and non-negative.");
        }
    }
}
