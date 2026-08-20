namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>
/// Versioned deterministic external electrical-demand reference for one operational challenge. This is training/evaluation
/// evidence only: it never mutates generator requested load, grid coupling or plant-control authority.
/// </summary>
public sealed class ExternalEnergyDemandProfileDefinition
{
    private readonly IReadOnlyList<ExternalEnergyDemandControlPoint> _controlPoints;

    public ExternalEnergyDemandProfileDefinition(
        string profileId,
        int version,
        double minimumDemandMegawatts,
        double maximumDemandMegawatts,
        IEnumerable<ExternalEnergyDemandControlPoint> controlPoints,
        bool exposeNextScheduledChange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        if (!double.IsFinite(minimumDemandMegawatts) || minimumDemandMegawatts < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumDemandMegawatts));
        }
        if (!double.IsFinite(maximumDemandMegawatts) || maximumDemandMegawatts <= minimumDemandMegawatts)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDemandMegawatts));
        }
        ArgumentNullException.ThrowIfNull(controlPoints);

        var points = controlPoints.ToArray();
        if (points.Length == 0 || points.Any(static point => point is null))
        {
            throw new ArgumentException("An external-demand profile requires at least one non-null control point.", nameof(controlPoints));
        }
        if (points[0].OffsetLogicalStep != 0)
        {
            throw new ArgumentException("The first external-demand control point must start at logical-step offset zero.", nameof(controlPoints));
        }
        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            if (point.DemandMegawatts < minimumDemandMegawatts || point.DemandMegawatts > maximumDemandMegawatts)
            {
                throw new ArgumentOutOfRangeException(nameof(controlPoints), point.DemandMegawatts, "External demand must remain inside the authored profile bounds.");
            }
            if (index > 0 && point.OffsetLogicalStep <= points[index - 1].OffsetLogicalStep)
            {
                throw new ArgumentException("External-demand control-point offsets must be strictly increasing.", nameof(controlPoints));
            }
        }
        if (points[^1].InterpolationToNext != ExternalEnergyDemandInterpolationMode.Hold)
        {
            throw new ArgumentException("The final external-demand control point must HOLD because there is no following point.", nameof(controlPoints));
        }

        ProfileId = profileId.Trim();
        Version = version;
        MinimumDemandMegawatts = minimumDemandMegawatts;
        MaximumDemandMegawatts = maximumDemandMegawatts;
        ExposeNextScheduledChange = exposeNextScheduledChange;
        _controlPoints = Array.AsReadOnly(points);
    }

    public string ProfileId { get; }
    public int Version { get; }
    public string ExactId => $"{ProfileId}@{Version}";
    public double MinimumDemandMegawatts { get; }
    public double MaximumDemandMegawatts { get; }
    public bool ExposeNextScheduledChange { get; }
    public IReadOnlyList<ExternalEnergyDemandControlPoint> ControlPoints => _controlPoints;

    public ExternalEnergyDemandProfileEvaluation Evaluate(long offsetLogicalStep)
    {
        if (offsetLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetLogicalStep));
        }

        var currentIndex = 0;
        for (var index = 1; index < _controlPoints.Count; index++)
        {
            if (_controlPoints[index].OffsetLogicalStep > offsetLogicalStep)
            {
                break;
            }
            currentIndex = index;
        }

        var current = _controlPoints[currentIndex];
        var next = currentIndex + 1 < _controlPoints.Count ? _controlPoints[currentIndex + 1] : null;
        var demand = current.DemandMegawatts;
        if (next is not null
            && current.InterpolationToNext == ExternalEnergyDemandInterpolationMode.Linear
            && offsetLogicalStep < next.OffsetLogicalStep)
        {
            var span = next.OffsetLogicalStep - current.OffsetLogicalStep;
            var elapsed = offsetLogicalStep - current.OffsetLogicalStep;
            var fraction = (double)elapsed / span;
            demand = current.DemandMegawatts + ((next.DemandMegawatts - current.DemandMegawatts) * fraction);
        }

        return new ExternalEnergyDemandProfileEvaluation(
            offsetLogicalStep,
            demand,
            currentIndex,
            current.InterpolationToNext,
            next);
    }

    public static ExternalEnergyDemandProfileDefinition Constant(
        string profileId,
        int version,
        double demandMegawatts,
        double maximumDemandMegawatts,
        bool exposeNextScheduledChange = false)
        => new(
            profileId,
            version,
            0d,
            maximumDemandMegawatts,
            new[] { new ExternalEnergyDemandControlPoint(0, demandMegawatts) },
            exposeNextScheduledChange);

    public static ExternalEnergyDemandProfileDefinition Step(
        string profileId,
        int version,
        double initialDemandMegawatts,
        long stepAtOffsetLogicalStep,
        double steppedDemandMegawatts,
        double maximumDemandMegawatts,
        bool exposeNextScheduledChange = true)
    {
        if (stepAtOffsetLogicalStep <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepAtOffsetLogicalStep));
        }
        return new ExternalEnergyDemandProfileDefinition(
            profileId,
            version,
            0d,
            maximumDemandMegawatts,
            new[]
            {
                new ExternalEnergyDemandControlPoint(0, initialDemandMegawatts),
                new ExternalEnergyDemandControlPoint(stepAtOffsetLogicalStep, steppedDemandMegawatts),
            },
            exposeNextScheduledChange);
    }

    public static ExternalEnergyDemandProfileDefinition Ramp(
        string profileId,
        int version,
        double initialDemandMegawatts,
        long rampStartOffsetLogicalStep,
        long rampEndOffsetLogicalStep,
        double finalDemandMegawatts,
        double maximumDemandMegawatts,
        bool exposeNextScheduledChange = true)
    {
        if (rampStartOffsetLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rampStartOffsetLogicalStep));
        }
        if (rampEndOffsetLogicalStep <= rampStartOffsetLogicalStep)
        {
            throw new ArgumentOutOfRangeException(nameof(rampEndOffsetLogicalStep));
        }

        var points = rampStartOffsetLogicalStep == 0
            ? new[]
            {
                new ExternalEnergyDemandControlPoint(0, initialDemandMegawatts, ExternalEnergyDemandInterpolationMode.Linear),
                new ExternalEnergyDemandControlPoint(rampEndOffsetLogicalStep, finalDemandMegawatts),
            }
            : new[]
            {
                new ExternalEnergyDemandControlPoint(0, initialDemandMegawatts),
                new ExternalEnergyDemandControlPoint(rampStartOffsetLogicalStep, initialDemandMegawatts, ExternalEnergyDemandInterpolationMode.Linear),
                new ExternalEnergyDemandControlPoint(rampEndOffsetLogicalStep, finalDemandMegawatts),
            };

        return new ExternalEnergyDemandProfileDefinition(
            profileId,
            version,
            0d,
            maximumDemandMegawatts,
            points,
            exposeNextScheduledChange);
    }

    public static ExternalEnergyDemandProfileDefinition Piecewise(
        string profileId,
        int version,
        double minimumDemandMegawatts,
        double maximumDemandMegawatts,
        IEnumerable<ExternalEnergyDemandControlPoint> controlPoints,
        bool exposeNextScheduledChange)
        => new(
            profileId,
            version,
            minimumDemandMegawatts,
            maximumDemandMegawatts,
            controlPoints,
            exposeNextScheduledChange);
}
