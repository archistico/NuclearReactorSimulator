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
    public const string AlgorithmId = "sha256-control-room-snapshot-v1";

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
