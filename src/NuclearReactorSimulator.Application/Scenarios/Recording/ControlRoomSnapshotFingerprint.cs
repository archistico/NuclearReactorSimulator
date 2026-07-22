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
            snapshot.PrimaryCircuit,
            snapshot.TurbineSecondary,
            snapshot.Electrical,
            snapshot.AlarmEvents,
            snapshot.Faults);

        var payload = JsonSerializer.SerializeToUtf8Bytes(normalized, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    }
}
