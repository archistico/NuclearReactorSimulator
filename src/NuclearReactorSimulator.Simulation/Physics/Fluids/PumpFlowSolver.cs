using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Solves a simplified centrifugal pump as an active pressure source plus quadratic internal resistance,
/// composed with the existing passive hydraulic path. No independent or imposed-flow law is introduced.
/// </summary>
public sealed class PumpFlowSolver
{
    private readonly PipeFlowSolver _pipeFlowSolver;

    public PumpFlowSolver()
        : this(new PipeFlowSolver())
    {
    }

    internal PumpFlowSolver(PipeFlowSolver pipeFlowSolver)
    {
        _pipeFlowSolver = pipeFlowSolver ?? throw new ArgumentNullException(nameof(pipeFlowSolver));
    }

    public PumpFlowResult Solve(
        PumpDefinition pump,
        PumpState state,
        FluidNodeState fromNode,
        FluidNodeState toNode)
    {
        ArgumentNullException.ThrowIfNull(pump);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fromNode);
        ArgumentNullException.ThrowIfNull(toNode);

        ValidateStateIdentity(pump, state);

        var effectiveSpeed = state.IsRunning ? state.Speed : PumpSpeed.Stopped;
        var speedSquared = effectiveSpeed.Fraction * effectiveSpeed.Fraction;
        var activePressureBoost = pump.RatedPressureBoost * speedSquared;
        var effectiveResistanceValue = pump.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared
            + pump.InternalResistance.PascalSecondsSquaredPerKilogramSquared;

        if (!double.IsFinite(effectiveResistanceValue) || effectiveResistanceValue <= 0d)
        {
            throw new ArithmeticException($"Pump '{pump.Id}' total hydraulic resistance is invalid.");
        }

        var effectivePipe = new PipeDefinition(
            pump.Pipe.Id,
            pump.Pipe.FromNodeId,
            pump.Pipe.ToNodeId,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(effectiveResistanceValue));
        var hydraulicFlow = _pipeFlowSolver.Solve(
            effectivePipe,
            fromNode,
            toNode,
            activePressureBoost);
        var massFlowRate = hydraulicFlow.MassFlowRate;
        var upstreamNode = massFlowRate.KilogramsPerSecond >= 0d ? fromNode : toNode;
        var volumetricFlowRate = massFlowRate == MassFlowRate.Zero
            ? VolumetricFlowRate.Zero
            : massFlowRate / upstreamNode.Density;
        var hydraulicPowerExchange = activePressureBoost * volumetricFlowRate;
        var shaftPowerDemand = hydraulicPowerExchange.Watts > 0d
            ? hydraulicPowerExchange / pump.Efficiency.Fraction
            : Power.Zero;
        var internalPressureLoss = CalculateInternalPressureLoss(pump, massFlowRate);

        var fromNodeBalance = hydraulicFlow.FromNodeBalance;
        var toNodeBalance = hydraulicFlow.ToNodeBalance;

        if (massFlowRate.KilogramsPerSecond > 0d)
        {
            toNodeBalance += new FluidNodeBalance(MassFlowRate.Zero, hydraulicPowerExchange);
        }
        else if (massFlowRate.KilogramsPerSecond < 0d)
        {
            fromNodeBalance += new FluidNodeBalance(MassFlowRate.Zero, hydraulicPowerExchange);
        }

        return new PumpFlowResult(
            effectiveSpeed,
            hydraulicFlow.PressureDifference,
            activePressureBoost,
            internalPressureLoss,
            hydraulicFlow,
            volumetricFlowRate,
            hydraulicPowerExchange,
            shaftPowerDemand,
            fromNodeBalance,
            toNodeBalance);
    }

    private static PressureDifference CalculateInternalPressureLoss(
        PumpDefinition pump,
        MassFlowRate massFlowRate)
    {
        var flow = massFlowRate.KilogramsPerSecond;
        var pascals = pump.InternalResistance.PascalSecondsSquaredPerKilogramSquared
            * flow
            * Math.Abs(flow);
        return PressureDifference.FromPascals(pascals);
    }

    private static void ValidateStateIdentity(PumpDefinition pump, PumpState state)
    {
        if (!string.Equals(pump.Id, state.PumpId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Pump '{pump.Id}' received state for pump '{state.PumpId}'.",
                nameof(state));
        }
    }
}
