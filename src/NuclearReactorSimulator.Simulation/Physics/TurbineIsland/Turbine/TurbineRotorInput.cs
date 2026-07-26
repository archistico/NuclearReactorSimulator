using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// M4.2 manually commanded mechanical load and explicit trip seam.
/// Overspeed indication is diagnostic only and does not automatically assert this command.
/// </summary>
public sealed record TurbineRotorInput
{
    public TurbineRotorInput(string rotorId, Torque externalLoadTorque, bool tripCommand = false)
        : this(rotorId, externalLoadTorque, tripCommand, allowSignedGeneratorGridTorque: false)
    {
    }

    private TurbineRotorInput(
        string rotorId,
        Torque externalLoadTorque,
        bool tripCommand,
        bool allowSignedGeneratorGridTorque)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Turbine rotor input id cannot be empty or whitespace.", nameof(rotorId));
        }

        if (!allowSignedGeneratorGridTorque && externalLoadTorque < Torque.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(externalLoadTorque), externalLoadTorque, "External turbine load torque cannot be negative.");
        }

        RotorId = rotorId.Trim();
        ExternalLoadTorque = externalLoadTorque;
        TripCommand = tripCommand;
    }

    /// <summary>
    /// Internal generator/grid-owned seam for signed electromagnetic torque. The public manual M4.2 input
    /// remains non-negative, while bidirectional infinite-bus coupling may assist the rotor during motoring.
    /// </summary>
    internal static TurbineRotorInput CreateGeneratorGridCoupled(
        string rotorId,
        Torque signedElectromagneticTorque,
        bool tripCommand = false)
        => new(rotorId, signedElectromagneticTorque, tripCommand, allowSignedGeneratorGridTorque: true);

    public string RotorId { get; }

    public Torque ExternalLoadTorque { get; }

    public bool TripCommand { get; }
}
