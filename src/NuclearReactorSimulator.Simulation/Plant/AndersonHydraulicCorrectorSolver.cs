using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Phase H.8 isolated safeguarded Anderson accelerator. The production path remains explicit.
/// Anderson changes the nonlinear search direction by combining recent unrelaxed hydraulic-map
/// evaluations. Every proposed direction is safeguarded by deterministic backtracking against the
/// same unrelaxed fixed-point merit used by H.7; when Anderson history is unusable or its direction
/// cannot reduce merit, the solver deterministically falls back to the H.7 residual direction.
/// </summary>
public sealed class AndersonHydraulicCorrectorSolver
{
    private readonly SemiImplicitHydraulicPrototypeSolver _hydraulicEvaluator;
    private readonly FluidNodeIntegrator _fluidNodeIntegrator;

    public AndersonHydraulicCorrectorSolver(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _hydraulicEvaluator = new SemiImplicitHydraulicPrototypeSolver(thermodynamicModel);
        _fluidNodeIntegrator = new FluidNodeIntegrator(thermodynamicModel);
    }

    public AndersonHydraulicCorrectorStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        AndersonHydraulicCorrectorOptions? options = null)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        options ??= AndersonHydraulicCorrectorOptions.H8AuditDefault;

        var hydraulicEvaluationCount = 0;
        var backtrackingTrialCount = 0;
        var andersonDirectionAttempts = 0;
        var andersonDirectionAcceptances = 0;
        var residualFallbackAttempts = 0;
        var residualFallbackAcceptances = 0;
        var leastSquaresRejectedCount = 0;
        var maximumAndersonCoefficientL1Norm = 0d;

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

        var history = new List<AndersonHistorySample>(options.MemoryDepth);
        AddHistorySample(history, residual, options.MemoryDepth);
        var iterations = new List<AndersonHydraulicIteration>(options.MaximumIterations)
        {
            ToIterationEvidence(
                iterationIndex: 1,
                directionKind: "explicit-predictor",
                historySampleCount: history.Count,
                relaxationFactor: 0d,
                backtrackingTrials: 0,
                coefficientL1Norm: 0d,
                residual),
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
                andersonDirectionAttempts,
                andersonDirectionAcceptances,
                residualFallbackAttempts,
                residualFallbackAcceptances,
                leastSquaresRejectedCount,
                maximumAndersonCoefficientL1Norm,
                hydraulicEvaluationCount,
                backtrackingTrialCount,
                minimumAcceptedRelaxationFactor: 0d);
        }

        var minimumAcceptedRelaxationFactor = double.PositiveInfinity;
        for (var iterationIndex = 2; iterationIndex <= options.MaximumIterations; iterationIndex++)
        {
            LineSearchAcceptance? acceptance = null;
            var acceptedDirectionKind = string.Empty;
            var acceptedCoefficientL1Norm = 0d;
            var historySampleCount = history.Count;

            if (history.Count >= 2)
            {
                andersonDirectionAttempts++;
                if (TryBuildAndersonTarget(
                    committedState,
                    history,
                    options,
                    out var andersonTarget,
                    out var coefficientL1Norm))
                {
                    maximumAndersonCoefficientL1Norm = Math.Max(maximumAndersonCoefficientL1Norm, coefficientL1Norm);
                    acceptance = TryLineSearch(
                        committedState,
                        deltaTime,
                        frozenNonHydraulicBalances,
                        options,
                        appliedIterate,
                        andersonTarget,
                        residual,
                        ref hydraulicEvaluationCount,
                        ref backtrackingTrialCount);
                    if (acceptance is not null)
                    {
                        andersonDirectionAcceptances++;
                        acceptedDirectionKind = "anderson";
                        acceptedCoefficientL1Norm = coefficientL1Norm;
                    }
                }
                else
                {
                    leastSquaresRejectedCount++;
                }
            }

            if (acceptance is null)
            {
                residualFallbackAttempts++;
                var residualTarget = FromEvaluation(residual.CurrentEvaluation);
                acceptance = TryLineSearch(
                    committedState,
                    deltaTime,
                    frozenNonHydraulicBalances,
                    options,
                    appliedIterate,
                    residualTarget,
                    residual,
                    ref hydraulicEvaluationCount,
                    ref backtrackingTrialCount);
                if (acceptance is not null)
                {
                    residualFallbackAcceptances++;
                    acceptedDirectionKind = "residual-fallback";
                }
            }

            if (acceptance is null)
            {
                return BuildResult(
                    iterateState,
                    residual,
                    appliedIterate,
                    iterations,
                    converged: false,
                    lineSearchExhausted: true,
                    andersonDirectionAttempts,
                    andersonDirectionAcceptances,
                    residualFallbackAttempts,
                    residualFallbackAcceptances,
                    leastSquaresRejectedCount,
                    maximumAndersonCoefficientL1Norm,
                    hydraulicEvaluationCount,
                    backtrackingTrialCount,
                    minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                        ? 0d
                        : minimumAcceptedRelaxationFactor);
            }

            iterateState = acceptance.State;
            residual = acceptance.Residual;
            appliedIterate = acceptance.Iterate;
            minimumAcceptedRelaxationFactor = Math.Min(minimumAcceptedRelaxationFactor, acceptance.RelaxationFactor);
            AddHistorySample(history, residual, options.MemoryDepth);
            iterations.Add(ToIterationEvidence(
                iterationIndex,
                acceptedDirectionKind,
                historySampleCount,
                acceptance.RelaxationFactor,
                acceptance.TrialCount,
                acceptedCoefficientL1Norm,
                residual));

            if (residual.Converged)
            {
                return BuildResult(
                    iterateState,
                    residual,
                    appliedIterate,
                    iterations,
                    converged: true,
                    lineSearchExhausted: false,
                    andersonDirectionAttempts,
                    andersonDirectionAcceptances,
                    residualFallbackAttempts,
                    residualFallbackAcceptances,
                    leastSquaresRejectedCount,
                    maximumAndersonCoefficientL1Norm,
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
            andersonDirectionAttempts,
            andersonDirectionAcceptances,
            residualFallbackAttempts,
            residualFallbackAcceptances,
            leastSquaresRejectedCount,
            maximumAndersonCoefficientL1Norm,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                ? 0d
                : minimumAcceptedRelaxationFactor);
    }

    private LineSearchAcceptance? TryLineSearch(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        AndersonHydraulicCorrectorOptions options,
        HydraulicIterate currentIterate,
        HydraulicIterate targetIterate,
        ResidualSample currentResidual,
        ref int hydraulicEvaluationCount,
        ref int backtrackingTrialCount)
    {
        var relaxationFactor = options.InitialRelaxationFactor;
        var trials = 0;
        while (relaxationFactor >= options.MinimumRelaxationFactor)
        {
            trials++;
            backtrackingTrialCount++;
            var trialIterate = BlendIterate(committedState, currentIterate, targetIterate, relaxationFactor);
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
                && StrictlyReducesMerit(currentResidual.NormalizedMeritResidual, trialResidual.NormalizedMeritResidual))
            {
                return new LineSearchAcceptance(trialState, trialIterate, trialResidual, relaxationFactor, trials);
            }

            relaxationFactor *= options.BacktrackingFactor;
        }

        return null;
    }

    private bool TryEvaluateTrial(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        AndersonHydraulicCorrectorOptions options,
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
        AndersonHydraulicCorrectorOptions options,
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
            var signature = BuildNormalizedResidualSignature(
                iterateState,
                mappedState,
                appliedIterate,
                currentEvaluation,
                options);

            if (!double.IsFinite(normalizedMerit) || signature.Any(static value => !double.IsFinite(value)))
            {
                throw new ArithmeticException("Anderson hydraulic corrector produced a non-finite fixed-point residual.");
            }

            return new ResidualSample(
                currentEvaluation,
                pressureResidual,
                flowResidual,
                normalizedMerit,
                signature,
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
                Array.Empty<double>(),
                Converged: false);
        }
    }

    private static double[] BuildNormalizedResidualSignature(
        PlantState iterateState,
        PlantState mappedState,
        HydraulicIterate appliedIterate,
        SemiImplicitHydraulicEvaluation mappedEvaluation,
        AndersonHydraulicCorrectorOptions options)
    {
        var mappedNodes = mappedState.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var componentCount = iterateState.FluidNodes.Count
            + appliedIterate.PipeMassFlowRates.Count
            + appliedIterate.ValveMassFlowRates.Count
            + appliedIterate.PumpMassFlowRates.Count;
        var signature = new double[componentCount];
        var index = 0;

        foreach (var node in iterateState.FluidNodes)
        {
            var mapped = mappedNodes[node.Id];
            var scale = Math.Max(Math.Max(Math.Abs(node.Pressure.Pascals), Math.Abs(mapped.Pressure.Pascals)), 1_000d);
            signature[index++] = ((mapped.Pressure.Pascals - node.Pressure.Pascals) / scale) / options.RelativePressureTolerance;
        }

        index = AppendFlowResiduals(signature, index, appliedIterate.PipeMassFlowRates, mappedEvaluation.PipeMassFlowRates, options.AbsoluteFlowToleranceKilogramsPerSecond);
        index = AppendFlowResiduals(signature, index, appliedIterate.ValveMassFlowRates, mappedEvaluation.ValveMassFlowRates, options.AbsoluteFlowToleranceKilogramsPerSecond);
        _ = AppendFlowResiduals(signature, index, appliedIterate.PumpMassFlowRates, mappedEvaluation.PumpMassFlowRates, options.AbsoluteFlowToleranceKilogramsPerSecond);
        return signature;
    }

    private static int AppendFlowResiduals(
        double[] destination,
        int startIndex,
        IReadOnlyDictionary<string, MassFlowRate> applied,
        IReadOnlyDictionary<string, MassFlowRate> mapped,
        double tolerance)
    {
        var index = startIndex;
        foreach (var entry in applied.OrderBy(static item => item.Key, StringComparer.Ordinal))
        {
            destination[index++] = (mapped[entry.Key].KilogramsPerSecond - entry.Value.KilogramsPerSecond) / tolerance;
        }

        return index;
    }

    private static void AddHistorySample(List<AndersonHistorySample> history, ResidualSample residual, int memoryDepth)
    {
        if (residual.NormalizedResidualSignature.Count == 0
            || residual.NormalizedResidualSignature.Any(static value => !double.IsFinite(value)))
        {
            return;
        }

        history.Add(new AndersonHistorySample(
            FromEvaluation(residual.CurrentEvaluation),
            residual.NormalizedResidualSignature.ToArray()));
        while (history.Count > memoryDepth)
        {
            history.RemoveAt(0);
        }
    }

    private static bool TryBuildAndersonTarget(
        PlantState state,
        IReadOnlyList<AndersonHistorySample> history,
        AndersonHydraulicCorrectorOptions options,
        out HydraulicIterate target,
        out double coefficientL1Norm)
    {
        if (!TrySolveAffineResidualMinimization(history, options.Regularization, out var coefficients))
        {
            target = history[^1].MappedIterate;
            coefficientL1Norm = double.PositiveInfinity;
            return false;
        }

        var coefficientSum = coefficients.Sum();
        if (!double.IsFinite(coefficientSum)
            || Math.Abs(coefficientSum) <= 1e-12d
            || Math.Abs(coefficientSum - 1d) > 1e-6d)
        {
            target = history[^1].MappedIterate;
            coefficientL1Norm = double.PositiveInfinity;
            return false;
        }

        for (var index = 0; index < coefficients.Length; index++)
        {
            coefficients[index] /= coefficientSum;
        }

        coefficientL1Norm = coefficients.Sum(static value => Math.Abs(value));
        if (!double.IsFinite(coefficientL1Norm) || coefficientL1Norm > options.MaximumCoefficientL1Norm)
        {
            target = history[^1].MappedIterate;
            return false;
        }

        target = AffineCombineMappedIterates(state, history, coefficients);
        return IsFinite(target);
    }

    private static bool TrySolveAffineResidualMinimization(
        IReadOnlyList<AndersonHistorySample> history,
        double regularization,
        out double[] coefficients)
    {
        var sampleCount = history.Count;
        coefficients = Array.Empty<double>();
        if (sampleCount < 2)
        {
            return false;
        }

        var residualLength = history[0].NormalizedResidualSignature.Count;
        if (residualLength == 0 || history.Any(item => item.NormalizedResidualSignature.Count != residualLength))
        {
            return false;
        }

        var systemSize = sampleCount + 1;
        var matrix = new double[systemSize, systemSize];
        var rightHandSide = new double[systemSize];
        var maximumDiagonal = 1d;

        for (var row = 0; row < sampleCount; row++)
        {
            for (var column = row; column < sampleCount; column++)
            {
                var dot = Dot(history[row].NormalizedResidualSignature, history[column].NormalizedResidualSignature);
                if (!double.IsFinite(dot))
                {
                    return false;
                }

                matrix[row, column] = dot;
                matrix[column, row] = dot;
            }

            maximumDiagonal = Math.Max(maximumDiagonal, Math.Abs(matrix[row, row]));
        }

        var diagonalRegularization = regularization * maximumDiagonal;
        for (var index = 0; index < sampleCount; index++)
        {
            matrix[index, index] += diagonalRegularization;
            matrix[index, sampleCount] = 1d;
            matrix[sampleCount, index] = 1d;
        }

        rightHandSide[sampleCount] = 1d;
        if (!TrySolveLinearSystem(matrix, rightHandSide, out var solution))
        {
            return false;
        }

        coefficients = solution.Take(sampleCount).ToArray();
        return coefficients.All(static value => double.IsFinite(value));
    }

    private static bool TrySolveLinearSystem(double[,] matrix, double[] rightHandSide, out double[] solution)
    {
        var size = rightHandSide.Length;
        solution = Array.Empty<double>();
        var augmented = new double[size, size + 1];
        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                augmented[row, column] = matrix[row, column];
            }

            augmented[row, size] = rightHandSide[row];
        }

        for (var pivotColumn = 0; pivotColumn < size; pivotColumn++)
        {
            var pivotRow = pivotColumn;
            var pivotMagnitude = Math.Abs(augmented[pivotRow, pivotColumn]);
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var candidateMagnitude = Math.Abs(augmented[row, pivotColumn]);
                if (candidateMagnitude > pivotMagnitude)
                {
                    pivotMagnitude = candidateMagnitude;
                    pivotRow = row;
                }
            }

            if (!double.IsFinite(pivotMagnitude) || pivotMagnitude <= 1e-14d)
            {
                return false;
            }

            if (pivotRow != pivotColumn)
            {
                SwapRows(augmented, pivotRow, pivotColumn, size + 1);
            }

            var pivot = augmented[pivotColumn, pivotColumn];
            for (var column = pivotColumn; column <= size; column++)
            {
                augmented[pivotColumn, column] /= pivot;
            }

            for (var row = 0; row < size; row++)
            {
                if (row == pivotColumn)
                {
                    continue;
                }

                var factor = augmented[row, pivotColumn];
                if (factor == 0d)
                {
                    continue;
                }

                for (var column = pivotColumn; column <= size; column++)
                {
                    augmented[row, column] -= factor * augmented[pivotColumn, column];
                }
            }
        }

        solution = new double[size];
        for (var row = 0; row < size; row++)
        {
            solution[row] = augmented[row, size];
            if (!double.IsFinite(solution[row]))
            {
                solution = Array.Empty<double>();
                return false;
            }
        }

        return true;
    }

    private static void SwapRows(double[,] matrix, int left, int right, int width)
    {
        for (var column = 0; column < width; column++)
        {
            var temporary = matrix[left, column];
            matrix[left, column] = matrix[right, column];
            matrix[right, column] = temporary;
        }
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var sum = 0d;
        var compensation = 0d;
        for (var index = 0; index < left.Count; index++)
        {
            var product = left[index] * right[index];
            var adjusted = product - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
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

    private static AndersonHydraulicCorrectorStepResult BuildResult(
        PlantState candidateState,
        ResidualSample residual,
        HydraulicIterate appliedIterate,
        IReadOnlyList<AndersonHydraulicIteration> iterations,
        bool converged,
        bool lineSearchExhausted,
        int andersonDirectionAttempts,
        int andersonDirectionAcceptances,
        int residualFallbackAttempts,
        int residualFallbackAcceptances,
        int leastSquaresRejectedCount,
        double maximumAndersonCoefficientL1Norm,
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
            andersonDirectionAttempts,
            andersonDirectionAcceptances,
            residualFallbackAttempts,
            residualFallbackAcceptances,
            leastSquaresRejectedCount,
            maximumAndersonCoefficientL1Norm,
            residual.MaximumRelativePressureFixedPointResidual,
            residual.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            residual.NormalizedMeritResidual,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor,
            iterations.ToArray());

    private static AndersonHydraulicIteration ToIterationEvidence(
        int iterationIndex,
        string directionKind,
        int historySampleCount,
        double relaxationFactor,
        int backtrackingTrials,
        double coefficientL1Norm,
        ResidualSample residual)
        => new(
            iterationIndex,
            directionKind,
            historySampleCount,
            relaxationFactor,
            backtrackingTrials,
            coefficientL1Norm,
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

    private static HydraulicIterate AffineCombineMappedIterates(
        PlantState state,
        IReadOnlyList<AndersonHistorySample> history,
        IReadOnlyList<double> coefficients)
        => new(
            state.FluidNodes.ToDictionary(
                static node => node.Id,
                node => WeightedBalance(history, coefficients, node.Id),
                StringComparer.Ordinal),
            state.Definition.Pipes.ToDictionary(
                static pipe => pipe.Id,
                pipe => WeightedFlow(history, coefficients, pipe.Id, static sample => sample.MappedIterate.PipeMassFlowRates),
                StringComparer.Ordinal),
            state.Definition.Valves.ToDictionary(
                static valve => valve.Id,
                valve => WeightedFlow(history, coefficients, valve.Id, static sample => sample.MappedIterate.ValveMassFlowRates),
                StringComparer.Ordinal),
            state.Definition.Pumps.ToDictionary(
                static pump => pump.Id,
                pump => WeightedFlow(history, coefficients, pump.Id, static sample => sample.MappedIterate.PumpMassFlowRates),
                StringComparer.Ordinal),
            Power.FromWatts(WeightedSum(history, coefficients, static sample => sample.MappedIterate.PumpHydraulicPowerExchange.Watts)));

    private static FluidNodeBalance WeightedBalance(
        IReadOnlyList<AndersonHistorySample> history,
        IReadOnlyList<double> coefficients,
        string nodeId)
        => new(
            MassFlowRate.FromKilogramsPerSecond(WeightedSum(history, coefficients, sample => sample.MappedIterate.FluidNodeBalances[nodeId].NetMassFlowRate.KilogramsPerSecond)),
            Power.FromWatts(WeightedSum(history, coefficients, sample => sample.MappedIterate.FluidNodeBalances[nodeId].NetEnergyRate.Watts)));

    private static MassFlowRate WeightedFlow(
        IReadOnlyList<AndersonHistorySample> history,
        IReadOnlyList<double> coefficients,
        string id,
        Func<AndersonHistorySample, IReadOnlyDictionary<string, MassFlowRate>> selector)
        => MassFlowRate.FromKilogramsPerSecond(WeightedSum(history, coefficients, sample => selector(sample)[id].KilogramsPerSecond));

    private static double WeightedSum(
        IReadOnlyList<AndersonHistorySample> history,
        IReadOnlyList<double> coefficients,
        Func<AndersonHistorySample, double> selector)
    {
        var sum = 0d;
        var compensation = 0d;
        for (var index = 0; index < history.Count; index++)
        {
            var value = coefficients[index] * selector(history[index]);
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }

    private static bool IsFinite(HydraulicIterate iterate)
        => iterate.FluidNodeBalances.Values.All(static item => double.IsFinite(item.NetMassFlowRate.KilogramsPerSecond) && double.IsFinite(item.NetEnergyRate.Watts))
            && iterate.PipeMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && iterate.ValveMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && iterate.PumpMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && double.IsFinite(iterate.PumpHydraulicPowerExchange.Watts);

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
        HydraulicIterate target,
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
            MassFlowRate.FromKilogramsPerSecond((beta * current.NetMassFlowRate.KilogramsPerSecond) + (alpha * target.NetMassFlowRate.KilogramsPerSecond)),
            Power.FromWatts((beta * current.NetEnergyRate.Watts) + (alpha * target.NetEnergyRate.Watts)));
    }

    private static MassFlowRate Blend(MassFlowRate current, MassFlowRate target, double alpha)
    {
        var beta = 1d - alpha;
        return MassFlowRate.FromKilogramsPerSecond((beta * current.KilogramsPerSecond) + (alpha * target.KilogramsPerSecond));
    }

    private static Power Blend(Power current, Power target, double alpha)
    {
        var beta = 1d - alpha;
        return Power.FromWatts((beta * current.Watts) + (alpha * target.Watts));
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

    private static double MaximumAbsoluteFlowDifference(HydraulicIterate appliedIterate, SemiImplicitHydraulicEvaluation unrelaxedMap)
    {
        var maximum = 0d;
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.PipeMassFlowRates, unrelaxedMap.PipeMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.ValveMassFlowRates, unrelaxedMap.ValveMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(appliedIterate.PumpMassFlowRates, unrelaxedMap.PumpMassFlowRates));
        return maximum;
    }

    private static double MaximumDifference(IReadOnlyDictionary<string, MassFlowRate> applied, IReadOnlyDictionary<string, MassFlowRate> mapped)
    {
        var maximum = 0d;
        foreach (var entry in applied)
        {
            maximum = Math.Max(maximum, Math.Abs(mapped[entry.Key].KilogramsPerSecond - entry.Value.KilogramsPerSecond));
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

    private static IReadOnlyDictionary<string, FluidNodeBalance> CanonicalCopy(IReadOnlyDictionary<string, FluidNodeBalance> source)
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
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Anderson-corrector step time must be greater than zero.");
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
        IReadOnlyList<double> NormalizedResidualSignature,
        bool Converged);

    private sealed record AndersonHistorySample(HydraulicIterate MappedIterate, IReadOnlyList<double> NormalizedResidualSignature);

    private sealed record LineSearchAcceptance(
        PlantState State,
        HydraulicIterate Iterate,
        ResidualSample Residual,
        double RelaxationFactor,
        int TrialCount);
}
