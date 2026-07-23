using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomInstrumentScaleSnapshotTests
{
    [Fact]
    public void Constructor_PreservesIndependentScaleOperatingTargetSetpointAndProtectionSemantics()
    {
        var normal = new ControlRoomInstrumentBandSnapshot(45, 55, ControlRoomInstrumentBandKind.NormalOperating, "NORMAL");
        var warning = new ControlRoomInstrumentBandSnapshot(35, 45, ControlRoomInstrumentBandKind.Warning, "LOW WARNING");
        var target = new ControlRoomTargetBandSnapshot(48, 52, "SCENARIO TARGET");
        var protection = new ControlRoomProtectionLimitSnapshot(25, ControlRoomLimitDirection.Low, "LOW-LOW TRIP");

        var scale = new ControlRoomInstrumentScaleSnapshot(
            0,
            100,
            new[] { warning, normal },
            target,
            50,
            new[] { protection });

        Assert.Equal(0d, scale.Minimum);
        Assert.Equal(100d, scale.Maximum);
        Assert.Equal(2, scale.OperatingBands.Count);
        Assert.Same(target, scale.TargetBand);
        Assert.Equal(50d, scale.Setpoint);
        Assert.Single(scale.ProtectionLimits);
        Assert.Equal("LOW-LOW TRIP", scale.ProtectionLimits[0].Label);
    }

    [Fact]
    public void Constructor_RejectsSemanticMarkersOutsideInstrumentScale()
    {
        var outsideBand = new ControlRoomInstrumentBandSnapshot(-1, 10, ControlRoomInstrumentBandKind.Warning, "OUTSIDE");
        var outsideTarget = new ControlRoomTargetBandSnapshot(90, 110, "TARGET");
        var outsideProtection = new ControlRoomProtectionLimitSnapshot(101, ControlRoomLimitDirection.High, "TRIP");

        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRoomInstrumentScaleSnapshot(0, 100, new[] { outsideBand }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRoomInstrumentScaleSnapshot(0, 100, targetBand: outsideTarget));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRoomInstrumentScaleSnapshot(0, 100, protectionLimits: new[] { outsideProtection }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRoomInstrumentScaleSnapshot(0, 100, setpoint: 101));
    }
}
