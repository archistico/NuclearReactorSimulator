using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesH24PostH28LongHorizonRequalificationCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.24 Requalification 1", descriptor.Milestone);
        Assert.Contains("H.28 is user-validated", descriptor.Status);
        Assert.Contains("4.6215 <= 8", descriptor.Status);
        Assert.Contains("10.6844 <= 12", descriptor.Status);
        Assert.Contains("bounded-but-costly", descriptor.Status);
        Assert.Contains("379/379 soak trigger/commit", descriptor.Status);
        Assert.Contains("518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38", descriptor.Status);
        Assert.Contains("30,000-interval/four-profile H.24 committed domain", descriptor.Status);
        Assert.Contains("makes no numerical runtime change", descriptor.Status);
        Assert.Contains("Standard factories remain ExplicitCommittedState at 10 ms", descriptor.Status);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn remains separately opt-in", descriptor.Status);
        Assert.Contains("H.29 remains blocked", descriptor.Status);
    }
}
