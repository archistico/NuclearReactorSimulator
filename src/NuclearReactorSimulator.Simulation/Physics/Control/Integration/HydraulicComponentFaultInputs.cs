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

public sealed record PressureDrivenBreakInput
{
    public PressureDrivenBreakInput(
        string faultId,
        string fluidNodeId,
        MassFlowRate referenceMassFlowRate,
        Pressure ambientPressure,
        PressureDifference referencePressureDifference,
        double maximumInventoryFractionPerStep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fluidNodeId);
        if (referenceMassFlowRate <= MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceMassFlowRate), "Reference break mass-flow rate must be greater than zero.");
        }
        if (referencePressureDifference <= PressureDifference.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(referencePressureDifference), "Reference break pressure difference must be greater than zero.");
        }
        if (!double.IsFinite(maximumInventoryFractionPerStep)
            || maximumInventoryFractionPerStep <= 0d
            || maximumInventoryFractionPerStep > 0.01d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInventoryFractionPerStep),
                "Maximum inventory fraction removed per step must be in (0, 0.01].");
        }

        FaultId = faultId.Trim();
        FluidNodeId = fluidNodeId.Trim();
        ReferenceMassFlowRate = referenceMassFlowRate;
        AmbientPressure = ambientPressure;
        ReferencePressureDifference = referencePressureDifference;
        MaximumInventoryFractionPerStep = maximumInventoryFractionPerStep;
    }

    public string FaultId { get; }
    public string FluidNodeId { get; }
    public MassFlowRate ReferenceMassFlowRate { get; }
    public Pressure AmbientPressure { get; }
    public PressureDifference ReferencePressureDifference { get; }
    public double MaximumInventoryFractionPerStep { get; }
}

/// <summary>
/// M8.2/M8.5 immutable hydraulic fault-effect inputs consumed inside the existing protected full-plant composition.
/// These are constraints/source terms only; they do not own or integrate physical state.
/// </summary>
public sealed class HydraulicComponentFaultInputs
{
    public HydraulicComponentFaultInputs(
        IEnumerable<PumpHydraulicFaultInput>? pumpFaults = null,
        IEnumerable<ValveHydraulicFaultInput>? valveFaults = null,
        IEnumerable<HydraulicPathRestrictionInput>? pathRestrictions = null,
        IEnumerable<HydraulicLeakInput>? leaks = null,
        IEnumerable<PressureDrivenBreakInput>? pressureDrivenBreaks = null)
    {
        PumpFaults = Canonicalize(pumpFaults, static x => x.FaultId, nameof(pumpFaults));
        ValveFaults = Canonicalize(valveFaults, static x => x.FaultId, nameof(valveFaults));
        PathRestrictions = Canonicalize(pathRestrictions, static x => x.FaultId, nameof(pathRestrictions));
        Leaks = Canonicalize(leaks, static x => x.FaultId, nameof(leaks));
        PressureDrivenBreaks = Canonicalize(pressureDrivenBreaks, static x => x.FaultId, nameof(pressureDrivenBreaks));
    }

    public IReadOnlyList<PumpHydraulicFaultInput> PumpFaults { get; }
    public IReadOnlyList<ValveHydraulicFaultInput> ValveFaults { get; }
    public IReadOnlyList<HydraulicPathRestrictionInput> PathRestrictions { get; }
    public IReadOnlyList<HydraulicLeakInput> Leaks { get; }
    public IReadOnlyList<PressureDrivenBreakInput> PressureDrivenBreaks { get; }

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
