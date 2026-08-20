using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

/// <summary>
/// Versioned score arithmetic policy. All weights, dominance caps, grade thresholds and assistance/authority modifiers
/// are authored here rather than in presentation code.
/// </summary>
public sealed class ChallengeScoringPolicyDefinition
{
    private readonly IReadOnlyList<ChallengeScoreDimensionPolicy> _dimensions;
    private readonly IReadOnlyDictionary<TrainingGuidanceMode, decimal> _guidanceModifiers;
    private readonly IReadOnlyDictionary<PlantControlAuthorityMode, decimal> _authorityModifiers;

    public ChallengeScoringPolicyDefinition(
        string policyId,
        int version,
        IEnumerable<ChallengeScoreDimensionPolicy> dimensions,
        IEnumerable<ChallengeGuidanceScoreModifier> guidanceModifiers,
        IEnumerable<ChallengeAuthorityScoreModifier> authorityModifiers,
        decimal passPercentage = 60m,
        decimal proficientPercentage = 75m,
        decimal excellentPercentage = 90m,
        decimal criticalSafetyCapPercentage = 39m,
        decimal criticalProcedureCapPercentage = 59m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(guidanceModifiers);
        ArgumentNullException.ThrowIfNull(authorityModifiers);

        var dimensionArray = dimensions.ToArray();
        if (dimensionArray.Length == 0 || dimensionArray.Any(static item => item is null))
        {
            throw new ArgumentException("A scoring policy requires non-null dimensions.", nameof(dimensions));
        }
        if (dimensionArray.Select(static item => item.Kind).Distinct().Count() != dimensionArray.Length)
        {
            throw new ArgumentException("Scoring policy dimensions must be unique.", nameof(dimensions));
        }
        if (!dimensionArray.Any(static item => item.Kind == ChallengeScoreDimensionKind.SafetyProtectionDiscipline)
            || !dimensionArray.Any(static item => item.Kind == ChallengeScoreDimensionKind.ProcedureRequiredActions))
        {
            throw new ArgumentException("Every scoring policy must contain safety and procedure dimensions.", nameof(dimensions));
        }
        if (dimensionArray.Sum(static item => item.MaximumPoints) != 100m)
        {
            throw new ArgumentException("Challenge scoring policy dimension maximums must total exactly 100 points.", nameof(dimensions));
        }

        ValidateThreshold(passPercentage, nameof(passPercentage));
        ValidateThreshold(proficientPercentage, nameof(proficientPercentage));
        ValidateThreshold(excellentPercentage, nameof(excellentPercentage));
        ValidateThreshold(criticalSafetyCapPercentage, nameof(criticalSafetyCapPercentage));
        ValidateThreshold(criticalProcedureCapPercentage, nameof(criticalProcedureCapPercentage));
        if (!(criticalSafetyCapPercentage < passPercentage
            && passPercentage < proficientPercentage
            && proficientPercentage < excellentPercentage))
        {
            throw new ArgumentException("Grade thresholds must satisfy safety-cap < pass < proficient < excellent.");
        }
        if (!(criticalSafetyCapPercentage < criticalProcedureCapPercentage
            && criticalProcedureCapPercentage < passPercentage))
        {
            throw new ArgumentException("Dominance caps must satisfy safety-cap < procedure-cap < pass threshold.");
        }

        var guidance = guidanceModifiers.ToArray();
        var authority = authorityModifiers.ToArray();
        _guidanceModifiers = BuildModifierMap(guidance, Enum.GetValues<TrainingGuidanceMode>(), static item => item.Mode, static item => item.Multiplier, nameof(guidanceModifiers));
        _authorityModifiers = BuildModifierMap(authority, Enum.GetValues<PlantControlAuthorityMode>(), static item => item.Mode, static item => item.Multiplier, nameof(authorityModifiers));

        PolicyId = policyId.Trim();
        Version = version;
        _dimensions = Array.AsReadOnly(dimensionArray);
        PassPercentage = passPercentage;
        ProficientPercentage = proficientPercentage;
        ExcellentPercentage = excellentPercentage;
        CriticalSafetyCapPercentage = criticalSafetyCapPercentage;
        CriticalProcedureCapPercentage = criticalProcedureCapPercentage;
    }

    public string PolicyId { get; }
    public int Version { get; }
    public string ExactId => $"{PolicyId}@{Version}";
    public IReadOnlyList<ChallengeScoreDimensionPolicy> Dimensions => _dimensions;
    public decimal PassPercentage { get; }
    public decimal ProficientPercentage { get; }
    public decimal ExcellentPercentage { get; }
    public decimal CriticalSafetyCapPercentage { get; }
    public decimal CriticalProcedureCapPercentage { get; }

    public decimal GuidanceMultiplier(TrainingGuidanceMode mode)
        => _guidanceModifiers.TryGetValue(mode, out var multiplier)
            ? multiplier
            : throw new ArgumentOutOfRangeException(nameof(mode));

    public decimal AuthorityMultiplier(PlantControlAuthorityMode mode)
        => _authorityModifiers.TryGetValue(mode, out var multiplier)
            ? multiplier
            : throw new ArgumentOutOfRangeException(nameof(mode));

    private static void ValidateThreshold(decimal value, string parameterName)
    {
        if (value < 0m || value > 100m)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static IReadOnlyDictionary<TMode, decimal> BuildModifierMap<TModifier, TMode>(
        IReadOnlyList<TModifier> modifiers,
        IReadOnlyList<TMode> requiredModes,
        Func<TModifier, TMode> modeSelector,
        Func<TModifier, decimal> multiplierSelector,
        string parameterName)
        where TMode : struct, Enum
    {
        if (modifiers.Any(static item => item is null))
        {
            throw new ArgumentException("Score modifier collections cannot contain null entries.", parameterName);
        }

        var map = new Dictionary<TMode, decimal>();
        foreach (var modifier in modifiers)
        {
            var mode = modeSelector(modifier);
            if (!map.TryAdd(mode, multiplierSelector(modifier)))
            {
                throw new ArgumentException($"Score modifier for mode '{mode}' is duplicated.", parameterName);
            }
        }
        if (requiredModes.Any(mode => !map.ContainsKey(mode)) || map.Count != requiredModes.Count)
        {
            throw new ArgumentException("Score modifiers must explicitly cover every defined mode exactly once.", parameterName);
        }

        return map;
    }
}
