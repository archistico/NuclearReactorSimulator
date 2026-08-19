using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941H28Requalification2Candidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.28 Requalification 2", descriptor.Milestone);
        Assert.Contains("user-validated H.28.1-G", descriptor.Status);
        Assert.Contains("79.7023 ms", descriptor.Status);
        Assert.Contains("88.3812 ms", descriptor.Status);
        Assert.Contains("35 logical hydraulic evaluations", descriptor.Status);
        Assert.Contains("32 finite-difference probes", descriptor.Status);
        Assert.Contains("Jacobian dimension 32", descriptor.Status);
        Assert.Contains("changes no numerical runtime code", descriptor.Status);
        Assert.Contains("median wall ratio <= 8", descriptor.Status);
        Assert.Contains("p95 wall ratio <= 12", descriptor.Status);
        Assert.Contains("median allocation ratio <= 16", descriptor.Status);
        Assert.Contains("Standard factories remain ExplicitCommittedState", descriptor.Status);
        Assert.Contains("H.29 default activation remains blocked", descriptor.Status);
        Assert.Contains("H.24 long-horizon requalification", descriptor.Status);
    }
}
