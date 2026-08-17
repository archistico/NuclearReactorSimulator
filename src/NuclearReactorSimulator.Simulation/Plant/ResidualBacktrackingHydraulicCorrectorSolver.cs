using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Phase H.7 isolated nonlinear hydraulic corrector. Unlike the historical H.3 Picard prototype,
/// convergence is evaluated against the unrelaxed fixed-point map itself rather than against the
/// motion of two already-relaxed iterates. A deterministic backtracking line search accepts a
/// relaxation factor only when it strictly reduces the normalized fixed-point merit residual.
/// </summary>
public sealed class ResidualBacktrackingHydraulicCorrectorSolver
{
    private readonly SemiImplicitHydraulicPrototypeSolver _hydraulicEvaluator;
    private readonly FluidNodeIntegrator _fluidNodeIntegrator;

    public ResidualBacktrackingHydraulicCorrectorSolver(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _hydraulicEvaluator = new SemiImplicitHydraulicPrototypeSolver(thermodynamicModel);
        _fluidNodeIntegrator = new FluidNodeIntegrator(thermodynamicModel);
    }

    public ResidualBacktrackingHydraulicCorrectorStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        ResidualBacktrackingHydraulicCorrectorOptions? options = null)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        options ??= ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault;

        var hydraulicEvaluationCount = 0;
        var backtrackingTrialCount = 0;
        var committedEvaluation = Evaluate(committedState, ref hydraulicEvaluationCount);
        var appliedIterate = FromEvaluation(committedEvaluation);
        var iterateState = IntegrateFromCommitted(
            committedState,
            CombineBalances(committedState, appliedIterate.FluidNodeBalances, frozenNonHydraulicBalances),
            deltaTime);
        var residual = EvaluateFixedPointResidual(
            committedState,
            iterateState,
            appliedIterate,
            deltaTime,
            frozenNonHydraulicBalances,
            options,
            ref hydraulicEvaluationCount);
        var iterations = new List<ResidualBacktrackingHydraulicIteration>(options.MaximumIterations)
        {
            ToIterationEvidence(1, 0d, 0, residual),
        };

        if (residual.Converged)
        {
            return BuildResult(
                iterateState,
                residual,
                appliedIterate,
                iterations,
                converged: true,
                lineSearchExhausted: false,
                hydraulicEvaluationCount,
                backtrackingTrialCount,
                minimumAcceptedRelaxationFactor: 0d);
        }

        var minimumAcceptedRelaxationFactor = double.PositiveInfinity;
        for (var iterationIndex = 2; iterationIndex <= options.MaximumIterations; iterationIndex++)
        {
            var relaxationFactor = options.InitialRelaxationFactor;
            var accepted = false;
            ResidualSample? acceptedResidual = null;
            HydraulicIterate? acceptedIterate = null;
            PlantState? acceptedState = null;
            var trialsThisIteration = 0;

            while (relaxationFactor >= options.MinimumRelaxationFactor)
            {
                trialsThisIteration++;
                backtrackingTrialCount++;
                var trialIterate = BlendIterate(
                    committedState,
                    appliedIterate,
                    residual.CurrentEvaluation,
                    relaxationFactor);

                if (TryEvaluateTrial(
                    committedState,
                    deltaTime,
                    frozenNonHydraulicBalances,
                    options,
                    trialIterate,
                    ref hydraulicEvaluationCount,
                    out var trialState,
                    out var trialResidual)
                    && trialState is not null
                    && trialResidual is not null
                    && StrictlyReducesMerit(residual.NormalizedMeritResidual, trialResidual.NormalizedMeritResidual))
                {
                    accepted = true;
                    acceptedState = trialState;
                    acceptedResidual = trialResidual;
                    acceptedIterate = trialIterate;
                    break;
                }

                relaxationFactor *= options.BacktrackingFactor;
            }

            if (!accepted || acceptedState is null || acceptedResidual is null || acceptedIterate is null)
            {
                return BuildResult(
                    iterateState,
                    residual,
                    appliedIterate,
                    iterations,
                    converged: false,
                    lineSearchExhausted: true,
                    hydraulicEvaluationCount,
                    backtrackingTrialCount,
                    minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                        ? 0d
                        : minimumAcceptedRelaxationFactor);
            }

            iterateState = acceptedState;
            residual = acceptedResidual;
            appliedIterate = acceptedIterate;
            minimumAcceptedRelaxationFactor = Math.Min(minimumAcceptedRelaxationFactor, relaxationFactor);
            iterations.Add(ToIterationEvidence(iterationIndex, relaxationFactor, trialsThisIteration, residual));

            if (residual.Converged)
            {
                return BuildResult(
                    iterateState,
                    residual,
                    appliedIterate,
                    iterations,
                    converged: true,
                    lineSearchExhausted: false,
                    hydraulicEvaluationCount,
                    backtrackingTrialCount,
                    minimumAcceptedRelaxationFactor);
            }
        }

        return BuildResult(
            iterateState,
            residual,
            appliedIterate,
            iterations,
            converged: false,
            lineSearchExhausted: false,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                ? 0d
                : minimumAcceptedRelaxationFactor);
    }

    private bool TryEvaluateTrial(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        ResidualBacktrackingHydraulicCorrectorOptions options,
        HydraulicIterate trialIterate,
        ref int hydraulicEvaluationCount,
        out PlantState? trialState,
        out ResidualSample? trialResidual)
    {
        try
        {
            trialState = IntegrateFromCommitted(
                committedState,
                CombineBalances(committedState, trialIterate.FluidNodeBalances, frozenNonHydraulicBalances),
                deltaTime);
            trialResidual = EvaluateFixedPointResidual(
                committedState,
                trialState,
                trialIterate,
                deltaTime,
                frozenNonHydraulicBalances,
                options,
                ref hydraulicEvaluationCount);
            return true;
        }
        catch (Exception exception) when (
            exception is FluidNodeDepletionException
            or WaterSteamStateOutOfRangeException
            or ArithmeticException)
        {
            trialState = null;
            trialResidual = null;
            return false;
        }
    }

    private ResidualSample EvaluateFixedPointResidual(
        PlantState committedState,
        PlantState iterateState,
        HydraulicIterate appliedIterate,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        ResidualBacktrackingHydraulicCorrectorOptions options,
        ref int hydraulicEvaluationCount)
    {
        var currentEvaluation = Evaluate(iterateState, ref hydraulicEvaluationCount);
        try
        {
            var mappedState = IntegrateFromCommitted(
                committedState,
                CombineBalances(committedState, currentEvaluation.FluidNodeBalances, frozenNonHydraulicBalances),
                deltaTime);
            var pressureResidual = MaximumRelativePressureDifference(iterateState, mappedState);
            var flowResidual = MaximumAbsoluteFlowDifference(appliedIterate, currentEvaluation);
            var normalizedMerit = Math.Max(
                pressureResidual / options.RelativePressureTolerance,
                flowResidual / options.AbsoluteFlowToleranceKilogramsPerSecond);

            if (!double.IsFinite(normalizedMerit))
            {
                throw new ArithmeticException("Residual/backtracking hydraulic corrector produced a non-finite fixed-point residual.");
            }

            return new ResidualSample(
                currentEvaluation,
                pressureResidual,
                flowResidual,
                normalizedMerit,
                pressureResidual <= options.RelativePressureTolerance
                    && flowResidual <= options.AbsoluteFlowToleranceKilogramsPerSecond);
        }
        catch (Exception exception) when (
            exception is FluidNodeDepletionException
            or WaterSteamStateOutOfRangeException
            or ArithmeticException)
        {
            return new ResidualSample(
                currentEvaluation,
                double.PositiveInfinity,
                double.PositiveInfinity,
                double.PositiveInfinity,
                Converged: false);
        }
    }

    private SemiImplicitHydraulicEvaluation Evaluate(PlantState state, ref int evaluationCount)
    {
        evaluationCount++;
        return _hydraulicEvaluator.Evaluate(state);
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

    private static ResidualBacktrackingHydraulicCorrectorStepResult BuildResult(
        PlantState candidateState,
        ResidualSample residual,
        HydraulicIterate appliedIterate,
        IReadOnlyList<ResidualBacktrackingHydraulicIteration> iterations,
        bool converged,
        bool lineSearchExhausted,
        int hydraulicEvaluationCount,
        int backtrackingTrialCount,
        double minimumAcceptedRelaxationFactor)
        => new(
            candidateState,
            residual.CurrentEvaluation,
            CanonicalCopy(appliedIterate.FluidNodeBalances),
            AppliedMassClosure(appliedIterate),
            AppliedEnergyOwnershipResidual(appliedIterate),
            iterations.Count,
            converged,
            lineSearchExhausted,
            residual.MaximumRelativePressureFixedPointResidual,
            residual.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            residual.NormalizedMeritResidual,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor,
            iterations.ToArray());

    private static ResidualBacktrackingHydraulicIteration ToIterationEvidence(
        int iterationIndex,
        double relaxationFactor,
        int backtrackingTrials,
        ResidualSample residual)
        => new(
            iterationIndex,
            relaxationFactor,
            backtrackingTrials,
            residual.MaximumRelativePressureFixedPointResidual,
            residual.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            residual.NormalizedMeritResidual);

    private static bool StrictlyReducesMerit(double currentMerit, double trialMerit)
    {
        if (double.IsPositiveInfinity(currentMerit))
        {
            return double.IsFinite(trialMerit);
        }

        if (!double.IsFinite(currentMerit) || !double.IsFinite(trialMerit))
        {
            return false;
        }

        var requiredReduction = Math.Max(1e-12d, Math.Abs(currentMerit) * 1e-12d);
        return trialMerit <= currentMerit - requiredReduction;
    }

    private static HydraulicIterate FromEvaluation(SemiImplicitHydraulicEvaluation evaluation)
        => new(
            evaluation.FluidNodeBalances,
            evaluation.PipeMassFlowRates,
            evaluation.ValveMassFlowRates,
            evaluation.PumpMassFlowRates,
            evaluation.PumpHydraulicPowerExchange);

    private static IReadOnlyDictionary<string, FluidNodeBalance> CombineBalances(
        PlantState state,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulicBalances,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
        => state.FluidNodes.ToDictionary(
            static node => node.Id,
            node => hydraulicBalances[node.Id]
                + (frozenNonHydraulicBalances.TryGetValue(node.Id, out var frozen) ? frozen : FluidNodeBalance.Zero),
            StringComparer.Ordinal);

    private static HydraulicIterate BlendIterate(
        PlantState state,
        HydraulicIterate current,
        SemiImplicitHydraulicEvaluation target,
        double relaxationFactor)
        => new(
            state.FluidNodes.ToDictionary(
                static node => node.Id,
                node => Blend(current.FluidNodeBalances[node.Id], target.FluidNodeBalances[node.Id], relaxationFactor),
                StringComparer.Ordinal),
            state.Definition.Pipes.ToDictionary(
                static pipe => pipe.Id,
                pipe => Blend(current.PipeMassFlowRates[pipe.Id], target.PipeMassFlowRates[pipe.Id], relaxationFactor),
                StringComparer.Ordinal),
            state.Definition.Valves.ToDictionary(
                static valve => valve.Id,
                valve => Blend(current.ValveMassFlowRates[valve.Id], target.ValveMassFlowRates[valve.Id], relaxationFactor),
                StringComparer.Ordinal),
            state.Definition.Pumps.ToDictionary(
                static pump => pump.Id,
                pump => Blend(current.PumpMassFlowRates[pump.Id], target.PumpMassFlowRates[pump.Id], relaxationFactor),
                StringComparer.Ordinal),
            Blend(current.PumpHydraulicPowerExchange, target.PumpHydraulicPowerExchange, relaxationFactor));

    private static FluidNodeBalance Blend(FluidNodeBalance current, FluidNodeBalance target, double alpha)
    {
        var beta = 1d - alpha;
        return new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(
                (beta * current.NetMassFlowRate.KilogramsPerSecond)
                + (alpha * target.NetMassFlowRate.KilogramsPerSecond)),
            Power.FromWatts(
                (beta * current.NetEnergyRate.Watts)
                + (alpha * target.NetEnergyRate.Watts)));
    }

    private static MassFlowRate Blend(MassFlowRate current, MassFlowRate target, double alpha)
    {
        var beta = 1d - alpha;
        return MassFlowRate.FromKilogramsPerSecond(
            (beta * current.KilogramsPerSecond)
            + (alpha * target.KilogramsPerSecond));
    }

    private static Power Blend(Power current, Power target, double alpha)
    {
        var beta = 1d - alpha;
        return Power.FromWatts(
            (beta * current.Watts)
            + (alpha * target.Watts));
    }

    private static double MaximumRelativePressureDifference(PlantState left, PlantState right)
    {
        var rightNodes = right.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;

        foreach (var leftNode in left.FluidNodes)
        {
            var rightNode = rightNodes[leftNode.Id];
            var leftPressure = leftNode.Pressure.Pascals;
            var rightPressure = rightNode.Pressure.Pascals;
            var scale = Math.Max(Math.Max(Math.Abs(leftPressure), Math.Abs(rightPressure)), 1_000d);
            maximum = Math.Max(maximum, Math.Abs(rightPressure - leftPressure) / scale);
        }

        return maximum;
    }

    private static double MaximumAbsoluteFlowDifference(
        HydraulicIterate appliedIterate,
        SemiImplicitHydraulicEvaluation unrelaxedMap)
    {
        var maximum = 0d;
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.PipeMassFlowRates, unrelaxedMap.PipeMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.ValveMassFlowRates, unrelaxedMap.ValveMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.PumpMassFlowRates, unrelaxedMap.PumpMassFlowRates));
        return maximum;
    }

    private static double MaximumDifference(
        IReadOnlyDictionary<string, MassFlowRate> applied,
        IReadOnlyDictionary<string, MassFlowRate> mapped)
    {
        var maximum = 0d;
        foreach (var entry in applied)
        {
            maximum = Math.Max(
                maximum,
                Math.Abs(mapped[entry.Key].KilogramsPerSecond - entry.Value.KilogramsPerSecond));
        }

        return maximum;
    }

    private static double AppliedMassClosure(HydraulicIterate iterate)
        => Math.Abs(CompensatedSum(iterate.FluidNodeBalances.Values.Select(static item => item.NetMassFlowRate.KilogramsPerSecond)));

    private static double AppliedEnergyOwnershipResidual(HydraulicIterate iterate)
    {
        var hydraulicEnergyRate = CompensatedSum(iterate.FluidNodeBalances.Values.Select(static item => item.NetEnergyRate.Watts));
        return Math.Abs(hydraulicEnergyRate - iterate.PumpHydraulicPowerExchange.Watts);
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

    private static IReadOnlyDictionary<string, FluidNodeBalance> CanonicalCopy(
        IReadOnlyDictionary<string, FluidNodeBalance> source)
    {
        var sorted = new SortedDictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        foreach (var entry in source)
        {
            sorted.Add(entry.Key, entry.Value);
        }

        return new ReadOnlyDictionary<string, FluidNodeBalance>(sorted);
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
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Residual-corrector step time must be greater than zero.");
        }

        var nodeIds = committedState.FluidNodes.Select(static node => node.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var nodeId in frozenNonHydraulicBalances.Keys)
        {
            if (!nodeIds.Contains(nodeId))
            {
                throw new ArgumentException(
                    $"Frozen non-hydraulic balance references unknown fluid node '{nodeId}'.",
                    nameof(frozenNonHydraulicBalances));
            }
        }
    }

    private sealed record HydraulicIterate(
        IReadOnlyDictionary<string, FluidNodeBalance> FluidNodeBalances,
        IReadOnlyDictionary<string, MassFlowRate> PipeMassFlowRates,
        IReadOnlyDictionary<string, MassFlowRate> ValveMassFlowRates,
        IReadOnlyDictionary<string, MassFlowRate> PumpMassFlowRates,
        Power PumpHydraulicPowerExchange);

    private sealed record ResidualSample(
        SemiImplicitHydraulicEvaluation CurrentEvaluation,
        double MaximumRelativePressureFixedPointResidual,
        double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
        double NormalizedMeritResidual,
        bool Converged);
}
