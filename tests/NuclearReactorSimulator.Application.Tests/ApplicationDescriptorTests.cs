using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10973Hotfix2Rev2DesktopHostSessionIntegrityCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.7.3", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 2 REV2", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Desktop Host Failure & Session Save Integrity", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 1 REV2 is VALIDATED", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("xUnit1051", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Hotfix 2 REV1 is also SUPERSEDED / NOT VALIDATED", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("InvalidDataException", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("backup cleanup", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("centralized policy", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("manual HMI checklist", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("ArithmeticException", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("OverflowException", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("PAUSE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("unknown programming failures remain unhandled", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("selects a destination before archive export", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("local desktop filesystem path", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("temporary sibling", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no longer truncates", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("invariant technical decimal convention", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("F1-F8/no-F9", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("physics and archive schema remain unchanged", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.7.4", descriptor.Status, StringComparison.Ordinal);
    }
}
