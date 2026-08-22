using NuclearReactorSimulator.App.Runtime;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Runtime;

public sealed class DesktopHostFailurePolicyTests
{
    [Fact]
    public void DeterministicStepPolicy_ContainsExpectedNumericalFailuresButNotUnknownProgrammingFailures()
    {
        Assert.True(DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(new InvalidOperationException("step")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(new ArithmeticException("numeric")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(new OverflowException("overflow")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(new Exception("unknown")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(new NullReferenceException("programming")));
    }

    [Fact]
    public void CommandBoundaryPolicy_ContainsExpectedCanonicalRejectionsButNotProgrammingFailures()
    {
        Assert.True(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new InvalidOperationException("runtime rejection")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new ArgumentException("target")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new ArgumentOutOfRangeException("command")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new KeyNotFoundException("target id")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new OverflowException("numeric")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new IOException("not command validation")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedCommandOperationFailure(new NullReferenceException("programming")));
    }

    [Fact]
    public void SessionBoundaryPolicies_AlignConstructionAndArchiveFailureFamilies()
    {
        Assert.True(DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure(new InvalidOperationException("construction")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure(new ArgumentException("definition")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure(new OverflowException("numeric")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure(new IOException("not a construction boundary")));

        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new InvalidDataException("archive")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new ArgumentException("archive argument")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new KeyNotFoundException("version")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new OverflowException("archive numeric")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new IOException("storage")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new UnauthorizedAccessException("storage")));
        Assert.True(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new NotSupportedException("provider")));
        Assert.False(DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(new NullReferenceException("programming")));
    }
}
