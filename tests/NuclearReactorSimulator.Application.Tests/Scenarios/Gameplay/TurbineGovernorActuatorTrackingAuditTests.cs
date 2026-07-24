using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-D.3 evidence gate for controller/actuator tracking under the current-v2 turbine-speed loop.
/// This audit deliberately changes no production control law. It measures whether the existing 2-second full-travel
/// valve actuator creates material integral windup before any tracking/back-calculation anti-windup is introduced.
/// </summary>
public sealed class TurbineGovernorActuatorTrackingAuditTests
{
    private const string SpeedControllerId = "speed-control";
    private const double MaterialCommandPositionGapPercent = 5d;
    private const double MaterialIntegralExcursionOutputPercent = 2d;

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineGovernorActuatorTrackingAudit")]
    public void CurrentV2RateLimitedSpeedActuator_QuantifiesCommandPositionLagAndIntegralExcursion()
    {
        var result = RunTrackingAudit();

        Console.WriteLine(result);

        Assert.True(result.MaximumCommandPositionGapPercent > MaterialCommandPositionGapPercent,
            "The D.3 audit stimulus must create a material controller-command/valve-position gap; otherwise the rate-limit tracking gate is not exercised.");
        Assert.True(double.IsFinite(result.MaximumIntegralExcursionWhileLaggedOutputPercent));
        Assert.True(double.IsFinite(result.FinalIntegralOffsetFromBaselineOutputPercent));
        Assert.True(double.IsFinite(result.MaximumAbsoluteSpeedErrorRpm));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineGovernorActuatorTrackingAudit")]
    public void CurrentV2RateLimitedSpeedActuator_DoesNotRequireTrackingAntiWindupUnlessIntegralExcursionIsMaterial()
    {
        var result = RunTrackingAudit();

        Console.WriteLine(result);

        Assert.True(
            result.MaximumIntegralExcursionWhileLaggedOutputPercent < MaterialIntegralExcursionOutputPercent,
            $"D.3 tracking correction is justified: while controller command and valve position were materially separated, "
            + $"the speed-loop integral moved by {result.MaximumIntegralExcursionWhileLaggedOutputPercent:F3} output percentage points "
            + $"(gate {MaterialIntegralExcursionOutputPercent:F3}). Evidence: {result}");
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineGovernorActuatorTrackingAudit")]
    public void CurrentV2GovernorTrackingAudit_IsDeterministic()
    {
        Assert.Equal(RunTrackingAudit(), RunTrackingAudit());
    }

    private static TrackingAuditResult RunTrackingAudit()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine());
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var expansion = engine.CurrentState.PlantDefinition.TurbineExpansionSystem;
        var rotorId = Assert.Single(expansion.Rotors).Id;
        var admissionTrain = Assert.Single(expansion.MainSteamNetwork.AdmissionTrains);
        var controlValveId = admissionTrain.ControlValveId;

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        // The pre-synchronization seed is already bumpless. Advancing it unloaded before the stimulus is a coast-down,
        // not settling, and closes the valve before controller/actuator tracking can be exercised.

        var baselineController = GetSpeedController(engine);
        var baselineIntegral = baselineController.IntegralTerm;
        var baselineControllerOutput = baselineController.LastOutput;
        var baselineSetpoint = engine.PersistentInputs.TurbineSecondaryInputs.Controllers
            .GetController(SpeedControllerId)
            .Setpoint;
        var baselineValvePercent = GetControlValvePercent(engine, controlValveId);

        // Five accepted presses are the canonical +50 rpm operator stimulus. This is large enough to make the
        // 50 percentage-point/second actuator travel rate observable without immediately turning the test into a
        // controller-output saturation test (which already has its own anti-windup path).
        for (var index = 0; index < 5; index++)
        {
            coordinator.Dispatch(new ControlRoomCommand(
                ControlRoomCommandKind.TurbineSpeedRaise,
                rotorId,
                ControlRoomCommandTargetKind.TurbineRotor));
        }

        var raisedSetpoint = engine.PersistentInputs.TurbineSecondaryInputs.Controllers
            .GetController(SpeedControllerId)
            .Setpoint;
        var samples = new List<TrackingSample>();
        CaptureForSteps(coordinator, engine, controlValveId, baselineIntegral, samples, 3 * 100);

        for (var index = 0; index < 5; index++)
        {
            coordinator.Dispatch(new ControlRoomCommand(
                ControlRoomCommandKind.TurbineSpeedLower,
                rotorId,
                ControlRoomCommandTargetKind.TurbineRotor));
        }

        CaptureForSteps(coordinator, engine, controlValveId, baselineIntegral, samples, 4 * 100);

        var laggedSamples = samples
            .Where(static sample => sample.CommandPositionGapPercent >= MaterialCommandPositionGapPercent)
            .ToArray();
        var maximumGap = samples.Max(static sample => sample.CommandPositionGapPercent);
        var minimumControllerOutput = samples.Min(static sample => sample.ControllerOutputPercent);
        var maximumControllerOutput = samples.Max(static sample => sample.ControllerOutputPercent);
        var minimumValvePosition = samples.Min(static sample => sample.ValvePositionPercent);
        var maximumValvePosition = samples.Max(static sample => sample.ValvePositionPercent);
        Assert.True(
            laggedSamples.Length > 0,
            $"The D.3 audit stimulus did not exercise the material actuator-lag gate. "
            + $"Maximum observed controller-command/valve-position gap was {maximumGap:F3} percentage points "
            + $"(gate {MaterialCommandPositionGapPercent:F3}). Baseline setpoint/output/valve: "
            + $"{baselineSetpoint:F3} rpm / {baselineControllerOutput:F3}% / {baselineValvePercent:F3}%; "
            + $"raised setpoint: {raisedSetpoint:F3} rpm; sampled output range: "
            + $"{minimumControllerOutput:F3}..{maximumControllerOutput:F3}%; valve range: "
            + $"{minimumValvePosition:F3}..{maximumValvePosition:F3}%.");

        var maximumIntegralExcursionWhileLagged = laggedSamples
            .Max(sample => Math.Abs(sample.IntegralTerm - baselineIntegral));
        var maximumAbsoluteSpeedError = samples.Max(static sample => Math.Abs(sample.SpeedErrorRpm));
        var finalController = GetSpeedController(engine);
        var finalValvePercent = GetControlValvePercent(engine, controlValveId);

        return new TrackingAuditResult(
            BaselineValvePercent: baselineValvePercent,
            MaximumCommandPositionGapPercent: maximumGap,
            MaximumIntegralExcursionWhileLaggedOutputPercent: maximumIntegralExcursionWhileLagged,
            FinalIntegralOffsetFromBaselineOutputPercent: finalController.IntegralTerm - baselineIntegral,
            MaximumAbsoluteSpeedErrorRpm: maximumAbsoluteSpeedError,
            FinalControllerOutputPercent: finalController.LastOutput,
            FinalValvePercent: finalValvePercent);
    }

    private static void CaptureForSteps(
        ControlRoomRuntimeCoordinator coordinator,
        IntegratedAutomaticOperationRuntimeEngine engine,
        string controlValveId,
        double baselineIntegral,
        ICollection<TrackingSample> samples,
        int stepCount)
    {
        for (var index = 0; index < stepCount; index++)
        {
            var result = coordinator.AdvanceRunning(1, publicationStride: 1);
            Assert.Equal(1, result.ExecutedStepCount);

            // Preserve the canonical 10 ms timestep: a 0.5 fraction/s actuator moves 5 percentage points in
            // 100 ms, so coarser sampling can skip the entire >=5-point material-lag window.
            var controller = GetSpeedController(engine);
            var valvePercent = GetControlValvePercent(engine, controlValveId);
            samples.Add(new TrackingSample(
                engine.LogicalStep,
                controller.LastOutput,
                valvePercent,
                Math.Abs(controller.LastOutput - valvePercent),
                controller.IntegralTerm,
                controller.IntegralTerm - baselineIntegral,
                controller.PreviousError));
        }
    }

    private static NuclearReactorSimulator.Simulation.Physics.Control.ControllerChannelState GetSpeedController(
        IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.CurrentState.TurbineSecondaryControlState.ControlAndActuator.Controllers.GetController(SpeedControllerId);

    private static double GetControlValvePercent(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string controlValveId)
        => 100d * engine.CurrentState.PlantState.PlantState.GetValve(controlValveId).Position.Fraction;

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

    private sealed record TrackingSample(
        long LogicalStep,
        double ControllerOutputPercent,
        double ValvePositionPercent,
        double CommandPositionGapPercent,
        double IntegralTerm,
        double IntegralOffsetFromBaseline,
        double SpeedErrorRpm);

    private sealed record TrackingAuditResult(
        double BaselineValvePercent,
        double MaximumCommandPositionGapPercent,
        double MaximumIntegralExcursionWhileLaggedOutputPercent,
        double FinalIntegralOffsetFromBaselineOutputPercent,
        double MaximumAbsoluteSpeedErrorRpm,
        double FinalControllerOutputPercent,
        double FinalValvePercent)
    {
        public override string ToString()
            => $"D.3 governor/actuator tracking: baseline-valve={BaselineValvePercent:F3}%; "
             + $"max command-position gap={MaximumCommandPositionGapPercent:F3} pp; "
             + $"max integral excursion while lagged={MaximumIntegralExcursionWhileLaggedOutputPercent:F3} pp; "
             + $"final integral offset={FinalIntegralOffsetFromBaselineOutputPercent:F3} pp; "
             + $"max |speed error|={MaximumAbsoluteSpeedErrorRpm:F3} rpm; "
             + $"final controller/valve={FinalControllerOutputPercent:F3}/{FinalValvePercent:F3}%";
    }
}
