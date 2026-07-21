using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;

/// <summary>
/// Maps aggregated zone fission power onto equivalent fuel-channel groups and emits staged source terms.
/// Hydraulic flow is observed through the canonical passive pipe from the same committed plant state;
/// the plant network orchestrator remains the only integration boundary.
/// </summary>
public sealed class FuelChannelGroupSolver
{
    private readonly FuelChannelGroupSetDefinition _definition;
    private readonly PipeFlowSolver _pipeFlowSolver;
    private readonly WaterSteamVoidFractionSolver _voidFractionSolver;

    public FuelChannelGroupSolver(FuelChannelGroupSetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _pipeFlowSolver = new PipeFlowSolver();
        _voidFractionSolver = new WaterSteamVoidFractionSolver(new SimplifiedWaterSteamThermodynamicModel());
    }

    public FuelChannelGroupSetDefinition Definition => _definition;

    public FuelChannelGroupStepResult Solve(
        AggregatedCoreSnapshot coreSnapshot,
        PlantState committedPlantState)
        => Solve(coreSnapshot, Power.Zero, committedPlantState);

    public FuelChannelGroupStepResult Solve(
        AggregatedCoreSnapshot coreSnapshot,
        Power totalDecayHeatPower,
        PlantState committedPlantState)
    {
        ArgumentNullException.ThrowIfNull(coreSnapshot);
        ArgumentNullException.ThrowIfNull(committedPlantState);

        if (totalDecayHeatPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDecayHeatPower), totalDecayHeatPower, "Total decay-heat power cannot be negative.");
        }

        if (!ReferenceEquals(coreSnapshot.Definition, _definition.CoreDefinition))
        {
            throw new ArgumentException("Aggregated-core snapshot does not use this channel-group definition's canonical core definition.", nameof(coreSnapshot));
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.CoreDefinition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the channel-group definition's canonical plant definition.", nameof(committedPlantState));
        }

        var plant = committedPlantState.Definition;
        var snapshots = new List<FuelChannelGroupSnapshot>(_definition.Groups.Count);
        var fluidBalances = plant.FluidNodes.ToDictionary(
            static node => node.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var thermalBalances = plant.ThermalBodies.ToDictionary(
            static body => body.Id,
            static _ => ThermalEnergyBalance.Zero,
            StringComparer.Ordinal);

        var allocatedCoreDecayWatts = 0d;
        for (var zoneIndex = 0; zoneIndex < _definition.CoreDefinition.Zones.Count; zoneIndex++)
        {
            var zone = _definition.CoreDefinition.Zones[zoneIndex];
            var zoneSnapshot = coreSnapshot.GetZone(zone.Id);
            var groups = _definition.Groups
                .Where(group => string.Equals(group.ZoneId, zone.Id, StringComparison.Ordinal))
                .OrderBy(static group => group.Id, StringComparer.Ordinal)
                .ToArray();

            var zoneDecayWatts = zoneIndex == _definition.CoreDefinition.Zones.Count - 1
                ? totalDecayHeatPower.Watts - allocatedCoreDecayWatts
                : totalDecayHeatPower.Watts * zoneSnapshot.PowerFraction.Fraction;
            if (zoneIndex != _definition.CoreDefinition.Zones.Count - 1)
            {
                allocatedCoreDecayWatts += zoneDecayWatts;
            }
            var allocatedZoneFissionWatts = 0d;
            var allocatedZoneDecayWatts = 0d;
            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var groupFissionWatts = index == groups.Length - 1
                    ? zoneSnapshot.FissionThermalPower.Watts - allocatedZoneFissionWatts
                    : zoneSnapshot.FissionThermalPower.Watts * group.ZonePowerFraction.Fraction;
                var groupDecayWatts = index == groups.Length - 1
                    ? zoneDecayWatts - allocatedZoneDecayWatts
                    : zoneDecayWatts * group.ZonePowerFraction.Fraction;

                if (index != groups.Length - 1)
                {
                    allocatedZoneFissionWatts += groupFissionWatts;
                    allocatedZoneDecayWatts += groupDecayWatts;
                }

                var groupTotalWatts = groupFissionWatts + groupDecayWatts;
                if (!double.IsFinite(groupFissionWatts) || !double.IsFinite(groupDecayWatts) || !double.IsFinite(groupTotalWatts)
                    || groupFissionWatts < 0d || groupDecayWatts < 0d || groupTotalWatts < 0d)
                {
                    throw new InvalidOperationException(
                        $"Fuel-channel group '{group.Id}' nuclear heat allocation produced an invalid result.");
                }

                var groupFissionPower = Power.FromWatts(groupFissionWatts);
                var groupDecayPower = Power.FromWatts(groupDecayWatts);
                var groupTotalPower = Power.FromWatts(groupTotalWatts);
                var pipe = plant.GetPipe(group.HydraulicPipeId);
                var inlet = committedPlantState.GetFluidNode(group.InletCoolantNodeId);
                var outlet = committedPlantState.GetFluidNode(group.OutletCoolantNodeId);
                var flow = _pipeFlowSolver.Solve(pipe, inlet, outlet);

                var (fuelPower, structurePower, coolantPower) = AllocateHeat(group, groupTotalPower);
                thermalBalances[group.FuelThermalBodyId] += new ThermalEnergyBalance(fuelPower);
                thermalBalances[group.StructureThermalBodyId] += new ThermalEnergyBalance(structurePower);
                fluidBalances[group.OutletCoolantNodeId] += new FluidNodeBalance(MassFlowRate.Zero, coolantPower);

                snapshots.Add(new FuelChannelGroupSnapshot(
                    group.Id,
                    group.ZoneId,
                    group.RepresentedChannelCount,
                    group.ZonePowerFraction,
                    groupFissionPower,
                    groupDecayPower,
                    groupTotalPower,
                    groupFissionPower / group.RepresentedChannelCount,
                    groupTotalPower / group.RepresentedChannelCount,
                    flow.MassFlowRate,
                    flow.MassFlowRate / group.RepresentedChannelCount,
                    flow.PressureDifference,
                    inlet.Temperature,
                    outlet.Temperature,
                    inlet.Pressure,
                    outlet.Pressure,
                    outlet.Phase,
                    outlet.VaporQuality,
                    ResolveVoidFraction(outlet),
                    fuelPower,
                    structurePower,
                    coolantPower));
            }
        }

        var nonZeroFluidBalances = fluidBalances
            .Where(static entry => entry.Value != FluidNodeBalance.Zero)
            .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal);
        var nonZeroThermalBalances = thermalBalances
            .Where(static entry => entry.Value != ThermalEnergyBalance.Zero)
            .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal);

        var snapshot = new FuelChannelGroupSetSnapshot(
            _definition.Id,
            coreSnapshot.TotalFissionThermalPower,
            totalDecayHeatPower,
            snapshots);
        var sourceTerms = new PlantNetworkSourceTerms(
            nonZeroFluidBalances,
            nonZeroThermalBalances,
            coreSnapshot.TotalFissionThermalPower + totalDecayHeatPower);

        return new FuelChannelGroupStepResult(snapshot, sourceTerms);
    }

    private static (Power Fuel, Power Structure, Power Coolant) AllocateHeat(
        FuelChannelGroupDefinition group,
        Power totalPower)
    {
        var fuelWatts = totalPower.Watts * group.FuelHeatFraction.Fraction;
        var structureWatts = totalPower.Watts * group.StructureHeatFraction.Fraction;
        var coolantWatts = totalPower.Watts - fuelWatts - structureWatts;

        if (!double.IsFinite(fuelWatts) || !double.IsFinite(structureWatts) || !double.IsFinite(coolantWatts)
            || fuelWatts < 0d || structureWatts < 0d || coolantWatts < 0d)
        {
            throw new InvalidOperationException($"Fuel-channel group '{group.Id}' heat allocation produced an invalid result.");
        }

        return (Power.FromWatts(fuelWatts), Power.FromWatts(structureWatts), Power.FromWatts(coolantWatts));
    }

    private VoidFraction? ResolveVoidFraction(FluidNodeState coolant)
        => coolant.Phase == FluidPhase.Unspecified
            ? null
            : _voidFractionSolver.Resolve(coolant.Thermodynamics);
}
