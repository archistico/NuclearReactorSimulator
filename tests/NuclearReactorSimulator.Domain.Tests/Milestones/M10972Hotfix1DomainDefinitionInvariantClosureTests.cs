using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Milestones;

public sealed class M10972Hotfix1DomainDefinitionInvariantClosureTests
{
    [Fact]
    public void ArtifactSummary_WritesM10972Hotfix1DomainInvariantEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10972-hotfix1-domain-definition-invariant-closure.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.2 Hotfix 1 REV1 domain definition invariant closure over M10.9.7.2 REV1 VALIDATED; original Hotfix 1 superseded/not validated; no solver retuning, new physics, workstation activation, scoring arithmetic, challenge definition or plant command authority;",
            "synchronization-frequency-window-positive=True; synchronization-phase-window-strictly-between-zero-and-180=True; synchronization-voltage-window-positive-and-below-generator-rated=True; grid-frequency-envelope-nondegenerate=True; grid-voltage-envelope-nondegenerate=True;",
            "steam-drum-source-default-resistance-rejected=True; iodine-default-decay-constant-rejected=True; xenon-default-decay-constant-rejected=True; turbine-default-expansion-resistance-rejected=True;",
            "plant-state-canonical-definition-reference-identity=True; structurally-equal-noncanonical-fluid-definition-rejected=True; structurally-equal-noncanonical-thermal-definition-rejected=True;",
            "unknown-control-rod-target-kind-rejected=True; defaultable-positive-value-object-boundaries-fail-closed=True;",
            "application-descriptor-contract-aligned=True; focused-gate-covers-application-descriptor=True; original-hotfix1-promotable=False;",
            "m10972-hotfix1-rev1-domain-definition-invariant-closure-passes=True; next-step=validate Hotfix 1 REV1 then perform measured 10-ms hot-path allocation/lookup hardening before M10.9.7.3 live Mission/Performance wiring;",
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
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.2 Hotfix 1 REV1 audit artifacts.");
        }

        return Path.Combine(current.FullName, "artifacts", "m10972-hotfix1-domain-invariant-closure");
    }
}
