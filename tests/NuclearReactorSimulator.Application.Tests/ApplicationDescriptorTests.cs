using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10972Hotfix3Rev1JsonDocumentExceptionTypeAlignmentCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.7.2 Hotfix 3 REV1", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("JsonDocument Parse Exception-Type Test Alignment", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.7.2 Hotfix 2 REV1 are validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("ControlRoomCommand.NumericValue persistence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("original Hotfix 3 is superseded/not validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("assignable JsonException check", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("incomplete manual-demand payloads", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("post-incident command persistence DTO-owned", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("malformed scenario/checkpoint/post-incident JSON", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Session archive schema v1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("numeric enum representation remain unchanged", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("string-enum schema migration and streaming persistence APIs remain separately deferred", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("UiRouteActivated=false", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("plant command authority remain unchanged", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.7.3 live Mission/Performance wiring remains blocked", descriptor.Status, StringComparison.Ordinal);
    }
}
