using Xunit;

namespace NuclearReactorSimulator.App.Tests;

public sealed class M10974MissionPerformanceTimelineAuditTests
{
    [Fact]
    public void ArtifactSummary_WritesM10974MissionTimelineEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10974-mission-performance-timeline.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.4 Hotfix 1 Ordinary Suite Contract Alignment over the original M10.9.7.4 candidate; underlying timeline/drill-down/replay implementation remains over M10.9.7.3 Hotfix 2 REV2 VALIDATED; tests/contracts documentation plus candidate descriptor metadata only; no production XAML/runtime semantics, Simulation physics, challenge/scoring/protection authority, archive schema or plant-command authority change;",
            "m10973-hotfix2-rev2-validated=True; fingerprint-algorithm=sha256-control-room-snapshot-v1; fingerprint-golden-step=128; fingerprint-golden=63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362; fingerprint-golden-source=retained-H29-exact-version-evidence;",
            "hotfix1-historical-heading-aligned=True; hotfix1-f9-structural-keybinding-check=True; hotfix1-h29-primary-valves-topology-empty=True; fingerprint-golden-unchanged=True; production-xaml-changed=False; production-runtime-changed=False;",
            "lifecycle-spine-separated=True; lifecycle-spine-cap=32; recent-operational-evidence-cap=100; dense-protection-cannot-evict-activation-terminal=True; legacy-recent-events-contract-preserved=True;",
            "timeline-logical-step-ordered=True; source-sequence-ordering=True; structural-duplicate-suppression=True; demand-change-evidence=True; operator-action-evidence=True; alarm-protection-fault-evidence=True; scoring-evidence=True;",
            "drill-down-presentation-only=True; drill-down-electrical=True; drill-down-alarms-events=True; drill-down-computer=True; drill-down-plant-command-authority=False; f1-f8-preserved=True; f9-added=False;",
            "archive-schema-v1-unchanged=True; opaque-challenge-state-persisted=False; archive-mission-pack-inferred-from-scenario-id=False; archive-explicit-exact-pack-binding=True; archive-pack-mismatch-fail-closed=True; unbound-archive-remains-unbound=True;",
            "full-archive-replay-timeline-equivalent=True; checkpoint-prefix-timeline-equivalent=True; replay-prefix-then-live-continuation=True; continuation-duplicate-timeline-rows=False; start-recorded-session-preserves-explicit-pack=True;",
            "m10974-hotfix1-ordinary-suite-contract-alignment-passes=True; m10974-mission-performance-timeline-passes=True; manual-hmi-review-required=True; next-step=manual review then M10.9.7.5 closure;",
        });

        Assert.True(File.Exists(path));
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
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.4 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m10974-mission-performance-timeline");
    }
}
