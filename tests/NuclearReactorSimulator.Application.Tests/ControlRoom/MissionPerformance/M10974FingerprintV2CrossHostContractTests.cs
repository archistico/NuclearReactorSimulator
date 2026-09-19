using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10974FingerprintV2CrossHostContractTests
{
    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-DETERMINISM-V2-FOCUSED
    [Fact]
    public void CrossHostV2_CanonicalizesOnlyPresentationNumericValuePrecision()
    {
        var left = Encoding.UTF8.GetBytes(
            """{"logicalStep":126,"reading":{"valueText":"5.602","unit":"MW","numericValue":5.602040801785741,"state":0},"unavailable":{"valueText":"—","unit":"","numericValue":null,"state":1},"other":17}""");
        var right = Encoding.UTF8.GetBytes(
            """{"logicalStep":126,"reading":{"valueText":"5.602","unit":"MW","numericValue":5.602040801785755,"state":0},"unavailable":{"valueText":"—","unit":"","numericValue":null,"state":1},"other":17}""");

        var leftCanonical = ControlRoomSnapshotFingerprint.CanonicalizeV1PayloadForCrossHostV2(left);
        var rightCanonical = ControlRoomSnapshotFingerprint.CanonicalizeV1PayloadForCrossHostV2(right);

        Assert.True(leftCanonical.AsSpan().SequenceEqual(rightCanonical));

        var canonicalText = Encoding.UTF8.GetString(leftCanonical);
        Assert.Contains("\"numericValue\":0", canonicalText);
        Assert.Contains("\"numericValue\":null", canonicalText);
        Assert.Contains("\"valueText\":\"5.602\"", canonicalText);
        Assert.Contains("\"other\":17", canonicalText);
    }

    [Fact]
    public void CrossHostV2_PresentationChangeStillChangesCanonicalPayload()
    {
        var baseline = Encoding.UTF8.GetBytes(
            """{"reading":{"valueText":"5.602","unit":"MW","numericValue":5.602040801785741,"state":0}}""");
        var changedPresentation = Encoding.UTF8.GetBytes(
            """{"reading":{"valueText":"5.603","unit":"MW","numericValue":5.602040801785741,"state":0}}""");

        var baselineCanonical = ControlRoomSnapshotFingerprint.CanonicalizeV1PayloadForCrossHostV2(baseline);
        var changedCanonical = ControlRoomSnapshotFingerprint.CanonicalizeV1PayloadForCrossHostV2(changedPresentation);

        Assert.False(baselineCanonical.AsSpan().SequenceEqual(changedCanonical));
        Assert.Equal(
            "sha256-control-room-snapshot-v2-presentation-canonical",
            ControlRoomSnapshotFingerprint.CrossHostAlgorithmId);
    }
}
