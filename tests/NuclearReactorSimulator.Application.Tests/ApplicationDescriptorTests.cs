using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesH30PhaseHClosureDecisionCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.30", descriptor.Milestone);
        Assert.Contains("Phase H Closure", descriptor.Milestone);
        Assert.Contains("Production Qualification Decision", descriptor.Milestone);
        Assert.Contains("H.29 is user-validated", descriptor.Status);
        Assert.Contains("400/400 qualified commits", descriptor.Status);
        Assert.Contains("zero rollback/fallback/unsafe/untargeted disagreement", descriptor.Status);
        Assert.Contains("exact replay/checkpoint compatibility", descriptor.Status);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status);
        Assert.Contains("OPT-IN ONLY", descriptor.Status);
        Assert.Contains("bounded-but-costly", descriptor.Status);
        Assert.Contains("4.6214685710690242", descriptor.Status);
        Assert.Contains("10.684444741413872", descriptor.Status);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("closes Phase H", descriptor.Status);
        Assert.Contains("unblocks Phase I", descriptor.Status);
    }
}
