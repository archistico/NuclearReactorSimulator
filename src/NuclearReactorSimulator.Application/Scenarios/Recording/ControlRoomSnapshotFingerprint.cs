using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// Versioned deterministic presentation-state fingerprint. Host run/pause state is normalized because it changes only
/// execution/publication orchestration, not deterministic plant evolution.
/// </summary>
public static class ControlRoomSnapshotFingerprint
{
    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-DETERMINISM-V2
    public const string AlgorithmId = "sha256-control-room-snapshot-v1";
    public const string CrossHostAlgorithmId = "sha256-control-room-snapshot-v2-presentation-canonical";

    private static readonly byte[] NumericValueMarker = System.Text.Encoding.UTF8.GetBytes("\"numericValue\":");
    private static readonly byte[] NullToken = System.Text.Encoding.UTF8.GetBytes("null");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string Compute(ControlRoomSnapshot snapshot)
    {
        var payload = SerializeCanonicalPayload(snapshot);
        return Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    }

    public static string ComputeCrossHostV2(ControlRoomSnapshot snapshot)
    {
        var payload = SerializeCrossHostCanonicalPayloadV2(snapshot);
        return Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    }

    internal static string ComputeCrossHostV2FromCanonicalV1Payload(byte[] canonicalV1Payload)
    {
        ArgumentNullException.ThrowIfNull(canonicalV1Payload);
        var payload = CanonicalizeV1PayloadForCrossHostV2(canonicalV1Payload);
        return Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    }

    internal static byte[] SerializeCrossHostCanonicalPayloadV2(ControlRoomSnapshot snapshot)
        => CanonicalizeV1PayloadForCrossHostV2(SerializeCanonicalPayload(snapshot));

    internal static byte[] SerializeCanonicalPayload(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalized = new ControlRoomSnapshot(
            snapshot.LogicalStep,
            ControlRoomRunState.Paused,
            snapshot.TotalMeasuredSignalCount,
            snapshot.InvalidMeasuredSignalCount,
            snapshot.AnnunciatedAlarmCount,
            snapshot.UnacknowledgedAlarmCount,
            snapshot.ReactorScramActive,
            snapshot.TurbineTripActive,
            snapshot.GeneratorTripActive,
            snapshot.ReactorCore,
            NormalizeLegacyV1PrimaryCircuit(snapshot.PrimaryCircuit),
            snapshot.TurbineSecondary,
            snapshot.Electrical,
            snapshot.AlarmEvents,
            snapshot.Faults);

        return JsonSerializer.SerializeToUtf8Bytes(normalized, SerializerOptions);
    }


    internal static byte[] CanonicalizeV1PayloadForCrossHostV2(byte[] canonicalV1Payload)
    {
        ArgumentNullException.ThrowIfNull(canonicalV1Payload);

        using var output = new MemoryStream(canonicalV1Payload.Length);
        var source = canonicalV1Payload.AsSpan();
        var offset = 0;

        while (offset < source.Length)
        {
            var relativeMarkerIndex = source[offset..].IndexOf(NumericValueMarker);
            if (relativeMarkerIndex < 0)
            {
                output.Write(source[offset..]);
                break;
            }

            var markerStart = offset + relativeMarkerIndex;
            var valueStart = markerStart + NumericValueMarker.Length;
            output.Write(source[offset..valueStart]);

            if (source[valueStart..].StartsWith(NullToken))
            {
                output.Write(NullToken);
                offset = valueStart + 4;
                continue;
            }

            if (valueStart >= source.Length ||
                (source[valueStart] != (byte)'-' &&
                 (source[valueStart] < (byte)'0' || source[valueStart] > (byte)'9')))
            {
                throw new InvalidDataException("Fingerprint V2 expected numericValue to contain a JSON number or null.");
            }

            var valueEnd = valueStart;
            while (valueEnd < source.Length && IsJsonNumberByte(source[valueEnd]))
            {
                valueEnd++;
            }

            output.WriteByte((byte)'0');
            offset = valueEnd;
        }

        return output.ToArray();
    }

    private static bool IsJsonNumberByte(byte value)
        => (value >= (byte)'0' && value <= (byte)'9')
           || value == (byte)'-'
           || value == (byte)'+'
           || value == (byte)'.'
           || value == (byte)'e'
           || value == (byte)'E';

    private static PrimaryCircuitPanelSnapshot NormalizeLegacyV1PrimaryCircuit(PrimaryCircuitPanelSnapshot primaryCircuit)
    {
        if (primaryCircuit.Loops.Count == 0)
        {
            return primaryCircuit;
        }

        var loops = primaryCircuit.Loops
            .Select(static loop => loop with
            {
                Branches = loop.Branches
                    .Select(static branch => branch with
                    {
                        VoidText = NormalizeLegacyV1BranchVoidText(branch.VoidText),
                    })
                    .ToArray(),
            })
            .ToArray();

        return primaryCircuit with { Loops = loops };
    }

    private static string NormalizeLegacyV1BranchVoidText(string voidText)
    {
        // Fingerprint v1 historically captured this one branch presentation leaf under it-IT, where the decimal
        // separator was a comma. The live presentation is now invariant-culture, but v1 must preserve its frozen
        // byte contract across hosts rather than silently redefining every dependent historical fingerprint.
        if (!voidText.StartsWith("Void ", StringComparison.Ordinal) ||
            !voidText.EndsWith('%') ||
            voidText.IndexOf('.') < 0)
        {
            return voidText;
        }

        return voidText.Replace('.', ',');
    }
}
