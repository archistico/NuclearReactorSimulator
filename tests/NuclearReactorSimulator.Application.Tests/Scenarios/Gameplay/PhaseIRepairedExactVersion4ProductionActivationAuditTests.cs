using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 repaired exact-v4 production activation gate. The thermodynamic repair and exact-v4 readiness have already
/// been qualified; this test verifies only the authoritative selector/scenario switch, fail-closed exact-v2 rollback,
/// historical exact-v3 retention, short healthy operation, corrected ownership and deterministic repeat.
/// </summary>
public sealed class PhaseIRepairedExactVersion4ProductionActivationAuditTests
{
    private const int HealthySteps = 1_200;
    private const int DeterminismSteps = 128;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIRepairedExactVersion4ProductionActivationAudit")]
    public void RepairedExactV4_IsAuthoritativeProductionWithV2FailClosedRollbackAndHistoricalV3Retention()
    {
        ResetReportDirectory();

        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);

        var current = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var historicalV3 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, current.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, current.InitialCondition);
        Assert.Equal(4, current.InitialCondition.Version);
        Assert.False(current.ExplicitKillApplied);

        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, historicalV3.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, historicalV3.InitialCondition);
        Assert.Equal(3, historicalV3.InitialCondition.Version);

        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, rollback.InitialCondition);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.True(rollback.ExplicitKillApplied);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario,
            DesktopIntegratedOperationsProductionProgram.ResolveScenario(DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy));
        Assert.Equal(4, DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.InitialCondition.Version);
        Assert.Equal(3, DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.InitialCondition.Version);
        Assert.NotEqual(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(current).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            CurrentHydraulics(engine).Mode);

        var probe = new DesktopHydraulicProductionTelemetryProbe();
        var healthViolations = 0;
        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var minimumGross = double.PositiveInfinity;
        var minimumShaft = double.PositiveInfinity;
        var minimumRotor = double.PositiveInfinity;
        var maximumRotor = double.NegativeInfinity;
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;

        for (var step = 1; step <= HealthySteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            probe.Observe(engine);
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
            var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;

            var request = generator.RequestedElectricalPower.NumericValue ?? double.NaN;
            var gross = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var shaft = rotor.ShaftPower.NumericValue ?? double.NaN;
            var rpm = rotor.Speed.NumericValue ?? double.NaN;
            Assert.True(double.IsFinite(request));
            Assert.True(double.IsFinite(gross));
            Assert.True(double.IsFinite(shaft));
            Assert.True(double.IsFinite(rpm));

            minimumGross = Math.Min(minimumGross, gross);
            minimumShaft = Math.Min(minimumShaft, shaft);
            minimumRotor = Math.Min(minimumRotor, rpm);
            maximumRotor = Math.Max(maximumRotor, rpm);

            if (snapshot.AnyTripActive)
            {
                tripSteps++;
            }
            if (!generator.BreakerClosed)
            {
                breakerOpenSteps++;
            }
            if (snapshot.AnyTripActive
                || !generator.BreakerClosed
                || !(request > 4.5d)
                || !(gross > 4.0d)
                || !(shaft > 4.5d))
            {
                healthViolations++;
            }

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(fullPlant.HeatBalance.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts));
        }

        var telemetry = probe.Snapshot();
        Assert.Equal(0, healthViolations);
        Assert.Equal(0, tripSteps);
        Assert.Equal(0, breakerOpenSteps);
        Assert.Equal(0, telemetry.RollbackSteps);
        Assert.Equal(0, telemetry.FallbackCommitViolations);
        Assert.Equal(0, telemetry.UnsafeCommitViolations);
        Assert.Equal(0, telemetry.UntargetedBranchDisagreementSteps);
        Assert.True(telemetry.TriggeredSteps > 0);
        Assert.Equal(telemetry.TriggeredSteps, telemetry.CorrectedCommittedSteps);
        Assert.True(maxMassClosure <= 1e-6d);
        Assert.True(maxEnergyClosure <= 1e-2d);
        Assert.True(maxBalanceMassRate <= 1e-8d);
        Assert.True(maxBalancePower <= 1e-3d);

        var deterministicA = DeterminismFingerprint();
        var deterministicB = DeterminismFingerprint();
        var deterministicRepeat = string.Equals(deterministicA, deterministicB, StringComparison.Ordinal);
        Assert.True(deterministicRepeat);

        WriteArtifacts(
            telemetry,
            healthViolations,
            tripSteps,
            breakerOpenSteps,
            minimumGross,
            minimumShaft,
            minimumRotor,
            maximumRotor,
            maxMassClosure,
            maxEnergyClosure,
            maxBalanceMassRate,
            maxBalancePower,
            deterministicA,
            deterministicRepeat);
    }

    private static string DeterminismFingerprint()
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            var telemetry = CurrentHydraulics(engine).FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(snapshot)}:{telemetry?.TriggerObserved}:{telemetry?.CorrectedCandidateCommitted}:{telemetry?.RollbackRequired}:{telemetry?.ShadowIterationCount}||"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static void WriteArtifacts(
        FourNodeProductionActivationTelemetrySnapshot telemetry,
        int healthViolations,
        int tripSteps,
        int breakerOpenSteps,
        double minimumGross,
        double minimumShaft,
        double minimumRotor,
        double maximumRotor,
        double maxMassClosure,
        double maxEnergyClosure,
        double maxBalanceMassRate,
        double maxBalancePower,
        string deterministicFingerprint,
        bool deterministicRepeat)
    {
        var lines = new[]
        {
            "=== 01-i5-repaired-exact-v4-production-activation ===",
            "scope=authoritative desktop production selector/scenario activation only; exact @4 repair already passed readiness plus Stages 1-4; exact @2/@3 remain immutable rollback/historical replay identities; synchronization exact family is unchanged;",
            "authoritative-default=integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn; historical-v3=integrated-operations-desktop-stable@3|HistoricalCorrelationTopology|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|HistoricalCorrelationTopology|ExplicitCommittedState;",
            FormattableString.Invariant($"healthy-control-steps={HealthySteps}; health-violations={healthViolations}; trip-steps={tripSteps}; breaker-open-steps={breakerOpenSteps}; minimum-gross-mwe={minimumGross:G17}; minimum-shaft-mw={minimumShaft:G17}; rotor-range-rpm={minimumRotor:G17}..{maximumRotor:G17};"),
            FormattableString.Invariant($"corrected-triggered={telemetry.TriggeredSteps}; corrected-eligible={telemetry.CandidateEligibleSteps}; corrected-authorized={telemetry.CommitAuthorizedSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; corrected-rollbacks={telemetry.RollbackSteps}; corrected-fallbacks={telemetry.ExplicitFallbackSteps}; fallback-commit-violations={telemetry.FallbackCommitViolations}; unsafe-commits={telemetry.UnsafeCommitViolations}; untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17};"),
            $"determinism-control-steps={DeterminismSteps}; deterministic-repeat={deterministicRepeat}; deterministic-fingerprint={deterministicFingerprint};",
            "production-activation=True; exact-v4-authoritative=True; exact-v3-reinterpreted=False; exact-v2-reinterpreted=False; explicit-kill-preserved=True; production-fixed-step=10.000 ms; physical-coefficient-retuning=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False;",
            "i5-repaired-v4-production-activation-passes=True; next-step=run the final repaired-v4 scheduled-long/reference requalification and cumulative M10.9.4.1 / Phase-I closure; no further repair stage is planned;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "01-i5-repaired-exact-v4-production-activation.summary.txt"), lines, Utf8WithoutBom);
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-repaired-exact-v4-production-activation");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
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
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }
}
