using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Integration;

public sealed class IntegratedSecondaryCycleSolverTests
{
    [Fact]
    public void Step_ReconcilesClosedMassLoopAndFullThermofluidMechanicalElectricalEnergyPath()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var solver = new IntegratedSecondaryCycleSolver(definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.ElectricalState,
            inputs,
            TimeSpan.FromMilliseconds(1d));
        var audit = result.Snapshot.HeatBalance;

        Assert.Same(result.GeneratorGridStep.Snapshot, result.Snapshot.GeneratorGrid);
        Assert.Equal(MassFlowRate.Zero, audit.ExternalMassFlowRate);
        Assert.InRange(Math.Abs(audit.MassClosureResidualKilograms), 0d, 1e-8d);
        Assert.InRange(Math.Abs(audit.SupplementalPowerClassificationResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(audit.ShaftTransferPowerResidualWatts), 0d, 1e-9d);
        Assert.InRange(Math.Abs(audit.MechanicalToElectricalPowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(audit.CoupledDomainEnergyClosureResidualJoules), 0d, 1e-3d);
        Assert.InRange(Math.Abs(audit.FullEnergyPathClosureResidualJoules), 0d, 1e-3d);
        Assert.True(audit.TurbineShaftPower > Power.Zero);
        Assert.True(audit.ElectricalExportPower > Power.Zero);
        Assert.True(audit.GeneratorConversionLossPower > Power.Zero);
    }

    [Fact]
    public void Step_ProjectsCanonicalHeatBalanceTermsFromTheExistingValidatedSubsystemSnapshots()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var solver = new IntegratedSecondaryCycleSolver(definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.ElectricalState,
            inputs,
            TimeSpan.FromMilliseconds(1d));
        var snapshot = result.Snapshot;
        var audit = snapshot.HeatBalance;

        Assert.Equal(snapshot.PrimaryCircuit.TotalNuclearHeatPower, audit.NuclearHeatInputPower);
        Assert.Equal(snapshot.PrimaryCircuit.Boundaries.NetExternalPower, audit.PrimaryBoundaryNetExternalPower);
        Assert.Equal(snapshot.CondensateFeedwater.TotalThermalConditioningPower, audit.FeedwaterConditioningPower);
        Assert.Equal(snapshot.Condenser.TotalHeatRejectionPower, audit.CondenserHeatRejectionPower);
        Assert.Equal(snapshot.TurbineExpansion.TotalShaftPower, audit.TurbineShaftPower);
        Assert.Equal(snapshot.GeneratorGrid.ElectricalAudit.MechanicalInputPower, audit.GeneratorMechanicalInputPower);
        Assert.Equal(snapshot.TotalElectricalOutputPower, audit.ElectricalExportPower);
        Assert.Equal(snapshot.GeneratorGrid.TotalGeneratorLossPower, audit.GeneratorConversionLossPower);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedCrossDomainState()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 2d,
            gridPhaseDegrees: 1d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var solver = new IntegratedSecondaryCycleSolver(definition, fixture.ThermodynamicModel);

        var left = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.ElectricalState,
            inputs,
            TimeSpan.FromMilliseconds(10d));
        var right = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.ElectricalState,
            inputs,
            TimeSpan.FromMilliseconds(10d));

        Assert.Equal(left.Snapshot.HeatBalance, right.Snapshot.HeatBalance);
        Assert.Equal(left.CandidateElectricalState.GridPhaseAngle, right.CandidateElectricalState.GridPhaseAngle);
        Assert.Equal(left.CandidateTurbineState.GetRotor("rotor"), right.CandidateTurbineState.GetRotor("rotor"));
    }

    [Fact]
    public void RepeatedManualCoupledSteps_RemainMassClosedAndEnergyAuditableWithoutHiddenCorrection()
    {
        var fixture = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d);
        var definition = new IntegratedSecondaryCycleDefinition("secondary-cycle", fixture.Definition);
        var inputs = new IntegratedSecondaryCycleInputs(definition, fixture.Inputs);
        var solver = new IntegratedSecondaryCycleSolver(definition, fixture.ThermodynamicModel);
        var plantState = fixture.PlantState;
        var turbineState = fixture.TurbineState;
        var electricalState = fixture.ElectricalState;

        for (var index = 0; index < 20; index++)
        {
            var step = solver.Step(
                plantState,
                turbineState,
                electricalState,
                inputs,
                TimeSpan.FromMilliseconds(1d));

            Assert.Equal(MassFlowRate.Zero, step.Snapshot.HeatBalance.ExternalMassFlowRate);
            Assert.InRange(Math.Abs(step.Snapshot.HeatBalance.MassClosureResidualKilograms), 0d, 1e-8d);
            Assert.InRange(Math.Abs(step.Snapshot.HeatBalance.FullEnergyPathClosureResidualJoules), 0d, 1e-3d);

            plantState = step.CandidatePlantState;
            turbineState = step.CandidateTurbineState;
            electricalState = step.CandidateElectricalState;
        }
    }
}
