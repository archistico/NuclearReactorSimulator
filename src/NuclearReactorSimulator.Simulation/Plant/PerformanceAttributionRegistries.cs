using System.Runtime.CompilerServices;
using System.Threading;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.28.1-A architecture-safe audit measurement seam. The Simulation assembly never reads a wall clock or
/// allocation counter directly. Application.Tests may temporarily inject readers while the focused diagnostic
/// gate is running; production and ordinary runtime construction leave the readers unset, so measurement values
/// remain zero and can never participate in numerical decisions.
/// </summary>
internal static class PerformanceAttributionMeasurement
{
    private static readonly AsyncLocal<MeasurementReaders?> Current = new();

    internal static long ReadTimestamp() => Current.Value?.TimestampReader() ?? 0L;

    internal static long ReadAllocatedBytes() => Current.Value?.AllocatedBytesReader() ?? 0L;

    internal static IDisposable Push(
        Func<long> timestampReader,
        Func<long> allocatedBytesReader)
    {
        ArgumentNullException.ThrowIfNull(timestampReader);
        ArgumentNullException.ThrowIfNull(allocatedBytesReader);

        var previous = Current.Value;
        Current.Value = new MeasurementReaders(timestampReader, allocatedBytesReader);
        return new MeasurementScope(previous);
    }

    private sealed record MeasurementReaders(
        Func<long> TimestampReader,
        Func<long> AllocatedBytesReader);

    private sealed class MeasurementScope : IDisposable
    {
        private readonly MeasurementReaders? _previous;
        private bool _disposed;

        internal MeasurementScope(MeasurementReaders? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Current.Value = _previous;
            _disposed = true;
        }
    }
}

/// <summary>
/// H.28.1-A observational store for H.9 cost attribution. Weak keys keep diagnostics outside deterministic result
/// equality and avoid extending object lifetime. Numerical code never reads these values to make a decision.
/// </summary>
internal static class JacobianHydraulicCorrectorPerformanceAttributionRegistry
{
    private static readonly ConditionalWeakTable<JacobianHydraulicCorrectorStepResult, JacobianHydraulicCorrectorPerformanceAttribution> Values = new();

    internal static void Set(JacobianHydraulicCorrectorStepResult result, JacobianHydraulicCorrectorPerformanceAttribution attribution)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(attribution);
        Values.Remove(result);
        Values.Add(result, attribution);
    }

    internal static bool TryGet(
        JacobianHydraulicCorrectorStepResult result,
        out JacobianHydraulicCorrectorPerformanceAttribution? attribution)
        => Values.TryGetValue(result, out attribution);
}

/// <summary>
/// Internal bridge carrying sidecar attribution to the orchestrator without adding nondeterministic timing fields
/// to the deterministic H.21/H.22 step-result record.
/// </summary>
internal static class FourNodeBranchContinuitySidecarPerformanceAttributionRegistry
{
    private static readonly ConditionalWeakTable<FourNodeBranchContinuityShadowIntegrationStepResult, FourNodeBranchContinuityPerformanceAttribution> Values = new();

    internal static void Set(FourNodeBranchContinuityShadowIntegrationStepResult result, FourNodeBranchContinuityPerformanceAttribution attribution)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(attribution);
        Values.Remove(result);
        Values.Add(result, attribution);
    }

    internal static bool TryGet(
        FourNodeBranchContinuityShadowIntegrationStepResult result,
        out FourNodeBranchContinuityPerformanceAttribution? attribution)
        => Values.TryGetValue(result, out attribution);
}

/// <summary>
/// Public read-only H.28.1-A observation seam. Attribution is keyed by telemetry object identity rather than being
/// embedded in the record, so existing deterministic record equality remains unchanged.
/// </summary>
internal static class FourNodeBranchContinuityPerformanceAttributionRegistry
{
    private static readonly ConditionalWeakTable<FourNodeBranchContinuityIntegrationTelemetry, FourNodeBranchContinuityPerformanceAttribution> Values = new();

    internal static void Set(FourNodeBranchContinuityIntegrationTelemetry telemetry, FourNodeBranchContinuityPerformanceAttribution attribution)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(attribution);
        Values.Remove(telemetry);
        Values.Add(telemetry, attribution);
    }

    internal static bool TryGet(
        FourNodeBranchContinuityIntegrationTelemetry telemetry,
        out FourNodeBranchContinuityPerformanceAttribution? attribution)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        return Values.TryGetValue(telemetry, out attribution);
    }
}
