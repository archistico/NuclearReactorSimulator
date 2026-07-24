using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// M4.2 manually commanded mechanical load and explicit trip seam.
/// Positive external load torque resists rotor rotation. The public M4.2 seam remains non-negative.
/// E.2 generator/grid integration may internally supply signed electromagnetic torque so negative torque
/// represents grid motoring that assists rotor rotation.
/// Overspeed indication is diagnostic only and does not automatically assert this command.
/// </summary>
public sealed record TurbineRotorInput
{
    public TurbineRotorInput(string rotorId, Torque externalLoadTorque, bool tripCommand = false)
        : this(rotorId, externalLoadTorque, tripCommand, allowSignedElectromagneticTorque: false)
    {
    }

    private TurbineRotorInput(
        string rotorId,
        Torque externalLoadTorque,
        bool tripCommand,
        bool allowSignedElectromagneticTorque)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Turbine rotor input id cannot be empty or whitespace.", nameof(rotorId));
        }

        if (!allowSignedElectromagneticTorque && externalLoadTorque < Torque.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(externalLoadTorque), externalLoadTorque, "External turbine load torque cannot be negative.");
        }

        RotorId = rotorId.Trim();
        ExternalLoadTorque = externalLoadTorque;
        TripCommand = tripCommand;
    }

    internal static TurbineRotorInput FromSignedElectromagneticTorque(
        string rotorId,
        Torque electromagneticTorque,
        bool tripCommand = false)
        => new(rotorId, electromagneticTorque, tripCommand, allowSignedElectromagneticTorque: true);

    public string RotorId { get; }

    /// <summary>
    /// Positive values oppose rotor rotation. Negative values are reserved for the internal E.2
    /// bidirectional generator/grid seam and represent motoring torque supplied by the grid.
    /// </summary>
    public Torque ExternalLoadTorque { get; }

    public bool TripCommand { get; }
}
