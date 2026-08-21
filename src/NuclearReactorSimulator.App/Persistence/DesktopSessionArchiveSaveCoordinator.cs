namespace NuclearReactorSimulator.App.Persistence;

/// <summary>
/// Orders the desktop save workflow so destination selection always happens before full-session serialization.
/// </summary>
internal sealed class DesktopSessionArchiveSaveCoordinator
{
    private readonly DesktopSessionArchiveFileWriter _fileWriter;

    public DesktopSessionArchiveSaveCoordinator()
        : this(new DesktopSessionArchiveFileWriter())
    {
    }

    internal DesktopSessionArchiveSaveCoordinator(DesktopSessionArchiveFileWriter fileWriter)
        => _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));

    public async Task<DesktopSessionArchiveSaveTarget?> SaveAsync(
        Func<Task<DesktopSessionArchiveSaveTarget?>> chooseTargetAsync,
        Func<string> exportArchive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chooseTargetAsync);
        ArgumentNullException.ThrowIfNull(exportArchive);

        var target = await chooseTargetAsync().ConfigureAwait(true);
        if (target is null)
        {
            return null;
        }

        var content = exportArchive();
        await _fileWriter.SaveAsync(target.LocalPath, content, cancellationToken).ConfigureAwait(true);
        return target;
    }

    public static DesktopSessionArchiveSaveTarget RequireLocalTarget(string? localPath, string displayName)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new NotSupportedException(
                "The selected storage provider does not expose a local filesystem path required for non-destructive session replacement.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new DesktopSessionArchiveSaveTarget(Path.GetFullPath(localPath), displayName);
    }
}

internal sealed record DesktopSessionArchiveSaveTarget(string LocalPath, string DisplayName);
