using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core;

/// <summary>
/// Deterministically partitions global fission thermal power across aggregated zones and captures
/// local committed-state diagnostics. The solver does not alter point kinetics or plant inventories.
/// </summary>
public sealed class AggregatedCorePowerSolver
{
    private readonly AggregatedCoreDefinition _definition;
    private readonly WaterSteamVoidFractionSolver _voidFractionSolver;

    public AggregatedCorePowerSolver(AggregatedCoreDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _voidFractionSolver = new WaterSteamVoidFractionSolver(new SimplifiedWaterSteamThermodynamicModel());
    }

    public AggregatedCoreDefinition Definition => _definition;

    public AggregatedCoreSnapshot Solve(
        AggregatedCoreState coreState,
        Power totalFissionThermalPower,
        PlantState committedPlantState)
    {
        ArgumentNullException.ThrowIfNull(coreState);
        ArgumentNullException.ThrowIfNull(committedPlantState);

        if (totalFissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalFissionThermalPower), totalFissionThermalPower, "Global fission thermal power cannot be negative.");
        }

        if (!ReferenceEquals(coreState.Definition, _definition))
        {
            throw new ArgumentException("Aggregated-core state does not use this solver's canonical definition.", nameof(coreState));
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the core definition's canonical plant definition.", nameof(committedPlantState));
        }

        var snapshots = new CoreZoneSnapshot[_definition.Zones.Count];
        var allocatedWatts = 0d;

        for (var index = 0; index < _definition.Zones.Count; index++)
        {
            var zoneDefinition = _definition.Zones[index];
            var zoneState = coreState.GetZone(zoneDefinition.Id);
            double zoneWatts;

            if (index == _definition.Zones.Count - 1)
            {
                zoneWatts = totalFissionThermalPower.Watts - allocatedWatts;
            }
            else
            {
                var normalizedFraction = zoneState.PowerFraction.Fraction / coreState.PowerFractionSum;
                zoneWatts = totalFissionThermalPower.Watts * normalizedFraction;
                allocatedWatts += zoneWatts;
            }

            if (!double.IsFinite(zoneWatts) || zoneWatts < 0d)
            {
                throw new InvalidOperationException(
                    $"Core-zone '{zoneDefinition.Id}' power allocation produced invalid power {zoneWatts:R} W.");
            }

            var fuel = committedPlantState.GetThermalBody(zoneDefinition.FuelThermalBodyId);
            var structure = committedPlantState.GetThermalBody(zoneDefinition.StructureThermalBodyId);
            var coolant = committedPlantState.GetFluidNode(zoneDefinition.CoolantFluidNodeId);

            snapshots[index] = new CoreZoneSnapshot(
                zoneDefinition.Id,
                zoneDefinition.Coordinate,
                zoneState.PowerFraction,
                Power.FromWatts(zoneWatts),
                fuel.Temperature,
                structure.Temperature,
                coolant.Temperature,
                coolant.Pressure,
                coolant.Phase,
                coolant.VaporQuality,
                ResolveVoidFraction(coolant));
        }

        return new AggregatedCoreSnapshot(_definition, totalFissionThermalPower, snapshots);
    }

    private VoidFraction? ResolveVoidFraction(FluidNodeState coolant)
    {
        return coolant.Phase == FluidPhase.Unspecified
            ? null
            : _voidFractionSolver.Resolve(coolant.Thermodynamics);
    }
}
