using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10975MissionPerformanceClosureCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.7.5", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Mission/Performance Closure", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 1", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.7.4 Hotfix 1 VALIDATED", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no active mission", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("active mission without external demand", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bounded demand-following", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("completed and failed mission", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required generator-trip evidence", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unexpected-trip failure", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plant logical time continues", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assistance-mode changes", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requested/effective control-authority divergence", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("F1-F8", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("F9 remains absent", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no plant-command authority", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.6 owner", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("sha256-control-room-snapshot-v1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("archive schema v1", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("run-m1097-mission-performance-closure-audit.cmd", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("removes all CALL :label batch subroutines", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md", descriptor.Status, StringComparison.Ordinal);
    }
}
