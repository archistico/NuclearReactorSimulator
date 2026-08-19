namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

internal static class FrozenLargeEvidenceManifest
{
    public static string CanonicalSha256(string repositoryRoot, string logicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalPath);

        var manifestPath = Path.Combine(repositoryRoot, "eng", "frozen-evidence", "large-payload-manifest.csv");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Compact large frozen-evidence manifest is missing.", manifestPath);
        }

        var normalizedLogicalPath = logicalPath.Replace('\\', '/');
        foreach (var line in File.ReadLines(manifestPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = line.Split(',');
            if (fields.Length != 4)
            {
                throw new InvalidDataException($"Unexpected large frozen-evidence manifest row: {line}");
            }

            if (string.Equals(fields[0], normalizedLogicalPath, StringComparison.Ordinal))
            {
                return fields[1];
            }
        }

        throw new KeyNotFoundException($"Large frozen-evidence manifest does not contain '{normalizedLogicalPath}'.");
    }
}
