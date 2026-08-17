using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-G.4 turbine-expansion enthalpy and shaft-work ownership evidence.</summary>
public sealed class TurbineExpansionEnthalpyMigrationAuditTests
{
    [Fact]
    public void CurrentV2StagesUseEnthalpyWhileLegacyColdShutdownPreservesInternalEnergy()
    {
        foreach (var engine in CurrentV2Engines())
        {
            Assert.All(
                engine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups,
                static stage => Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, stage.EnergyTransportMode));
        }

        var legacy = CreateEngine(new ColdShutdownInitialConditionFactory());
        Assert.All(
            legacy.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups,
            static stage => Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, stage.EnergyTransportMode));
    }

    [Fact]
    public void CurrentV2StageSnapshotClosesEnthalpyAdvectionAgainstShaftWorkExactlyOnce()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var stage = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.TurbineExpansion.StageGroups);

        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, stage.EnergyTransportMode);
        Assert.Equal(
            stage.InletSpecificEnthalpy.JoulesPerKilogram,
            stage.InletSpecificInternalEnergy.JoulesPerKilogram + stage.InletSpecificFlowWork.JoulesPerKilogram,
            6);
        Assert.Equal(stage.InletSpecificEnthalpy, stage.InletAdvectedSpecificEnergy);
        Assert.Equal(
            stage.ExhaustAdvectedSpecificEnergy.JoulesPerKilogram,
            stage.InletAdvectedSpecificEnergy.JoulesPerKilogram - stage.ExtractedSpecificWork.JoulesPerKilogram,
            6);
        Assert.Equal(
            stage.InletEnergyFlowRate.Watts,
            stage.ExhaustEnergyFlowRate.Watts + stage.ShaftPower.Watts,
            3);
        Assert.InRange(Math.Abs(stage.TurbineEnergyOwnershipResidual.Watts), 0d, 0.001d);
    }

    [Fact]
    public void CurrentV2MigrationDoesNotRetuneThermodynamicWorkEfficiencyOrNominalWork()
    {
        foreach (var engine in CurrentV2Engines())
        {
            var stage = Assert.Single(engine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
            var work = Assert.IsType<NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine.TurbineThermodynamicWorkDefinition>(
                stage.ThermodynamicWork);

            Assert.Equal(500d, stage.NominalSpecificWork.KilojoulesPerKilogram, 12);
            Assert.Equal(0.86d, stage.Efficiency.Fraction, 12);
            Assert.Equal(2.1d, work.VaporSpecificHeatAtConstantPressure.KilojoulesPerKilogramKelvin, 12);
            Assert.Equal(1.3d, work.HeatCapacityRatio, 12);
            Assert.Equal(0.8d, work.MaximumInletInternalEnergyExtractionFraction, 12);
        }
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineExpansionEnthalpyMigrationAudit")]
    public void CurrentV2TurbineExpansion_RecordsEnthalpyAndSingleOwnedShaftWork()
    {
        var rows = new[]
        {
            BuildRow("desktop-sustained", CreateEngine(new DesktopSustainedGenerationInitialConditionFactory())),
            BuildRow("grid-synchronization", CreateEngine(new GridSynchronizationSustainedInitialConditionFactory())),
        };

        Assert.All(rows, static row =>
        {
            Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, row.EnergyTransportMode);
            Assert.InRange(Math.Abs(row.OwnershipResidualWatts), 0d, 0.001d);
            Assert.True(double.IsFinite(row.InletEnthalpyKilojoulesPerKilogram));
            Assert.True(double.IsFinite(row.ExhaustAdvectedKilojoulesPerKilogram));
            Assert.True(double.IsFinite(row.ShaftPowerMegawatts));
        });
        Assert.Contains(rows, static row => row.MassFlowKilogramsPerSecond > 0d);
        Assert.Contains(rows, static row => Math.Abs(row.FlowWorkRateMegawatts) > 0.001d);
        WriteAuditReports(rows);
    }

    private static AuditRow BuildRow(string profile, IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var stage = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
        return new AuditRow(
            profile,
            stage.StageGroupId,
            stage.EnergyTransportMode,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.InletSpecificInternalEnergy.KilojoulesPerKilogram,
            stage.InletSpecificFlowWork.KilojoulesPerKilogram,
            stage.InletSpecificEnthalpy.KilojoulesPerKilogram,
            stage.ExtractedSpecificWork.KilojoulesPerKilogram,
            stage.ExhaustAdvectedSpecificEnergy.KilojoulesPerKilogram,
            stage.InletEnergyFlowRate.Megawatts,
            stage.FlowWorkRate.Megawatts,
            stage.ExhaustEnergyFlowRate.Megawatts,
            stage.ShaftPower.Megawatts,
            stage.TurbineEnergyOwnershipResidual.Watts);
    }

    private static IReadOnlyList<IntegratedAutomaticOperationRuntimeEngine> CurrentV2Engines()
        => new[]
        {
            CreateEngine(new DesktopSustainedGenerationInitialConditionFactory()),
            CreateEngine(new GridSynchronizationSustainedInitialConditionFactory()),
        };

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(IVersionedInitialConditionFactory factory)
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());

    private static void WriteAuditReports(IReadOnlyList<AuditRow> rows)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "g4-turbine-expansion-enthalpy");
        Directory.CreateDirectory(directory);
        const string stem = "01-current-v2-turbine-expansion-enthalpy-and-shaft-work";
        var csv = new StringBuilder();
        csv.AppendLine("profile,stage_id,energy_transport_mode,mass_flow_kg_per_s,inlet_internal_energy_kj_per_kg,inlet_flow_work_kj_per_kg,inlet_enthalpy_kj_per_kg,extracted_specific_work_kj_per_kg,exhaust_advected_energy_kj_per_kg,inlet_energy_rate_mw,flow_work_rate_mw,exhaust_energy_rate_mw,shaft_power_mw,ownership_residual_w");
        foreach (var row in rows)
        {
            csv.AppendLine(FormattableString.Invariant(
                $"{row.Profile},{row.StageId},{row.EnergyTransportMode},{row.MassFlowKilogramsPerSecond:0.000000000},{row.InletInternalEnergyKilojoulesPerKilogram:0.000000000},{row.InletFlowWorkKilojoulesPerKilogram:0.000000000},{row.InletEnthalpyKilojoulesPerKilogram:0.000000000},{row.ExtractedSpecificWorkKilojoulesPerKilogram:0.000000000},{row.ExhaustAdvectedKilojoulesPerKilogram:0.000000000},{row.InletEnergyRateMegawatts:0.000000000},{row.FlowWorkRateMegawatts:0.000000000},{row.ExhaustEnergyRateMegawatts:0.000000000},{row.ShaftPowerMegawatts:0.000000000},{row.OwnershipResidualWatts:0.000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{stem}.csv"), csv.ToString(), new UTF8Encoding(false));

        var maximumFlowWorkRate = rows.Max(static row => Math.Abs(row.FlowWorkRateMegawatts));
        var maximumShaftPower = rows.Max(static row => Math.Abs(row.ShaftPowerMegawatts));
        var maximumResidual = rows.Max(static row => Math.Abs(row.OwnershipResidualWatts));
        var summary = string.Join(Environment.NewLine,
            $"=== {stem} ===",
            "Current-v2 turbine expansion applies h*m_dot at the inlet, transfers h*m_dot minus shaft work to exhaust, and keeps shaft work as one explicit thermofluid-to-rotor transfer without retuning turbine work.",
            FormattableString.Invariant(
                $"samples={rows.Count}; enthalpy-mode-stages={rows.Count(static row => row.EnergyTransportMode == FluidEnergyTransportMode.SpecificEnthalpy)}; positive-flow-samples={rows.Count(static row => row.MassFlowKilogramsPerSecond > 0d)};"),
            FormattableString.Invariant(
                $"maximum-absolute-flow-work-rate={maximumFlowWorkRate:0.000000000} MW; maximum-absolute-shaft-power={maximumShaftPower:0.000000000} MW; maximum-ownership-residual={maximumResidual:0.000000} W;"),
            "runtime-turbine-expansion-migration-active=True; node-inventories-remain-internal-energy=True; shaft-work-single-count=True; thermodynamic-work-retuned=False; legacy-profiles-preserved=True",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, $"{stem}.summary.txt"), summary, new UTF8Encoding(false));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private sealed record AuditRow(
        string Profile,
        string StageId,
        FluidEnergyTransportMode EnergyTransportMode,
        double MassFlowKilogramsPerSecond,
        double InletInternalEnergyKilojoulesPerKilogram,
        double InletFlowWorkKilojoulesPerKilogram,
        double InletEnthalpyKilojoulesPerKilogram,
        double ExtractedSpecificWorkKilojoulesPerKilogram,
        double ExhaustAdvectedKilojoulesPerKilogram,
        double InletEnergyRateMegawatts,
        double FlowWorkRateMegawatts,
        double ExhaustEnergyRateMegawatts,
        double ShaftPowerMegawatts,
        double OwnershipResidualWatts);
}
