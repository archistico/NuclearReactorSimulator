using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Phase H.9 isolated Jacobian-informed hydraulic root corrector. The production path remains explicit.
/// The solver constructs a deterministic finite-difference Jacobian of the conservative hydraulic fixed-point
/// residual, solves for a damped Newton direction, and safeguards every accepted iterate against the unchanged
/// H.7 pressure/flow merit. If the Jacobian is unusable or its direction cannot reduce merit, the solver falls
/// back deterministically to the H.7 residual direction.
/// </summary>
public sealed class JacobianHydraulicCorrectorSolver
{
    private readonly SemiImplicitHydraulicPrototypeSolver _hydraulicEvaluator;
    private readonly FluidNodeIntegrator _fluidNodeIntegrator;

    public JacobianHydraulicCorrectorSolver(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _hydraulicEvaluator = new SemiImplicitHydraulicPrototypeSolver(thermodynamicModel);
        _fluidNodeIntegrator = new FluidNodeIntegrator(thermodynamicModel);
    }

    public JacobianHydraulicCorrectorStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions? options = null)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        options ??= JacobianHydraulicCorrectorOptions.H9AuditDefault;

        var layout = CoordinateLayout.Create(committedState);
        var hydraulicEvaluationCount = 0;
        var backtrackingTrialCount = 0;
        var jacobianBuildAttempts = 0;
        var jacobianDirectionAcceptances = 0;
        var jacobianRejectedCount = 0;
        var residualFallbackAttempts = 0;
        var residualFallbackAcceptances = 0;
        var probeEvaluationCount = 0;
        var maximumJacobianDimension = 0;
        var maximumPivotConditionEstimate = 0d;
        var maximumNormalizedNewtonStepInfinityNorm = 0d;
        var maximumCoordinateResidualInfinityNorm = 0d;

        var committedEvaluation = Evaluate(committedState, ref hydraulicEvaluationCount);
        var appliedIterate = ProjectConservative(layout, FromEvaluation(committedEvaluation));
        var iterateState = IntegrateFromCommitted(
            committedState,
            CombineBalances(committedState, appliedIterate.FluidNodeBalances, frozenNonHydraulicBalances),
            deltaTime);
        var residual = EvaluateFixedPointResidual(
            committedState,
            iterateState,
            appliedIterate,
            layout,
            deltaTime,
            frozenNonHydraulicBalances,
            options,
            ref hydraulicEvaluationCount);
        maximumCoordinateResidualInfinityNorm = Math.Max(maximumCoordinateResidualInfinityNorm, residual.CoordinateResidualInfinityNorm);

        var iterations = new List<JacobianHydraulicIteration>(options.MaximumIterations)
        {
            ToIterationEvidence(
                iterationIndex: 1,
                directionKind: "explicit-predictor",
                relaxationFactor: 0d,
                backtrackingTrials: 0,
                jacobianDimension: 0,
                probeEvaluations: 0,
                pivotConditionEstimate: 0d,
                normalizedNewtonStepInfinityNorm: 0d,
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
                jacobianBuildAttempts,
                jacobianDirectionAcceptances,
                jacobianRejectedCount,
                residualFallbackAttempts,
                residualFallbackAcceptances,
                probeEvaluationCount,
                maximumJacobianDimension,
                maximumPivotConditionEstimate,
                maximumNormalizedNewtonStepInfinityNorm,
                maximumCoordinateResidualInfinityNorm,
                hydraulicEvaluationCount,
                backtrackingTrialCount,
                minimumAcceptedRelaxationFactor: 0d);
        }

        var minimumAcceptedRelaxationFactor = double.PositiveInfinity;
        for (var iterationIndex = 2; iterationIndex <= options.MaximumIterations; iterationIndex++)
        {
            LineSearchAcceptance? acceptance = null;
            var acceptedDirectionKind = string.Empty;
            var acceptedJacobianDimension = 0;
            var acceptedProbeEvaluations = 0;
            var acceptedPivotConditionEstimate = 0d;
            var acceptedNewtonStepInfinityNorm = 0d;

            jacobianBuildAttempts++;
            var jacobianBuild = TryBuildNewtonTarget(
                committedState,
                deltaTime,
                frozenNonHydraulicBalances,
                options,
                layout,
                appliedIterate,
                residual,
                ref hydraulicEvaluationCount,
                ref probeEvaluationCount);

            maximumJacobianDimension = Math.Max(maximumJacobianDimension, jacobianBuild.Dimension);
            maximumPivotConditionEstimate = Math.Max(maximumPivotConditionEstimate, jacobianBuild.PivotConditionEstimate);
            maximumNormalizedNewtonStepInfinityNorm = Math.Max(maximumNormalizedNewtonStepInfinityNorm, jacobianBuild.NormalizedNewtonStepInfinityNorm);

            if (jacobianBuild.Success && jacobianBuild.TargetIterate is not null)
            {
                acceptance = TryLineSearch(
                    committedState,
                    deltaTime,
                    frozenNonHydraulicBalances,
                    options,
                    layout,
                    appliedIterate,
                    jacobianBuild.TargetIterate,
                    residual,
                    ref hydraulicEvaluationCount,
                    ref backtrackingTrialCount);
                if (acceptance is not null)
                {
                    jacobianDirectionAcceptances++;
                    acceptedDirectionKind = "damped-newton";
                    acceptedJacobianDimension = jacobianBuild.Dimension;
                    acceptedProbeEvaluations = jacobianBuild.ProbeEvaluations;
                    acceptedPivotConditionEstimate = jacobianBuild.PivotConditionEstimate;
                    acceptedNewtonStepInfinityNorm = jacobianBuild.NormalizedNewtonStepInfinityNorm;
                }
            }
            else
            {
                jacobianRejectedCount++;
            }

            if (acceptance is null)
            {
                residualFallbackAttempts++;
                var residualTarget = ProjectConservative(
                    layout,
                    FromEvaluation(residual.CurrentEvaluation));
                acceptance = TryLineSearch(
                    committedState,
                    deltaTime,
                    frozenNonHydraulicBalances,
                    options,
                    layout,
                    appliedIterate,
                    residualTarget,
                    residual,
                    ref hydraulicEvaluationCount,
                    ref backtrackingTrialCount);
                if (acceptance is not null)
                {
                    residualFallbackAcceptances++;
                    acceptedDirectionKind = "residual-fallback";
                    acceptedJacobianDimension = jacobianBuild.Dimension;
                    acceptedProbeEvaluations = jacobianBuild.ProbeEvaluations;
                    acceptedPivotConditionEstimate = jacobianBuild.PivotConditionEstimate;
                    acceptedNewtonStepInfinityNorm = jacobianBuild.NormalizedNewtonStepInfinityNorm;
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
                    jacobianBuildAttempts,
                    jacobianDirectionAcceptances,
                    jacobianRejectedCount,
                    residualFallbackAttempts,
                    residualFallbackAcceptances,
                    probeEvaluationCount,
                    maximumJacobianDimension,
                    maximumPivotConditionEstimate,
                    maximumNormalizedNewtonStepInfinityNorm,
                    maximumCoordinateResidualInfinityNorm,
                    hydraulicEvaluationCount,
                    backtrackingTrialCount,
                    minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                        ? 0d
                        : minimumAcceptedRelaxationFactor);
            }

            iterateState = acceptance.State;
            residual = acceptance.Residual;
            appliedIterate = acceptance.Iterate;
            maximumCoordinateResidualInfinityNorm = Math.Max(maximumCoordinateResidualInfinityNorm, residual.CoordinateResidualInfinityNorm);
            minimumAcceptedRelaxationFactor = Math.Min(minimumAcceptedRelaxationFactor, acceptance.RelaxationFactor);
            iterations.Add(ToIterationEvidence(
                iterationIndex,
                acceptedDirectionKind,
                acceptance.RelaxationFactor,
                acceptance.TrialCount,
                acceptedJacobianDimension,
                acceptedProbeEvaluations,
                acceptedPivotConditionEstimate,
                acceptedNewtonStepInfinityNorm,
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
                    jacobianBuildAttempts,
                    jacobianDirectionAcceptances,
                    jacobianRejectedCount,
                    residualFallbackAttempts,
                    residualFallbackAcceptances,
                    probeEvaluationCount,
                    maximumJacobianDimension,
                    maximumPivotConditionEstimate,
                    maximumNormalizedNewtonStepInfinityNorm,
                    maximumCoordinateResidualInfinityNorm,
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
            jacobianBuildAttempts,
            jacobianDirectionAcceptances,
            jacobianRejectedCount,
            residualFallbackAttempts,
            residualFallbackAcceptances,
            probeEvaluationCount,
            maximumJacobianDimension,
            maximumPivotConditionEstimate,
            maximumNormalizedNewtonStepInfinityNorm,
            maximumCoordinateResidualInfinityNorm,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor: double.IsPositiveInfinity(minimumAcceptedRelaxationFactor)
                ? 0d
                : minimumAcceptedRelaxationFactor);
    }

    private NewtonBuildResult TryBuildNewtonTarget(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions options,
        CoordinateLayout layout,
        HydraulicIterate currentIterate,
        ResidualSample currentResidual,
        ref int hydraulicEvaluationCount,
        ref int probeEvaluationCount)
    {
        var probeEvaluationCountAtStart = probeEvaluationCount;
        var currentCoordinates = Encode(layout, currentIterate);
        var mappedCoordinates = Encode(layout, FromEvaluation(currentResidual.CurrentEvaluation));
        var scales = BuildCoordinateScales(layout, currentCoordinates, mappedCoordinates);
        var baseResidual = BuildNormalizedCoordinateResidual(currentCoordinates, mappedCoordinates, scales);
        var dimension = currentCoordinates.Length;
        if (dimension == 0 || baseResidual.Length != dimension || baseResidual.Any(static value => !double.IsFinite(value)))
        {
            return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart);
        }

        var jacobian = new double[dimension, dimension];
        for (var column = 0; column < dimension; column++)
        {
            var physicalStep = scales[column] * options.FiniteDifferenceRelativeStep;
            if (!double.IsFinite(physicalStep) || physicalStep <= 0d)
            {
                return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart);
            }

            if (!TryEvaluateProbeResidual(
                committedState,
                deltaTime,
                frozenNonHydraulicBalances,
                options,
                layout,
                currentCoordinates,
                scales,
                column,
                physicalStep,
                ref hydraulicEvaluationCount,
                ref probeEvaluationCount,
                out var probeResidual,
                out var normalizedProbeStep))
            {
                return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart);
            }

            for (var row = 0; row < dimension; row++)
            {
                jacobian[row, column] = (probeResidual[row] - baseResidual[row]) / normalizedProbeStep;
                if (!double.IsFinite(jacobian[row, column]))
                {
                    return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart);
                }
            }
        }

        for (var index = 0; index < dimension; index++)
        {
            jacobian[index, index] += options.JacobianDiagonalRegularization;
        }

        var rightHandSide = baseResidual.Select(static value => -value).ToArray();
        if (!TrySolveLinearSystem(
            jacobian,
            rightHandSide,
            options.MaximumPivotConditionEstimate,
            out var normalizedNewtonStep,
            out var pivotConditionEstimate))
        {
            return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart, pivotConditionEstimate);
        }

        var rawStepInfinityNorm = MaximumAbsolute(normalizedNewtonStep);
        if (!double.IsFinite(rawStepInfinityNorm))
        {
            return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart, pivotConditionEstimate);
        }

        var stepScale = rawStepInfinityNorm > options.MaximumNormalizedNewtonStep
            ? options.MaximumNormalizedNewtonStep / rawStepInfinityNorm
            : 1d;
        var appliedStepInfinityNorm = rawStepInfinityNorm * stepScale;
        var targetCoordinates = new double[dimension];
        for (var index = 0; index < dimension; index++)
        {
            targetCoordinates[index] = currentCoordinates[index] + (normalizedNewtonStep[index] * stepScale * scales[index]);
        }

        if (targetCoordinates.Any(static value => !double.IsFinite(value)))
        {
            return NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart, pivotConditionEstimate, appliedStepInfinityNorm);
        }

        var target = Decode(layout, targetCoordinates);
        return IsFinite(target)
            ? NewtonBuildResult.Accepted(target, dimension, probeEvaluationCount - probeEvaluationCountAtStart, pivotConditionEstimate, appliedStepInfinityNorm)
            : NewtonBuildResult.Rejected(dimension, probeEvaluationCount - probeEvaluationCountAtStart, pivotConditionEstimate, appliedStepInfinityNorm);
    }

    private bool TryEvaluateProbeResidual(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions options,
        CoordinateLayout layout,
        IReadOnlyList<double> currentCoordinates,
        IReadOnlyList<double> scales,
        int coordinateIndex,
        double physicalStep,
        ref int hydraulicEvaluationCount,
        ref int probeEvaluationCount,
        out double[] normalizedResidual,
        out double normalizedProbeStep)
    {
        if (TryEvaluateProbeResidualWithSignedStep(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            options,
            layout,
            currentCoordinates,
            scales,
            coordinateIndex,
            physicalStep,
            ref hydraulicEvaluationCount,
            ref probeEvaluationCount,
            out normalizedResidual))
        {
            normalizedProbeStep = options.FiniteDifferenceRelativeStep;
            return true;
        }

        if (TryEvaluateProbeResidualWithSignedStep(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            options,
            layout,
            currentCoordinates,
            scales,
            coordinateIndex,
            -physicalStep,
            ref hydraulicEvaluationCount,
            ref probeEvaluationCount,
            out normalizedResidual))
        {
            normalizedProbeStep = -options.FiniteDifferenceRelativeStep;
            return true;
        }

        normalizedResidual = Array.Empty<double>();
        normalizedProbeStep = 0d;
        return false;
    }

    private bool TryEvaluateProbeResidualWithSignedStep(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions options,
        CoordinateLayout layout,
        IReadOnlyList<double> currentCoordinates,
        IReadOnlyList<double> scales,
        int coordinateIndex,
        double signedPhysicalStep,
        ref int hydraulicEvaluationCount,
        ref int probeEvaluationCount,
        out double[] normalizedResidual)
    {
        var probeCoordinates = currentCoordinates.ToArray();
        probeCoordinates[coordinateIndex] += signedPhysicalStep;
        var probeIterate = Decode(layout, probeCoordinates);
        probeEvaluationCount++;

        if (!TryEvaluateTrial(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            options,
            layout,
            probeIterate,
            ref hydraulicEvaluationCount,
            out _,
            out var probeResidual)
            || probeResidual is null
            || !double.IsFinite(probeResidual.NormalizedMeritResidual))
        {
            normalizedResidual = Array.Empty<double>();
            return false;
        }

        var mappedCoordinates = Encode(layout, FromEvaluation(probeResidual.CurrentEvaluation));
        normalizedResidual = BuildNormalizedCoordinateResidual(probeCoordinates, mappedCoordinates, scales);
        return normalizedResidual.All(static value => double.IsFinite(value));
    }

    private LineSearchAcceptance? TryLineSearch(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions options,
        CoordinateLayout layout,
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
            var trialIterate = BlendIterate(layout, currentIterate, targetIterate, relaxationFactor);
            if (TryEvaluateTrial(
                committedState,
                deltaTime,
                frozenNonHydraulicBalances,
                options,
                layout,
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
        JacobianHydraulicCorrectorOptions options,
        CoordinateLayout layout,
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
                layout,
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
        CoordinateLayout layout,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        JacobianHydraulicCorrectorOptions options,
        ref int hydraulicEvaluationCount)
    {
        var currentEvaluation = Evaluate(iterateState, ref hydraulicEvaluationCount);
        try
        {
            var mappedIterate = ProjectConservative(layout, FromEvaluation(currentEvaluation));
            var mappedState = IntegrateFromCommitted(
                committedState,
                CombineBalances(committedState, mappedIterate.FluidNodeBalances, frozenNonHydraulicBalances),
                deltaTime);
            var pressureResidual = MaximumRelativePressureDifference(iterateState, mappedState);
            var flowResidual = MaximumAbsoluteFlowDifference(appliedIterate, currentEvaluation);
            var normalizedMerit = Math.Max(
                pressureResidual / options.RelativePressureTolerance,
                flowResidual / options.AbsoluteFlowToleranceKilogramsPerSecond);
            var appliedCoordinates = Encode(layout, appliedIterate);
            var mappedCoordinates = Encode(layout, FromEvaluation(currentEvaluation));
            var coordinateScales = BuildCoordinateScales(layout, appliedCoordinates, mappedCoordinates);
            var coordinateResidual = BuildNormalizedCoordinateResidual(appliedCoordinates, mappedCoordinates, coordinateScales);
            var coordinateResidualInfinityNorm = MaximumAbsolute(coordinateResidual);

            if (!double.IsFinite(normalizedMerit) || !double.IsFinite(coordinateResidualInfinityNorm))
            {
                throw new ArithmeticException("Jacobian hydraulic corrector produced a non-finite fixed-point residual.");
            }

            return new ResidualSample(
                currentEvaluation,
                pressureResidual,
                flowResidual,
                normalizedMerit,
                coordinateResidualInfinityNorm,
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
                double.PositiveInfinity,
                Converged: false);
        }
    }

    private static double[] BuildCoordinateScales(
        CoordinateLayout layout,
        IReadOnlyList<double> current,
        IReadOnlyList<double> mapped)
    {
        var scales = new double[current.Count];
        for (var index = 0; index < scales.Length; index++)
        {
            var floor = layout.CoordinateKinds[index] switch
            {
                CoordinateKind.MassBalance => 1d,
                CoordinateKind.EnergyBalance => 1_000d,
                CoordinateKind.PumpPower => 1_000d,
                CoordinateKind.Flow => 1d,
                _ => throw new InvalidOperationException("Unknown H.9 coordinate kind."),
            };
            scales[index] = Math.Max(Math.Max(Math.Abs(current[index]), Math.Abs(mapped[index])), floor);
        }

        return scales;
    }

    private static double[] BuildNormalizedCoordinateResidual(
        IReadOnlyList<double> applied,
        IReadOnlyList<double> mapped,
        IReadOnlyList<double> scales)
    {
        var residual = new double[applied.Count];
        for (var index = 0; index < residual.Length; index++)
        {
            residual[index] = (mapped[index] - applied[index]) / scales[index];
        }

        return residual;
    }

    private static bool TrySolveLinearSystem(
        double[,] matrix,
        double[] rightHandSide,
        double maximumPivotConditionEstimate,
        out double[] solution,
        out double pivotConditionEstimate)
    {
        var size = rightHandSide.Length;
        solution = Array.Empty<double>();
        pivotConditionEstimate = 0d;
        var augmented = new double[size, size + 1];
        var rowScales = new double[size];
        for (var row = 0; row < size; row++)
        {
            var maximum = 0d;
            for (var column = 0; column < size; column++)
            {
                augmented[row, column] = matrix[row, column];
                maximum = Math.Max(maximum, Math.Abs(matrix[row, column]));
            }

            augmented[row, size] = rightHandSide[row];
            rowScales[row] = maximum;
            if (!double.IsFinite(maximum) || maximum <= 1e-14d)
            {
                return false;
            }
        }

        var maximumPivot = 0d;
        var minimumPivot = double.PositiveInfinity;
        for (var pivotColumn = 0; pivotColumn < size; pivotColumn++)
        {
            var pivotRow = -1;
            var bestScaledMagnitude = -1d;
            for (var row = pivotColumn; row < size; row++)
            {
                var scaledMagnitude = Math.Abs(augmented[row, pivotColumn]) / rowScales[row];
                if (scaledMagnitude > bestScaledMagnitude)
                {
                    bestScaledMagnitude = scaledMagnitude;
                    pivotRow = row;
                }
            }

            if (pivotRow < 0)
            {
                return false;
            }

            var pivotMagnitude = Math.Abs(augmented[pivotRow, pivotColumn]);
            if (!double.IsFinite(pivotMagnitude) || pivotMagnitude <= 1e-14d)
            {
                return false;
            }

            maximumPivot = Math.Max(maximumPivot, pivotMagnitude);
            minimumPivot = Math.Min(minimumPivot, pivotMagnitude);
            pivotConditionEstimate = maximumPivot / minimumPivot;
            if (!double.IsFinite(pivotConditionEstimate) || pivotConditionEstimate > maximumPivotConditionEstimate)
            {
                return false;
            }

            if (pivotRow != pivotColumn)
            {
                SwapRows(augmented, pivotRow, pivotColumn, size + 1);
                (rowScales[pivotRow], rowScales[pivotColumn]) = (rowScales[pivotColumn], rowScales[pivotRow]);
            }

            var pivot = augmented[pivotColumn, pivotColumn];
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var factor = augmented[row, pivotColumn] / pivot;
                if (!double.IsFinite(factor))
                {
                    return false;
                }

                augmented[row, pivotColumn] = 0d;
                for (var column = pivotColumn + 1; column <= size; column++)
                {
                    augmented[row, column] -= factor * augmented[pivotColumn, column];
                }
            }
        }

        solution = new double[size];
        for (var row = size - 1; row >= 0; row--)
        {
            var value = augmented[row, size];
            for (var column = row + 1; column < size; column++)
            {
                value -= augmented[row, column] * solution[column];
            }

            var pivot = augmented[row, row];
            if (!double.IsFinite(pivot) || Math.Abs(pivot) <= 1e-14d)
            {
                solution = Array.Empty<double>();
                return false;
            }

            solution[row] = value / pivot;
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

    private static JacobianHydraulicCorrectorStepResult BuildResult(
        PlantState candidateState,
        ResidualSample residual,
        HydraulicIterate appliedIterate,
        IReadOnlyList<JacobianHydraulicIteration> iterations,
        bool converged,
        bool lineSearchExhausted,
        int jacobianBuildAttempts,
        int jacobianDirectionAcceptances,
        int jacobianRejectedCount,
        int residualFallbackAttempts,
        int residualFallbackAcceptances,
        int probeEvaluationCount,
        int maximumJacobianDimension,
        double maximumPivotConditionEstimate,
        double maximumNormalizedNewtonStepInfinityNorm,
        double maximumCoordinateResidualInfinityNorm,
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
            jacobianBuildAttempts,
            jacobianDirectionAcceptances,
            jacobianRejectedCount,
            residualFallbackAttempts,
            residualFallbackAcceptances,
            probeEvaluationCount,
            maximumJacobianDimension,
            maximumPivotConditionEstimate,
            maximumNormalizedNewtonStepInfinityNorm,
            maximumCoordinateResidualInfinityNorm,
            residual.MaximumRelativePressureFixedPointResidual,
            residual.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            residual.NormalizedMeritResidual,
            hydraulicEvaluationCount,
            backtrackingTrialCount,
            minimumAcceptedRelaxationFactor,
            iterations.ToArray());

    private static JacobianHydraulicIteration ToIterationEvidence(
        int iterationIndex,
        string directionKind,
        double relaxationFactor,
        int backtrackingTrials,
        int jacobianDimension,
        int probeEvaluations,
        double pivotConditionEstimate,
        double normalizedNewtonStepInfinityNorm,
        ResidualSample residual)
        => new(
            iterationIndex,
            directionKind,
            relaxationFactor,
            backtrackingTrials,
            jacobianDimension,
            probeEvaluations,
            pivotConditionEstimate,
            normalizedNewtonStepInfinityNorm,
            residual.CoordinateResidualInfinityNorm,
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

    private static HydraulicIterate ProjectConservative(CoordinateLayout layout, HydraulicIterate iterate)
        => Decode(layout, Encode(layout, iterate));

    private static double[] Encode(CoordinateLayout layout, HydraulicIterate iterate)
    {
        var coordinates = new double[layout.Dimension];
        var index = 0;
        for (var nodeIndex = 0; nodeIndex < layout.NonAnchorNodeIds.Count; nodeIndex++)
        {
            coordinates[index++] = iterate.FluidNodeBalances[layout.NonAnchorNodeIds[nodeIndex]].NetMassFlowRate.KilogramsPerSecond;
        }

        for (var nodeIndex = 0; nodeIndex < layout.NonAnchorNodeIds.Count; nodeIndex++)
        {
            coordinates[index++] = iterate.FluidNodeBalances[layout.NonAnchorNodeIds[nodeIndex]].NetEnergyRate.Watts;
        }

        coordinates[index++] = iterate.PumpHydraulicPowerExchange.Watts;
        foreach (var id in layout.PipeIds)
        {
            coordinates[index++] = iterate.PipeMassFlowRates[id].KilogramsPerSecond;
        }

        foreach (var id in layout.ValveIds)
        {
            coordinates[index++] = iterate.ValveMassFlowRates[id].KilogramsPerSecond;
        }

        foreach (var id in layout.PumpIds)
        {
            coordinates[index++] = iterate.PumpMassFlowRates[id].KilogramsPerSecond;
        }

        return coordinates;
    }

    private static HydraulicIterate Decode(CoordinateLayout layout, IReadOnlyList<double> coordinates)
    {
        if (coordinates.Count != layout.Dimension)
        {
            throw new ArgumentException("H.9 coordinate vector does not match the conservative layout dimension.", nameof(coordinates));
        }

        var massRates = new Dictionary<string, double>(StringComparer.Ordinal);
        var energyRates = new Dictionary<string, double>(StringComparer.Ordinal);
        var index = 0;
        var massSum = 0d;
        foreach (var nodeId in layout.NonAnchorNodeIds)
        {
            var value = coordinates[index++];
            massRates.Add(nodeId, value);
            massSum += value;
        }

        massRates.Add(layout.AnchorNodeId, -massSum);

        var energySum = 0d;
        foreach (var nodeId in layout.NonAnchorNodeIds)
        {
            var value = coordinates[index++];
            energyRates.Add(nodeId, value);
            energySum += value;
        }

        var pumpPowerWatts = coordinates[index++];
        energyRates.Add(layout.AnchorNodeId, pumpPowerWatts - energySum);

        var balances = layout.AllNodeIds.ToDictionary(
            static id => id,
            id => new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond(massRates[id]),
                Power.FromWatts(energyRates[id])),
            StringComparer.Ordinal);

        var pipeFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        foreach (var id in layout.PipeIds)
        {
            pipeFlows.Add(id, MassFlowRate.FromKilogramsPerSecond(coordinates[index++]));
        }

        var valveFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        foreach (var id in layout.ValveIds)
        {
            valveFlows.Add(id, MassFlowRate.FromKilogramsPerSecond(coordinates[index++]));
        }

        var pumpFlows = new Dictionary<string, MassFlowRate>(StringComparer.Ordinal);
        foreach (var id in layout.PumpIds)
        {
            pumpFlows.Add(id, MassFlowRate.FromKilogramsPerSecond(coordinates[index++]));
        }

        return new HydraulicIterate(
            balances,
            pipeFlows,
            valveFlows,
            pumpFlows,
            Power.FromWatts(pumpPowerWatts));
    }

    private static HydraulicIterate BlendIterate(
        CoordinateLayout layout,
        HydraulicIterate current,
        HydraulicIterate target,
        double relaxationFactor)
    {
        var currentCoordinates = Encode(layout, current);
        var targetCoordinates = Encode(layout, target);
        var blended = new double[currentCoordinates.Length];
        var beta = 1d - relaxationFactor;
        for (var index = 0; index < blended.Length; index++)
        {
            blended[index] = (beta * currentCoordinates[index]) + (relaxationFactor * targetCoordinates[index]);
        }

        return Decode(layout, blended);
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

    private static double MaximumAbsolute(IReadOnlyList<double> values)
    {
        var maximum = 0d;
        foreach (var value in values)
        {
            maximum = Math.Max(maximum, Math.Abs(value));
        }

        return maximum;
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

    private static IReadOnlyDictionary<string, FluidNodeBalance> CombineBalances(
        PlantState state,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulicBalances,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
        => state.FluidNodes.ToDictionary(
            static node => node.Id,
            node => hydraulicBalances[node.Id]
                + (frozenNonHydraulicBalances.TryGetValue(node.Id, out var frozen) ? frozen : FluidNodeBalance.Zero),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, FluidNodeBalance> CanonicalCopy(IReadOnlyDictionary<string, FluidNodeBalance> source)
    {
        var sorted = new SortedDictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        foreach (var entry in source)
        {
            sorted.Add(entry.Key, entry.Value);
        }

        return new ReadOnlyDictionary<string, FluidNodeBalance>(sorted);
    }

    private static bool IsFinite(HydraulicIterate iterate)
        => iterate.FluidNodeBalances.Values.All(static item => double.IsFinite(item.NetMassFlowRate.KilogramsPerSecond) && double.IsFinite(item.NetEnergyRate.Watts))
            && iterate.PipeMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && iterate.ValveMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && iterate.PumpMassFlowRates.Values.All(static item => double.IsFinite(item.KilogramsPerSecond))
            && double.IsFinite(iterate.PumpHydraulicPowerExchange.Watts);

    private static void ValidateStepArguments(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(frozenNonHydraulicBalances);
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Jacobian-corrector step time must be greater than zero.");
        }

        if (committedState.FluidNodes.Count < 2)
        {
            throw new ArgumentException("Jacobian-corrector conservative coordinates require at least two fluid nodes.", nameof(committedState));
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
        double CoordinateResidualInfinityNorm,
        bool Converged);

    private sealed record LineSearchAcceptance(
        PlantState State,
        HydraulicIterate Iterate,
        ResidualSample Residual,
        double RelaxationFactor,
        int TrialCount);

    private sealed record NewtonBuildResult(
        bool Success,
        HydraulicIterate? TargetIterate,
        int Dimension,
        int ProbeEvaluations,
        double PivotConditionEstimate,
        double NormalizedNewtonStepInfinityNorm)
    {
        public static NewtonBuildResult Accepted(
            HydraulicIterate target,
            int dimension,
            int probeEvaluations,
            double pivotConditionEstimate,
            double normalizedNewtonStepInfinityNorm)
            => new(true, target, dimension, probeEvaluations, pivotConditionEstimate, normalizedNewtonStepInfinityNorm);

        public static NewtonBuildResult Rejected(
            int dimension,
            int probeEvaluations,
            double pivotConditionEstimate = 0d,
            double normalizedNewtonStepInfinityNorm = 0d)
            => new(false, null, dimension, probeEvaluations, pivotConditionEstimate, normalizedNewtonStepInfinityNorm);
    }

    private enum CoordinateKind
    {
        MassBalance,
        EnergyBalance,
        PumpPower,
        Flow,
    }

    private sealed record CoordinateLayout(
        IReadOnlyList<string> AllNodeIds,
        IReadOnlyList<string> NonAnchorNodeIds,
        string AnchorNodeId,
        IReadOnlyList<string> PipeIds,
        IReadOnlyList<string> ValveIds,
        IReadOnlyList<string> PumpIds,
        IReadOnlyList<CoordinateKind> CoordinateKinds)
    {
        public int Dimension => CoordinateKinds.Count;

        public static CoordinateLayout Create(PlantState state)
        {
            var nodeIds = state.FluidNodes.Select(static node => node.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
            if (nodeIds.Length < 2)
            {
                throw new ArgumentException("H.9 conservative coordinate layout requires at least two fluid nodes.", nameof(state));
            }

            var nonAnchor = nodeIds.Take(nodeIds.Length - 1).ToArray();
            var pipeIds = state.Definition.Pipes.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
            var valveIds = state.Definition.Valves.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
            var pumpIds = state.Definition.Pumps.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
            var kinds = new List<CoordinateKind>((2 * nonAnchor.Length) + 1 + pipeIds.Length + valveIds.Length + pumpIds.Length);
            kinds.AddRange(Enumerable.Repeat(CoordinateKind.MassBalance, nonAnchor.Length));
            kinds.AddRange(Enumerable.Repeat(CoordinateKind.EnergyBalance, nonAnchor.Length));
            kinds.Add(CoordinateKind.PumpPower);
            kinds.AddRange(Enumerable.Repeat(CoordinateKind.Flow, pipeIds.Length + valveIds.Length + pumpIds.Length));
            return new CoordinateLayout(nodeIds, nonAnchor, nodeIds[^1], pipeIds, valveIds, pumpIds, kinds.ToArray());
        }
    }
}
