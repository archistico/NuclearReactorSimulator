using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Deterministic P/PI/PID controller bank consuming measured signals only.</summary>
public sealed class ControllerSystemSolver
{
    private readonly ControlSystemDefinition _definition;

    public ControllerSystemSolver(ControlSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public ControllerSystemStepResult Step(
        MeasuredSignalFrame measuredSignals,
        ControllerSystemState committedState,
        ControllerInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        if (!ReferenceEquals(measuredSignals.Definition, _definition.Instrumentation))
        {
            throw new ArgumentException("Measured-signal frame does not use the controller system's canonical instrumentation definition.", nameof(measuredSignals));
        }
        if (!ReferenceEquals(committedState.Definition, _definition) || !ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Controller state/inputs do not use this solver's canonical definition.");
        }
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Controller timestep must be positive.");
        }

        var candidateStates = new List<ControllerChannelState>(_definition.Controllers.Count);
        var outputs = new List<ControllerOutput>(_definition.Controllers.Count);
        var diagnostics = new List<ControllerDiagnosticSnapshot>(_definition.Controllers.Count);

        foreach (var definition in _definition.Controllers)
        {
            var state = committedState.GetController(definition.Id);
            var input = inputs.GetController(definition.Id);
            var measurement = measuredSignals.GetSignal(definition.MeasurementChannelId);
            Evaluate(definition, state, input, measurement, deltaTime, out var candidate, out var output, out var diagnostic);
            candidateStates.Add(candidate);
            outputs.Add(output);
            diagnostics.Add(diagnostic);
        }

        var candidateState = new ControllerSystemState(_definition, candidateStates);
        var frame = new ControllerOutputFrame(_definition, outputs);
        return new ControllerSystemStepResult(candidateState, new ControllerSystemSnapshot(_definition, frame, diagnostics));
    }

    private static void Evaluate(
        PidControllerDefinition definition,
        ControllerChannelState state,
        ControllerInput input,
        MeasuredSignal measurement,
        TimeSpan deltaTime,
        out ControllerChannelState candidateState,
        out ControllerOutput output,
        out ControllerDiagnosticSnapshot diagnostic)
    {
        var measurementAvailable = measurement.EngineeringValue.HasValue && measurement.Validity == SignalValidity.Valid;
        var measurementValue = measurement.EngineeringValue;
        var error = measurementAvailable ? input.Setpoint - measurementValue!.Value : state.PreviousError;
        var p = 0d;
        var i = state.IntegralTerm;
        var d = 0d;
        var unsaturated = state.LastOutput;
        var command = state.LastOutput;
        var saturated = false;
        var antiWindup = false;
        var bumpless = false;
        ControllerExecutionStatus status;

        if (input.Mode == ControllerMode.Manual)
        {
            unsaturated = input.ManualOutput;
            command = definition.OutputRange.Clamp(unsaturated);
            saturated = command != unsaturated;
            status = ControllerExecutionStatus.Manual;
        }
        else if (!measurementAvailable)
        {
            unsaturated = state.IsInitialized ? state.LastOutput : definition.OutputRange.Clamp(input.ManualOutput);
            command = definition.OutputRange.Clamp(unsaturated);
            status = ControllerExecutionStatus.MeasurementUnavailable;
        }
        else
        {
            status = ControllerExecutionStatus.Automatic;
            p = definition.ProportionalGain * error;
            var enteringAutomatic = !state.IsInitialized || state.LastMode != ControllerMode.Automatic;
            var derivative = !enteringAutomatic
                ? (error - state.PreviousError) / deltaTime.TotalSeconds
                : 0d;
            d = definition.DerivativeGainSeconds * derivative;

            if (enteringAutomatic && definition.IntegralGainPerSecond != 0d)
            {
                var trackedOutput = state.IsInitialized ? state.LastOutput : definition.OutputRange.Clamp(input.ManualOutput);
                i = trackedOutput - p - d;
                bumpless = true;
            }
            else if (definition.IntegralGainPerSecond == 0d)
            {
                i = 0d;
            }

            var integralIncrement = bumpless
                ? 0d
                : definition.IntegralGainPerSecond * error * deltaTime.TotalSeconds;
            var candidateIntegral = i + integralIncrement;
            unsaturated = p + candidateIntegral + d;
            command = definition.OutputRange.Clamp(unsaturated);
            saturated = command != unsaturated;

            var drivesFurtherHigh = unsaturated > definition.OutputRange.Maximum && integralIncrement > 0d;
            var drivesFurtherLow = unsaturated < definition.OutputRange.Minimum && integralIncrement < 0d;
            if (saturated && (drivesFurtherHigh || drivesFurtherLow))
            {
                antiWindup = true;
                candidateIntegral = i;
                unsaturated = p + candidateIntegral + d;
                command = definition.OutputRange.Clamp(unsaturated);
                saturated = command != unsaturated;
            }

            i = candidateIntegral;
        }

        var candidateMode = input.Mode == ControllerMode.Automatic && !measurementAvailable
            ? state.LastMode
            : input.Mode;
        candidateState = new ControllerChannelState(definition.Id, true, candidateMode, i, error, command);
        output = new ControllerOutput(definition.Id, command, unsaturated, saturated, status);
        diagnostic = new ControllerDiagnosticSnapshot(
            definition.Id,
            definition.MeasurementChannelId,
            input.Mode,
            input.Setpoint,
            measurementValue,
            measurement.Validity,
            measurement.Quality,
            error,
            p,
            i,
            d,
            unsaturated,
            command,
            saturated,
            antiWindup,
            bumpless,
            status);
    }
}
