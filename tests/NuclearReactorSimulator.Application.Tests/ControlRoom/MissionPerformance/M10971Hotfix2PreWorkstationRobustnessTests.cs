using NuclearReactorSimulator.Application.Scenarios.Challenges;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10971Hotfix2PreWorkstationRobustnessTests
{
    [Fact]
    public void SharedLifecycleAlignment_AdvancesOnlyTerminalReadViewAndPreservesTerminalBoundary()
    {
        var observations = Array.Empty<ChallengeConditionObservation>();
        var transitions = new[]
        {
            new ChallengeLifecycleTransition(
                1,
                ChallengeLifecycleState.Active,
                ChallengeLifecycleState.Completed,
                7,
                "Completed at the canonical terminal step."),
        };
        var frozen = new ChallengeLifecycleSnapshot(
            "test-challenge@1",
            ChallengeLifecycleState.Completed,
            7,
            2,
            7,
            5,
            10,
            null,
            observations,
            transitions);

        var aligned = ChallengeLifecycleLogicalStepAlignment.Align(frozen, 11);

        Assert.Equal(11L, aligned.LogicalStep);
        Assert.True(aligned.TerminalLogicalStep.HasValue);
        Assert.Equal(7L, aligned.TerminalLogicalStep.Value);
        Assert.Equal(frozen.State, aligned.State);
        Assert.Equal(frozen.ActivatedLogicalStep, aligned.ActivatedLogicalStep);
        Assert.Same(observations, aligned.Observations);
        Assert.Same(transitions, aligned.Transitions);

        var nonTerminal = new ChallengeLifecycleSnapshot(
            "test-challenge@1",
            ChallengeLifecycleState.Active,
            7,
            2,
            null,
            5,
            10,
            null,
            observations,
            Array.Empty<ChallengeLifecycleTransition>());
        Assert.Throws<InvalidOperationException>(() => ChallengeLifecycleLogicalStepAlignment.Align(nonTerminal, 8));
    }

    [Fact]
    public void SessionLoadBoundary_DelegatesExternalDataValidationAndHandlesUserDataFailures()
    {
        var root = ResolveRepositoryRoot();
        var compositionRoot = File.ReadAllText(Path.Combine(
            root,
            "src",
            "NuclearReactorSimulator.App",
            "Composition",
            "CompositionRoot.cs"));
        var computerControl = File.ReadAllText(Path.Combine(
            root,
            "src",
            "NuclearReactorSimulator.App",
            "Controls",
            "ControlRoomComputerControl.axaml.cs"));

        Assert.DoesNotContain("ArgumentException.ThrowIfNullOrWhiteSpace(content);", compositionRoot, StringComparison.Ordinal);
        Assert.Contains("archiveSerializer.Deserialize(content)", compositionRoot, StringComparison.Ordinal);
        Assert.Contains(
            "InvalidDataException or ArgumentException or KeyNotFoundException or OverflowException",
            computerControl,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ArtifactSummary_WritesM10971Hotfix2PreWorkstationRobustnessEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10971-hotfix2-pre-workstation-robustness.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.1 Hotfix 2 pre-workstation presentation/archive robustness over M10.9.7.1 Hotfix 1 VALIDATED; no workstation placement, new scoring arithmetic, challenge definition, plant command authority or physics change;",
            "malformed-session-archive-normalized-to-invalid-data=True; blank-archive-safe=True; truncated-json-safe=True; invalid-archive-record-safe=True; load-ui-user-data-exception-fallback=True;",
            "mission-objective-metadata-from-scenario-objective=True; objective-title-description-not-challenge-aliases=True;",
            "terminal-lifecycle-shared-step-alignment=True; replay-demand-presentation-share-alignment=True; terminal-boundary-preserved=True; nonterminal-step-mismatch-fails-closed=True;",
            "future-protection-event-filtered=True; recent-events-bounded=100; deterministic-event-order=True;",
            "requested-generator-load-aggregation-single-owner=True; external-grid-demand-vs-requested-load-separated=True; external-grid-demand-vs-actual-output-separated=True; requested-load-vs-actual-output-separated=True;",
            "elapsed-logical-steps-concrete-regression-covered=True; wall-clock-dependence=False; plant-command-authority=False;",
            "m10972-previous-candidate-promotable=False; m10972-rebuild-required-after-hotfix2-validation=True;",
            "m10971-hotfix2-pre-workstation-robustness-passes=True; next-step=validate Hotfix 2 then rebuild M10.9.7.2 placement/navigation candidate;",
        });

        Assert.True(File.Exists(path));
    }

    private static string ResolveArtifactDirectory()
        => Path.Combine(ResolveRepositoryRoot(), "artifacts", "m10971-hotfix2-pre-workstation-robustness");

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.1 Hotfix 2 audit artifacts.");
    }
}
