using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// M4.2 manually commanded mechanical load and explicit trip seam.
/// Overspeed indication is diagnostic only and does not automatically assert this command.
/// </summary>
public sealed record TurbineRotorInput
{
    public TurbineRotorInput(string rotorId, Torque externalLoadTorque, bool tripCommand = false)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Turbine rotor input id cannot be empty or whitespace.", nameof(rotorId));
        }

        if (externalLoadTorque < Torque.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(externalLoadTorque), externalLoadTorque, "External turbine load torque cannot be negative.");
        }

        RotorId = rotorId.Trim();
        ExternalLoadTorque = externalLoadTorque;
        TripCommand = tripCommand;
    }

    public string RotorId { get; }

    public Torque ExternalLoadTorque { get; }

    public bool TripCommand { get; }
}
