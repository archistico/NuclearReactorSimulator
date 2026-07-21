using Xunit;

namespace NuclearReactorSimulator.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void AssemblyMarker_BelongsToDomainAssembly()
    {
        Assert.Equal(
            "NuclearReactorSimulator.Domain",
            typeof(global::NuclearReactorSimulator.Domain.AssemblyMarker).Assembly.GetName().Name);
    }
}
