using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Temporary M4.1 external sink at the turbine-admission seam.
/// Energy removal uses committed-state specific internal energy and remains explicitly declared in plant-network audits.
/// </summary>
internal sealed class TurbineAdmissionBoundarySolver
{
    private readonly MainSteamNetworkDefinition _definition;

    public TurbineAdmissionBoundarySolver(MainSteamNetworkDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public TurbineAdmissionBoundaryStepResult Solve(PlantState committedState, MainSteamNetworkInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);

        if (!ReferenceEquals(committedState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Committed plant state does not use the main-steam network's canonical plant definition.",
                nameof(committedState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Main-steam inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var snapshots = new List<TurbineAdmissionBoundarySnapshot>(_definition.TurbineAdmissionBoundaries.Count);
        var externalMassFlowRate = MassFlowRate.Zero;
        var externalPower = Power.Zero;

        foreach (var boundary in _definition.TurbineAdmissionBoundaries)
        {
            var input = inputs.GetTurbineAdmissionBoundaryInput(boundary.Id);
            var sourceState = committedState.GetFluidNode(boundary.SourceNodeId);
            var energyExportRate = sourceState.SpecificInternalEnergy * input.MassFlowRate;
            var balance = new FluidNodeBalance(-input.MassFlowRate, -energyExportRate);
            balances[boundary.SourceNodeId] = balances.TryGetValue(boundary.SourceNodeId, out var existing)
                ? existing + balance
                : balance;
            externalMassFlowRate -= input.MassFlowRate;
            externalPower -= energyExportRate;

            snapshots.Add(new TurbineAdmissionBoundarySnapshot(
                boundary.Id,
                boundary.AdmissionTrainId,
                boundary.SourceNodeId,
                input.MassFlowRate,
                sourceState.SpecificInternalEnergy,
                energyExportRate));
        }

        return new TurbineAdmissionBoundaryStepResult(
            snapshots,
            new PlantNetworkSourceTerms(
                balances,
                new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
                externalMassFlowRate,
                externalPower));
    }
}
