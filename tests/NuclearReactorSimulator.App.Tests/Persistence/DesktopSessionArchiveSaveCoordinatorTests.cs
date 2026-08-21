using NuclearReactorSimulator.App.Persistence;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Persistence;

public sealed class DesktopSessionArchiveSaveCoordinatorTests
{
    [Fact]
    public async Task CancelledPicker_DoesNotExportArchive()
    {
        var fileSystem = new FakeFileSystem();
        var coordinator = new DesktopSessionArchiveSaveCoordinator(new DesktopSessionArchiveFileWriter(fileSystem));
        var exportCalls = 0;

        var result = await coordinator.SaveAsync(
            static () => Task.FromResult<DesktopSessionArchiveSaveTarget?>(null),
            () =>
            {
                exportCalls++;
                return "should not be serialized";
            },
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(0, exportCalls);
        Assert.Empty(fileSystem.OperationKinds);
    }

    [Fact]
    public async Task DestinationSelection_CompletesBeforeArchiveExportAndWrite()
    {
        var fileSystem = new FakeFileSystem();
        var coordinator = new DesktopSessionArchiveSaveCoordinator(new DesktopSessionArchiveFileWriter(fileSystem));
        var order = new List<string>();
        var target = new DesktopSessionArchiveSaveTarget(
            Path.GetFullPath(Path.Combine("test-output", "ordered.nrs-session.json")),
            "ordered.nrs-session.json");

        var result = await coordinator.SaveAsync(
            () =>
            {
                order.Add("picker");
                return Task.FromResult<DesktopSessionArchiveSaveTarget?>(target);
            },
            () =>
            {
                order.Add("export");
                return "archive";
            },
            TestContext.Current.CancellationToken);
        order.AddRange(fileSystem.OperationKinds);

        Assert.Same(target, result);
        Assert.Equal(new[] { "picker", "export", "write", "move", "cleanup" }, order);
    }

    [Fact]
    public void ProviderWithoutLocalFilesystemPath_FailsClosedBeforeExportContractCanBegin()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => DesktopSessionArchiveSaveCoordinator.RequireLocalTarget(null, "session.nrs-session.json"));

        Assert.Contains("local filesystem path", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FakeFileSystem : IDesktopSessionArchiveFileSystem
    {
        private readonly HashSet<string> _files = new(StringComparer.Ordinal);
        public List<string> OperationKinds { get; } = new();

        public bool FileExists(string path) => _files.Contains(Path.GetFullPath(path));

        public Task WriteTextDurablyAsync(string path, string content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OperationKinds.Add("write");
            _files.Add(Path.GetFullPath(path));
            return Task.CompletedTask;
        }

        public void ReplaceExistingFile(string sourcePath, string destinationPath, string backupPath)
        {
            OperationKinds.Add("replace");
            _files.Remove(Path.GetFullPath(sourcePath));
            _files.Add(Path.GetFullPath(destinationPath));
            _files.Add(Path.GetFullPath(backupPath));
        }

        public void MoveNewFile(string sourcePath, string destinationPath)
        {
            OperationKinds.Add("move");
            _files.Remove(Path.GetFullPath(sourcePath));
            _files.Add(Path.GetFullPath(destinationPath));
        }

        public void TryDeleteFile(string path)
        {
            OperationKinds.Add("cleanup");
            _files.Remove(Path.GetFullPath(path));
        }
    }
}
