using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>
/// Persistent normal operator request for a turbine stop/isolation valve. Protection remains a later arbitration layer
/// and can therefore force the effective position closed without erasing the operator's requested lineup.
/// </summary>
public sealed record TurbineIsolationValveCommand
{
    public TurbineIsolationValveCommand(
        string valveId,
        ValvePosition requestedPosition,
        ActuatorTravelRate? travelRate)
    {
        if (string.IsNullOrWhiteSpace(valveId))
        {
            throw new ArgumentException("Isolation-valve id cannot be empty or whitespace.", nameof(valveId));
        }

        ValveId = valveId.Trim();
        RequestedPosition = requestedPosition;
        TravelRate = travelRate;
    }

    public string ValveId { get; }
    public ValvePosition RequestedPosition { get; }
    public ActuatorTravelRate? TravelRate { get; }
}
