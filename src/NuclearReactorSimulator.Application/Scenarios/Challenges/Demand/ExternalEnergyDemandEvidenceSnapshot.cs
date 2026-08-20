namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>
/// Presentation/evaluation-only external-demand evidence. External demand, generator request and actual output remain
/// separate values; demand/output error is observational and never a plant command.
/// </summary>
public sealed record ExternalEnergyDemandEvidenceSnapshot
{
    public ExternalEnergyDemandEvidenceSnapshot(
        bool isAvailable,
        string? profileExactId,
        long logicalStep,
        long? profileOffsetLogicalStep,
        double? externalDemandMegawatts,
        double? requestedGeneratorLoadMegawatts,
        double? actualElectricalOutputMegawatts,
        double? demandOutputErrorMegawatts,
        long? nextScheduledChangeLogicalStep,
        double? nextScheduledDemandMegawatts)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (isAvailable && string.IsNullOrWhiteSpace(profileExactId))
        {
            throw new ArgumentException("Available external-demand evidence requires a profile exact ID.", nameof(profileExactId));
        }
        if (isAvailable && (!profileOffsetLogicalStep.HasValue || !externalDemandMegawatts.HasValue))
        {
            throw new ArgumentException("Available external-demand evidence requires logical offset and demand value.");
        }

        IsAvailable = isAvailable;
        ProfileExactId = profileExactId;
        LogicalStep = logicalStep;
        ProfileOffsetLogicalStep = profileOffsetLogicalStep;
        ExternalDemandMegawatts = externalDemandMegawatts;
        RequestedGeneratorLoadMegawatts = requestedGeneratorLoadMegawatts;
        ActualElectricalOutputMegawatts = actualElectricalOutputMegawatts;
        DemandOutputErrorMegawatts = demandOutputErrorMegawatts;
        NextScheduledChangeLogicalStep = nextScheduledChangeLogicalStep;
        NextScheduledDemandMegawatts = nextScheduledDemandMegawatts;
    }

    public bool IsAvailable { get; }
    public string? ProfileExactId { get; }
    public long LogicalStep { get; }
    public long? ProfileOffsetLogicalStep { get; }
    public double? ExternalDemandMegawatts { get; }
    public double? RequestedGeneratorLoadMegawatts { get; }
    public double? ActualElectricalOutputMegawatts { get; }
    public double? DemandOutputErrorMegawatts { get; }
    public long? NextScheduledChangeLogicalStep { get; }
    public double? NextScheduledDemandMegawatts { get; }

    public static ExternalEnergyDemandEvidenceSnapshot Unavailable(long logicalStep)
        => new(false, null, logicalStep, null, null, null, null, null, null, null);
}
