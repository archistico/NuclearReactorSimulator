using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Applies valve state and characteristic to an existing passive pipe-flow model.
/// A valve changes effective hydraulic resistance; it does not introduce a second flow law.
/// </summary>
public sealed class ValveFlowSolver
{
    private readonly PipeFlowSolver _pipeFlowSolver;
    private readonly ValveCharacteristicSolver _characteristicSolver;

    public ValveFlowSolver()
        : this(new PipeFlowSolver(), new ValveCharacteristicSolver())
    {
    }

    internal ValveFlowSolver(
        PipeFlowSolver pipeFlowSolver,
        ValveCharacteristicSolver characteristicSolver)
    {
        _pipeFlowSolver = pipeFlowSolver ?? throw new ArgumentNullException(nameof(pipeFlowSolver));
        _characteristicSolver = characteristicSolver ?? throw new ArgumentNullException(nameof(characteristicSolver));
    }

    public ValveFlowResult Solve(
        ValveDefinition valve,
        ValveState state,
        FluidNodeState fromNode,
        FluidNodeState toNode)
    {
        ArgumentNullException.ThrowIfNull(valve);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fromNode);
        ArgumentNullException.ThrowIfNull(toNode);

        ValidateStateIdentity(valve, state);
        ValidateEndpoints(valve.Pipe, fromNode, toNode);

        var effectivePosition = ResolveEffectivePosition(valve, state);
        var flowCoefficient = _characteristicSolver.Evaluate(valve.Characteristic, effectivePosition);

        if (flowCoefficient.IsClosed)
        {
            var pressureDifference = fromNode.Pressure - toNode.Pressure;
            return new ValveFlowResult(
                effectivePosition,
                flowCoefficient,
                new PipeFlowResult(pressureDifference, MassFlowRate.Zero, Power.Zero));
        }

        var coefficientSquared = flowCoefficient.Fraction * flowCoefficient.Fraction;
        var effectiveResistanceValue = valve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared
            / coefficientSquared;

        if (!double.IsFinite(effectiveResistanceValue))
        {
            throw new ArithmeticException($"Valve '{valve.Id}' effective hydraulic resistance is non-finite.");
        }

        var effectivePipe = new PipeDefinition(
            valve.Pipe.Id,
            valve.Pipe.FromNodeId,
            valve.Pipe.ToNodeId,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(effectiveResistanceValue));
        var hydraulicFlow = _pipeFlowSolver.Solve(effectivePipe, fromNode, toNode);

        return new ValveFlowResult(effectivePosition, flowCoefficient, hydraulicFlow);
    }

    private static ValvePosition ResolveEffectivePosition(
        ValveDefinition valve,
        ValveState state)
    {
        if (!state.IsFailSafeActive)
        {
            return state.Position;
        }

        return valve.FailSafeAction switch
        {
            ValveFailSafeAction.FailClosed => ValvePosition.Closed,
            ValveFailSafeAction.FailOpen => ValvePosition.FullyOpen,
            ValveFailSafeAction.HoldLastPosition => state.Position,
            _ => throw new ArgumentOutOfRangeException(
                nameof(valve),
                valve.FailSafeAction,
                "Unknown valve fail-safe action."),
        };
    }

    private static void ValidateEndpoints(
        PipeDefinition pipe,
        FluidNodeState fromNode,
        FluidNodeState toNode)
    {
        if (!string.Equals(pipe.FromNodeId, fromNode.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Valve path '{pipe.Id}' expects from-node '{pipe.FromNodeId}', but received '{fromNode.Id}'.",
                nameof(fromNode));
        }

        if (!string.Equals(pipe.ToNodeId, toNode.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Valve path '{pipe.Id}' expects to-node '{pipe.ToNodeId}', but received '{toNode.Id}'.",
                nameof(toNode));
        }
    }

    private static void ValidateStateIdentity(ValveDefinition valve, ValveState state)
    {
        if (!string.Equals(valve.Id, state.ValveId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Valve '{valve.Id}' received state for valve '{state.ValveId}'.",
                nameof(state));
        }
    }
}
