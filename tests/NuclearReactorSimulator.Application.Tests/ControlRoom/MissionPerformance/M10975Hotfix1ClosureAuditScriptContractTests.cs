using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10975Hotfix1ClosureAuditScriptContractTests
{
    [Fact]
    public void ClosureAudit_UsesDirectTestInvocationsAndNoBatchLabelSubroutines()
    {
        var scriptPath = Path.Combine(
            ResolveRepositoryRoot(),
            "scripts",
            "run-m1097-mission-performance-closure-audit.cmd");
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("call :run_application_class", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("call :run_app_class", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\n:run_application_class", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\n:run_app_class", script, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "--filter-class \"NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10975MissionPerformanceClosureContractTests\"",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "--filter-class \"NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests\"",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "--filter-class \"NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceArchiveRestoreTests\"",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "--filter-class \"NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceDrillDownUiTests\"",
            script,
            StringComparison.Ordinal);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.5 Hotfix 1 script audit.");
    }
}
