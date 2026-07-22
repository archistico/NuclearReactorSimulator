namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Explicit absolute/relative tolerance budget for one quantitative reference target.</summary>
public sealed record ReferenceValidationToleranceBudget
{
    public ReferenceValidationToleranceBudget(double absoluteTolerance, double relativeToleranceFraction = 0d)
    {
        if (!double.IsFinite(absoluteTolerance) || absoluteTolerance < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteTolerance));
        }
        if (!double.IsFinite(relativeToleranceFraction) || relativeToleranceFraction < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeToleranceFraction));
        }

        AbsoluteTolerance = absoluteTolerance;
        RelativeToleranceFraction = relativeToleranceFraction;
    }

    public double AbsoluteTolerance { get; }

    public double RelativeToleranceFraction { get; }

    public double AllowedError(double referenceValue)
        => Math.Max(AbsoluteTolerance, Math.Abs(referenceValue) * RelativeToleranceFraction);
}
