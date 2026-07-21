using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Solves a memoryless bidirectional passive pipe using a lumped quadratic pressure-loss relation.
/// </summary>
public sealed class PipeFlowSolver
{
    public PipeFlowResult Solve(
        PipeDefinition pipe,
        FluidNodeState fromNode,
        FluidNodeState toNode)
    {
        return Solve(pipe, fromNode, toNode, PressureDifference.Zero);
    }

    internal PipeFlowResult Solve(
        PipeDefinition pipe,
        FluidNodeState fromNode,
        FluidNodeState toNode,
        PressureDifference additionalDrivingPressure)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentNullException.ThrowIfNull(fromNode);
        ArgumentNullException.ThrowIfNull(toNode);

        ValidateEndpoints(pipe, fromNode, toNode);

        var nodePressureDifference = fromNode.Pressure - toNode.Pressure;
        var drivingPressure = nodePressureDifference + additionalDrivingPressure;
        if (drivingPressure == PressureDifference.Zero)
        {
            return new PipeFlowResult(
                nodePressureDifference,
                MassFlowRate.Zero,
                Power.Zero);
        }

        var squaredMassFlow = Math.Abs(drivingPressure.Pascals)
            / pipe.Resistance.PascalSecondsSquaredPerKilogramSquared;

        if (!double.IsFinite(squaredMassFlow))
        {
            throw new ArithmeticException($"Pipe '{pipe.Id}' flow calculation produced a non-finite squared mass flow.");
        }

        var massFlowMagnitude = Math.Sqrt(squaredMassFlow);
        var signedMassFlow = drivingPressure.Pascals > 0d
            ? massFlowMagnitude
            : -massFlowMagnitude;

        var massFlowRate = MassFlowRate.FromKilogramsPerSecond(signedMassFlow);
        var upstreamSpecificInternalEnergy = signedMassFlow > 0d
            ? fromNode.SpecificInternalEnergy
            : toNode.SpecificInternalEnergy;
        var internalEnergyFlowRate = upstreamSpecificInternalEnergy * massFlowRate;

        return new PipeFlowResult(
            nodePressureDifference,
            massFlowRate,
            internalEnergyFlowRate);
    }

    private static void ValidateEndpoints(
        PipeDefinition pipe,
        FluidNodeState fromNode,
        FluidNodeState toNode)
    {
        if (!string.Equals(pipe.FromNodeId, fromNode.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Pipe '{pipe.Id}' expects from-node '{pipe.FromNodeId}', but received '{fromNode.Id}'.",
                nameof(fromNode));
        }

        if (!string.Equals(pipe.ToNodeId, toNode.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Pipe '{pipe.Id}' expects to-node '{pipe.ToNodeId}', but received '{toNode.Id}'.",
                nameof(toNode));
        }
    }
}
