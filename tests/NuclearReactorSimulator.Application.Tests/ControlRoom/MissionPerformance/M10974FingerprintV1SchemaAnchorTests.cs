using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10974FingerprintV1SchemaAnchorTests
{
    private const string GoldenFingerprint = "63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362";

    [Fact]
    public void FingerprintV1_PopulatedExactVersionFixtureMatchesFrozenGoldenHash()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);
        var session = factory.Load(DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var advance = session.Coordinator.AdvanceRunning(stepCount: 128, publicationStride: 128);
        Assert.Equal(128, advance.ExecutedStepCount);
        var snapshot = session.Coordinator.Current;

        // This exact-version fixture is deliberately structurally populated rather than ShellOnly. Combined with the
        // frozen hash, additions/removals to the fingerprint-visible presentation graph become an explicit compatibility
        // decision instead of a silent v1 redefinition.
        Assert.NotEmpty(snapshot.ReactorCore.Zones);
        Assert.NotEmpty(snapshot.ReactorCore.Rods);
        Assert.NotEmpty(snapshot.ReactorCore.RodTargets);
        Assert.NotEmpty(snapshot.PrimaryCircuit.Loops);
        Assert.NotEmpty(snapshot.PrimaryCircuit.Loops.SelectMany(static loop => loop.Pumps));
        Assert.NotEmpty(snapshot.PrimaryCircuit.Loops.SelectMany(static loop => loop.Branches));
        Assert.NotEmpty(snapshot.PrimaryCircuit.SteamDrums);
        // The retained H29 topology has no valve whose endpoint belongs to the primary-node projection. Its
        // stop/control/admission valves are represented by the turbine/secondary panel instead. Empty here is
        // therefore part of the frozen exact-version fixture, not a ShellOnly/unpopulated snapshot symptom.
        Assert.Empty(snapshot.PrimaryCircuit.Valves);
        Assert.NotEmpty(snapshot.TurbineSecondary.SteamLines);
        Assert.NotEmpty(snapshot.TurbineSecondary.AdmissionTrains);
        Assert.NotEmpty(snapshot.TurbineSecondary.Rotors);
        Assert.NotEmpty(snapshot.TurbineSecondary.StageGroups);
        Assert.NotEmpty(snapshot.TurbineSecondary.Condensers);
        Assert.NotEmpty(snapshot.TurbineSecondary.FeedwaterTrains);
        Assert.NotEmpty(snapshot.Electrical.Generators);

        Assert.Equal("sha256-control-room-snapshot-v1", ControlRoomSnapshotFingerprint.AlgorithmId);
        var actualFingerprint = ControlRoomSnapshotFingerprint.Compute(snapshot);
        M10974FingerprintV1CrossHostDiagnostic1.TryWrite(snapshot, GoldenFingerprint, actualFingerprint);
        Assert.Equal(GoldenFingerprint, actualFingerprint);
    }

    [Fact]
    public void FingerprintV1_PopulatedExactVersionFixtureIsInvariantAcrossItalianAndUsCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var italian = ComputeFixtureFingerprintAndVoidText(CultureInfo.GetCultureInfo("it-IT"));
            var us = ComputeFixtureFingerprintAndVoidText(CultureInfo.GetCultureInfo("en-US"));

            Assert.Equal("Void 0.0%", italian.VoidText);
            Assert.Equal("Void 0.0%", us.VoidText);
            Assert.Equal(GoldenFingerprint, italian.Fingerprint);
            Assert.Equal(GoldenFingerprint, us.Fingerprint);
            Assert.Equal(italian.Fingerprint, us.Fingerprint);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static (string Fingerprint, string VoidText) ComputeFixtureFingerprintAndVoidText(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);
        var session = factory.Load(DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var advance = session.Coordinator.AdvanceRunning(stepCount: 128, publicationStride: 128);
        Assert.Equal(128, advance.ExecutedStepCount);
        var snapshot = session.Coordinator.Current;
        var branch = Assert.Single(Assert.Single(snapshot.PrimaryCircuit.Loops).Branches);

        return (ControlRoomSnapshotFingerprint.Compute(snapshot), branch.VoidText);
    }
}
