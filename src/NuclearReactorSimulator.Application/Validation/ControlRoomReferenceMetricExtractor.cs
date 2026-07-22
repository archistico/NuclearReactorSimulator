using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Validation;

/// <summary>
/// M9.6 presentation-level metric projection. It reads only immutable ControlRoomSnapshot values and never reaches into private
/// Simulation state, so calibration evidence remains explicit about what the validated presentation contract can support.
/// </summary>
public static class ControlRoomReferenceMetricExtractor
{
    public static ReferenceValidationSample Extract(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var metrics = new Dictionary<string, double?>(StringComparer.Ordinal)
        {
            [ReferenceValidationMetricIds.ReactorThermalPowerMw] = snapshot.ReactorCore.ReactorThermalPower.NumericValue,
            [ReferenceValidationMetricIds.ReactorAverageRodWithdrawalPercent] = snapshot.ReactorCore.AverageRodWithdrawal.NumericValue,
            [ReferenceValidationMetricIds.ReactorXenonReactivityPcm] = snapshot.ReactorCore.XenonReactivity.NumericValue,
            [ReferenceValidationMetricIds.PrimaryTotalMassKg] = snapshot.PrimaryCircuit.TotalPrimaryMass.NumericValue,
            [ReferenceValidationMetricIds.PrimaryRunningPumpCount] = snapshot.PrimaryCircuit.Pumps.Count(static pump => pump.IsRunning),
            [ReferenceValidationMetricIds.SecondaryMaximumRotorSpeedRpm] = MaximumAvailable(snapshot.TurbineSecondary.Rotors.Select(static rotor => rotor.Speed.NumericValue)),
            [ReferenceValidationMetricIds.SecondaryTurbineShaftPowerMw] = snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue,
            [ReferenceValidationMetricIds.ElectricalGrossOutputMwe] = snapshot.Electrical.GrossElectricalOutput.NumericValue,
            [ReferenceValidationMetricIds.ElectricalTotalGeneratorOutputMwe] = SumAvailable(snapshot.Electrical.Generators.Select(static generator => generator.ElectricalOutput.NumericValue)),
            [ReferenceValidationMetricIds.ElectricalClosedBreakerCount] = snapshot.Electrical.Generators.Count(static generator => generator.BreakerClosed),
            [ReferenceValidationMetricIds.ElectricalSynchronizationReadyGeneratorCount] = snapshot.Electrical.Generators.Count(static generator => generator.SynchronizationConditionsSatisfied),
            [ReferenceValidationMetricIds.InstrumentationInvalidSignalCount] = snapshot.InvalidMeasuredSignalCount,
            [ReferenceValidationMetricIds.AlarmUnacknowledgedCount] = snapshot.UnacknowledgedAlarmCount,
            [ReferenceValidationMetricIds.ProtectionReactorScramActive] = snapshot.ReactorScramActive ? 1d : 0d,
            [ReferenceValidationMetricIds.ProtectionTurbineTripActive] = snapshot.TurbineTripActive ? 1d : 0d,
            [ReferenceValidationMetricIds.ProtectionGeneratorTripActive] = snapshot.GeneratorTripActive ? 1d : 0d,
        };

        return new ReferenceValidationSample(snapshot.LogicalStep, metrics);
    }

    private static double? SumAvailable(IEnumerable<double?> values)
    {
        var available = values.Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        return available.Length == 0 ? null : available.Sum();
    }

    private static double? MaximumAvailable(IEnumerable<double?> values)
    {
        var available = values.Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        return available.Length == 0 ? null : available.Max();
    }
}
