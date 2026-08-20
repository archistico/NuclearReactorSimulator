using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Demand;

public sealed class M10962ExternalEnergyDemandProfileTests
{
    [Fact]
    public void Profiles_ConstantStepRampAndPiecewiseEvaluateFromLogicalStepsOnly()
    {
        var constant = ExternalEnergyDemandProfileDefinition.Constant("constant", 1, 5d, 10d);
        var step = ExternalEnergyDemandProfileDefinition.Step("step", 1, 5d, 50, 8d, 10d);
        var ramp = ExternalEnergyDemandProfileDefinition.Ramp("ramp", 1, 4d, 10, 20, 8d, 10d);
        var piecewise = ExternalEnergyDemandProfileDefinition.Piecewise(
            "piecewise",
            1,
            0d,
            10d,
            new[]
            {
                new ExternalEnergyDemandControlPoint(0, 3d),
                new ExternalEnergyDemandControlPoint(5, 5d, ExternalEnergyDemandInterpolationMode.Linear),
                new ExternalEnergyDemandControlPoint(10, 7d),
            },
            exposeNextScheduledChange: true);

        Close(5d, constant.Evaluate(0).DemandMegawatts);
        Close(5d, constant.Evaluate(10_000).DemandMegawatts);
        Close(5d, step.Evaluate(49).DemandMegawatts);
        Close(8d, step.Evaluate(50).DemandMegawatts);
        Close(4d, ramp.Evaluate(9).DemandMegawatts);
        Close(6d, ramp.Evaluate(15).DemandMegawatts);
        Close(8d, ramp.Evaluate(20).DemandMegawatts);
        Close(5.8d, piecewise.Evaluate(7).DemandMegawatts);
        Close(7d, piecewise.Evaluate(100).DemandMegawatts);
    }

    [Fact]
    public void Evidence_SeparatesExternalDemandRequestedGeneratorLoadAndActualOutputWithoutMutatingRequest()
    {
        var profile = ExternalEnergyDemandProfileDefinition.Constant("tracking", 1, 7d, 10d);
        var challenge = CreateChallenge(profile);
        var lifecycle = Lifecycle(challenge, logicalStep: 120, activatedLogicalStep: 100);
        var snapshot = Snapshot(logicalStep: 120, requestedMegawatts: 6d, actualMegawatts: 4.8d);
        var requestedBefore = snapshot.Electrical.Generators[0].RequestedElectricalPower.NumericValue;

        var evidence = ScenarioChallengeExternalDemandProjector.Project(challenge, lifecycle, snapshot);

        Assert.True(evidence.IsAvailable);
        Assert.Equal("tracking@1", evidence.ProfileExactId);
        Assert.Equal(20L, evidence.ProfileOffsetLogicalStep);
        Close(7d, evidence.ExternalDemandMegawatts);
        Close(6d, evidence.RequestedGeneratorLoadMegawatts);
        Close(4.8d, evidence.ActualElectricalOutputMegawatts);
        Close(2.2d, evidence.DemandOutputErrorMegawatts);
        Assert.Equal(requestedBefore, snapshot.Electrical.Generators[0].RequestedElectricalPower.NumericValue);
        Assert.NotEqual(evidence.ExternalDemandMegawatts, evidence.RequestedGeneratorLoadMegawatts);
        Assert.NotEqual(evidence.ExternalDemandMegawatts, evidence.ActualElectricalOutputMegawatts);
    }

    [Fact]
    public void Evidence_IsUnavailableWithoutOwnedProfileOrBeforeChallengeActivation()
    {
        var noProfile = CreateChallenge(null);
        var noProfileEvidence = ScenarioChallengeExternalDemandProjector.Project(
            noProfile,
            Lifecycle(noProfile, logicalStep: 20, activatedLogicalStep: 10),
            Snapshot(20, 5d, 5d));
        Assert.False(noProfileEvidence.IsAvailable);

        var profile = ExternalEnergyDemandProfileDefinition.Constant("owned", 1, 5d, 10d);
        var notActivated = CreateChallenge(profile);
        var preActivationEvidence = ScenarioChallengeExternalDemandProjector.Project(
            notActivated,
            Lifecycle(notActivated, logicalStep: 20, activatedLogicalStep: null),
            Snapshot(20, 5d, 5d));
        Assert.False(preActivationEvidence.IsAvailable);
    }

    [Fact]
    public void FutureScheduleVisibility_IsDefinitionOwnedAndUsesAbsoluteLogicalStep()
    {
        var visible = ExternalEnergyDemandProfileDefinition.Step("visible", 1, 5d, 50, 8d, 10d, exposeNextScheduledChange: true);
        var hidden = ExternalEnergyDemandProfileDefinition.Step("hidden", 1, 5d, 50, 8d, 10d, exposeNextScheduledChange: false);
        var visibleChallenge = CreateChallenge(visible);
        var hiddenChallenge = CreateChallenge(hidden);
        var snapshot = Snapshot(120, 5d, 5d);

        var visibleEvidence = ScenarioChallengeExternalDemandProjector.Project(
            visibleChallenge,
            Lifecycle(visibleChallenge, 120, 100),
            snapshot);
        var hiddenEvidence = ScenarioChallengeExternalDemandProjector.Project(
            hiddenChallenge,
            Lifecycle(hiddenChallenge, 120, 100),
            snapshot);

        Assert.Equal(150L, visibleEvidence.NextScheduledChangeLogicalStep);
        Close(8d, visibleEvidence.NextScheduledDemandMegawatts);
        Assert.Null(hiddenEvidence.NextScheduledChangeLogicalStep);
        Assert.Null(hiddenEvidence.NextScheduledDemandMegawatts);
    }

    [Fact]
    public void DemandTimeline_ReconstructsExactlyAndDoesNotDependOnProjectionCadence()
    {
        var profile = ExternalEnergyDemandProfileDefinition.Piecewise(
            "replay-demand",
            3,
            0d,
            10d,
            new[]
            {
                new ExternalEnergyDemandControlPoint(0, 4d),
                new ExternalEnergyDemandControlPoint(40, 4d, ExternalEnergyDemandInterpolationMode.Linear),
                new ExternalEnergyDemandControlPoint(80, 7d),
                new ExternalEnergyDemandControlPoint(120, 5d),
            },
            exposeNextScheduledChange: true);
        var challenge = CreateChallenge(profile);
        var logicalSteps = new long[] { 200, 201, 217, 240, 260, 280, 319, 320, 400 };
        var left = logicalSteps.Select(step => Fingerprint(Project(challenge, step, 200))).ToArray();

        _ = Project(challenge, 205, 200);
        _ = Project(challenge, 250, 200);
        _ = Project(challenge, 399, 200);
        var right = logicalSteps.Select(step => Fingerprint(Project(challenge, step, 200))).ToArray();

        Assert.Equal(left, right);
    }

    [Fact]
    public void Profiles_RejectInvalidBoundsOrderingAndTerminalInterpolationFailClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExternalEnergyDemandProfileDefinition.Constant("bad", 1, 11d, 10d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExternalEnergyDemandProfileDefinition.Step("bad-step", 1, 5d, 0, 7d, 10d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExternalEnergyDemandProfileDefinition.Ramp("bad-ramp", 1, 5d, 10, 10, 7d, 10d));
        Assert.Throws<ArgumentException>(() => new ExternalEnergyDemandProfileDefinition(
            "unordered",
            1,
            0d,
            10d,
            new[]
            {
                new ExternalEnergyDemandControlPoint(0, 5d),
                new ExternalEnergyDemandControlPoint(5, 6d),
                new ExternalEnergyDemandControlPoint(5, 7d),
            },
            false));
        Assert.Throws<ArgumentException>(() => new ExternalEnergyDemandProfileDefinition(
            "terminal-linear",
            1,
            0d,
            10d,
            new[] { new ExternalEnergyDemandControlPoint(0, 5d, ExternalEnergyDemandInterpolationMode.Linear) },
            false));
    }

    [Fact]
    public void PublicDemandContract_HasNoWallClockOrPlantCommandAuthority()
    {
        var publicTypes = new[]
        {
            typeof(ExternalEnergyDemandProfileDefinition),
            typeof(ExternalEnergyDemandControlPoint),
            typeof(ExternalEnergyDemandProfileEvaluation),
            typeof(ExternalEnergyDemandEvidenceSnapshot),
            typeof(ScenarioChallengeExternalDemandProjector),
        };

        Assert.DoesNotContain(
            publicTypes.SelectMany(static type => type.GetProperties()),
            static property => property.PropertyType == typeof(DateTime)
                || property.PropertyType == typeof(DateTimeOffset)
                || property.PropertyType == typeof(TimeSpan));
        Assert.DoesNotContain(
            publicTypes.SelectMany(static type => type.GetMembers()),
            static member => member.Name.Contains("Dispatch", StringComparison.Ordinal)
                || member.Name.Contains("Authority", StringComparison.Ordinal)
                || member.Name.Contains("Setpoint", StringComparison.Ordinal)
                || member.Name.Contains("Torque", StringComparison.Ordinal));
    }

    [Fact]
    public void Audit_WritesDeterministicExternalDemandContractSummary()
    {
        var profile = ExternalEnergyDemandProfileDefinition.Ramp("audit-ramp", 1, 4d, 20, 60, 8d, 10d);
        var challenge = CreateChallenge(profile);
        var first = Project(challenge, 130, 100);
        var repeat = Project(challenge, 130, 100);
        Assert.Equal(Fingerprint(first), Fingerprint(repeat));
        Assert.True(first.IsAvailable);
        Assert.NotNull(first.NextScheduledChangeLogicalStep);

        var artifactDirectory = ResolveArtifactDirectory();
        Directory.CreateDirectory(artifactDirectory);
        var summary = string.Join(
            Environment.NewLine,
            "=== 01-m1096-external-energy-demand-profile-contract ===",
            "scope=M10.9.6.2 deterministic Application-layer external energy-demand profiles only; no score arithmetic, UI, generator-request mutation, grid coupling, supervisory authority or physics change;",
            "profile-primitives=constant|step|bounded-ramp|piecewise-hold-linear; logical-time-source=challenge activation logical step + profile offset;",
            "external-grid-demand-vs-generator-request-separated=True; external-grid-demand-vs-actual-output-separated=True; demand-output-error-observational=True;",
            "demand-unavailable-without-owned-profile=True; future-schedule-visibility-definition-owned=True; requested-generator-load-mutated=False;",
            "projection-cadence-independent=True; replay-reconstruction-deterministic=True; wall-clock-dependence=False; plant-command-authority=False;",
            "m10962-external-demand-contract-passes=True; next-step=if green, preserve versioned demand semantics and move to M10.9.6.3 multidimensional scoring contract without allowing demand to command the generator;");
        File.WriteAllText(
            Path.Combine(artifactDirectory, "01-m1096-external-energy-demand-profile-contract.summary.txt"),
            summary + Environment.NewLine,
            new UTF8Encoding(false));
    }

    private static ExternalEnergyDemandEvidenceSnapshot Project(ChallengeDefinition challenge, long logicalStep, long activationStep)
        => ScenarioChallengeExternalDemandProjector.Project(
            challenge,
            Lifecycle(challenge, logicalStep, activationStep),
            Snapshot(logicalStep, 6d, 5d));

    private static ChallengeDefinition CreateChallenge(ExternalEnergyDemandProfileDefinition? profile)
        => new(
            "m10962-demand-challenge",
            1,
            "m10962-scenario",
            "track-demand",
            "Track external demand",
            "Observe deterministic external demand separately from requested and actual generator output.",
            new ChallengeConditionDefinition("active", "Active", "Authored activation evidence."),
            new[] { new ChallengeConditionDefinition("observe", "Observe", "Observe the demand evidence.") },
            new[] { new ChallengeConditionDefinition("complete", "Complete", "Authored completion evidence.") },
            null,
            new ChallengeLogicalTimeContract(),
            new ChallengeAssistancePolicy(new[] { TrainingGuidanceMode.Hidden }, "unscored-m10962-contract"),
            profile);

    private static ChallengeLifecycleSnapshot Lifecycle(ChallengeDefinition challenge, long logicalStep, long? activatedLogicalStep)
        => new(
            challenge.ExactId,
            activatedLogicalStep.HasValue ? ChallengeLifecycleState.Active : ChallengeLifecycleState.Ready,
            logicalStep,
            activatedLogicalStep,
            null,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            Array.Empty<ChallengeLifecycleTransition>());

    private static ControlRoomSnapshot Snapshot(long logicalStep, double requestedMegawatts, double actualMegawatts)
    {
        var generator = new GeneratorPresentationSnapshot(
            "generator-1",
            "rotor-1",
            "breaker-1",
            Value(50d, "Hz"),
            Value(actualMegawatts, "MWe"),
            Value(15d, "kV"),
            Value(15d, "kV"),
            Value(0d, "deg"),
            Value(5d, "MW"),
            Value(0.1d, "MW"),
            true,
            true,
            true,
            false)
        {
            RequestedElectricalPower = Value(requestedMegawatts, "MWe"),
        };
        var electrical = new ElectricalPanelSnapshot(
            ElectricalGridPresentationSnapshot.Unavailable,
            new[] { generator },
            Value(actualMegawatts, "MWe"),
            false);
        return new ControlRoomSnapshot(
            logicalStep,
            ControlRoomRunState.Running,
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            electrical: electrical);
    }

    private static ControlRoomValueSnapshot Value(double value, string unit)
        => new(
            value.ToString("0.###", CultureInfo.InvariantCulture),
            unit,
            value,
            ControlRoomVisualState.Normal);

    private static string Fingerprint(ExternalEnergyDemandEvidenceSnapshot evidence)
        => string.Join(
            "|",
            evidence.ProfileExactId ?? "-",
            evidence.LogicalStep.ToString(CultureInfo.InvariantCulture),
            evidence.ProfileOffsetLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-",
            evidence.ExternalDemandMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-",
            evidence.RequestedGeneratorLoadMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-",
            evidence.ActualElectricalOutputMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-",
            evidence.DemandOutputErrorMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-",
            evidence.NextScheduledChangeLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-",
            evidence.NextScheduledDemandMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-");

    private static void Close(double expected, double? actual)
    {
        Assert.True(actual.HasValue);
        Assert.InRange(Math.Abs(actual.Value - expected), 0d, 1e-10);
    }

    private static void Close(double expected, double actual)
        => Assert.InRange(Math.Abs(actual - expected), 0d, 1e-10);

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.6.2 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1096-external-energy-demand");
    }
}
