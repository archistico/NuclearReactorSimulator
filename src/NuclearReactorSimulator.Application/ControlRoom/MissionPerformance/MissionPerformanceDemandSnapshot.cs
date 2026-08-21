namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Presentation-only separation of external demand, requested generator load and actual electrical output.
/// External demand may be unavailable while requested/actual electrical evidence remains available.
/// </summary>
public sealed record MissionPerformanceDemandSnapshot(
    bool ExternalDemandAvailable,
    string? ExternalDemandProfileExactId,
    double? ExternalDemandMegawatts,
    double? RequestedGeneratorLoadMegawatts,
    double? ActualElectricalOutputMegawatts,
    double? DemandOutputErrorMegawatts,
    long? NextScheduledDemandChangeLogicalStep,
    double? NextScheduledDemandMegawatts);
