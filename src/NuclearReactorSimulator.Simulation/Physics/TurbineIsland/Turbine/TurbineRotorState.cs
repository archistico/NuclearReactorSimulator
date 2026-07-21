using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Immutable committed mechanical state for one lumped turbine rotor.
/// Trip/protection state is deliberately not latched here in M4.2.
/// </summary>
public sealed record TurbineRotorState
{
    public TurbineRotorState(string rotorId, AngularSpeed angularSpeed)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Turbine rotor-state id cannot be empty or whitespace.", nameof(rotorId));
        }

        RotorId = rotorId.Trim();
        AngularSpeed = angularSpeed;
    }

    public string RotorId { get; }

    public AngularSpeed AngularSpeed { get; }
}
