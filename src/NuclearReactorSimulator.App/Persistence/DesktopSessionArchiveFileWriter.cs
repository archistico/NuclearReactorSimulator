using System.Text;

namespace NuclearReactorSimulator.App.Persistence;

/// <summary>
/// Writes a complete session archive to a temporary sibling and commits it only after the temporary file has been
/// fully written and flushed. Existing destinations are replaced with File.Replace so truncate-first overwrite is
/// never used by the desktop host.
/// </summary>
internal sealed class DesktopSessionArchiveFileWriter
{
    private readonly IDesktopSessionArchiveFileSystem _fileSystem;

    public DesktopSessionArchiveFileWriter()
        : this(new PhysicalDesktopSessionArchiveFileSystem())
    {
    }

    internal DesktopSessionArchiveFileWriter(IDesktopSessionArchiveFileSystem fileSystem)
        => _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    public async Task SaveAsync(string destinationPath, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(content);

        var fullDestinationPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullDestinationPath);
        var fileName = Path.GetFileName(fullDestinationPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A session archive destination must identify a local filesystem file.", nameof(destinationPath));
        }

        var token = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        var temporaryPath = Path.Combine(directory, $".{fileName}.{token}.nrs-tmp");
        var backupPath = Path.Combine(directory, $".{fileName}.{token}.nrs-bak");
        var replacementCommitted = false;

        try
        {
            await _fileSystem.WriteTextDurablyAsync(temporaryPath, content, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (_fileSystem.FileExists(fullDestinationPath))
            {
                _fileSystem.ReplaceExistingFile(temporaryPath, fullDestinationPath, backupPath);
                replacementCommitted = true;
            }
            else
            {
                _fileSystem.MoveNewFile(temporaryPath, fullDestinationPath);
            }

        }
        finally
        {
            _fileSystem.TryDeleteFile(temporaryPath);
            if (replacementCommitted)
            {
                _fileSystem.TryDeleteFile(backupPath);
            }
        }
    }
}

internal interface IDesktopSessionArchiveFileSystem
{
    bool FileExists(string path);

    Task WriteTextDurablyAsync(string path, string content, CancellationToken cancellationToken);

    void ReplaceExistingFile(string sourcePath, string destinationPath, string backupPath);

    void MoveNewFile(string sourcePath, string destinationPath);

    void TryDeleteFile(string path);
}

internal sealed class PhysicalDesktopSessionArchiveFileSystem : IDesktopSessionArchiveFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public async Task WriteTextDurablyAsync(string path, string content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);
        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 16 * 1024,
            leaveOpen: true);

        await writer.WriteAsync(content).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        stream.Flush(flushToDisk: true);
    }

    public void ReplaceExistingFile(string sourcePath, string destinationPath, string backupPath)
        => File.Replace(sourcePath, destinationPath, backupPath, ignoreMetadataErrors: true);

    public void MoveNewFile(string sourcePath, string destinationPath)
        => File.Move(sourcePath, destinationPath);

    public void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Cleanup is explicitly best-effort. Never convert an already successful commit into a reported save failure.
        }
    }
}
