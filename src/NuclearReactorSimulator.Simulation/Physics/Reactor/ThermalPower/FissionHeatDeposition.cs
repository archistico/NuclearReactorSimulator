using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;

/// <summary>
/// Instantaneous non-negative fission heat assigned to one named thermal destination.
/// </summary>
public sealed record FissionHeatDeposition
{
    public FissionHeatDeposition(string targetDomainId, Power thermalPower)
    {
        if (string.IsNullOrWhiteSpace(targetDomainId))
        {
            throw new ArgumentException("Fission-heat target-domain id cannot be empty.", nameof(targetDomainId));
        }

        if (thermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(thermalPower), thermalPower, "Fission-heat deposition power cannot be negative.");
        }

        TargetDomainId = targetDomainId;
        ThermalPower = thermalPower;
    }

    public string TargetDomainId { get; }

    public Power ThermalPower { get; }

    public ThermalEnergyBalance ToThermalEnergyBalance() => new(ThermalPower);

    public FluidNodeBalance ToFluidNodeBalance() => new(MassFlowRate.Zero, ThermalPower);
}
