using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests;

public sealed class InfrastructureAssemblyTests
{
    [Fact]
    public void AssemblyMarker_BelongsToInfrastructureAssembly()
    {
        Assert.Equal(
            "NuclearReactorSimulator.Infrastructure",
            typeof(global::NuclearReactorSimulator.Infrastructure.AssemblyMarker).Assembly.GetName().Name);
    }
}
