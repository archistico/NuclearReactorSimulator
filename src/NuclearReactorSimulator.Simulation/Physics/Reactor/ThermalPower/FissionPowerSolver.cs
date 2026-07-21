using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;

/// <summary>
/// Deterministically maps normalized neutron population to instantaneous fission thermal power
/// and partitions that power across configured heat-deposition destinations.
/// </summary>
public sealed class FissionPowerSolver
{
    private readonly FissionPowerDefinition _definition;

    public FissionPowerSolver(FissionPowerDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
    }

    public FissionPowerDefinition Definition => _definition;

    public FissionPowerSnapshot Solve(NeutronPopulation neutronPopulation)
    {
        var calibration = _definition.Calibration;
        var scale = neutronPopulation.Relative / calibration.ReferenceNeutronPopulation.Relative;
        var totalWatts = calibration.ReferenceThermalPower.Watts * scale;

        if (!double.IsFinite(totalWatts) || totalWatts < 0d)
        {
            throw new FissionPowerNumericalException(
                $"Fission-power scaling produced invalid thermal power {totalWatts:R} W for neutron population {neutronPopulation.Relative:R}.");
        }

        var totalPower = Power.FromWatts(totalWatts);
        var depositions = Allocate(totalPower);

        return new FissionPowerSnapshot(
            _definition.Id,
            neutronPopulation,
            totalPower,
            depositions);
    }

    private IReadOnlyList<FissionHeatDeposition> Allocate(Power totalPower)
    {
        var destinations = _definition.HeatDestinations;
        var result = new FissionHeatDeposition[destinations.Count];
        var allocatedWatts = 0d;

        for (var index = 0; index < destinations.Count; index++)
        {
            var destination = destinations[index];
            double watts;

            if (index == destinations.Count - 1)
            {
                watts = totalPower.Watts - allocatedWatts;
            }
            else
            {
                var normalizedFraction = destination.Fraction.Fraction / _definition.HeatDestinationFractionSum;
                watts = totalPower.Watts * normalizedFraction;
                allocatedWatts += watts;
            }

            if (!double.IsFinite(watts) || watts < 0d)
            {
                throw new FissionPowerNumericalException(
                    $"Fission-heat allocation for target '{destination.TargetDomainId}' produced invalid power {watts:R} W.");
            }

            result[index] = new FissionHeatDeposition(
                destination.TargetDomainId,
                Power.FromWatts(watts));
        }

        return result;
    }
}
