using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// R3 failure diagnostic only. Replays the exact conserved state reported by the returned
/// R3 mode-2 shadow failure and isolates the H.28.1-E same-instance fused branch-continuity path.
/// No production repair is performed by this test.
/// </summary>
public sealed class M10FinalVr2R3Mode2BranchContinuityFusionFailureDiagnostic1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_FUSION_DIAGNOSTIC1";
    private const double FailureSpecificVolume = 0.048580627845180926d;
    private const double FailureSpecificEnergy = 2525533.2846314958d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3Mode2BranchContinuityFusionFailureDiagnostic1")]
    public void FailureState_IsResolvedByMode2Directly_ButRejectedBySameInstanceFusedContinuityPath()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var definition = new FluidNodeDefinition(
            "turbine-inlet",
            Volume.FromCubicMetres(FailureSpecificVolume));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(1d),
            Energy.FromJoules(FailureSpecificEnergy));
        var previous = new FluidThermodynamicState(
            Pressure.FromMegapascals(3.9d),
            Temperature.FromKelvins(522d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.95d));

        var resolver = new ReferenceConsistentTabulatedInverseResolver();
        var resolverSuccess = resolver.TryResolveWithPath(
            FailureSpecificVolume,
            FailureSpecificEnergy,
            out var resolverState,
            out var resolverPath);

        var directModel = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        FluidThermodynamicState? directState = null;
        Exception? directException = null;
        try
        {
            directState = directModel.Resolve(definition, inventory, previous);
        }
        catch (Exception ex)
        {
            directException = ex;
        }

        WaterSteamInverseBranchSelectionDiagnostic? legacyDiagnostic = null;
        Exception? diagnosticException = null;
        try
        {
            legacyDiagnostic = directModel.DiagnoseInverseBranchSelection(definition, inventory, previous);
        }
        catch (Exception ex)
        {
            diagnosticException = ex;
        }

        var fused = new ThermodynamicBranchContinuityModel(
            directModel,
            directModel,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            new[] { "turbine-inlet" });
        FluidThermodynamicState? fusedState = null;
        Exception? fusedException = null;
        try
        {
            fusedState = fused.Resolve(definition, inventory, previous);
        }
        catch (Exception ex)
        {
            fusedException = ex;
        }

        var splitProduction = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var splitDiagnostic = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var split = new ThermodynamicBranchContinuityModel(
            splitProduction,
            splitDiagnostic,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            new[] { "turbine-inlet" });
        FluidThermodynamicState? splitState = null;
        Exception? splitException = null;
        try
        {
            splitState = split.Resolve(definition, inventory, previous);
        }
        catch (Exception ex)
        {
            splitException = ex;
        }

        var anyLegacyRoot = legacyDiagnostic?.Candidates.Any(static candidate => candidate.RootFound) ?? false;
        var splitEqualsDirect = directState is not null && splitState is not null && directState == splitState;

        WriteResolutionMatrix(
            resolverSuccess,
            resolverState,
            resolverPath,
            directState,
            directException,
            legacyDiagnostic,
            diagnosticException,
            fusedState,
            fusedException,
            splitState,
            splitException,
            anyLegacyRoot,
            splitEqualsDirect);

        WriteMechanismSummary(
            resolverSuccess,
            directState is not null && directException is null,
            fusedState is not null && fusedException is null,
            splitState is not null && splitException is null,
            anyLegacyRoot,
            splitEqualsDirect);

        Assert.True(resolverSuccess);
        Assert.Equal(ReferenceConsistentTabulatedInverseResolver.ResolutionPath.C2MixturePrefix, resolverPath);
        Assert.Equal(FluidPhase.SaturatedMixture, resolverState.Phase);

        Assert.Null(directException);
        Assert.NotNull(directState);
        Assert.Equal(resolverState.ToFluidThermodynamicState(), directState);

        Assert.Null(diagnosticException);
        Assert.NotNull(legacyDiagnostic);
        Assert.Equal("none", legacyDiagnostic.ProductionSelectedBranch);
        Assert.False(legacyDiagnostic.MultiplePhaseRootsAvailable);
        Assert.False(anyLegacyRoot);

        Assert.Null(fusedState);
        Assert.IsType<WaterSteamStateOutOfRangeException>(fusedException);

        Assert.Null(splitException);
        Assert.NotNull(splitState);
        Assert.Equal(directState, splitState);
        Assert.True(splitEqualsDirect);
    }

    private static void WriteResolutionMatrix(
        bool resolverSuccess,
        ReferenceConsistentTabulatedInverseResolver.ResolvedState resolverState,
        ReferenceConsistentTabulatedInverseResolver.ResolutionPath resolverPath,
        FluidThermodynamicState? directState,
        Exception? directException,
        WaterSteamInverseBranchSelectionDiagnostic? legacyDiagnostic,
        Exception? diagnosticException,
        FluidThermodynamicState? fusedState,
        Exception? fusedException,
        FluidThermodynamicState? splitState,
        Exception? splitException,
        bool anyLegacyRoot,
        bool splitEqualsDirect)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "01-failure-state-resolution-matrix.txt"),
            new[]
            {
                "status=DIAGNOSTIC-EVIDENCE-WRITTEN",
                "node-id=turbine-inlet",
                FormattableString.Invariant($"specific-volume-m3-kg={FailureSpecificVolume:R}"),
                FormattableString.Invariant($"specific-internal-energy-j-kg={FailureSpecificEnergy:R}"),
                $"resolver-success={resolverSuccess}",
                $"resolver-path={resolverPath}",
                $"resolver-phase={resolverState.Phase}",
                FormattableString.Invariant($"resolver-temperature-c={resolverState.TemperatureCelsius:R}"),
                FormattableString.Invariant($"resolver-pressure-mpa={resolverState.PressureMegapascals:R}"),
                FormattableString.Invariant($"resolver-quality={(resolverState.VaporQuality?.ToString("R", CultureInfo.InvariantCulture) ?? "null")}"),
                $"direct-mode2-success={directState is not null && directException is null}",
                $"direct-mode2-exception={(directException?.GetType().FullName ?? "none")}",
                $"direct-mode2-state={DescribeState(directState)}",
                $"legacy-diagnostic-success={legacyDiagnostic is not null && diagnosticException is null}",
                $"legacy-diagnostic-exception={(diagnosticException?.GetType().FullName ?? "none")}",
                $"legacy-diagnostic-selected-branch={(legacyDiagnostic?.ProductionSelectedBranch ?? "unavailable")}",
                $"legacy-diagnostic-multiple-roots={(legacyDiagnostic?.MultiplePhaseRootsAvailable.ToString() ?? "unavailable")}",
                $"legacy-diagnostic-any-root={anyLegacyRoot}",
                $"fused-same-instance-success={fusedState is not null && fusedException is null}",
                $"fused-same-instance-exception={(fusedException?.GetType().FullName ?? "none")}",
                $"split-distinct-instance-success={splitState is not null && splitException is null}",
                $"split-distinct-instance-exception={(splitException?.GetType().FullName ?? "none")}",
                $"split-equals-direct={splitEqualsDirect}",
                $"split-state={DescribeState(splitState)}",
            },
            Utf8WithoutBom);
    }

    private static void WriteMechanismSummary(
        bool resolverSuccess,
        bool directSuccess,
        bool fusedSuccess,
        bool splitSuccess,
        bool anyLegacyRoot,
        bool splitEqualsDirect)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "02-fusion-mechanism-summary.txt"),
            new[]
            {
                "status=DIAGNOSTIC-EVIDENCE-WRITTEN",
                $"resolver-mode2-success={resolverSuccess}",
                $"direct-mode2-success={directSuccess}",
                $"legacy-diagnostic-any-root={anyLegacyRoot}",
                $"same-instance-fused-success={fusedSuccess}",
                $"distinct-instance-nonfused-success={splitSuccess}",
                $"nonfused-equals-direct={splitEqualsDirect}",
                "hypothesis=H28.1-E-SAME-INSTANCE-FUSION-BYPASSES-MODE2-PRODUCTION-RESOLVE",
                "production-repair-applied=False",
                "r3-remains-blocking=True",
                "r4-planning-authorized=False",
            },
            Utf8WithoutBom);
    }

    private static string DescribeState(FluidThermodynamicState? state)
        => state is null
            ? "null"
            : FormattableString.Invariant(
                $"{state.Phase}|T={state.Temperature.Kelvins:R}K|P={state.Pressure.Pascals:R}Pa|q={(state.VaporQuality?.Fraction.ToString("R", CultureInfo.InvariantCulture) ?? "null")}");

    private static void RequireOptIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled R3 failure diagnostic runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1");

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the R3 diagnostic test output directory.");
    }
}
