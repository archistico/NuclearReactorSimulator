using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-D.2 audit-only evidence for the current-v2 turbine admission resistance budget.
/// These tests do not retune production physics. They freeze the canonical resistance/characteristic map so the
/// subsequent authority correction can be selected from explicit evidence rather than from trial-and-error tuning.
/// </summary>
public sealed class TurbineAdmissionAuthorityAuditTests
{
    private static readonly double[] AuditPositionsPercent = [10d, 20d, 28d, 40d, 46d, 60d, 80d, 100d];

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineAdmissionAuthorityAudit")]
    public void CurrentV2AdmissionResistanceBudget_ProducesDeterministicAuthorityMap()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var definition = engine.CurrentState.PlantDefinition;
        var plant = definition.PlantDefinition;
        var expansion = definition.TurbineExpansionSystem;
        var mainSteam = expansion.MainSteamNetwork;
        var stage = Assert.Single(expansion.StageGroups);
        var train = Assert.Single(mainSteam.AdmissionTrains);
        var line = Assert.Single(mainSteam.SteamLines);
        var drum = Assert.Single(definition.PrimaryCircuit.SteamDrumSystem.Drums);

        var steamSource = drum.SteamSource
            ?? throw new InvalidOperationException("Current-v2 sustained seed must define a drum steam source.");
        var sourceResistance = steamSource.HydraulicResistance.PascalSecondsSquaredPerKilogramSquared;
        var mainLineResistance = plant.GetPipe(line.PipeId).Resistance.PascalSecondsSquaredPerKilogramSquared;
        var stopValve = plant.GetValve(train.StopValveId);
        var controlValve = plant.GetValve(train.ControlValveId);
        var admissionValve = plant.GetValve(train.AdmissionValveId);
        var expansionResistance = stage.ExpansionResistance
            ?? throw new InvalidOperationException("Current-v2 sustained stage must define an expansion resistance.");
        var stageResistance = expansionResistance.PascalSecondsSquaredPerKilogramSquared;

        Assert.Equal(100d, sourceResistance, 9);
        Assert.Equal(1_000d, mainLineResistance, 9);
        Assert.Equal(1_000d, stopValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared, 9);
        Assert.Equal(1_000d, controlValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared, 9);
        Assert.Equal(1_000d, admissionValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared, 9);
        Assert.Equal(21_400d, stageResistance, 9);
        Assert.Equal(ValveCharacteristicKind.Linear, controlValve.Characteristic.Kind);

        var seededControlBiasPercent = engine.PersistentInputs.TurbineSecondaryInputs.Controllers
            .GetController("speed-control")
            .ManualOutput;
        Assert.Equal(28d, seededControlBiasPercent, 9);

        var evidence = AuditPositionsPercent
            .Select(position => ResolveEvidence(
                position,
                sourceResistance,
                mainLineResistance,
                stopValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared,
                controlValve,
                admissionValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared,
                stageResistance))
            .ToArray();

        for (var index = 1; index < evidence.Length; index++)
        {
            Assert.True(evidence[index].NormalizedFlowCapacity > evidence[index - 1].NormalizedFlowCapacity);
            Assert.True(evidence[index].ControlValveResistanceShare < evidence[index - 1].ControlValveResistanceShare);
        }

        var current = Assert.Single(evidence, static item => item.PositionPercent == 28d);
        var sixty = Assert.Single(evidence, static item => item.PositionPercent == 60d);
        var eighty = Assert.Single(evidence, static item => item.PositionPercent == 80d);
        var full = Assert.Single(evidence, static item => item.PositionPercent == 100d);

        Assert.InRange(current.NormalizedFlowCapacity, 0.8272d, 0.8275d);
        Assert.InRange(current.FullOpenCapacityGainFraction, 0.208d, 0.210d);
        Assert.InRange(current.ControlValveResistanceShare, 0.342d, 0.343d);

        // The current 28% seed retains material opening headroom, but authority compresses rapidly once the valve is
        // already well open because fixed source/main-line/stop/admission/stage resistance dominates the series budget.
        Assert.True(sixty.ControlValveResistanceShare < 0.11d);
        Assert.True(eighty.ControlValveResistanceShare < 0.061d);
        Assert.True(full.ControlValveResistanceShare < 0.04d);
        Assert.True(eighty.FullOpenCapacityGainFraction < 0.012d);
    }


    [Fact(Explicit = true)]
    [Trait("Category", "TurbineAdmissionAuthorityAudit")]
    public void CurrentV2TenRpmGovernorPerturbation_CollectsOperationalAuthorityEvidenceWithoutRetuning()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var rotorId = Assert.Single(engine.CurrentState.PlantDefinition.TurbineExpansionSystem.Rotors).Id;

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        Advance(coordinator, 5 * 100);
        var baseline = CaptureOperationalEvidence(engine, "baseline");

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            rotorId,
            ControlRoomCommandTargetKind.TurbineRotor));
        Advance(coordinator, 10 * 100);
        var raised = CaptureOperationalEvidence(engine, "+10 rpm reference");

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedLower,
            rotorId,
            ControlRoomCommandTargetKind.TurbineRotor));
        Advance(coordinator, 10 * 100);
        var restored = CaptureOperationalEvidence(engine, "reference restored");

        Console.WriteLine(baseline);
        Console.WriteLine(raised);
        Console.WriteLine(restored);

        Assert.True(raised.ControlValvePercentOpen > baseline.ControlValvePercentOpen);
        Assert.All(new[] { baseline, raised, restored }, static item =>
        {
            Assert.True(double.IsFinite(item.TurbineInletPressureMegapascals));
            Assert.True(double.IsFinite(item.CommandedStageFlowKilogramsPerSecond));
            Assert.True(double.IsFinite(item.EffectiveStageFlowKilogramsPerSecond));
            Assert.True(double.IsFinite(item.ShaftPowerMegawatts));
            Assert.True(item.CommandedStageFlowKilogramsPerSecond >= 0d);
            Assert.True(item.EffectiveStageFlowKilogramsPerSecond >= 0d);
        });
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineAdmissionAuthorityAudit")]
    public void CurrentV2AdmissionAuthorityMap_IsDeterministicAndDoesNotRequirePhysicsMutation()
    {
        var left = CreateMap();
        var right = CreateMap();

        Assert.Equal(left, right);
        Assert.Equal(AuditPositionsPercent, left.Select(static item => item.PositionPercent).ToArray());
    }

    private static IReadOnlyList<AuthorityEvidence> CreateMap()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var definition = engine.CurrentState.PlantDefinition;
        var plant = definition.PlantDefinition;
        var expansion = definition.TurbineExpansionSystem;
        var mainSteam = expansion.MainSteamNetwork;
        var stage = Assert.Single(expansion.StageGroups);
        var train = Assert.Single(mainSteam.AdmissionTrains);
        var line = Assert.Single(mainSteam.SteamLines);
        var drum = Assert.Single(definition.PrimaryCircuit.SteamDrumSystem.Drums);
        var controlValve = plant.GetValve(train.ControlValveId);
        var steamSource = drum.SteamSource
            ?? throw new InvalidOperationException("Current-v2 sustained seed must define a drum steam source.");
        var expansionResistance = stage.ExpansionResistance
            ?? throw new InvalidOperationException("Current-v2 sustained stage must define an expansion resistance.");

        return AuditPositionsPercent
            .Select(position => ResolveEvidence(
                position,
                steamSource.HydraulicResistance.PascalSecondsSquaredPerKilogramSquared,
                plant.GetPipe(line.PipeId).Resistance.PascalSecondsSquaredPerKilogramSquared,
                plant.GetValve(train.StopValveId).Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared,
                controlValve,
                plant.GetValve(train.AdmissionValveId).Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared,
                expansionResistance.PascalSecondsSquaredPerKilogramSquared))
            .ToArray();
    }


    private static OperationalAuthorityEvidence CaptureOperationalEvidence(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string label)
    {
        var cycle = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
        var train = Assert.Single(cycle.TurbineExpansion.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);

        return new OperationalAuthorityEvidence(
            label,
            engine.LogicalStep,
            100d * train.ControlValve.EffectivePosition.Fraction,
            train.TurbineInletPressure.Megapascals,
            stage.CommandedMassFlowRate.KilogramsPerSecond,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.ShaftPower.Megawatts);
    }

    private static void Advance(ControlRoomRuntimeCoordinator coordinator, int stepCount)
    {
        var remaining = stepCount;
        while (remaining > 0)
        {
            var requested = Math.Min(remaining, coordinator.ExecutionBudget.MaximumSimulationStepsPerBatch);
            var result = coordinator.AdvanceRunning(requested, publicationStride: requested);
            Assert.Equal(requested, result.ExecutedStepCount);
            remaining -= result.ExecutedStepCount;
        }
    }

    private static AuthorityEvidence ResolveEvidence(
        double positionPercent,
        double sourceResistance,
        double mainLineResistance,
        double stopResistance,
        ValveDefinition controlValve,
        double admissionResistance,
        double stageResistance)
    {
        var characteristic = new ValveCharacteristicSolver().Evaluate(
            controlValve.Characteristic,
            ValvePosition.FromPercent(positionPercent));
        var effectiveControlResistance = controlValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared
            / (characteristic.Fraction * characteristic.Fraction);
        var fixedResistance = sourceResistance
            + mainLineResistance
            + stopResistance
            + admissionResistance
            + stageResistance;
        var totalResistance = fixedResistance + effectiveControlResistance;
        var fullOpenTotalResistance = fixedResistance
            + controlValve.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared;
        var normalizedFlowCapacity = Math.Sqrt(fullOpenTotalResistance / totalResistance);
        var fullOpenCapacityGainFraction = (1d / normalizedFlowCapacity) - 1d;

        return new AuthorityEvidence(
            positionPercent,
            effectiveControlResistance,
            totalResistance,
            normalizedFlowCapacity,
            fullOpenCapacityGainFraction,
            effectiveControlResistance / totalResistance);
    }


    private sealed record OperationalAuthorityEvidence(
        string Label,
        long LogicalStep,
        double ControlValvePercentOpen,
        double TurbineInletPressureMegapascals,
        double CommandedStageFlowKilogramsPerSecond,
        double EffectiveStageFlowKilogramsPerSecond,
        double ShaftPowerMegawatts)
    {
        public override string ToString()
            => $"{Label}: step={LogicalStep}; control={ControlValvePercentOpen:F3}%; "
             + $"pin={TurbineInletPressureMegapascals:F6} MPa; "
             + $"stage={CommandedStageFlowKilogramsPerSecond:F6}/{EffectiveStageFlowKilogramsPerSecond:F6} kg/s; "
             + $"shaft={ShaftPowerMegawatts:F6} MW";
    }

    private sealed record AuthorityEvidence(
        double PositionPercent,
        double EffectiveControlValveResistance,
        double TotalSeriesResistance,
        double NormalizedFlowCapacity,
        double FullOpenCapacityGainFraction,
        double ControlValveResistanceShare);
}
