using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Resolves the accepted open-control-volume advective energy convention without mutating plant state.
/// Node inventories remain mass plus internal energy. Advective transport is diagnosed as enthalpy
/// h = u + p/rho, while shaft work, heat transfer and external boundary power remain separate terms.
/// </summary>
public sealed class OpenControlVolumeEnergyTransportSolver
{
    public OpenControlVolumeEnergyTransportResult Solve(
        FluidNodeState fromNode,
        FluidNodeState toNode,
        MassFlowRate referenceMassFlowRate)
    {
        ArgumentNullException.ThrowIfNull(fromNode);
        ArgumentNullException.ThrowIfNull(toNode);

        if (string.Equals(fromNode.Id, toNode.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Open-control-volume energy transport requires distinct fluid-node identities.",
                nameof(toNode));
        }

        var upstream = referenceMassFlowRate.KilogramsPerSecond >= 0d ? fromNode : toNode;
        var downstream = ReferenceEquals(upstream, fromNode) ? toNode : fromNode;
        var density = upstream.Density;

        if (density <= Density.Zero)
        {
            throw new ArithmeticException(
                $"Upstream fluid node '{upstream.Id}' has non-positive density.");
        }

        var specificFlowWork = SpecificEnergy.FromJoulesPerKilogram(
            upstream.Pressure.Pascals / density.KilogramsPerCubicMetre);
        var specificEnthalpy = SpecificEnergy.FromJoulesPerKilogram(
            upstream.SpecificInternalEnergy.JoulesPerKilogram
            + specificFlowWork.JoulesPerKilogram);

        var internalEnergyRate = upstream.SpecificInternalEnergy * referenceMassFlowRate;
        var flowWorkRate = specificFlowWork * referenceMassFlowRate;
        var enthalpyRate = specificEnthalpy * referenceMassFlowRate;

        var legacyFrom = new FluidNodeBalance(-referenceMassFlowRate, -internalEnergyRate);
        var legacyTo = new FluidNodeBalance(referenceMassFlowRate, internalEnergyRate);
        var enthalpyFrom = new FluidNodeBalance(-referenceMassFlowRate, -enthalpyRate);
        var enthalpyTo = new FluidNodeBalance(referenceMassFlowRate, enthalpyRate);

        return new OpenControlVolumeEnergyTransportResult(
            fromNode.Id,
            toNode.Id,
            upstream.Id,
            downstream.Id,
            referenceMassFlowRate,
            upstream.Pressure,
            density,
            upstream.SpecificInternalEnergy,
            specificFlowWork,
            specificEnthalpy,
            internalEnergyRate,
            flowWorkRate,
            enthalpyRate,
            legacyFrom,
            legacyTo,
            enthalpyFrom,
            enthalpyTo);
    }
}
