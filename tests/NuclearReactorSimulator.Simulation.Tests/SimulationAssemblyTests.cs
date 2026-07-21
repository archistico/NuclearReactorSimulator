using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests;

public sealed class SimulationAssemblyTests
{
    [Fact]
    public void SimulationAndDomain_AreSeparateAssemblies()
    {
        Assert.NotEqual(
            typeof(global::NuclearReactorSimulator.Domain.AssemblyMarker).Assembly,
            typeof(global::NuclearReactorSimulator.Simulation.AssemblyMarker).Assembly);
    }
}
