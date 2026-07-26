using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-D.3 evidence gate separating effective governor-setpoint authority, controller-output saturation,
/// conditional-integration anti-windup and finite-rate physical valve tracking. No production tuning is performed here.
/// </summary>
public sealed class TurbineGovernorActuatorTrackingAuditTests
{
    private const int StepsPerSecond = 100;
    private const int SampleStrideSteps = 10;

    [Fact]
    public void CurrentV2GovernorContracts_FreezeDistinctSustainedProfilesWithoutRetuning()
    {
        var desktopEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var synchronizationEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine());

        var desktopDefinition = desktopEngine.PersistentInputs.TurbineSecondaryInputs.Definition;
        var synchronizationDefinition = synchronizationEngine.PersistentInputs.TurbineSecondaryInputs.Definition;
        var desktopController = desktopDefinition.ActuatorSystem.ControlSystem.GetController("speed-control");
        var synchronizationController = synchronizationDefinition.ActuatorSystem.ControlSystem.GetController("speed-control");
        var desktopActuator = desktopDefinition.ActuatorSystem.GetActuator("speed-actuator");
        var synchronizationActuator = synchronizationDefinition.ActuatorSystem.GetActuator("speed-actuator");
        var desktopGovernor = Assert.IsType<TurbineGovernorDroopDefinition>(desktopDefinition.GovernorDroop);
        var synchronizationGovernor = Assert.IsType<TurbineGovernorDroopDefinition>(synchronizationDefinition.GovernorDroop);

        Assert.Equal(ControllerAlgorithmKind.ProportionalIntegralDerivative, desktopController.Algorithm);
        Assert.Equal(1d, desktopController.ProportionalGain, 12);
        Assert.Equal(0.02d, desktopController.IntegralGainPerSecond, 12);
        Assert.Equal(0.2d, desktopController.DerivativeGainSeconds, 12);
        Assert.Equal(0d, desktopController.OutputRange.Minimum, 12);
        Assert.Equal(100d, desktopController.OutputRange.Maximum, 12);

        Assert.Equal(ControllerAlgorithmKind.ProportionalIntegral, synchronizationController.Algorithm);
        Assert.Equal(0.5d, synchronizationController.ProportionalGain, 12);
        Assert.Equal(0.02d, synchronizationController.IntegralGainPerSecond, 12);
        Assert.Equal(0d, synchronizationController.DerivativeGainSeconds, 12);
        Assert.Equal(0d, synchronizationController.OutputRange.Minimum, 12);
        Assert.Equal(100d, synchronizationController.OutputRange.Maximum, 12);

        Assert.Equal(0.5d, desktopActuator.TravelRate.GetValueOrDefault().FractionPerSecond, 12);
        Assert.Equal(0.5d, synchronizationActuator.TravelRate.GetValueOrDefault().FractionPerSecond, 12);
        Assert.Equal(1.5d, desktopGovernor.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
        Assert.Equal(1.5d, synchronizationGovernor.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
        Assert.Equal(
            28d,
            desktopEngine.PersistentInputs.TurbineSecondaryInputs.Controllers.GetController("speed-control").ManualOutput,
            12);
        Assert.Equal(
            28d,
            synchronizationEngine.PersistentInputs.TurbineSecondaryInputs.Controllers.GetController("speed-control").ManualOutput,
            12);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineGovernorActuatorTrackingAudit")]
    public void BreakerOpenTenRpmReferenceStep_ExercisesEffectiveSetpointAndCollectsTrackingEvidence()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine());
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var rotorId = Assert.Single(engine.CurrentState.PlantDefinition.TurbineExpansionSystem.Rotors).Id;

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var baseline = AdvanceUntilBreakerOpenGovernorControllable(coordinator, engine);
        Assert.False(baseline.BreakerClosed);
        Assert.False(baseline.TurbineTripActive);
        Assert.False(baseline.GeneratorTripActive);

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            rotorId,
            ControlRoomCommandTargetKind.TurbineRotor));
        Advance(coordinator, 1);
        var raisedRequest = Capture(engine, "+10 rpm request accepted");
        GovernorTrackingEvidence[] raised =
        [
            raisedRequest,
            .. AdvanceAndSample(
                coordinator,
                engine,
                (10 * StepsPerSecond) - 1,
                "+10 rpm")
        ];

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedLower,
            rotorId,
            ControlRoomCommandTargetKind.TurbineRotor));
        Advance(coordinator, 1);
        var restoredRequest = Capture(engine, "reference restore accepted");
        GovernorTrackingEvidence[] restored =
        [
            restoredRequest,
            .. AdvanceAndSample(
                coordinator,
                engine,
                (10 * StepsPerSecond) - 1,
                "reference restored")
        ];

        Console.WriteLine(baseline);
        Console.WriteLine(raisedRequest);
        Console.WriteLine(Summarize("breaker-open +10 rpm", baseline, raised));
        Console.WriteLine(restoredRequest);
        Console.WriteLine(Summarize("breaker-open restore", raised[^1], restored));

        Assert.All(raised, item => Assert.Equal(baseline.EffectiveGovernorSetpointRpm + 10d, item.EffectiveGovernorSetpointRpm, 9));
        Assert.All(restored, item => Assert.Equal(baseline.EffectiveGovernorSetpointRpm, item.EffectiveGovernorSetpointRpm, 9));
        Assert.True(raisedRequest.ControllerOutputPercent > baseline.ControllerOutputPercent);
        Assert.True(raisedRequest.PhysicalControlValvePercentOpen > baseline.PhysicalControlValvePercentOpen);

        RequireFinite([baseline, .. raised, .. restored]);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineGovernorActuatorTrackingAudit")]
    public void BreakerClosedFiveMegawattLoadStep_UsesDroopDerivedEffectiveSetpointAndCollectsScaleEvidence()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var generatorId = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators).Id;

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        Advance(coordinator, 5 * StepsPerSecond);
        var baseline = Capture(engine, "breaker-closed baseline");
        Assert.True(baseline.BreakerClosed);

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        Advance(coordinator, 1);
        var raisedRequest = Capture(engine, "+5 MWe request accepted");
        var loaded = AdvanceAndSample(coordinator, engine, (10 * StepsPerSecond) - 1, "+5 MWe droop response");

        coordinator.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        Advance(coordinator, 1);
        var restoredRequest = Capture(engine, "load request restored");
        var restored = AdvanceAndSample(coordinator, engine, (10 * StepsPerSecond) - 1, "droop response restored");

        Assert.Equal(baseline.RequestedElectricalPowerMegawatts + 5d, raisedRequest.RequestedElectricalPowerMegawatts, 9);
        Assert.Equal(baseline.EffectiveGovernorSetpointRpm + 0.75d, raisedRequest.EffectiveGovernorSetpointRpm, 9);
        Assert.Equal(baseline.RequestedElectricalPowerMegawatts, restoredRequest.RequestedElectricalPowerMegawatts, 9);
        Assert.Equal(baseline.EffectiveGovernorSetpointRpm, restoredRequest.EffectiveGovernorSetpointRpm, 9);

        RequireFinite([baseline, raisedRequest, .. loaded, restoredRequest, .. restored]);
        Console.WriteLine(baseline);
        Console.WriteLine(raisedRequest);
        Console.WriteLine(Summarize("breaker-closed +5 MWe", raisedRequest, loaded));
        Console.WriteLine(restoredRequest);
        Console.WriteLine(Summarize("breaker-closed restore", restoredRequest, restored));
    }

    private static GovernorTrackingEvidence AdvanceUntilBreakerOpenGovernorControllable(
        ControlRoomRuntimeCoordinator coordinator,
        IntegratedAutomaticOperationRuntimeEngine engine)
    {
        const int maximumSettlingSeconds = 90;
        GovernorTrackingEvidence candidate = Capture(engine, "breaker-open settling start");

        for (var elapsedSteps = 0; elapsedSteps < maximumSettlingSeconds * StepsPerSecond; elapsedSteps += SampleStrideSteps)
        {
            Advance(coordinator, SampleStrideSteps);
            ResetProtectionWhenAvailable(coordinator);
            candidate = Capture(engine, "breaker-open baseline");
            var speedErrorMagnitude = Math.Abs(candidate.EffectiveGovernorSetpointRpm - candidate.MeasuredRotorSpeedRpm);
            if (!candidate.BreakerClosed
                && !candidate.TurbineTripActive
                && !candidate.GeneratorTripActive
                && speedErrorMagnitude <= 5d
                && candidate.PhysicalControlValvePercentOpen > 0.1d
                && candidate.EffectiveStageFlowKilogramsPerSecond > 0d
                && candidate.ShaftPowerMegawatts > 0d
                && !candidate.ControllerOutputSaturated)
            {
                return candidate;
            }
        }

        Console.WriteLine(candidate);
        Assert.Fail(
            $"Breaker-open governor did not enter the controllable ±5 rpm band with protection clear within {maximumSettlingSeconds} simulated seconds.");
        return candidate;
    }

    private static void ResetProtectionWhenAvailable(ControlRoomRuntimeCoordinator coordinator)
    {
        var snapshot = coordinator.Current;
        if (!snapshot.TurbineTripActive && !snapshot.GeneratorTripActive)
        {
            return;
        }

        Assert.False(snapshot.ReactorScramActive);
        if (!snapshot.ProtectionReset.CanResetNow)
        {
            return;
        }

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ProtectionReset));
        Advance(coordinator, 1);
        Assert.True(coordinator.Current.ProtectionReset.LastResetAccepted);
        Assert.False(coordinator.Current.TurbineTripActive);
        Assert.False(coordinator.Current.GeneratorTripActive);
    }

    private static IReadOnlyList<GovernorTrackingEvidence> AdvanceAndSample(
        ControlRoomRuntimeCoordinator coordinator,
        IntegratedAutomaticOperationRuntimeEngine engine,
        int totalStepCount,
        string label)
    {
        var samples = new List<GovernorTrackingEvidence>();
        var remaining = totalStepCount;
        while (remaining > 0)
        {
            var requested = Math.Min(remaining, SampleStrideSteps);
            Advance(coordinator, requested);
            remaining -= requested;
            samples.Add(Capture(engine, label));
        }

        return samples;
    }

    private static GovernorTrackingEvidence Capture(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string label)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var speedLoop = Assert.Single(
            protectedControl.TurbineSecondary.Loops,
            static loop => loop.Kind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission);
        var diagnostic = protectedControl.TurbineSecondary.ControlAndActuator.Controllers
            .GetDiagnostic(speedLoop.ControllerId);
        var train = Assert.Single(cycle.TurbineExpansion.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
        var rotor = Assert.Single(cycle.TurbineExpansion.Rotors);
        var generator = Assert.Single(cycle.Generators);

        return new GovernorTrackingEvidence(
            label,
            engine.LogicalStep,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive,
            generator.RequestedElectricalPower.Megawatts,
            speedLoop.Setpoint,
            diagnostic.Measurement ?? double.NaN,
            diagnostic.Error,
            diagnostic.ProportionalTerm,
            diagnostic.IntegralTerm,
            diagnostic.DerivativeTerm,
            diagnostic.UnsaturatedOutput,
            diagnostic.Output,
            diagnostic.IsSaturated,
            diagnostic.AntiWindupActive,
            100d * train.ControlValve.EffectivePosition.Fraction,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            stage.CommandedMassFlowRate.KilogramsPerSecond,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.ShaftPower.Megawatts,
            rotor.PassiveMechanicalLossPower.Megawatts);
    }

    private static TrackingSummary Summarize(
        string label,
        GovernorTrackingEvidence initial,
        IReadOnlyList<GovernorTrackingEvidence> samples)
        => new(
            label,
            samples.Count,
            samples.Count(static item => item.ControllerOutputSaturated),
            samples.Count(static item => item.ConditionalAntiWindupActive),
            samples.Max(item => Math.Abs(item.ControllerOutputPercent - item.PhysicalControlValvePercentOpen)),
            samples.Max(static item => item.ControllerOutputPercent),
            samples.Max(static item => item.PhysicalControlValvePercentOpen),
            samples[^1].IntegralTerm - initial.IntegralTerm,
            samples[^1].RotorSpeedRpm - initial.RotorSpeedRpm,
            samples[^1].ShaftPowerMegawatts - initial.ShaftPowerMegawatts);

    private static void RequireFinite(IEnumerable<GovernorTrackingEvidence> samples)
    {
        Assert.All(samples, static item =>
        {
            Assert.True(double.IsFinite(item.RequestedElectricalPowerMegawatts));
            Assert.True(double.IsFinite(item.EffectiveGovernorSetpointRpm));
            Assert.True(double.IsFinite(item.MeasuredRotorSpeedRpm));
            Assert.True(double.IsFinite(item.ControllerErrorRpm));
            Assert.True(double.IsFinite(item.ProportionalTerm));
            Assert.True(double.IsFinite(item.IntegralTerm));
            Assert.True(double.IsFinite(item.DerivativeTerm));
            Assert.True(double.IsFinite(item.UnsaturatedControllerOutputPercent));
            Assert.InRange(item.ControllerOutputPercent, 0d, 100d);
            Assert.InRange(item.PhysicalControlValvePercentOpen, 0d, 100d);
            Assert.True(double.IsFinite(item.RotorSpeedRpm));
            Assert.True(double.IsFinite(item.CommandedStageFlowKilogramsPerSecond));
            Assert.True(double.IsFinite(item.EffectiveStageFlowKilogramsPerSecond));
            Assert.True(double.IsFinite(item.ShaftPowerMegawatts));
            Assert.True(double.IsFinite(item.PassiveMechanicalLossMegawatts));
            Assert.True(item.PassiveMechanicalLossMegawatts >= 0d);
        });
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

    private sealed record GovernorTrackingEvidence(
        string Label,
        long LogicalStep,
        bool BreakerClosed,
        bool TurbineTripActive,
        bool GeneratorTripActive,
        double RequestedElectricalPowerMegawatts,
        double EffectiveGovernorSetpointRpm,
        double MeasuredRotorSpeedRpm,
        double ControllerErrorRpm,
        double ProportionalTerm,
        double IntegralTerm,
        double DerivativeTerm,
        double UnsaturatedControllerOutputPercent,
        double ControllerOutputPercent,
        bool ControllerOutputSaturated,
        bool ConditionalAntiWindupActive,
        double PhysicalControlValvePercentOpen,
        double RotorSpeedRpm,
        double CommandedStageFlowKilogramsPerSecond,
        double EffectiveStageFlowKilogramsPerSecond,
        double ShaftPowerMegawatts,
        double PassiveMechanicalLossMegawatts)
    {
        public override string ToString()
            => $"{Label}: step={LogicalStep}; breaker={(BreakerClosed ? "CLOSED" : "OPEN")}; "
             + $"trip={TurbineTripActive}/{GeneratorTripActive}; load={RequestedElectricalPowerMegawatts:F3} MWe; "
             + $"sp/pv={EffectiveGovernorSetpointRpm:F6}/{MeasuredRotorSpeedRpm:F6} rpm; "
             + $"PID={ProportionalTerm:F6}/{IntegralTerm:F6}/{DerivativeTerm:F6}; "
             + $"out={UnsaturatedControllerOutputPercent:F6}->{ControllerOutputPercent:F6}%; "
             + $"sat={ControllerOutputSaturated}; aw={ConditionalAntiWindupActive}; valve={PhysicalControlValvePercentOpen:F6}%; "
             + $"rotor={RotorSpeedRpm:F6} rpm; stage={CommandedStageFlowKilogramsPerSecond:F6}/{EffectiveStageFlowKilogramsPerSecond:F6} kg/s; "
             + $"shaft={ShaftPowerMegawatts:F6} MW; passive-loss={PassiveMechanicalLossMegawatts:F6} MW";
    }

    private sealed record TrackingSummary(
        string Label,
        int SampleCount,
        int SaturatedSampleCount,
        int AntiWindupSampleCount,
        double MaximumControllerValveGapPercent,
        double MaximumControllerOutputPercent,
        double MaximumPhysicalValvePositionPercent,
        double FinalIntegralChange,
        double FinalRotorSpeedChangeRpm,
        double FinalShaftPowerChangeMegawatts)
    {
        public override string ToString()
            => $"{Label}: samples={SampleCount}; saturated={SaturatedSampleCount}; anti-windup={AntiWindupSampleCount}; "
             + $"max command-valve gap={MaximumControllerValveGapPercent:F6}%; max command={MaximumControllerOutputPercent:F6}%; "
             + $"max valve={MaximumPhysicalValvePositionPercent:F6}%; ΔI={FinalIntegralChange:F6}; "
             + $"Δrpm={FinalRotorSpeedChangeRpm:F6}; Δshaft={FinalShaftPowerChangeMegawatts:F6} MW";
    }
}
