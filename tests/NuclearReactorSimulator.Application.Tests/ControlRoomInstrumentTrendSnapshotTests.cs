using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomInstrumentTrendSnapshotTests
{
    [Theory]
    [InlineData(10d, 12d, ControlRoomInstrumentTrendDirection.Rising, 1d)]
    [InlineData(12d, 10d, ControlRoomInstrumentTrendDirection.Falling, -1d)]
    [InlineData(10d, 10.000001d, ControlRoomInstrumentTrendDirection.Steady, 0.0000005d)]
    public void Between_UsesLogicalStepsOnly(
        double previous,
        double current,
        ControlRoomInstrumentTrendDirection expectedDirection,
        double expectedRate)
    {
        var trend = ControlRoomInstrumentTrendSnapshot.Between(
            previousLogicalStep: 10,
            previous,
            currentLogicalStep: 12,
            current,
            steadyTolerance: 0.000001d,
            unit: "MW");

        Assert.Equal(expectedDirection, trend.Direction);
        Assert.Equal(expectedRate, trend.RatePerLogicalStep!.Value, 7);
        Assert.Equal("MW", trend.Unit);
    }

    [Fact]
    public void Between_ReturnsUnavailableAcrossSameStepOrRuntimeReset()
    {
        var sameStep = ControlRoomInstrumentTrendSnapshot.Between(10, 1d, 10, 2d, 0d, "MW");
        var reset = ControlRoomInstrumentTrendSnapshot.Between(10, 1d, 0, 2d, 0d, "MW");

        Assert.Equal(ControlRoomInstrumentTrendDirection.Unavailable, sameStep.Direction);
        Assert.Equal(ControlRoomInstrumentTrendDirection.Unavailable, reset.Direction);
        Assert.Null(reset.RatePerLogicalStep);
    }

    [Fact]
    public void Between_RejectsInvalidInputsButNotAValidLogicalStepReset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlRoomInstrumentTrendSnapshot.Between(-1, 1d, 0, 2d, 0d, "MW"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlRoomInstrumentTrendSnapshot.Between(0, 1d, -1, 2d, 0d, "MW"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlRoomInstrumentTrendSnapshot.Between(0, 1d, 1, 2d, -1d, "MW"));
    }
}
