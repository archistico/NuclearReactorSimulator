using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Scoring;

public sealed class M10963ChallengeScoringContractTests
{
    [Fact]
    public void StandardPolicies_FreezeExactWeightsDominanceCapsAndGradeThresholds()
    {
        var general = StandardChallengeScoringPolicies.GeneralOperationsV1;
        var demand = StandardChallengeScoringPolicies.DemandFollowingV1;

        Assert.Equal("general-operations@1", general.ExactId);
        Assert.Collection(
            general.Dimensions,
            item => AssertDimension(item, ChallengeScoreDimensionKind.SafetyProtectionDiscipline, 45m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.ProcedureRequiredActions, 30m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.StabilityOperatingQuality, 20m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, 5m));

        Assert.Equal("demand-following@1", demand.ExactId);
        Assert.Collection(
            demand.Dimensions,
            item => AssertDimension(item, ChallengeScoreDimensionKind.SafetyProtectionDiscipline, 40m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.ProcedureRequiredActions, 25m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.StabilityOperatingQuality, 15m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.DemandTracking, 15m),
            item => AssertDimension(item, ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, 5m));

        Assert.Equal(60m, demand.PassPercentage);
        Assert.Equal(75m, demand.ProficientPercentage);
        Assert.Equal(90m, demand.ExcellentPercentage);
        Assert.Equal(39m, demand.CriticalSafetyCapPercentage);
        Assert.Equal(59m, demand.CriticalProcedureCapPercentage);
    }

    [Fact]
    public void DemandFollowing_EvaluatesFiveIndependentDimensionsDeterministically()
    {
        var policy = StandardChallengeScoringPolicies.DemandFollowingV1;
        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: true);
        var evidence = new[]
        {
            Available(ChallengeScoreDimensionKind.SafetyProtectionDiscipline, 1m, "protection-journal", "No authored critical safety violation observed."),
            Available(ChallengeScoreDimensionKind.ProcedureRequiredActions, 0.8m, "required-actions", "Four of five weighted required-action evidence units satisfied."),
            Available(ChallengeScoreDimensionKind.StabilityOperatingQuality, 0.6m, "stability-window", "Bounded operating-quality evidence fraction 0.60."),
            Available(ChallengeScoreDimensionKind.DemandTracking, 0.5m, "demand-output-error", "Demand-tracking evidence fraction 0.50."),
            Available(ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, 1m, "logical-time-window", "Completed inside authored target window."),
        };

        var first = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            evidence);
        var repeat = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            evidence);

        Assert.Equal(81.5m, first.RawScore);
        Assert.Equal(81.5m, first.FinalScore);
        Assert.Equal(81.5m, first.FinalPercentage);
        Assert.True(first.IsEvidenceComplete);
        Assert.True(first.IsPassing);
        Assert.Equal(ChallengeScoreDominanceOutcome.None, first.DominanceOutcome);
        Assert.Equal(ChallengeScoreGrade.Proficient, first.Grade);
        Assert.Equal(ScoreFingerprint(first), ScoreFingerprint(repeat));
    }

    [Fact]
    public void CriticalSafetyFailure_DominatesPerfectOtherDimensionsAndCapsAtThirtyNinePercent()
    {
        var policy = StandardChallengeScoringPolicies.DemandFollowingV1;
        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: true);
        var evidence = PerfectEvidence(policy)
            .Select(item => item.Kind == ChallengeScoreDimensionKind.SafetyProtectionDiscipline
                ? Available(item.Kind, 1m, "protection-trip", "Authored critical safety failure observed.", critical: true)
                : item)
            .ToArray();

        var result = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            evidence);

        Assert.Equal(100m, result.RawScore);
        Assert.Equal(39m, result.FinalScore);
        Assert.Equal(39m, result.AppliedDominanceCapPercentage);
        Assert.False(result.IsPassing);
        Assert.Equal(ChallengeScoreDominanceOutcome.CriticalSafetyFailure, result.DominanceOutcome);
        Assert.Equal(ChallengeScoreGrade.Unsafe, result.Grade);
    }

    [Fact]
    public void CriticalProcedureFailure_DominatesDemandAndSpeedAndCapsAtFiftyNinePercent()
    {
        var policy = StandardChallengeScoringPolicies.DemandFollowingV1;
        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: true);
        var evidence = PerfectEvidence(policy)
            .Select(item => item.Kind == ChallengeScoreDimensionKind.ProcedureRequiredActions
                ? Available(item.Kind, 1m, "procedure-critical", "Authored critical procedural failure observed.", critical: true)
                : item)
            .ToArray();

        var result = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            evidence);

        Assert.Equal(100m, result.RawScore);
        Assert.Equal(59m, result.FinalScore);
        Assert.Equal(59m, result.AppliedDominanceCapPercentage);
        Assert.False(result.IsPassing);
        Assert.Equal(ChallengeScoreDominanceOutcome.CriticalProcedureFailure, result.DominanceOutcome);
        Assert.Equal(ChallengeScoreGrade.ProcedureFailure, result.Grade);
    }

    [Fact]
    public void UnavailableRequiredEvidence_ScoresZeroAndCannotSilentlyPass()
    {
        var policy = StandardChallengeScoringPolicies.GeneralOperationsV1;
        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: false);
        var evidence = PerfectEvidence(policy)
            .Select(item => item.Kind == ChallengeScoreDimensionKind.StabilityOperatingQuality
                ? new ChallengeScoreDimensionEvidence(
                    item.Kind,
                    false,
                    null,
                    "stability-window",
                    "Required operating-quality evidence is unavailable.")
                : item)
            .ToArray();

        var result = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            evidence);

        Assert.Equal(80m, result.RawScore);
        Assert.False(result.IsEvidenceComplete);
        Assert.False(result.IsPassing);
        Assert.Equal(ChallengeScoreGrade.IncompleteEvidence, result.Grade);
        Assert.Contains(result.Dimensions, item => item.Kind == ChallengeScoreDimensionKind.StabilityOperatingQuality && !item.IsEvidenceAvailable && item.AwardedPoints == 0m);
    }

    [Fact]
    public void StandardV1GuidanceAndAuthorityModifiers_AreExplicitAndNeutral()
    {
        foreach (var policy in new[] { StandardChallengeScoringPolicies.GeneralOperationsV1, StandardChallengeScoringPolicies.DemandFollowingV1 })
        {
            foreach (var mode in Enum.GetValues<TrainingGuidanceMode>())
            {
                Assert.Equal(1m, policy.GuidanceMultiplier(mode));
            }
            foreach (var authority in Enum.GetValues<PlantControlAuthorityMode>())
            {
                Assert.Equal(1m, policy.AuthorityMultiplier(authority));
            }
        }
    }

    [Fact]
    public void NonNeutralModifiers_MustBeAuthoredInVersionedPolicyAndRemainObservable()
    {
        var policy = new ChallengeScoringPolicyDefinition(
            "explicit-modifiers",
            1,
            StandardChallengeScoringPolicies.GeneralOperationsV1.Dimensions,
            new[]
            {
                new ChallengeGuidanceScoreModifier(TrainingGuidanceMode.Hidden, 1m),
                new ChallengeGuidanceScoreModifier(TrainingGuidanceMode.ChecklistOnly, 0.95m),
                new ChallengeGuidanceScoreModifier(TrainingGuidanceMode.Guided, 0.85m),
            },
            new[]
            {
                new ChallengeAuthorityScoreModifier(PlantControlAuthorityMode.Manual, 1m),
                new ChallengeAuthorityScoreModifier(PlantControlAuthorityMode.Assisted, 0.95m),
                new ChallengeAuthorityScoreModifier(PlantControlAuthorityMode.SupervisoryAutomatic, 0.90m),
            });
        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: false);

        var result = ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Guided,
            PlantControlAuthorityMode.SupervisoryAutomatic,
            PerfectEvidence(policy));

        Assert.Equal(1m, result.RawScore / 100m);
        Assert.Equal(0.85m, result.GuidanceMultiplier);
        Assert.Equal(0.90m, result.AuthorityMultiplier);
        Assert.Equal(76.5m, result.FinalScore);
        Assert.Equal(ChallengeScoreGrade.Proficient, result.Grade);
    }

    [Fact]
    public void PolicyIdentityAndEvidenceShape_FailClosed()
    {
        var policy = StandardChallengeScoringPolicies.GeneralOperationsV1;
        var wrongChallenge = CreateChallenge("different-policy@1", withDemandProfile: false);
        Assert.Throws<InvalidOperationException>(() => ChallengeScoreCalculator.Evaluate(
            wrongChallenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            PerfectEvidence(policy)));

        var challenge = CreateChallenge(policy.ExactId, withDemandProfile: false);
        var missing = PerfectEvidence(policy).Where(item => item.Kind != ChallengeScoreDimensionKind.StabilityOperatingQuality).ToArray();
        Assert.Throws<ArgumentException>(() => ChallengeScoreCalculator.Evaluate(
            challenge,
            policy,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual,
            missing));
    }

    [Fact]
    public void PublicScoringContract_HasNoWallClockOrPlantCommandAuthoritySeam()
    {
        var publicTypes = typeof(ChallengeScoreCalculator).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ChallengeScoreCalculator).Namespace)
            .ToArray();

        Assert.DoesNotContain(publicTypes, static type => type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan));
        foreach (var type in publicTypes)
        {
            foreach (var memberType in PublicMemberTypes(type))
            {
                Assert.DoesNotContain("IControlRoomCommandDispatcher", memberType.FullName ?? memberType.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("IPlantControlAuthorityDispatcher", memberType.FullName ?? memberType.Name, StringComparison.Ordinal);
                Assert.NotEqual(typeof(DateTime), memberType);
                Assert.NotEqual(typeof(DateTimeOffset), memberType);
                Assert.NotEqual(typeof(TimeSpan), memberType);
            }
        }
    }

    [Fact]
    public void ArtifactSummary_WritesFrozenScoringContractEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m1096-multidimensional-scoring-contract.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.6.3 deterministic observational scoring arithmetic only; no challenge pack, UI, command dispatch, supervisory authority, protection ownership or physics change;",
            "standard-policy-general=general-operations@1:safety45|procedure30|stability20|logical-time5;",
            "standard-policy-demand=demand-following@1:safety40|procedure25|stability15|demand15|logical-time5;",
            "grade-thresholds=pass60|proficient75|excellent90; dominance-caps=critical-safety39|critical-procedure59;",
            "critical-safety-dominates=True; critical-procedure-dominates=True; unavailable-evidence-can-pass=False; generic-trip-is-global-failure=False;",
            "standard-guidance-modifiers-neutral=True; standard-authority-modifiers-neutral=True; non-neutral-modifiers-versioned-policy-owned=True;",
            "scoring-policy-id-fail-closed=True; evidence-shape-fail-closed=True; score-observational=True; plant-command-authority=False; wall-clock-dependence=False;",
            "m10963-multidimensional-scoring-contract-passes=True; next-step=if green, preserve weights/caps/dominance semantics and move to M10.9.6.4 initial challenge packs using only existing validated plant/scenario/fault owners;",
        });

        Assert.True(File.Exists(path));
        Assert.Contains("m10963-multidimensional-scoring-contract-passes=True", File.ReadAllText(path), StringComparison.Ordinal);
    }

    private static string ScoreFingerprint(ChallengeScoreEvaluationResult result)
        => string.Join(
            "|",
            result.ChallengeExactId,
            result.ScoringPolicyExactId,
            result.RawScore,
            result.GuidanceMultiplier,
            result.AuthorityMultiplier,
            result.FinalScore,
            result.DominanceOutcome,
            result.Grade,
            result.IsEvidenceComplete,
            result.IsPassing,
            string.Join(",", result.Dimensions.Select(static item => $"{item.Kind}:{item.AwardedPoints}:{item.IsEvidenceAvailable}:{item.IsCriticalFailure}")));

    private static void AssertDimension(ChallengeScoreDimensionPolicy item, ChallengeScoreDimensionKind kind, decimal maximumPoints)
    {
        Assert.Equal(kind, item.Kind);
        Assert.Equal(maximumPoints, item.MaximumPoints);
    }

    private static ChallengeScoreDimensionEvidence[] PerfectEvidence(ChallengeScoringPolicyDefinition policy)
        => policy.Dimensions
            .Select(item => Available(item.Kind, 1m, $"{item.Kind}-source", $"Complete observational evidence for {item.Kind}."))
            .ToArray();

    private static ChallengeScoreDimensionEvidence Available(
        ChallengeScoreDimensionKind kind,
        decimal fraction,
        string source,
        string summary,
        bool critical = false)
        => new(kind, true, fraction, source, summary, critical);

    private static ChallengeDefinition CreateChallenge(string scoringPolicyId, bool withDemandProfile)
        => new(
            "m10963-scoring-challenge",
            1,
            "m10963-scenario",
            "evaluate-operation",
            "Evaluate operation",
            "Exercise deterministic multidimensional challenge scoring without control authority.",
            new ChallengeConditionDefinition("active", "Active", "Authored activation evidence."),
            new[] { new ChallengeConditionDefinition("observe", "Observe", "Authored required evidence.") },
            new[] { new ChallengeConditionDefinition("complete", "Complete", "Authored completion evidence.") },
            null,
            new ChallengeLogicalTimeContract(0, 10, 100, 120),
            new ChallengeAssistancePolicy(Enum.GetValues<TrainingGuidanceMode>(), scoringPolicyId),
            withDemandProfile ? ExternalEnergyDemandProfileDefinition.Constant("m10963-demand", 1, 5d, 10d) : null);

    private static IEnumerable<Type> PublicMemberTypes(Type type)
    {
        foreach (var constructor in type.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var method in type.GetMethods().Where(static method => method.IsPublic))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var property in type.GetProperties())
        {
            yield return property.PropertyType;
        }
    }

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.6.3 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1096-multidimensional-scoring");
    }
}
