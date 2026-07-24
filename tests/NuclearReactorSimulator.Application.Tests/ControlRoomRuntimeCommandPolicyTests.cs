using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ControlRoomRuntimeCommandPolicyTests
{
    [Fact]
    public void Default_UsesDocumentedMomentarySpeedAndLoadIncrements()
    {
        var policy = ControlRoomRuntimeCommandPolicy.Default;

        Assert.Equal(10d, policy.TurbineSpeedSetpointIncrementRpm);
        Assert.Equal(5_000_000d, policy.GeneratorLoadSetpointIncrementWatts);
    }
}
