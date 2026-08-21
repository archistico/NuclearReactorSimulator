using NuclearReactorSimulator.App.Persistence;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Persistence;

public sealed class DesktopSessionArchiveFileWriterTests
{
    [Fact]
    public async Task ExistingDestination_IsReplacedOnlyAfterTemporaryWriteAndCleanup()
    {
        var fileSystem = new FakeFileSystem();
        var destination = Path.GetFullPath(Path.Combine("test-output", "session.nrs-session.json"));
        fileSystem.SetFile(destination, "old archive");
        var writer = new DesktopSessionArchiveFileWriter(fileSystem);

        await writer.SaveAsync(destination, "new archive", TestContext.Current.CancellationToken);

        Assert.Equal("new archive", fileSystem.ReadFile(destination));
        Assert.Equal(new[] { "write", "replace", "cleanup", "cleanup" }, fileSystem.OperationKinds);
        Assert.DoesNotContain(fileSystem.Files.Keys, static path => path.EndsWith(".nrs-tmp", StringComparison.Ordinal));
        Assert.DoesNotContain(fileSystem.Files.Keys, static path => path.EndsWith(".nrs-bak", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InjectedWriteFailure_PreservesPreviousDestinationByteForByteAndAttemptsTemporaryCleanup()
    {
        var fileSystem = new FakeFileSystem { FailWrite = true };
        var destination = Path.GetFullPath(Path.Combine("test-output", "session.nrs-session.json"));
        fileSystem.SetFile(destination, "old archive byte-for-byte");
        var writer = new DesktopSessionArchiveFileWriter(fileSystem);

        var exception = await Assert.ThrowsAsync<IOException>(() => writer.SaveAsync(destination, "new archive", TestContext.Current.CancellationToken));

        Assert.Contains("injected write failure", exception.Message, StringComparison.Ordinal);
        Assert.Equal("old archive byte-for-byte", fileSystem.ReadFile(destination));
        Assert.Contains("cleanup", fileSystem.OperationKinds);
        Assert.DoesNotContain("replace", fileSystem.OperationKinds);
    }

    [Fact]
    public async Task InjectedReplaceFailure_PreservesPreviousDestinationByteForByteAndRemovesTemporaryFile()
    {
        var fileSystem = new FakeFileSystem { FailReplace = true };
        var destination = Path.GetFullPath(Path.Combine("test-output", "session.nrs-session.json"));
        fileSystem.SetFile(destination, "old archive byte-for-byte");
        var writer = new DesktopSessionArchiveFileWriter(fileSystem);

        var exception = await Assert.ThrowsAsync<IOException>(() => writer.SaveAsync(destination, "new archive", TestContext.Current.CancellationToken));

        Assert.Contains("injected replace failure", exception.Message, StringComparison.Ordinal);
        Assert.Equal("old archive byte-for-byte", fileSystem.ReadFile(destination));
        Assert.Contains("replace", fileSystem.OperationKinds);
        Assert.Contains("cleanup", fileSystem.OperationKinds);
        Assert.DoesNotContain(fileSystem.Files.Keys, static path => path.EndsWith(".nrs-tmp", StringComparison.Ordinal));
    }

    private sealed class FakeFileSystem : IDesktopSessionArchiveFileSystem
    {
        public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
        public List<string> OperationKinds { get; } = new();
        public bool FailWrite { get; init; }
        public bool FailReplace { get; init; }

        public void SetFile(string path, string content) => Files[Path.GetFullPath(path)] = content;

        public string ReadFile(string path) => Files[Path.GetFullPath(path)];

        public bool FileExists(string path) => Files.ContainsKey(Path.GetFullPath(path));

        public Task WriteTextDurablyAsync(string path, string content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OperationKinds.Add("write");
            if (FailWrite)
            {
                throw new IOException("injected write failure");
            }
            Files[Path.GetFullPath(path)] = content;
            return Task.CompletedTask;
        }

        public void ReplaceExistingFile(string sourcePath, string destinationPath, string backupPath)
        {
            OperationKinds.Add("replace");
            if (FailReplace)
            {
                throw new IOException("injected replace failure");
            }

            var source = Path.GetFullPath(sourcePath);
            var destination = Path.GetFullPath(destinationPath);
            var backup = Path.GetFullPath(backupPath);
            Files[backup] = Files[destination];
            Files[destination] = Files[source];
            Files.Remove(source);
        }

        public void MoveNewFile(string sourcePath, string destinationPath)
        {
            OperationKinds.Add("move");
            var source = Path.GetFullPath(sourcePath);
            var destination = Path.GetFullPath(destinationPath);
            Files[destination] = Files[source];
            Files.Remove(source);
        }

        public void TryDeleteFile(string path)
        {
            OperationKinds.Add("cleanup");
            Files.Remove(Path.GetFullPath(path));
        }
    }
}
