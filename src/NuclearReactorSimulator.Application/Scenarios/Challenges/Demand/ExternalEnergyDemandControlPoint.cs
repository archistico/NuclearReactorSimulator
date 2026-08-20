namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>One logical-step control point in an authored external electrical-demand profile.</summary>
public sealed record ExternalEnergyDemandControlPoint
{
    public ExternalEnergyDemandControlPoint(
        long offsetLogicalStep,
        double demandMegawatts,
        ExternalEnergyDemandInterpolationMode interpolationToNext = ExternalEnergyDemandInterpolationMode.Hold)
    {
        if (offsetLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetLogicalStep));
        }
        if (!double.IsFinite(demandMegawatts) || demandMegawatts < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(demandMegawatts));
        }
        if (!Enum.IsDefined(interpolationToNext))
        {
            throw new ArgumentOutOfRangeException(nameof(interpolationToNext));
        }

        OffsetLogicalStep = offsetLogicalStep;
        DemandMegawatts = demandMegawatts;
        InterpolationToNext = interpolationToNext;
    }

    public long OffsetLogicalStep { get; }
    public double DemandMegawatts { get; }
    public ExternalEnergyDemandInterpolationMode InterpolationToNext { get; }
}
