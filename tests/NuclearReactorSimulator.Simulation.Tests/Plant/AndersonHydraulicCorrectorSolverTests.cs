using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class AndersonHydraulicCorrectorSolverTests
{
    private static readonly TimeSpan StiffStep = TimeSpan.FromMilliseconds(100d);

    [Fact]
    public void Options_RejectInvalidMemoryRegularizationSafeguardAndToleranceValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(0, 3, 1e-8d, 16d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 0, 1e-8d, 16d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 9, 1e-8d, 16d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 0d, 16d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 0.9d, 1d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 16d, 0d, 0.5d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 16d, 1d, 1d, 1d / 1024d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 16d, 1d, 0.5d, 0d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 16d, 1d, 0.5d, 1d / 1024d, 0d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndersonHydraulicCorrectorOptions(8, 3, 1e-8d, 16d, 1d, 0.5d, 1d / 1024d, 1e-5d, 0d));
    }

    [Fact]
    public void Step_UsesUnrelaxedResidualAndPreservesStrictAcceptedMeritDecrease()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new AndersonHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var options = AndersonHydraulicCorrectorOptions.H8AuditDefault;

        var result = solver.Step(state, StiffStep, EmptyBalances(), options);

        Assert.True(result.Converged, result.ToString());
        Assert.False(result.LineSearchExhausted);
        Assert.InRange(result.MaximumRelativePressureFixedPointResidual, 0d, options.RelativePressureTolerance);
        Assert.InRange(result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond, 0d, options.AbsoluteFlowToleranceKilogramsPerSecond);
        Assert.InRange(result.NormalizedMeritResidual, 0d, 1d);
        Assert.True(result.IterationCount >= 2);
        Assert.True(result.HydraulicEvaluationCount >= 3);
        Assert.Equal(result.IterationCount - 1, result.AndersonDirectionAcceptances + result.ResidualFallbackAcceptances);
        Assert.True(result.AndersonDirectionAcceptances <= result.AndersonDirectionAttempts);
        Assert.True(result.ResidualFallbackAcceptances <= result.ResidualFallbackAttempts);
        Assert.InRange(result.MaximumAndersonCoefficientL1Norm, 0d, options.MaximumCoefficientL1Norm);

        for (var index = 1; index < result.Iterations.Count; index++)
        {
            Assert.True(
                result.Iterations[index].NormalizedMeritResidual < result.Iterations[index - 1].NormalizedMeritResidual,
                $"Accepted H.8 iterate {index + 1} did not reduce the fixed-point merit residual.");
        }
    }

    [Fact]
    public void Step_DoesNotTreatTinySafeguardedMotionAsFixedPointConvergence()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new AndersonHydraulicCorrectorSolver(new LinearCompressibilityModel());
        var options = new AndersonHydraulicCorrectorOptions(
            maximumIterations: 2,
            memoryDepth: 2,
            regularization: 1e-8d,
            maximumCoefficientL1Norm: 16d,
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
            "A tiny safeguarded step must not qualify unless the unrelaxed fixed-point residual itself is small.");
    }

    [Fact]
    public void Step_IntegratesCommittedInventoryExactlyOnceFromAcceptedAffineHydraulicBalances()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new AndersonHydraulicCorrectorSolver(new LinearCompressibilityModel());
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
        var solver = new AndersonHydraulicCorrectorSolver(new LinearCompressibilityModel());

        var left = solver.Step(state, StiffStep, EmptyBalances());
        var right = solver.Step(state, StiffStep, EmptyBalances());

        Assert.Equal(left.IterationCount, right.IterationCount);
        Assert.Equal(left.Converged, right.Converged);
        Assert.Equal(left.LineSearchExhausted, right.LineSearchExhausted);
        Assert.Equal(left.AndersonDirectionAttempts, right.AndersonDirectionAttempts);
        Assert.Equal(left.AndersonDirectionAcceptances, right.AndersonDirectionAcceptances);
        Assert.Equal(left.ResidualFallbackAttempts, right.ResidualFallbackAttempts);
        Assert.Equal(left.ResidualFallbackAcceptances, right.ResidualFallbackAcceptances);
        Assert.Equal(left.LeastSquaresRejectedCount, right.LeastSquaresRejectedCount);
        Assert.Equal(left.MaximumAndersonCoefficientL1Norm, right.MaximumAndersonCoefficientL1Norm);
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
        var solver = new AndersonHydraulicCorrectorSolver(new LinearCompressibilityModel());
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
            "h8-anderson-stiff-pair",
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
