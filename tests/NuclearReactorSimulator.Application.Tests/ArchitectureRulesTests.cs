using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ArchitectureRulesTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["NuclearReactorSimulator.Domain"] = [],
            ["NuclearReactorSimulator.Simulation"] = ["NuclearReactorSimulator.Domain"],
            ["NuclearReactorSimulator.Application"] =
                ["NuclearReactorSimulator.Domain", "NuclearReactorSimulator.Simulation"],
            ["NuclearReactorSimulator.Infrastructure"] =
                ["NuclearReactorSimulator.Application", "NuclearReactorSimulator.Domain"],
            ["NuclearReactorSimulator.App"] =
                ["NuclearReactorSimulator.Application", "NuclearReactorSimulator.Infrastructure"],
        };

    [Fact]
    public void ProductionProjects_HaveOnlyApprovedProjectReferences()
    {
        var repositoryRoot = FindRepositoryRoot();

        foreach (var (projectName, allowedReferences) in AllowedProjectReferences)
        {
            var projectFile = GetProjectFile(repositoryRoot, projectName);
            var actualReferences = ReadProjectReferences(projectFile)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var expectedReferences = allowedReferences
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedReferences, actualReferences);
        }
    }

    [Fact]
    public void AvaloniaPackages_AreReferencedOnlyByAppProject()
    {
        var repositoryRoot = FindRepositoryRoot();

        foreach (var projectName in AllowedProjectReferences.Keys)
        {
            var projectFile = GetProjectFile(repositoryRoot, projectName);
            var avaloniaPackages = ReadPackageReferences(projectFile)
                .Where(package => package.StartsWith("Avalonia", StringComparison.Ordinal))
                .ToArray();

            if (projectName == "NuclearReactorSimulator.App")
            {
                Assert.NotEmpty(avaloniaPackages);
            }
            else
            {
                Assert.Empty(avaloniaPackages);
            }
        }
    }



    [Fact]
    public void AvaloniaApp_DoesNotReferenceSimulationNamespaces()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "NuclearReactorSimulator.App");

        var sourceFiles = Directory
            .EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path));

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);

            Assert.False(
                content.Contains("NuclearReactorSimulator.Simulation", StringComparison.Ordinal),
                $"Avalonia source must not reference Simulation namespaces directly: {sourceFile}.");
        }
    }

    [Fact]
    public void ControlRoomPresentationHistory_DoesNotUseWallClockApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var controlRoomRoot = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "NuclearReactorSimulator.Application",
            "ControlRoom");

        var forbiddenTokens = new[]
        {
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTimeOffset.Now",
            "DateTimeOffset.UtcNow",
            "Stopwatch",
        };

        foreach (var sourceFile in Directory.EnumerateFiles(controlRoomRoot, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(sourceFile);
            foreach (var forbiddenToken in forbiddenTokens)
            {
                Assert.False(
                    content.Contains(forbiddenToken, StringComparison.Ordinal),
                    $"Control-room presentation history must remain logical-step based; forbidden token '{forbiddenToken}' found in {sourceFile}.");
            }
        }
    }


    [Fact]
    public void FaultScheduling_DoesNotUseWallClockOrRandomApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var faultRoot = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "NuclearReactorSimulator.Application",
            "Scenarios",
            "Faults");

        var forbiddenTokens = new[]
        {
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTimeOffset.Now",
            "DateTimeOffset.UtcNow",
            "Stopwatch",
            "Random(",
            "Random.Shared",
            "Guid.NewGuid",
        };

        foreach (var sourceFile in Directory.EnumerateFiles(faultRoot, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(sourceFile);
            foreach (var forbiddenToken in forbiddenTokens)
            {
                Assert.False(
                    content.Contains(forbiddenToken, StringComparison.Ordinal),
                    $"M8.1 fault scheduling must remain deterministic; forbidden token '{forbiddenToken}' found in {sourceFile}.");
            }
        }
    }

    [Fact]
    public void SimulationProject_DoesNotUseWallClockTimerOrDelayApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var simulationRoot = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "NuclearReactorSimulator.Simulation");

        var forbiddenTokens = new[]
        {
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTimeOffset.Now",
            "DateTimeOffset.UtcNow",
            "Stopwatch",
            "Task.Delay",
            "Thread.Sleep",
            "PeriodicTimer",
            "System.Threading.Timer",
        };

        var sourceFiles = Directory
            .EnumerateFiles(simulationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path));

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);

            foreach (var forbiddenToken in forbiddenTokens)
            {
                Assert.False(
                    content.Contains(forbiddenToken, StringComparison.Ordinal),
                    $"Forbidden timing API token '{forbiddenToken}' found in {sourceFile}.");
            }
        }
    }

    private static FileInfo GetProjectFile(DirectoryInfo repositoryRoot, string projectName)
    {
        return new FileInfo(Path.Combine(
            repositoryRoot.FullName,
            "src",
            projectName,
            $"{projectName}.csproj"));
    }

    private static string[] ReadProjectReferences(FileInfo projectFile)
    {
        var document = XDocument.Load(projectFile.FullName);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFileNameWithoutExtension(include)
                ?? throw new InvalidDataException($"Could not determine the project name from reference '{include}'."))
            .ToArray();
    }

    private static string[] ReadPackageReferences(FileInfo projectFile)
    {
        var document = XDocument.Load(projectFile.FullName);

        return document
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToArray();
    }


    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the NuclearReactorSimulator repository root.");
    }
}
