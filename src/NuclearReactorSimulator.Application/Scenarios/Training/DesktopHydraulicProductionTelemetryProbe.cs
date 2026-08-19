using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// H.29 application-layer diagnostic observer for the desktop production-activation candidate. It reads the existing
/// canonical numerical telemetry after a step and accumulates deployment counters without projecting them into the operator UI
/// or participating in state ownership. Hosts may sample after each executed step; omission of this observer cannot affect physics.
/// </summary>
public sealed class DesktopHydraulicProductionTelemetryProbe
{
    private readonly FourNodeProductionActivationTelemetryCounter _counter = new();

    public void Observe(IControlRoomRuntimeEngine runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        var engine = runtime as IntegratedAutomaticOperationRuntimeEngine
            ?? throw new ArgumentException(
                "H.29 desktop hydraulic production telemetry requires the integrated automatic-operation runtime.",
                nameof(runtime));
        var numerics = engine.LatestCanonicalSnapshot
            .Control
            .ProtectedControl
            .FullPlant
            .IntegratedCycle
            .PrimaryCircuit
            .HydraulicNumerics;
        _counter.Observe(numerics);
    }

    public FourNodeProductionActivationTelemetrySnapshot Snapshot()
        => _counter.Snapshot();
}
