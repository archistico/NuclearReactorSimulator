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

        var definition = state.Definition;
        var fluidNodes = state.FluidNodes.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var valveStates = state.Valves.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpStates = state.Pumps.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var balances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var pipeFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var valveFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var pumpFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var pumpHydraulicPower = Power.Zero;

        foreach (var pipe in definition.Pipes)
        {
            var result = _pipeFlowSolver.Solve(pipe, fluidNodes[pipe.FromNodeId], fluidNodes[pipe.ToNodeId]);
            balances[pipe.FromNodeId] += result.FromNodeBalance;
            balances[pipe.ToNodeId] += result.ToNodeBalance;
            pipeFlows.Add(pipe.Id, result.MassFlowRate);
        }

        foreach (var valve in definition.Valves)
        {
            var result = _valveFlowSolver.Solve(
                valve,
                valveStates[valve.Id],
                fluidNodes[valve.Pipe.FromNodeId],
                fluidNodes[valve.Pipe.ToNodeId]);
            balances[valve.Pipe.FromNodeId] += result.FromNodeBalance;
            balances[valve.Pipe.ToNodeId] += result.ToNodeBalance;
            valveFlows.Add(valve.Id, result.MassFlowRate);
        }

        foreach (var pump in definition.Pumps)
        {
            var result = _pumpFlowSolver.Solve(
                pump,
                pumpStates[pump.Id],
                fluidNodes[pump.Pipe.FromNodeId],
                fluidNodes[pump.Pipe.ToNodeId]);
            balances[pump.Pipe.FromNodeId] += result.FromNodeBalance;
            balances[pump.Pipe.ToNodeId] += result.ToNodeBalance;
            pumpFlows.Add(pump.Id, result.MassFlowRate);
            pumpHydraulicPower += result.HydraulicPowerExchange;
        }

        var massRateClosure = Math.Abs(CompensatedSum(balances.Values.Select(static balance => balance.NetMassFlowRate.KilogramsPerSecond)));
        var hydraulicEnergyRate = CompensatedSum(balances.Values.Select(static balance => balance.NetEnergyRate.Watts));
        var energyOwnershipResidual = Math.Abs(hydraulicEnergyRate - pumpHydraulicPower.Watts);

        return new SemiImplicitHydraulicEvaluation(
            balances,
            pipeFlows,
            valveFlows,
            pumpFlows,
            pumpHydraulicPower,
            massRateClosure,
            energyOwnershipResidual);
    }

    public SemiImplicitHydraulicPrototypeStepResult StepExplicit(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        var hydraulic = Evaluate(committedState);
        var totalBalances = CombineBalances(committedState, hydraulic.FluidNodeBalances, frozenNonHydraulicBalances);
        var candidate = IntegrateFromCommitted(committedState, totalBalances, deltaTime);

        return new SemiImplicitHydraulicPrototypeStepResult(
            candidate,
            hydraulic,
            hydraulic.FluidNodeBalances,
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
}
