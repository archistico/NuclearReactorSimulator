using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class JacobianHydraulicCorrectorSolverTests
{
    private static readonly TimeSpan StiffStep = TimeSpan.FromMilliseconds(100d);

    [Fact]
    public void Options_RejectInvalidJacobianSafeguardAndToleranceValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(0, 1e-4d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 0d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 0.2d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, -1d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 0.5d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 0d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 8d, 0d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 8d, 1d, 1d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 0d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 0d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JacobianHydraulicCorrectorOptions(8, 1e-4d, 1e-8d, 1e12d, 8d, 1d, 0.5d, 1d / 1024d, 1e-5d, 0d));
    }

    [Fact]
    public void Step_UsesJacobianDirectionWithStrictMeritSafeguard()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new JacobianHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;

        var result = solver.Step(state, StiffStep, EmptyBalances(), options);

        Assert.True(result.Converged, result.ToString());
        Assert.False(result.LineSearchExhausted);
        Assert.InRange(result.MaximumRelativePressureFixedPointResidual, 0d, options.RelativePressureTolerance);
        Assert.InRange(result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond, 0d, options.AbsoluteFlowToleranceKilogramsPerSecond);
        Assert.InRange(result.NormalizedMeritResidual, 0d, 1d);
        Assert.True(result.IterationCount >= 2);
        Assert.True(result.JacobianBuildAttempts >= 1);
        Assert.True(result.ProbeEvaluationCount > 0);
        Assert.True(result.JacobianDirectionAcceptances <= result.JacobianBuildAttempts);
        Assert.True(result.ResidualFallbackAcceptances <= result.ResidualFallbackAttempts);
        Assert.True(double.IsFinite(result.MaximumPivotConditionEstimate));
        Assert.InRange(result.MaximumNormalizedNewtonStepInfinityNorm, 0d, options.MaximumNormalizedNewtonStep);

        for (var index = 1; index < result.Iterations.Count; index++)
        {
            Assert.True(
                result.Iterations[index].NormalizedMeritResidual < result.Iterations[index - 1].NormalizedMeritResidual,
                $"Accepted H.9 iterate {index + 1} did not reduce the fixed-point merit residual.");
        }
    }

    [Fact]
    public void Step_DoesNotTreatTinyDampedMotionAsFixedPointConvergence()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new JacobianHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var options = new JacobianHydraulicCorrectorOptions(
            maximumIterations: 2,
            finiteDifferenceRelativeStep: 1e-4d,
            jacobianDiagonalRegularization: 1e-8d,
            maximumPivotConditionEstimate: 1e12d,
            maximumNormalizedNewtonStep: 8d,
            initialRelaxationFactor: 1e-6d,
            backtrackingFactor: 0.5d,
            minimumRelaxationFactor: 1e-6d,
            relativePressureTolerance: 1e-5d,
            absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

        var result = solver.Step(state, StiffStep, EmptyBalances(), options);

        Assert.False(result.Converged);
        Assert.True(
            result.MaximumRelativePressureFixedPointResidual > options.RelativePressureTolerance
                || result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond > options.AbsoluteFlowToleranceKilogramsPerSecond,
            "A tiny damped Newton step must not qualify unless the unrelaxed fixed-point residual itself is small.");
    }

    [Fact]
    public void Step_PreservesConservativeHydraulicCoordinatesAndIntegratesCommittedInventoryOnce()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new JacobianHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var frozen = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
        {
            ["a"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(0.1d), Power.FromKilowatts(5d)),
            ["b"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(-0.1d), Power.FromKilowatts(-5d)),
        };

        var result = solver.Step(state, TimeSpan.FromMilliseconds(20d), frozen);
        const double seconds = 0.020d;

        Assert.InRange(result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond, 0d, 1e-8d);
        Assert.InRange(result.AppliedHydraulicEnergyOwnershipResidualWatts, 0d, 1e-3d);
        foreach (var startNode in state.FluidNodes)
        {
            var total = result.AppliedHydraulicBalances[startNode.Id] + frozen[startNode.Id];
            var endNode = result.CandidateState.GetFluidNode(startNode.Id);
            var expectedMass = startNode.Mass.Kilograms + (total.NetMassFlowRate.KilogramsPerSecond * seconds);
            var expectedEnergy = startNode.InternalEnergy.Joules + (total.NetEnergyRate.Watts * seconds);
            Assert.InRange(Math.Abs(endNode.Mass.Kilograms - expectedMass), 0d, 1e-9d);
            Assert.InRange(Math.Abs(endNode.InternalEnergy.Joules - expectedEnergy), 0d, 1e-4d);
        }
    }

    [Fact]
    public void Step_IsExactlyDeterministicForTheSameInputs()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new JacobianHydraulicCorrectorSolver(new LinearCompressibilityModel());

        var left = solver.Step(state, StiffStep, EmptyBalances());
        var right = solver.Step(state, StiffStep, EmptyBalances());

        Assert.Equal(left.IterationCount, right.IterationCount);
        Assert.Equal(left.Converged, right.Converged);
        Assert.Equal(left.LineSearchExhausted, right.LineSearchExhausted);
        Assert.Equal(left.JacobianBuildAttempts, right.JacobianBuildAttempts);
        Assert.Equal(left.JacobianDirectionAcceptances, right.JacobianDirectionAcceptances);
        Assert.Equal(left.JacobianRejectedCount, right.JacobianRejectedCount);
        Assert.Equal(left.ResidualFallbackAttempts, right.ResidualFallbackAttempts);
        Assert.Equal(left.ResidualFallbackAcceptances, right.ResidualFallbackAcceptances);
        Assert.Equal(left.ProbeEvaluationCount, right.ProbeEvaluationCount);
        Assert.Equal(left.MaximumPivotConditionEstimate, right.MaximumPivotConditionEstimate);
        Assert.Equal(left.MaximumNormalizedNewtonStepInfinityNorm, right.MaximumNormalizedNewtonStepInfinityNorm);
        Assert.Equal(left.MaximumRelativePressureFixedPointResidual, right.MaximumRelativePressureFixedPointResidual);
        Assert.Equal(left.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond, right.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond);
        Assert.Equal(left.NormalizedMeritResidual, right.NormalizedMeritResidual);
        Assert.Equal(left.HydraulicEvaluationCount, right.HydraulicEvaluationCount);
        Assert.Equal(left.BacktrackingTrialCount, right.BacktrackingTrialCount);
        Assert.Equal(left.MinimumAcceptedRelaxationFactor, right.MinimumAcceptedRelaxationFactor);
        Assert.True(left.Iterations.SequenceEqual(right.Iterations));
    }

    [Fact]
    public void FrozenBalanceForUnknownNode_IsRejected()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new JacobianHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var frozen = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
        {
            ["missing"] = FluidNodeBalance.Zero,
        };

        Assert.Throws<ArgumentException>(() => solver.Step(state, TimeSpan.FromMilliseconds(10d), frozen));
    }

    private static Dictionary<string, FluidNodeBalance> EmptyBalances() => new(StringComparer.Ordinal);

    private static PlantState CreateTwoNodeState(double massAKilograms, double massBKilograms)
    {
        var nodeA = new FluidNodeDefinition("a", Volume.FromCubicMetres(1d));
        var nodeB = new FluidNodeDefinition("b", Volume.FromCubicMetres(1d));
        var pipe = new PipeDefinition("link", "a", "b", QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100d));
        var definition = new PlantDefinition(
            "h9-jacobian-stiff-pair",
            new[] { nodeA, nodeB },
            new[] { pipe },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var thermodynamics = new LinearCompressibilityModel();

        FluidNodeState State(FluidNodeDefinition node, double mass)
        {
            var inventory = new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromMegajoules(mass * 0.5d));
            return new FluidNodeState(
                node,
                inventory,
                thermodynamics.Resolve(
                    node,
                    inventory,
                    new FluidThermodynamicState(Pressure.FromMegapascals(5d), Temperature.FromDegreesCelsius(250d))));
        }

        return new PlantState(
            definition,
            new[] { State(nodeA, massAKilograms), State(nodeB, massBKilograms) },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private sealed class LinearCompressibilityModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            var pressurePascals = 5_000_000d + ((inventory.Mass.Kilograms - 1_000d) * 100_000d);
            if (pressurePascals <= 0d)
            {
                throw new InvalidOperationException("Synthetic test compressibility left its supported positive-pressure range.");
            }

            return new FluidThermodynamicState(
                Pressure.FromPascals(pressurePascals),
                previousState.Temperature,
                FluidPhase.SubcooledLiquid,
                null);
        }
    }
}
