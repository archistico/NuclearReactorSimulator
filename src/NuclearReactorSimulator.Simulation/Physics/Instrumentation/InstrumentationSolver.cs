using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>
/// Deterministic M5.1 observation solver. It reads one immutable full-plant true-state snapshot and evolves only
/// instrumentation/filter state; it never mutates or re-integrates physical plant state.
/// </summary>
public sealed class InstrumentationSolver
{
    private readonly InstrumentationSystemDefinition _definition;
    private readonly InstrumentSignalSourceCatalog _sources;

    public InstrumentationSolver(
        InstrumentationSystemDefinition definition,
        InstrumentSignalSourceCatalog sources)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _sources.Validate(definition);
    }

    public InstrumentationSystemDefinition Definition => _definition;

    public InstrumentationStepResult Step(
        FullPlantSnapshot trueStateSnapshot,
        InstrumentationState committedState,
        InstrumentationInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(trueStateSnapshot);
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);

        if (_sources.FullPlantDefinition is not null
            && !ReferenceEquals(trueStateSnapshot.IntegratedCycle.Definition, _sources.FullPlantDefinition))
        {
            throw new ArgumentException("True-state snapshot does not use the source catalog's canonical full-plant definition.", nameof(trueStateSnapshot));
        }

        if (!ReferenceEquals(committedState.Definition, _definition))
        {
            throw new ArgumentException("Committed instrumentation state does not use this solver's canonical definition.", nameof(committedState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Instrumentation inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Instrumentation timestep must be positive.");
        }

        var candidateChannels = new List<InstrumentationChannelState>(_definition.Channels.Count);
        var measuredSignals = new List<MeasuredSignal>(_definition.Channels.Count);
        var diagnostics = new List<InstrumentChannelDiagnosticSnapshot>(_definition.Channels.Count);

        foreach (var channel in _definition.Channels)
        {
            var source = _sources.GetSource(channel.SourceId);
            var committedChannel = committedState.GetChannel(channel.Id);
            var fault = inputs.GetSensorFault(channel.Id);
            var trueValue = source.Read(trueStateSnapshot);
            var filteredValue = ApplyLag(channel, committedChannel, trueValue, deltaTime);
            var outOfRange = !channel.MeasurementRange.Contains(filteredValue);
            var normalOutput = channel.ClampToMeasurementRange
                ? channel.MeasurementRange.Clamp(filteredValue)
                : filteredValue;

            var output = normalOutput;
            var validity = SignalValidity.Valid;
            var quality = outOfRange ? SignalQuality.Suspect : SignalQuality.Good;

            switch (fault.Mode)
            {
                case SensorFaultMode.None:
                    break;
                case SensorFaultMode.Bias:
                    output = filteredValue + fault.BiasEngineeringUnits;
                    outOfRange = !channel.MeasurementRange.Contains(output);
                    if (channel.ClampToMeasurementRange)
                    {
                        output = channel.MeasurementRange.Clamp(output);
                    }

                    quality = SignalQuality.Suspect;
                    break;
                case SensorFaultMode.Freeze:
                    output = committedChannel.IsInitialized ? committedChannel.LastOutputEngineeringValue : normalOutput;
                    validity = SignalValidity.Invalid;
                    quality = SignalQuality.Suspect;
                    break;
                case SensorFaultMode.FailedLow:
                    output = channel.MeasurementRange.Minimum;
                    validity = SignalValidity.Invalid;
                    quality = SignalQuality.Bad;
                    break;
                case SensorFaultMode.FailedHigh:
                    output = channel.MeasurementRange.Maximum;
                    validity = SignalValidity.Invalid;
                    quality = SignalQuality.Bad;
                    break;
                case SensorFaultMode.Unavailable:
                    validity = SignalValidity.Invalid;
                    quality = SignalQuality.Unavailable;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported sensor fault mode '{fault.Mode}'.");
            }

            double? measuredValue = fault.Mode == SensorFaultMode.Unavailable ? null : output;
            double? scaledValue = measuredValue.HasValue
                ? channel.OutputScale.Map(measuredValue.Value, channel.MeasurementRange)
                : null;
            var candidateLastOutput = measuredValue ?? committedChannel.LastOutputEngineeringValue;

            candidateChannels.Add(new InstrumentationChannelState(channel.Id, true, filteredValue, candidateLastOutput));
            measuredSignals.Add(new MeasuredSignal(
                channel.Id,
                channel.EngineeringUnitSymbol,
                measuredValue,
                scaledValue,
                validity,
                quality,
                outOfRange,
                fault.Mode));
            diagnostics.Add(new InstrumentChannelDiagnosticSnapshot(
                channel.Id,
                channel.SourceId,
                trueValue,
                filteredValue,
                measuredValue,
                outOfRange,
                fault.Mode,
                validity,
                quality));
        }

        var candidateState = new InstrumentationState(_definition, candidateChannels);
        var frame = new MeasuredSignalFrame(_definition, measuredSignals);
        var snapshot = new InstrumentationSnapshot(_definition, frame, diagnostics);
        return new InstrumentationStepResult(candidateState, snapshot);
    }

    private static double ApplyLag(
        InstrumentChannelDefinition channel,
        InstrumentationChannelState committedState,
        double trueValue,
        TimeSpan deltaTime)
    {
        if (!committedState.IsInitialized || channel.LagTimeConstant == TimeSpan.Zero)
        {
            return trueValue;
        }

        var tauSeconds = channel.LagTimeConstant.TotalSeconds;
        var alpha = 1d - Math.Exp(-deltaTime.TotalSeconds / tauSeconds);
        return committedState.FilteredEngineeringValue + (alpha * (trueValue - committedState.FilteredEngineeringValue));
    }
}
