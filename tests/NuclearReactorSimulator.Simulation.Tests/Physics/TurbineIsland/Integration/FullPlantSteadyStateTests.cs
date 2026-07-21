using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Integration;

public sealed class FullPlantSteadyStateTests
{
    [Fact]
    public void FullPlantState_RejectsCrossDomainStateFromAnotherCanonicalDefinition()
    {
        var left = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var right = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", left.Definition);

        Assert.Throws<ArgumentException>(() => new FullPlantState(
            definition,
            right.PlantState,
            left.TurbineState,
            left.ElectricalState));
    }

    [Fact]
    public void Step_ExposesCanonicalPlantLevelSnapshotWithoutAddingAnotherPhysicalIntegrator()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var state = new FullPlantState(definition, fixture.PlantState, fixture.TurbineState, fixture.ElectricalState);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var solver = new FullPlantSolver(definition, fixture.ThermodynamicModel);

        var result = solver.Step(state, inputs, TimeSpan.FromMilliseconds(1d));

        Assert.Same(result.IntegratedCycleStep.CandidatePlantState, result.CandidateState.PlantState);
        Assert.Same(result.IntegratedCycleStep.CandidateTurbineState, result.CandidateState.TurbineState);
        Assert.Same(result.IntegratedCycleStep.CandidateElectricalState, result.CandidateState.ElectricalState);
        Assert.Same(result.IntegratedCycleStep.Snapshot, result.Snapshot.IntegratedCycle);
        Assert.Same(result.Snapshot.IntegratedCycle.PrimaryCircuit.CandidatePlant, result.Snapshot.CandidatePlant);
        Assert.Equal(result.Snapshot.HeatBalance.ElectricalExportPower, result.Snapshot.GrossElectricalOutputPower);
    }

    [Fact]
    public void PerformanceDiagnostics_LeaveThermalEfficiencyUndefinedWhenReferenceHasNoNuclearHeatInput()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var state = new FullPlantState(definition, fixture.PlantState, fixture.TurbineState, fixture.ElectricalState);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var result = new FullPlantSolver(definition, fixture.ThermodynamicModel)
            .Step(state, inputs, TimeSpan.FromMilliseconds(1d));

        Assert.Equal(Power.Zero, result.Snapshot.Performance.ReactorThermalPower);
        Assert.Null(result.Snapshot.Performance.GrossThermalEfficiencyFraction);
        Assert.Null(result.Snapshot.Performance.TurbineShaftToReactorHeatFraction);
        Assert.True(result.Snapshot.Performance.GeneratorConversionEfficiencyFraction is > 0d and <= 1d);
        Assert.Null(result.Snapshot.Performance.GrossHeatRateJoulesThermalPerJouleElectrical);
    }

    [Fact]
    public void LongRun_FixedInputReferencePoint_IsDeterministicAndKeepsRawClosureResidualsBounded()
    {
        var leftFixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: false, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var rightFixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: false, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var criteria = new FullPlantSteadyStateCriteria(1e-9d, 1e-3d, 1e-9d, 1e-6d, 1e-9d, 1e-3d);
        var left = CreateQuiescentOperatingPoint("reference", leftFixture, criteria);
        var right = CreateQuiescentOperatingPoint("reference", rightFixture, criteria);
        var leftRunner = new FullPlantLongRunRunner(left.Definition, leftFixture.ThermodynamicModel);
        var rightRunner = new FullPlantLongRunRunner(right.Definition, rightFixture.ThermodynamicModel);

        var leftResult = leftRunner.Run(left, 1_000);
        var rightResult = rightRunner.Run(right, 1_000);

        Assert.Equal(TimeSpan.FromSeconds(1d), leftResult.SimulatedDuration);
        Assert.True(leftResult.SteadyStateCriteriaSatisfied);
        Assert.InRange(leftResult.MaximumAbsoluteMassClosureResidualKilograms, 0d, 1e-6d);
        Assert.InRange(leftResult.MaximumAbsoluteFullEnergyPathClosureResidualJoules, 0d, 1e-2d);
        Assert.InRange(Math.Abs(leftResult.MassInventoryDriftKilograms), 0d, 1e-9d);
        Assert.InRange(leftResult.MaximumAbsoluteMassInventoryDriftKilograms, 0d, 1e-9d);
        Assert.InRange(Math.Abs(leftResult.CoupledStoredEnergyDriftJoules), 0d, 1e-3d);
        Assert.InRange(leftResult.MaximumAbsoluteCoupledStoredEnergyDriftJoules, 0d, 1e-3d);
        Assert.InRange(leftResult.MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute, 0d, 1e-9d);
        Assert.InRange(Math.Abs(leftResult.ElectricalOutputDriftWatts), 0d, 1e-6d);
        Assert.InRange(leftResult.MaximumAbsoluteElectricalOutputDriftWatts, 0d, 1e-6d);
        Assert.Equal(leftResult.MassInventoryDriftKilograms, rightResult.MassInventoryDriftKilograms);
        Assert.Equal(leftResult.CoupledStoredEnergyDriftJoules, rightResult.CoupledStoredEnergyDriftJoules);
        Assert.Equal(leftResult.MaximumAbsoluteMassInventoryDriftKilograms, rightResult.MaximumAbsoluteMassInventoryDriftKilograms);
        Assert.Equal(leftResult.MaximumAbsoluteCoupledStoredEnergyDriftJoules, rightResult.MaximumAbsoluteCoupledStoredEnergyDriftJoules);
        Assert.Equal(leftResult.MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute, rightResult.MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute);
        Assert.Equal(leftResult.ElectricalOutputDriftWatts, rightResult.ElectricalOutputDriftWatts);
        Assert.Equal(leftResult.MaximumAbsoluteElectricalOutputDriftWatts, rightResult.MaximumAbsoluteElectricalOutputDriftWatts);
        Assert.Equal(leftResult.AverageElectricalOutputPower, rightResult.AverageElectricalOutputPower);
    }

    [Fact]
    public void Criteria_DoNotCorrectDriftAndCanExplicitlyRejectAReferenceRun()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var criteria = new FullPlantSteadyStateCriteria(0d, 0d, 0d, 0d, 1e-6d, 1e-2d);
        var operatingPoint = CreateOperatingPoint("strict-reference", fixture, criteria);
        var result = new FullPlantLongRunRunner(operatingPoint.Definition, fixture.ThermodynamicModel)
            .Run(operatingPoint, 20);

        Assert.False(result.SteadyStateCriteriaSatisfied);
        Assert.True(
            Math.Abs(result.CoupledStoredEnergyDriftJoules) > 0d
            || result.MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute > 0d
            || Math.Abs(result.ElectricalOutputDriftWatts) > 0d);
    }


    private static FullPlantReferenceOperatingPoint CreateQuiescentOperatingPoint(
        string id,
        global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.Fixture fixture,
        FullPlantSteadyStateCriteria criteria)
    {
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var equalizedFluidNodes = fixture.PlantState.FluidNodes.Select(node => new FluidNodeState(
            node.Definition,
            node.Inventory,
            new FluidThermodynamicState(
                Pressure.FromMegapascals(1d),
                node.Temperature,
                node.Phase,
                node.VaporQuality)));
        var plantState = new PlantState(
            fixture.PlantState.Definition,
            equalizedFluidNodes,
            fixture.PlantState.Valves,
            fixture.PlantState.Pumps.Select(static pump => new PumpState(pump.PumpId, PumpSpeed.Stopped, isRunning: false)),
            fixture.PlantState.ThermalBodies,
            fixture.PlantState.HeatSources);

        var originalGeneratorInputs = fixture.Inputs;
        var originalFeedwater = originalGeneratorInputs.CondensateFeedwaterInputs;
        var originalCondenser = originalFeedwater.CondenserInputs;
        var originalTurbine = originalCondenser.TurbineExpansionInputs;
        var turbineInputs = new TurbineExpansionInputs(
            originalTurbine.Definition,
            originalTurbine.MainSteamInputs,
            originalTurbine.StageGroupInputs.Select(static input => new TurbineStageGroupInput(input.StageGroupId, MassFlowRate.Zero)),
            originalTurbine.RotorInputs);
        var condenserInputs = new CondenserSystemInputs(
            originalCondenser.Definition,
            turbineInputs,
            originalCondenser.CoolingBoundaryInputs);
        var feedwaterInputs = new CondensateFeedwaterSystemInputs(
            originalFeedwater.Definition,
            condenserInputs,
            originalFeedwater.TrainInputs);
        var generatorInputs = new GeneratorGridInputs(
            originalGeneratorInputs.Definition,
            feedwaterInputs,
            originalGeneratorInputs.GeneratorInputs);
        var inputs = new IntegratedSecondaryCycleInputs(definition, generatorInputs);
        var state = new FullPlantState(definition, plantState, fixture.TurbineState, fixture.ElectricalState);

        return new FullPlantReferenceOperatingPoint(
            id,
            definition,
            state,
            inputs,
            TimeSpan.FromMilliseconds(1d),
            criteria);
    }

    private static FullPlantReferenceOperatingPoint CreateOperatingPoint(
        string id,
        global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.Fixture fixture,
        FullPlantSteadyStateCriteria criteria)
    {
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var state = new FullPlantState(definition, fixture.PlantState, fixture.TurbineState, fixture.ElectricalState);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        return new FullPlantReferenceOperatingPoint(
            id,
            definition,
            state,
            inputs,
            TimeSpan.FromMilliseconds(1d),
            criteria);
    }
}
