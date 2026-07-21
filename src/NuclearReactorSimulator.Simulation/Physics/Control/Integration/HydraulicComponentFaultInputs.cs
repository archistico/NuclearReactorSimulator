using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public enum HydraulicValveFaultMode
{
    FailOpen = 0,
    FailClosed = 1,
    Stuck = 2,
}

public sealed record PumpHydraulicFaultInput
{
    public PumpHydraulicFaultInput(string faultId, string pumpId, bool forceTrip, double capacityFraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pumpId);
        if (!double.IsFinite(capacityFraction) || capacityFraction < 0d || capacityFraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityFraction));
        }

        FaultId = faultId.Trim();
        PumpId = pumpId.Trim();
        ForceTrip = forceTrip;
        CapacityFraction = capacityFraction;
    }

    public string FaultId { get; }
    public string PumpId { get; }
    public bool ForceTrip { get; }
    public double CapacityFraction { get; }
}

public sealed record ValveHydraulicFaultInput
{
    public ValveHydraulicFaultInput(string faultId, string valveId, HydraulicValveFaultMode mode, ValvePosition stuckPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(valveId);
        FaultId = faultId.Trim();
        ValveId = valveId.Trim();
        Mode = mode;
        StuckPosition = stuckPosition;
    }

    public string FaultId { get; }
    public string ValveId { get; }
    public HydraulicValveFaultMode Mode { get; }
    public ValvePosition StuckPosition { get; }
}

public sealed record HydraulicPathRestrictionInput
{
    public HydraulicPathRestrictionInput(string faultId, string valveId, double maximumOpenFraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(valveId);
        if (!double.IsFinite(maximumOpenFraction) || maximumOpenFraction < 0d || maximumOpenFraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumOpenFraction));
        }

        FaultId = faultId.Trim();
        ValveId = valveId.Trim();
        MaximumOpenFraction = maximumOpenFraction;
    }

    public string FaultId { get; }
    public string ValveId { get; }
    public double MaximumOpenFraction { get; }
}

public sealed record HydraulicLeakInput
{
    public HydraulicLeakInput(string faultId, string fluidNodeId, MassFlowRate massFlowRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fluidNodeId);
        if (massFlowRate <= MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), "Leak mass-flow rate must be greater than zero.");
        }

        FaultId = faultId.Trim();
        FluidNodeId = fluidNodeId.Trim();
        MassFlowRate = massFlowRate;
    }

    public string FaultId { get; }
    public string FluidNodeId { get; }
    public MassFlowRate MassFlowRate { get; }
}

/// <summary>
/// M8.2 immutable fault-effect inputs consumed inside the existing protected full-plant composition.
/// These are constraints/source terms only; they do not own or integrate physical state.
/// </summary>
public sealed class HydraulicComponentFaultInputs
{
    public HydraulicComponentFaultInputs(
        IEnumerable<PumpHydraulicFaultInput>? pumpFaults = null,
        IEnumerable<ValveHydraulicFaultInput>? valveFaults = null,
        IEnumerable<HydraulicPathRestrictionInput>? pathRestrictions = null,
        IEnumerable<HydraulicLeakInput>? leaks = null)
    {
        PumpFaults = Canonicalize(pumpFaults, static x => x.FaultId, nameof(pumpFaults));
        ValveFaults = Canonicalize(valveFaults, static x => x.FaultId, nameof(valveFaults));
        PathRestrictions = Canonicalize(pathRestrictions, static x => x.FaultId, nameof(pathRestrictions));
        Leaks = Canonicalize(leaks, static x => x.FaultId, nameof(leaks));
    }

    public IReadOnlyList<PumpHydraulicFaultInput> PumpFaults { get; }
    public IReadOnlyList<ValveHydraulicFaultInput> ValveFaults { get; }
    public IReadOnlyList<HydraulicPathRestrictionInput> PathRestrictions { get; }
    public IReadOnlyList<HydraulicLeakInput> Leaks { get; }

    public static HydraulicComponentFaultInputs Empty { get; } = new();

    private static IReadOnlyList<T> Canonicalize<T>(IEnumerable<T>? source, Func<T, string> id, string parameterName)
        where T : class
    {
        var items = (source ?? Array.Empty<T>())
            .Select(item => item ?? throw new ArgumentException("Hydraulic fault input collections cannot contain null entries.", parameterName))
            .OrderBy(id, StringComparer.Ordinal)
            .ToArray();
        if (items.Select(id).Distinct(StringComparer.Ordinal).Count() != items.Length)
        {
            throw new ArgumentException("Hydraulic fault input IDs must be unique within each category.", parameterName);
        }
        return new ReadOnlyCollection<T>(items);
    }
}
