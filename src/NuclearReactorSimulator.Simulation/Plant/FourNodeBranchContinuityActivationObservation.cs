namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// One shadow observation presented to the H.20 activation supervisor.
/// It contains only deterministic evidence needed to decide whether corrected authority would be eligible.
/// </summary>
public sealed record FourNodeBranchContinuityActivationObservation
{
    public FourNodeBranchContinuityActivationObservation(
        string sampleId,
        bool triggerObserved,
        bool qualificationEvidenceAccepted,
        bool correctorConverged,
        bool lineSearchExhausted,
        double relativePressureResidual,
        double absoluteFlowResidualKilogramsPerSecond,
        double massClosureKilogramsPerSecond,
        double energyOwnershipResidualWatts,
        bool untargetedBranchDisagreementDetected)
    {
        if (string.IsNullOrWhiteSpace(sampleId))
        {
            throw new ArgumentException("Activation observation sample id must be non-empty.", nameof(sampleId));
        }

        ValidateNonNegativeFinite(relativePressureResidual, nameof(relativePressureResidual));
        ValidateNonNegativeFinite(absoluteFlowResidualKilogramsPerSecond, nameof(absoluteFlowResidualKilogramsPerSecond));
        ValidateNonNegativeFinite(massClosureKilogramsPerSecond, nameof(massClosureKilogramsPerSecond));
        ValidateNonNegativeFinite(energyOwnershipResidualWatts, nameof(energyOwnershipResidualWatts));

        SampleId = sampleId;
        TriggerObserved = triggerObserved;
        QualificationEvidenceAccepted = qualificationEvidenceAccepted;
        CorrectorConverged = correctorConverged;
        LineSearchExhausted = lineSearchExhausted;
        RelativePressureResidual = relativePressureResidual;
        AbsoluteFlowResidualKilogramsPerSecond = absoluteFlowResidualKilogramsPerSecond;
        MassClosureKilogramsPerSecond = massClosureKilogramsPerSecond;
        EnergyOwnershipResidualWatts = energyOwnershipResidualWatts;
        UntargetedBranchDisagreementDetected = untargetedBranchDisagreementDetected;
    }

    public string SampleId { get; }

    public bool TriggerObserved { get; }

    public bool QualificationEvidenceAccepted { get; }

    public bool CorrectorConverged { get; }

    public bool LineSearchExhausted { get; }

    public double RelativePressureResidual { get; }

    public double AbsoluteFlowResidualKilogramsPerSecond { get; }

    public double MassClosureKilogramsPerSecond { get; }

    public double EnergyOwnershipResidualWatts { get; }

    public bool UntargetedBranchDisagreementDetected { get; }

    private static void ValidateNonNegativeFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Activation observation metric must be finite and non-negative.");
        }
    }
}
