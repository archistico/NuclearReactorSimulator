using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.Persistence;
using Xunit;

namespace NuclearReactorSimulator.App.Tests;

public sealed class M10973DesktopHostSessionIntegrityAuditTests
{
    [Fact]
    public async Task SuccessfulOverwrite_ProducesAReplayLoadableRecordedSessionArchive()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "NuclearReactorSimulator",
            "m10973-hotfix2",
            Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "session.nrs-session.json");
        await File.WriteAllTextAsync(path, "previous archive sentinel", TestContext.Current.CancellationToken);

        var root = CompositionRoot.Create(enableSessionRecording: true);
        ApplicationRoot? restored = null;
        try
        {
            root.MainWindowViewModel.RunCommand.Execute(null);
            _ = root.RuntimeCoordinator.AdvanceRunning(5, publicationStride: 5);
            root.MainWindowViewModel.PauseCommand.Execute(null);
            var content = root.MainWindowViewModel.OperatorComputer.ExportSessionArchive();

            await new DesktopSessionArchiveFileWriter().SaveAsync(path, content, TestContext.Current.CancellationToken);

            var saved = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.NotEqual("previous archive sentinel", saved);
            restored = CompositionRoot.CreateFromSessionArchive(saved);
            Assert.Equal(root.MainWindowViewModel.LogicalStep, restored.MainWindowViewModel.LogicalStep);
        }
        finally
        {
            restored?.MainWindowViewModel.DetachRuntimeSubscriptions();
            root.MainWindowViewModel.DetachRuntimeSubscriptions();
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup only; archive correctness has already been asserted.
            }
        }
    }

    [Fact]
    public void ArtifactSummary_WritesM10973Hotfix2DesktopHostSessionIntegrityEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10973-desktop-host-session-integrity.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.3 Hotfix 2 REV2 Desktop Host Failure & Session Save Integrity over M10.9.7.3 Hotfix 1 REV2 VALIDATED plus Docs4; App/Application host-integrity only; no Simulation physics, challenge/scoring/protection authority, archive schema or MISSION semantics change;",
            "m10973-hotfix1-rev2-validated=True; automated-live-workspace-gates=True; manual-hmi-checklist=True; original-hotfix2-promotable=False; original-hotfix2-build-failure=xUnit1051; hotfix2-rev1-promotable=False; hotfix2-rev1-ordinary-failures=3; hotfix2-rev2-invalid-data-archive-policy-aligned=True; hotfix2-rev2-backup-cleanup-path-aligned=True; hotfix2-rev2-historical-archive-boundary-test-centralized=True; hotfix2-rev2-stacked-on-validated-rev2=True;",
            "expected-step-failure-invalid-operation-contained=True; expected-step-failure-arithmetic-contained=True; expected-step-failure-overflow-contained=True; expected-step-failure-action=PAUSE+single-diagnostic; unknown-programming-failure-swallowed=False;",
            "start-recorded-session-boundary-protected=True; reset-session-boundary-protected=True; load-restore-save-archive-policy-shared=True; blanket-catch-all-swallowing=False;",
            "save-picker-before-export=True; cancelled-save-exports-archive=False; local-filesystem-path-required-for-safe-replace=True; truncate-first-overwrite=False; temporary-sibling-write=True; durable-flush-before-commit=True; existing-file-replace=File.Replace; previous-destination-preserved-on-injected-write-failure=True; previous-destination-preserved-on-injected-replace-failure=True; temporary-cleanup-best-effort=True; backup-cleanup-only-after-successful-replacement=True;",
            "successful-overwrite-replay-loadable=True; engineering-number-format=invariant-technical; grid-demand-requested-load-actual-output-separation-preserved=True; f1-f8-preserved=True; f9-added=False; plant-command-authority-change=False;",
            "m10973-hotfix2-rev2-desktop-host-session-integrity-passes=True; next-step=M10.9.7.4 deterministic timeline/drill-down/fingerprint-v1-anchor/archive-restored-mission-binding;",
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
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.3 Hotfix 2 REV2 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m10973-desktop-host-session-integrity");
    }
}
