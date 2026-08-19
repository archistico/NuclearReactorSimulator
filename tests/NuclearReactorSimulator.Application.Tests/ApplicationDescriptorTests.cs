using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesH29ProductionActivationCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.29", descriptor.Milestone);
        Assert.Contains("Production Activation Candidate", descriptor.Milestone);
        Assert.Contains("H.28 is user-validated", descriptor.Status);
        Assert.Contains("bounded-but-costly", descriptor.Status);
        Assert.Contains("H.24 Requalification 1 is user-validated", descriptor.Status);
        Assert.Contains("30,000-interval/four-profile", descriptor.Status);
        Assert.Contains("9,626/9,626 corrected commits", descriptor.Status);
        Assert.Contains("7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE", descriptor.Status);
        Assert.Contains("initial-condition v3", descriptor.Status);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("v2 ExplicitCommittedState as the authoritative default", descriptor.Status);
        Assert.Contains("rollback/kill reference", descriptor.Status);
        Assert.Contains("save/replay/checkpoint", descriptor.Status);
        Assert.Contains("H.30 remains the sole authority", descriptor.Status);
    }
}
