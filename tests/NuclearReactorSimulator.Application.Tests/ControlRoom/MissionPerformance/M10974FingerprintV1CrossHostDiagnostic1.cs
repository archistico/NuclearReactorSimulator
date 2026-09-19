using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

internal static class M10974FingerprintV1CrossHostDiagnostic1
{
    internal const string EnvironmentVariable = "NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private static readonly JsonSerializerOptions SummarySerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static void TryWrite(ControlRoomSnapshot snapshot, string expectedFingerprint, string actualFingerprint)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(expectedFingerprint);
        ArgumentNullException.ThrowIfNull(actualFingerprint);

        var configuredDirectory = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return;
        }

        var outputDirectory = Path.GetFullPath(configuredDirectory);
        Directory.CreateDirectory(outputDirectory);

        var payload = ControlRoomSnapshotFingerprint.SerializeCanonicalPayload(snapshot);
        var payloadHash = Sha256(payload);
        var payloadText = Utf8NoBom.GetString(payload);

        using var document = JsonDocument.Parse(payload);
        var nodeRows = new List<NodeRow>();
        CollectNodes(document.RootElement, string.Empty, nodeRows);
        nodeRows.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));

        var topLevelRows = document.RootElement
            .EnumerateObject()
            .Select(static property => new TopLevelRow(
                "/" + EscapePointer(property.Name),
                property.Value.ValueKind.ToString(),
                Sha256(Utf8NoBom.GetBytes(property.Value.GetRawText()))))
            .OrderBy(static row => row.Path, StringComparer.Ordinal)
            .ToArray();

        var summary = new
        {
            schema = "m10974-fingerprint-v1-cross-host-diagnostic1",
            algorithmId = ControlRoomSnapshotFingerprint.AlgorithmId,
            expectedFingerprint,
            actualFingerprint,
            payloadSha256 = payloadHash,
            payloadHashMatchesActualFingerprint = string.Equals(payloadHash, actualFingerprint, StringComparison.Ordinal),
            logicalStep = snapshot.LogicalStep,
            runtime = new
            {
                framework = RuntimeInformation.FrameworkDescription,
                os = RuntimeInformation.OSDescription,
                processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                processorCount = Environment.ProcessorCount,
                currentCulture = CultureInfo.CurrentCulture.Name,
                currentUICulture = CultureInfo.CurrentUICulture.Name,
                is64BitProcess = Environment.Is64BitProcess,
            },
            counts = new
            {
                topLevelPropertyCount = topLevelRows.Length,
                jsonNodeCount = nodeRows.Count,
                scalarLeafCount = nodeRows.Count(static row => row.ScalarRawValue is not null),
            },
            topLevel = topLevelRows,
        };

        var summaryJson = JsonSerializer.Serialize(summary, SummarySerializerOptions);
        var summaryText = BuildSummaryText(
            expectedFingerprint,
            actualFingerprint,
            payloadHash,
            topLevelRows,
            nodeRows.Count,
            nodeRows.Count(static row => row.ScalarRawValue is not null));
        var nodesTsv = BuildNodesTsv(nodeRows);
        var topLevelTsv = BuildTopLevelTsv(topLevelRows);

        var zipPath = Path.Combine(outputDirectory, "fingerprint-v1-cross-host-diagnostic.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "summary.json", summaryJson);
            WriteEntry(archive, "summary.txt", summaryText);
            WriteEntry(archive, "normalized-control-room-snapshot-v1.json", payloadText);
            WriteEntry(archive, "top-level.tsv", topLevelTsv);
            WriteEntry(archive, "nodes.tsv", nodesTsv);
        }

        File.WriteAllText(
            Path.Combine(outputDirectory, "fingerprint-v1-cross-host-summary.txt"),
            summaryText,
            Utf8NoBom);
    }

    private static string BuildSummaryText(
        string expectedFingerprint,
        string actualFingerprint,
        string payloadHash,
        IReadOnlyList<TopLevelRow> topLevelRows,
        int nodeCount,
        int scalarLeafCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("schema=m10974-fingerprint-v1-cross-host-diagnostic1");
        builder.AppendLine("algorithm-id=" + ControlRoomSnapshotFingerprint.AlgorithmId);
        builder.AppendLine("expected-fingerprint=" + expectedFingerprint);
        builder.AppendLine("actual-fingerprint=" + actualFingerprint);
        builder.AppendLine("payload-sha256=" + payloadHash);
        builder.AppendLine("payload-hash-matches-actual=" + string.Equals(payloadHash, actualFingerprint, StringComparison.Ordinal));
        builder.AppendLine("framework=" + RuntimeInformation.FrameworkDescription);
        builder.AppendLine("os=" + RuntimeInformation.OSDescription);
        builder.AppendLine("process-architecture=" + RuntimeInformation.ProcessArchitecture);
        builder.AppendLine("os-architecture=" + RuntimeInformation.OSArchitecture);
        builder.AppendLine("processor-count=" + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("current-culture=" + CultureInfo.CurrentCulture.Name);
        builder.AppendLine("current-ui-culture=" + CultureInfo.CurrentUICulture.Name);
        builder.AppendLine("node-count=" + nodeCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("scalar-leaf-count=" + scalarLeafCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("top-level:");
        foreach (var row in topLevelRows)
        {
            builder.AppendLine(row.Path + "\t" + row.Kind + "\t" + row.Sha256);
        }

        return builder.ToString();
    }

    private static string BuildTopLevelTsv(IReadOnlyList<TopLevelRow> rows)
    {
        var builder = new StringBuilder("path\tkind\tsha256\n");
        foreach (var row in rows)
        {
            builder.Append(row.Path).Append('\t').Append(row.Kind).Append('\t').Append(row.Sha256).Append('\n');
        }

        return builder.ToString();
    }

    private static string BuildNodesTsv(IReadOnlyList<NodeRow> rows)
    {
        var builder = new StringBuilder("path\tkind\tsha256\tscalarRawValue\n");
        foreach (var row in rows)
        {
            builder
                .Append(row.Path)
                .Append('\t')
                .Append(row.Kind)
                .Append('\t')
                .Append(row.Sha256)
                .Append('\t')
                .Append(row.ScalarRawValue ?? string.Empty)
                .Append('\n');
        }

        return builder.ToString();
    }

    private static void CollectNodes(JsonElement element, string path, ICollection<NodeRow> rows)
    {
        var effectivePath = string.IsNullOrEmpty(path) ? "/" : path;
        var raw = element.GetRawText();
        var scalar = element.ValueKind is JsonValueKind.String
            or JsonValueKind.Number
            or JsonValueKind.True
            or JsonValueKind.False
            or JsonValueKind.Null
            ? raw
            : null;

        rows.Add(new NodeRow(
            effectivePath,
            element.ValueKind.ToString(),
            Sha256(Utf8NoBom.GetBytes(raw)),
            scalar));

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CollectNodes(
                        property.Value,
                        effectivePath == "/"
                            ? "/" + EscapePointer(property.Name)
                            : effectivePath + "/" + EscapePointer(property.Name),
                        rows);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    CollectNodes(
                        item,
                        effectivePath == "/"
                            ? "/" + index.ToString(CultureInfo.InvariantCulture)
                            : effectivePath + "/" + index.ToString(CultureInfo.InvariantCulture),
                        rows);
                    index++;
                }
                break;
        }
    }

    private static string EscapePointer(string value)
        => value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static string Sha256(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Utf8NoBom, 1024, leaveOpen: false);
        writer.Write(content);
    }

    private sealed record TopLevelRow(string Path, string Kind, string Sha256);

    private sealed record NodeRow(string Path, string Kind, string Sha256, string? ScalarRawValue);
}
