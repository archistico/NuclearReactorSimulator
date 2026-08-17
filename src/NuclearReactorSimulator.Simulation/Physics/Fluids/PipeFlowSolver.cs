using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Solves a memoryless bidirectional passive pipe using a lumped quadratic pressure-loss relation.
/// The pipe definition selects either the historical internal-energy advection convention or
/// the Phase G open-control-volume enthalpy convention for endpoint energy balances.
/// </summary>
public sealed class PipeFlowSolver
{
    private readonly OpenControlVolumeEnergyTransportSolver _energyTransportSolver;

    public PipeFlowSolver()
        : this(new OpenControlVolumeEnergyTransportSolver())
    {
    }

    internal PipeFlowSolver(OpenControlVolumeEnergyTransportSolver energyTransportSolver)
    {
        _energyTransportSolver = energyTransportSolver
            ?? throw new ArgumentNullException(nameof(energyTransportSolver));
    }

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
            return ZeroFlow(nodePressureDifference, pipe.EnergyTransportMode);
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
        var transport = _energyTransportSolver.Solve(fromNode, toNode, massFlowRate);
        var advectedEnergyFlowRate = pipe.EnergyTransportMode switch
        {
            FluidEnergyTransportMode.SpecificInternalEnergy => transport.SignedInternalEnergyAdvectionRate,
            FluidEnergyTransportMode.SpecificEnthalpy => transport.SignedEnthalpyTransportRate,
            _ => throw new InvalidOperationException(
                $"Pipe '{pipe.Id}' uses unsupported energy-transport mode '{pipe.EnergyTransportMode}'."),
        };

        return new PipeFlowResult(
            nodePressureDifference,
            massFlowRate,
            pipe.EnergyTransportMode,
            transport.SignedInternalEnergyAdvectionRate,
            transport.SignedFlowWorkRate,
            transport.SignedEnthalpyTransportRate,
            advectedEnergyFlowRate);
    }

    private static PipeFlowResult ZeroFlow(
        PressureDifference pressureDifference,
        FluidEnergyTransportMode energyTransportMode)
        => new(
            pressureDifference,
            MassFlowRate.Zero,
            energyTransportMode,
            Power.Zero,
            Power.Zero,
            Power.Zero,
            Power.Zero);

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
