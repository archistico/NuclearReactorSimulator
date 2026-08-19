using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic Picard pressure/flow corrector introduced as the isolated H.3 prototype.
/// It evaluates the existing pipe, valve and pump laws against a provisional end-of-step state,
/// under-relaxes only the numerical iterate, and integrates conserved mass/internal energy from the
/// original committed inventories exactly once to form each provisional candidate. H.5 reaches it only
/// through the H.4 deterministic hybrid gate for versioned current-v2 definitions.
/// </summary>
public sealed class SemiImplicitHydraulicPrototypeSolver
{
    private readonly PipeFlowSolver _pipeFlowSolver;
    private readonly ValveFlowSolver _valveFlowSolver;
    private readonly PumpFlowSolver _pumpFlowSolver;
    private readonly FluidNodeIntegrator _fluidNodeIntegrator;
    private HydraulicEvaluationLayout? _evaluationLayout;

    public SemiImplicitHydraulicPrototypeSolver(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _pipeFlowSolver = new PipeFlowSolver();
        _valveFlowSolver = new ValveFlowSolver();
        _pumpFlowSolver = new PumpFlowSolver();
        _fluidNodeIntegrator = new FluidNodeIntegrator(thermodynamicModel);
    }

    public SemiImplicitHydraulicEvaluation Evaluate(PlantState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Evaluate(state.Definition, state.FluidNodes, state.Valves, state.Pumps);
    }

    internal SemiImplicitHydraulicEvaluation Evaluate(
        PlantDefinition definition,
        IReadOnlyList<FluidNodeState> fluidNodeStates,
        IReadOnlyList<ValveState> valveStateSource,
        IReadOnlyList<PumpState> pumpStateSource)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(fluidNodeStates);
        ArgumentNullException.ThrowIfNull(valveStateSource);
        ArgumentNullException.ThrowIfNull(pumpStateSource);

        var layout = GetEvaluationLayout(definition, fluidNodeStates, valveStateSource, pumpStateSource);
        return EvaluateCore(
            layout,
            fluidNodeStates,
            valveStateSource,
            pumpStateSource,
            referenceSnapshot: null,
            out _,
            out _);
    }

    /// <summary>
    /// H.28.1-E exact incremental hydraulic-map seam. Component results are reused only when both endpoint
    /// fluid-node objects and the corresponding valve/pump state object are the exact references used by the
    /// reference evaluation. Any changed dependency is solved by the unchanged component solver.
    /// </summary>
    internal SemiImplicitHydraulicEvaluation EvaluateWithExactReferenceReuse(
        PlantDefinition definition,
        IReadOnlyList<FluidNodeState> fluidNodeStates,
        IReadOnlyList<ValveState> valveStateSource,
        IReadOnlyList<PumpState> pumpStateSource,
        SemiImplicitHydraulicEvaluation referenceEvaluation,
        out int reusedComponentCount,
        out int componentCount)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(fluidNodeStates);
        ArgumentNullException.ThrowIfNull(valveStateSource);
        ArgumentNullException.ThrowIfNull(pumpStateSource);
        ArgumentNullException.ThrowIfNull(referenceEvaluation);

        var layout = GetEvaluationLayout(definition, fluidNodeStates, valveStateSource, pumpStateSource);
        var snapshot = referenceEvaluation.ComponentSnapshot;
        if (snapshot is null
            || !ReferenceEquals(snapshot.Definition, definition)
            || snapshot.FluidNodeStates.Count != fluidNodeStates.Count
            || snapshot.ValveStates.Count != valveStateSource.Count
            || snapshot.PumpStates.Count != pumpStateSource.Count
            || snapshot.PipeResults.Length != layout.Pipes.Length
            || snapshot.ValveResults.Length != layout.Valves.Length
            || snapshot.PumpResults.Length != layout.Pumps.Length)
        {
            return EvaluateCore(
                layout,
                fluidNodeStates,
                valveStateSource,
                pumpStateSource,
                referenceSnapshot: null,
                out reusedComponentCount,
                out componentCount);
        }

        return EvaluateCore(
            layout,
            fluidNodeStates,
            valveStateSource,
            pumpStateSource,
            snapshot,
            out reusedComponentCount,
            out componentCount);
    }

    private SemiImplicitHydraulicEvaluation EvaluateCore(
        HydraulicEvaluationLayout layout,
        IReadOnlyList<FluidNodeState> fluidNodeStates,
        IReadOnlyList<ValveState> valveStateSource,
        IReadOnlyList<PumpState> pumpStateSource,
        HydraulicComponentEvaluationSnapshot? referenceSnapshot,
        out int reusedComponentCount,
        out int componentCount)
    {
        var balances = new FluidNodeBalance[layout.FluidNodeIds.Length];
        Array.Fill(balances, FluidNodeBalance.Zero);
        var pipeFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var valveFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var pumpFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var pipeResults = new PipeFlowResult[layout.Pipes.Length];
        var valveResults = new ValveFlowResult[layout.Valves.Length];
        var pumpResults = new PumpFlowResult[layout.Pumps.Length];
        var pumpHydraulicPower = Power.Zero;
        reusedComponentCount = 0;
        componentCount = layout.Pipes.Length + layout.Valves.Length + layout.Pumps.Length;

        for (var index = 0; index < layout.Pipes.Length; index++)
        {
            var binding = layout.Pipes[index];
            PipeFlowResult result;
            if (referenceSnapshot is not null
                && ReferenceEquals(fluidNodeStates[binding.FromNodeIndex], referenceSnapshot.FluidNodeStates[binding.FromNodeIndex])
                && ReferenceEquals(fluidNodeStates[binding.ToNodeIndex], referenceSnapshot.FluidNodeStates[binding.ToNodeIndex]))
            {
                result = referenceSnapshot.PipeResults[index];
                reusedComponentCount++;
            }
            else
            {
                result = _pipeFlowSolver.Solve(
                    binding.Definition,
                    fluidNodeStates[binding.FromNodeIndex],
                    fluidNodeStates[binding.ToNodeIndex]);
            }

            pipeResults[index] = result;
            balances[binding.FromNodeIndex] += result.FromNodeBalance;
            balances[binding.ToNodeIndex] += result.ToNodeBalance;
            pipeFlows.Add(binding.Definition.Id, result.MassFlowRate);
        }

        for (var index = 0; index < layout.Valves.Length; index++)
        {
            var binding = layout.Valves[index];
            ValveFlowResult result;
            if (referenceSnapshot is not null
                && ReferenceEquals(valveStateSource[binding.StateIndex], referenceSnapshot.ValveStates[binding.StateIndex])
                && ReferenceEquals(fluidNodeStates[binding.FromNodeIndex], referenceSnapshot.FluidNodeStates[binding.FromNodeIndex])
                && ReferenceEquals(fluidNodeStates[binding.ToNodeIndex], referenceSnapshot.FluidNodeStates[binding.ToNodeIndex]))
            {
                result = referenceSnapshot.ValveResults[index];
                reusedComponentCount++;
            }
            else
            {
                result = _valveFlowSolver.Solve(
                    binding.Definition,
                    valveStateSource[binding.StateIndex],
                    fluidNodeStates[binding.FromNodeIndex],
                    fluidNodeStates[binding.ToNodeIndex]);
            }

            valveResults[index] = result;
            balances[binding.FromNodeIndex] += result.FromNodeBalance;
            balances[binding.ToNodeIndex] += result.ToNodeBalance;
            valveFlows.Add(binding.Definition.Id, result.MassFlowRate);
        }

        for (var index = 0; index < layout.Pumps.Length; index++)
        {
            var binding = layout.Pumps[index];
            PumpFlowResult result;
            if (referenceSnapshot is not null
                && ReferenceEquals(pumpStateSource[binding.StateIndex], referenceSnapshot.PumpStates[binding.StateIndex])
                && ReferenceEquals(fluidNodeStates[binding.FromNodeIndex], referenceSnapshot.FluidNodeStates[binding.FromNodeIndex])
                && ReferenceEquals(fluidNodeStates[binding.ToNodeIndex], referenceSnapshot.FluidNodeStates[binding.ToNodeIndex]))
            {
                result = referenceSnapshot.PumpResults[index];
                reusedComponentCount++;
            }
            else
            {
                result = _pumpFlowSolver.Solve(
                    binding.Definition,
                    pumpStateSource[binding.StateIndex],
                    fluidNodeStates[binding.FromNodeIndex],
                    fluidNodeStates[binding.ToNodeIndex]);
            }

            pumpResults[index] = result;
            balances[binding.FromNodeIndex] += result.FromNodeBalance;
            balances[binding.ToNodeIndex] += result.ToNodeBalance;
            pumpFlows.Add(binding.Definition.Id, result.MassFlowRate);
            pumpHydraulicPower += result.HydraulicPowerExchange;
        }

        var balanceMap = new SortedDictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        for (var index = 0; index < layout.FluidNodeIds.Length; index++)
        {
            balanceMap.Add(layout.FluidNodeIds[index], balances[index]);
        }

        var massRateClosure = Math.Abs(CompensatedSum(balances.Select(static balance => balance.NetMassFlowRate.KilogramsPerSecond)));
        var hydraulicEnergyRate = CompensatedSum(balances.Select(static balance => balance.NetEnergyRate.Watts));
        var energyOwnershipResidual = Math.Abs(hydraulicEnergyRate - pumpHydraulicPower.Watts);
        var snapshot = new HydraulicComponentEvaluationSnapshot(
            layout.Definition,
            fluidNodeStates,
            valveStateSource,
            pumpStateSource,
            pipeResults,
            valveResults,
            pumpResults);

        return new SemiImplicitHydraulicEvaluation(
            balanceMap,
            pipeFlows,
            valveFlows,
            pumpFlows,
            pumpHydraulicPower,
            massRateClosure,
            energyOwnershipResidual,
            snapshot);
    }

    private HydraulicEvaluationLayout GetEvaluationLayout(
        PlantDefinition definition,
        IReadOnlyList<FluidNodeState> fluidNodeStates,
        IReadOnlyList<ValveState> valveStates,
        IReadOnlyList<PumpState> pumpStates)
    {
        if (_evaluationLayout is not null && ReferenceEquals(_evaluationLayout.Definition, definition))
        {
            return _evaluationLayout;
        }

        _evaluationLayout = HydraulicEvaluationLayout.Create(definition, fluidNodeStates, valveStates, pumpStates);
        return _evaluationLayout;
    }

    public SemiImplicitHydraulicPrototypeStepResult StepExplicit(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        return StepExplicitFromHydraulicEvaluation(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            Evaluate(committedState));
    }

    /// <summary>
    /// H.28.1-B internal reuse seam. It preserves the exact historical explicit-balance combination and
    /// integration order while accepting an already-computed committed-state hydraulic evaluation.
    /// </summary>
    internal SemiImplicitHydraulicPrototypeStepResult StepExplicitFromHydraulicEvaluation(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        SemiImplicitHydraulicEvaluation hydraulicEvaluation)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        ArgumentNullException.ThrowIfNull(hydraulicEvaluation);
        var totalBalances = CombineBalances(
            committedState,
            hydraulicEvaluation.FluidNodeBalances,
            frozenNonHydraulicBalances);
        var candidate = IntegrateFromCommitted(committedState, totalBalances, deltaTime);

        return new SemiImplicitHydraulicPrototypeStepResult(
            candidate,
            hydraulicEvaluation,
            hydraulicEvaluation.FluidNodeBalances,
            1,
            true,
            0d,
            0d);
    }

    /// <summary>
    /// H.28.1-B exact reuse seam. A historical explicit fluid-node result is reused only when the
    /// already-applied historical total balance is exactly equal to the canonical H.4
    /// hydraulic-plus-frozen-non-hydraulic balance. Nodes whose arithmetic history differs are
    /// reintegrated through the unchanged H.4 path, preserving bit-exact predictor semantics.
    /// </summary>
    internal SemiImplicitHydraulicPrototypeStepResult StepExplicitFromHistoricalCandidate(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        SemiImplicitHydraulicEvaluation hydraulicEvaluation,
        PlantState historicalExplicitCandidateState,
        IReadOnlyDictionary<string, FluidNodeBalance> historicalAppliedTotalBalances,
        out int reusedFluidNodeCount)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        ArgumentNullException.ThrowIfNull(hydraulicEvaluation);
        ArgumentNullException.ThrowIfNull(historicalExplicitCandidateState);
        ArgumentNullException.ThrowIfNull(historicalAppliedTotalBalances);

        if (!ReferenceEquals(committedState.Definition, historicalExplicitCandidateState.Definition))
        {
            throw new ArgumentException(
                "Historical explicit candidate must use the same plant definition as the committed state.",
                nameof(historicalExplicitCandidateState));
        }

        if (historicalExplicitCandidateState.FluidNodes.Count != committedState.FluidNodes.Count)
        {
            throw new ArgumentException(
                "Historical explicit candidate must contain the same number of fluid nodes as the committed state.",
                nameof(historicalExplicitCandidateState));
        }

        var canonicalTotalBalances = CombineBalances(
            committedState,
            hydraulicEvaluation.FluidNodeBalances,
            frozenNonHydraulicBalances);
        var candidateFluidNodes = new FluidNodeState[committedState.FluidNodes.Count];
        reusedFluidNodeCount = 0;

        for (var index = 0; index < committedState.FluidNodes.Count; index++)
        {
            var committedNode = committedState.FluidNodes[index];
            var historicalNode = historicalExplicitCandidateState.FluidNodes[index];
            if (!string.Equals(committedNode.Id, historicalNode.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Historical explicit candidate fluid-node order differs at index {index}: expected '{committedNode.Id}', found '{historicalNode.Id}'.",
                    nameof(historicalExplicitCandidateState));
            }

            if (!historicalAppliedTotalBalances.TryGetValue(committedNode.Id, out var historicalBalance))
            {
                throw new ArgumentException(
                    $"Historical applied total balances do not contain fluid node '{committedNode.Id}'.",
                    nameof(historicalAppliedTotalBalances));
            }

            var canonicalBalance = canonicalTotalBalances[committedNode.Id];
            if (historicalBalance.Equals(canonicalBalance))
            {
                candidateFluidNodes[index] = historicalNode;
                reusedFluidNodeCount++;
            }
            else
            {
                candidateFluidNodes[index] = _fluidNodeIntegrator.Step(committedNode, canonicalBalance, deltaTime);
            }
        }

        var candidate = new PlantState(
            committedState.Definition,
            candidateFluidNodes,
            committedState.Valves,
            committedState.Pumps,
            committedState.ThermalBodies,
            committedState.HeatSources);

        return new SemiImplicitHydraulicPrototypeStepResult(
            candidate,
            hydraulicEvaluation,
            hydraulicEvaluation.FluidNodeBalances,
            1,
            true,
            0d,
            0d);
    }

    public SemiImplicitHydraulicPrototypeStepResult StepSemiImplicit(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        SemiImplicitHydraulicPrototypeOptions? options = null)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        options ??= SemiImplicitHydraulicPrototypeOptions.H3AuditDefault;

        var previousEvaluation = Evaluate(committedState);
        var relaxedHydraulicBalances = previousEvaluation.FluidNodeBalances;
        var initialTotalBalances = CombineBalances(committedState, relaxedHydraulicBalances, frozenNonHydraulicBalances);
        var iterateState = IntegrateFromCommitted(committedState, initialTotalBalances, deltaTime);
        var pressureResidual = MaximumRelativePressureDifference(committedState, iterateState);
        var flowResidual = double.PositiveInfinity;

        for (var iteration = 2; iteration <= options.MaximumIterations; iteration++)
        {
            var currentEvaluation = Evaluate(iterateState);
            flowResidual = MaximumAbsoluteFlowDifference(previousEvaluation, currentEvaluation);
            relaxedHydraulicBalances = BlendBalances(
                committedState,
                relaxedHydraulicBalances,
                currentEvaluation.FluidNodeBalances,
                options.RelaxationFactor);
            var totalBalances = CombineBalances(committedState, relaxedHydraulicBalances, frozenNonHydraulicBalances);
            var candidateState = IntegrateFromCommitted(committedState, totalBalances, deltaTime);
            pressureResidual = MaximumRelativePressureDifference(iterateState, candidateState);

            if (pressureResidual <= options.RelativePressureTolerance
                && flowResidual <= options.AbsoluteFlowToleranceKilogramsPerSecond)
            {
                var finalEvaluation = Evaluate(candidateState);
                return new SemiImplicitHydraulicPrototypeStepResult(
                    candidateState,
                    finalEvaluation,
                    relaxedHydraulicBalances,
                    iteration,
                    true,
                    pressureResidual,
                    flowResidual);
            }

            iterateState = candidateState;
            previousEvaluation = currentEvaluation;
        }

        return new SemiImplicitHydraulicPrototypeStepResult(
            iterateState,
            Evaluate(iterateState),
            relaxedHydraulicBalances,
            options.MaximumIterations,
            false,
            pressureResidual,
            flowResidual);
    }

    private PlantState IntegrateFromCommitted(
        PlantState committedState,
        IReadOnlyDictionary<string, FluidNodeBalance> totalBalances,
        TimeSpan deltaTime)
    {
        var candidateFluidNodes = committedState.FluidNodes
            .Select(node => _fluidNodeIntegrator.Step(node, totalBalances[node.Id], deltaTime))
            .ToArray();

        return new PlantState(
            committedState.Definition,
            candidateFluidNodes,
            committedState.Valves,
            committedState.Pumps,
            committedState.ThermalBodies,
            committedState.HeatSources);
    }

    private static IReadOnlyDictionary<string, FluidNodeBalance> CombineBalances(
        PlantState state,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulicBalances,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        return state.FluidNodes.ToDictionary(
            static node => node.Id,
            node => hydraulicBalances[node.Id]
                + (frozenNonHydraulicBalances.TryGetValue(node.Id, out var frozen) ? frozen : FluidNodeBalance.Zero),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, FluidNodeBalance> BlendBalances(
        PlantState state,
        IReadOnlyDictionary<string, FluidNodeBalance> previous,
        IReadOnlyDictionary<string, FluidNodeBalance> current,
        double relaxationFactor)
    {
        return state.FluidNodes.ToDictionary(
            static node => node.Id,
            node => Blend(previous[node.Id], current[node.Id], relaxationFactor),
            StringComparer.Ordinal);
    }

    private static FluidNodeBalance Blend(FluidNodeBalance previous, FluidNodeBalance current, double alpha)
    {
        var beta = 1d - alpha;
        return new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(
                (beta * previous.NetMassFlowRate.KilogramsPerSecond)
                + (alpha * current.NetMassFlowRate.KilogramsPerSecond)),
            Power.FromWatts(
                (beta * previous.NetEnergyRate.Watts)
                + (alpha * current.NetEnergyRate.Watts)));
    }

    private static double MaximumRelativePressureDifference(PlantState left, PlantState right)
    {
        var rightNodes = right.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var leftNode in left.FluidNodes)
        {
            var rightPressure = rightNodes[leftNode.Id].Pressure.Pascals;
            var leftPressure = leftNode.Pressure.Pascals;
            var scale = Math.Max(Math.Max(Math.Abs(leftPressure), Math.Abs(rightPressure)), 1_000d);
            maximum = Math.Max(maximum, Math.Abs(rightPressure - leftPressure) / scale);
        }

        return maximum;
    }

    private static double MaximumAbsoluteFlowDifference(
        SemiImplicitHydraulicEvaluation previous,
        SemiImplicitHydraulicEvaluation current)
    {
        var maximum = 0d;
        maximum = Math.Max(maximum, MaximumDifference(previous.PipeMassFlowRates, current.PipeMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(previous.ValveMassFlowRates, current.ValveMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(previous.PumpMassFlowRates, current.PumpMassFlowRates));
        return maximum;
    }

    private static double MaximumDifference(
        IReadOnlyDictionary<string, MassFlowRate> previous,
        IReadOnlyDictionary<string, MassFlowRate> current)
    {
        var maximum = 0d;
        foreach (var entry in previous)
        {
            maximum = Math.Max(
                maximum,
                Math.Abs(current[entry.Key].KilogramsPerSecond - entry.Value.KilogramsPerSecond));
        }

        return maximum;
    }

    private static void ValidateStepArguments(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(frozenNonHydraulicBalances);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Prototype step time must be greater than zero.");
        }

        var nodeIds = committedState.FluidNodes.Select(static node => node.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var nodeId in frozenNonHydraulicBalances.Keys)
        {
            if (!nodeIds.Contains(nodeId))
            {
                throw new ArgumentException($"Frozen non-hydraulic balance references unknown fluid node '{nodeId}'.", nameof(frozenNonHydraulicBalances));
            }
        }
    }

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;
        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }
    private sealed record HydraulicEvaluationLayout(
        PlantDefinition Definition,
        string[] FluidNodeIds,
        PipeBinding[] Pipes,
        ValveBinding[] Valves,
        PumpBinding[] Pumps)
    {
        public static HydraulicEvaluationLayout Create(
            PlantDefinition definition,
            IReadOnlyList<FluidNodeState> fluidNodeStates,
            IReadOnlyList<ValveState> valveStates,
            IReadOnlyList<PumpState> pumpStates)
        {
            var fluidNodeIndexes = fluidNodeStates
                .Select(static (state, index) => (state.Id, Index: index))
                .ToDictionary(static item => item.Id, static item => item.Index, StringComparer.Ordinal);
            var valveIndexes = valveStates
                .Select(static (state, index) => (state.ValveId, Index: index))
                .ToDictionary(static item => item.ValveId, static item => item.Index, StringComparer.Ordinal);
            var pumpIndexes = pumpStates
                .Select(static (state, index) => (state.PumpId, Index: index))
                .ToDictionary(static item => item.PumpId, static item => item.Index, StringComparer.Ordinal);

            var fluidNodeIds = definition.FluidNodes.Select(static item => item.Id).ToArray();
            if (fluidNodeIds.Length != fluidNodeStates.Count)
            {
                throw new ArgumentException("Hydraulic evaluation fluid-node state count does not match the plant definition.", nameof(fluidNodeStates));
            }

            for (var index = 0; index < fluidNodeIds.Length; index++)
            {
                if (!string.Equals(fluidNodeIds[index], fluidNodeStates[index].Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Hydraulic evaluation fluid-node states must remain in canonical plant order.", nameof(fluidNodeStates));
                }
            }

            var pipes = definition.Pipes.Select(pipe => new PipeBinding(
                pipe,
                fluidNodeIndexes[pipe.FromNodeId],
                fluidNodeIndexes[pipe.ToNodeId])).ToArray();
            var valves = definition.Valves.Select(valve => new ValveBinding(
                valve,
                valveIndexes[valve.Id],
                fluidNodeIndexes[valve.Pipe.FromNodeId],
                fluidNodeIndexes[valve.Pipe.ToNodeId])).ToArray();
            var pumps = definition.Pumps.Select(pump => new PumpBinding(
                pump,
                pumpIndexes[pump.Id],
                fluidNodeIndexes[pump.Pipe.FromNodeId],
                fluidNodeIndexes[pump.Pipe.ToNodeId])).ToArray();

            return new HydraulicEvaluationLayout(definition, fluidNodeIds, pipes, valves, pumps);
        }
    }

    private readonly record struct PipeBinding(PipeDefinition Definition, int FromNodeIndex, int ToNodeIndex);

    private readonly record struct ValveBinding(ValveDefinition Definition, int StateIndex, int FromNodeIndex, int ToNodeIndex);

    private readonly record struct PumpBinding(PumpDefinition Definition, int StateIndex, int FromNodeIndex, int ToNodeIndex);

}
