using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-G.2 passive hydraulic enthalpy migration and pump-work ownership evidence.</summary>
public sealed class PassiveHydraulicEnthalpyMigrationAuditTests
{
    [Fact]
    public void CurrentV2_PassivePipesValvesAndPumpPathsUseEnthalpyAfterG3Migration()
    {
        var plants = new[]
        {
            CreatePlant(new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine()),
            CreatePlant(new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine()),
        };

        foreach (var plant in plants)
        {
            Assert.NotEmpty(plant.Definition.Pipes);
            Assert.All(
                plant.Definition.Pipes,
                static pipe => Assert.Equal(
                    FluidEnergyTransportMode.SpecificEnthalpy,
                    pipe.EnergyTransportMode));
            Assert.NotEmpty(plant.Definition.Valves);
            Assert.All(
                plant.Definition.Valves,
                static valve => Assert.Equal(
                    FluidEnergyTransportMode.SpecificEnthalpy,
                    valve.Pipe.EnergyTransportMode));
            Assert.NotEmpty(plant.Definition.Pumps);
            Assert.All(
                plant.Definition.Pumps,
                static pump => Assert.Equal(
                    FluidEnergyTransportMode.SpecificEnthalpy,
                    pump.Pipe.EnergyTransportMode));
        }
    }

    [Fact]
    public void LegacyColdShutdown_PreservesHistoricalInternalEnergyTransportEverywhere()
    {
        var plant = CreatePlant(new ColdShutdownInitialConditionFactory().CreateRuntimeEngine());

        Assert.All(
            plant.Definition.Pipes,
            static pipe => Assert.Equal(
                FluidEnergyTransportMode.SpecificInternalEnergy,
                pipe.EnergyTransportMode));
        Assert.All(
            plant.Definition.Valves,
            static valve => Assert.Equal(
                FluidEnergyTransportMode.SpecificInternalEnergy,
                valve.Pipe.EnergyTransportMode));
        Assert.All(
            plant.Definition.Pumps,
            static pump => Assert.Equal(
                FluidEnergyTransportMode.SpecificInternalEnergy,
                pump.Pipe.EnergyTransportMode));
    }

    [Fact]
    public void CurrentV2CommittedState_ClosesPassiveTransfersAndMigratedPumpFluidWorkExactly()
    {
        var plant = CreatePlant(new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var passiveRows = BuildPassiveRows(plant);
        var pumpRows = BuildPumpRows(plant);

        Assert.Equal(plant.Definition.Pipes.Count + plant.Definition.Valves.Count, passiveRows.Count);
        Assert.All(passiveRows, static row =>
        {
            Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, row.EnergyTransportMode);
            Assert.Equal(
                row.FlowWorkRateMegawatts,
                row.AdvectedEnergyRateMegawatts - row.InternalEnergyRateMegawatts,
                9);
            Assert.Equal(0d, row.EndpointMassClosureKilogramsPerSecond, 12);
            Assert.Equal(0d, row.EndpointEnergyClosureWatts, 6);
        });
        Assert.Contains(passiveRows, static row => Math.Abs(row.FlowWorkRateMegawatts) > 0.001d);

        Assert.Equal(plant.Definition.Pumps.Count, pumpRows.Count);
        Assert.All(pumpRows, static row =>
        {
            Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, row.EnergyTransportMode);
            Assert.Equal(0d, row.MassClosureKilogramsPerSecond, 12);
            Assert.Equal(0d, row.FluidWorkOwnershipResidualWatts, 6);
            Assert.Equal(0d, row.PositiveShaftEfficiencyResidualWatts, 6);
        });
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PassiveHydraulicEnthalpyMigrationAudit")]
    public void CurrentV2PassiveHydraulics_RecordEnthalpyTransportAndPumpWorkOwnership()
    {
        var plant = CreatePlant(new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var passiveRows = BuildPassiveRows(plant);
        var pumpRows = BuildPumpRows(plant);

        WriteAuditReports(passiveRows, pumpRows);
    }

    private static PlantState CreatePlant(IControlRoomRuntimeEngine runtimeEngine)
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(runtimeEngine)
            .CurrentState
            .PlantState
            .PlantState;

    private static IReadOnlyList<PassiveTransportRow> BuildPassiveRows(PlantState plant)
    {
        var rows = new List<PassiveTransportRow>();
        var pipeSolver = new PipeFlowSolver();
        var valveSolver = new ValveFlowSolver();

        foreach (var pipe in plant.Definition.Pipes.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var result = pipeSolver.Solve(
                pipe,
                plant.GetFluidNode(pipe.FromNodeId),
                plant.GetFluidNode(pipe.ToNodeId));
            rows.Add(CreatePassiveRow(
                "pipe",
                pipe.Id,
                pipe.FromNodeId,
                pipe.ToNodeId,
                result));
        }

        foreach (var valve in plant.Definition.Valves.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var result = valveSolver.Solve(
                valve,
                plant.GetValve(valve.Id),
                plant.GetFluidNode(valve.Pipe.FromNodeId),
                plant.GetFluidNode(valve.Pipe.ToNodeId));
            rows.Add(CreatePassiveRow(
                "valve",
                valve.Id,
                valve.Pipe.FromNodeId,
                valve.Pipe.ToNodeId,
                result.HydraulicFlow));
        }

        return rows;
    }

    private static PassiveTransportRow CreatePassiveRow(
        string componentKind,
        string componentId,
        string fromNodeId,
        string toNodeId,
        PipeFlowResult result)
    {
        var endpointBalance = result.FromNodeBalance + result.ToNodeBalance;
        return new PassiveTransportRow(
            componentKind,
            componentId,
            fromNodeId,
            toNodeId,
            result.EnergyTransportMode,
            result.MassFlowRate.KilogramsPerSecond,
            result.InternalEnergyFlowRate.Megawatts,
            result.FlowWorkRate.Megawatts,
            result.EnthalpyFlowRate.Megawatts,
            result.AdvectedEnergyFlowRate.Megawatts,
            endpointBalance.NetMassFlowRate.KilogramsPerSecond,
            endpointBalance.NetEnergyRate.Watts);
    }

    private static IReadOnlyList<PumpWorkRow> BuildPumpRows(PlantState plant)
    {
        var solver = new PumpFlowSolver();
        return plant.Definition.Pumps
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(pump =>
            {
                var result = solver.Solve(
                    pump,
                    plant.GetPump(pump.Id),
                    plant.GetFluidNode(pump.Pipe.FromNodeId),
                    plant.GetFluidNode(pump.Pipe.ToNodeId));
                var combined = result.FromNodeBalance + result.ToNodeBalance;
                var shaftEfficiencyResidual = result.HydraulicPowerExchange.Watts > 0d
                    ? (result.ShaftPowerDemand.Watts * pump.Efficiency.Fraction) - result.HydraulicPowerExchange.Watts
                    : 0d;
                return new PumpWorkRow(
                    pump.Id,
                    pump.Pipe.FromNodeId,
                    pump.Pipe.ToNodeId,
                    result.EnergyTransportMode,
                    result.MassFlowRate.KilogramsPerSecond,
                    result.AdvectedEnergyFlowRate.Megawatts,
                    result.HydraulicPowerExchange.Megawatts,
                    result.ShaftPowerDemand.Megawatts,
                    combined.NetMassFlowRate.KilogramsPerSecond,
                    combined.NetEnergyRate.Watts - result.HydraulicPowerExchange.Watts,
                    shaftEfficiencyResidual);
            })
            .ToArray();
    }

    private static void WriteAuditReports(
        IReadOnlyList<PassiveTransportRow> passiveRows,
        IReadOnlyList<PumpWorkRow> pumpRows)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "g2-passive-hydraulic-enthalpy");
        Directory.CreateDirectory(directory);

        var passiveCsv = new StringBuilder();
        passiveCsv.AppendLine("component_kind,component_id,from_node,to_node,energy_transport_mode,mass_flow_kg_per_s,internal_energy_rate_mw,flow_work_rate_mw,enthalpy_rate_mw,advected_energy_rate_mw,endpoint_mass_closure_kg_per_s,endpoint_energy_closure_w");
        foreach (var row in passiveRows)
        {
            passiveCsv.AppendLine(FormattableString.Invariant(
                $"{row.ComponentKind},{row.ComponentId},{row.FromNodeId},{row.ToNodeId},{row.EnergyTransportMode},{row.MassFlowKilogramsPerSecond:0.000000000},{row.InternalEnergyRateMegawatts:0.000000000},{row.FlowWorkRateMegawatts:0.000000000},{row.EnthalpyRateMegawatts:0.000000000},{row.AdvectedEnergyRateMegawatts:0.000000000},{row.EndpointMassClosureKilogramsPerSecond:0.000000000000},{row.EndpointEnergyClosureWatts:0.000000}"));
        }
        File.WriteAllText(
            Path.Combine(directory, "01-current-v2-passive-enthalpy-transport.csv"),
            passiveCsv.ToString(),
            new UTF8Encoding(false));

        var pumpCsv = new StringBuilder();
        pumpCsv.AppendLine("pump_id,from_node,to_node,energy_transport_mode,mass_flow_kg_per_s,advected_energy_rate_mw,hydraulic_power_exchange_mw,shaft_power_demand_mw,mass_closure_kg_per_s,fluid_work_ownership_residual_w,positive_shaft_efficiency_residual_w");
        foreach (var row in pumpRows)
        {
            pumpCsv.AppendLine(FormattableString.Invariant(
                $"{row.PumpId},{row.FromNodeId},{row.ToNodeId},{row.EnergyTransportMode},{row.MassFlowKilogramsPerSecond:0.000000000},{row.AdvectedEnergyRateMegawatts:0.000000000},{row.HydraulicPowerExchangeMegawatts:0.000000000},{row.ShaftPowerDemandMegawatts:0.000000000},{row.MassClosureKilogramsPerSecond:0.000000000000},{row.FluidWorkOwnershipResidualWatts:0.000000},{row.PositiveShaftEfficiencyResidualWatts:0.000000}"));
        }
        File.WriteAllText(
            Path.Combine(directory, "02-current-v2-pump-work-ownership.csv"),
            pumpCsv.ToString(),
            new UTF8Encoding(false));

        var maximumFlowWork = passiveRows.Max(static row => Math.Abs(row.FlowWorkRateMegawatts));
        var totalFlowWork = passiveRows.Sum(static row => Math.Abs(row.FlowWorkRateMegawatts));
        var maximumPassiveClosure = passiveRows.Max(static row => Math.Abs(row.EndpointEnergyClosureWatts));
        var maximumPumpWorkResidual = pumpRows.Max(static row => Math.Abs(row.FluidWorkOwnershipResidualWatts));
        var maximumShaftResidual = pumpRows.Max(static row => Math.Abs(row.PositiveShaftEfficiencyResidualWatts));
        var summary = string.Join(Environment.NewLine,
            "=== 01-current-v2-passive-enthalpy-and-pump-work-ownership ===",
            "Current-v2 passive pipes, valve paths and G.3 pump paths apply h*m_dot to node balances while hydraulic fluid work and shaft demand remain separately audited.",
            FormattableString.Invariant(
                $"passive-components={passiveRows.Count}; pipes={passiveRows.Count(static row => row.ComponentKind == "pipe")}; valves={passiveRows.Count(static row => row.ComponentKind == "valve")}; enthalpy-mode-components={passiveRows.Count(static row => row.EnergyTransportMode == FluidEnergyTransportMode.SpecificEnthalpy)};"),
            FormattableString.Invariant(
                $"maximum-absolute-flow-work-rate={maximumFlowWork:0.000000000} MW; total-absolute-flow-work-rate={totalFlowWork:0.000000000} MW; maximum-passive-transfer-closure={maximumPassiveClosure:0.000000} W;"),
            FormattableString.Invariant(
                $"pump-components={pumpRows.Count}; enthalpy-pump-paths={pumpRows.Count(static row => row.EnergyTransportMode == FluidEnergyTransportMode.SpecificEnthalpy)}; maximum-pump-fluid-work-residual={maximumPumpWorkResidual:0.000000} W; maximum-positive-shaft-efficiency-residual={maximumShaftResidual:0.000000} W;"),
            "runtime-passive-migration-active=True; pump-path-migration-active=True; pump-hydraulic-work-single-count=True; pump-shaft-demand-single-count=True; turbine-work-retuned=False",
            string.Empty);
        File.WriteAllText(
            Path.Combine(directory, "01-current-v2-passive-enthalpy-and-pump-work-ownership.summary.txt"),
            summary,
            new UTF8Encoding(false));
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

    private sealed record PassiveTransportRow(
        string ComponentKind,
        string ComponentId,
        string FromNodeId,
        string ToNodeId,
        FluidEnergyTransportMode EnergyTransportMode,
        double MassFlowKilogramsPerSecond,
        double InternalEnergyRateMegawatts,
        double FlowWorkRateMegawatts,
        double EnthalpyRateMegawatts,
        double AdvectedEnergyRateMegawatts,
        double EndpointMassClosureKilogramsPerSecond,
        double EndpointEnergyClosureWatts);

    private sealed record PumpWorkRow(
        string PumpId,
        string FromNodeId,
        string ToNodeId,
        FluidEnergyTransportMode EnergyTransportMode,
        double MassFlowKilogramsPerSecond,
        double AdvectedEnergyRateMegawatts,
        double HydraulicPowerExchangeMegawatts,
        double ShaftPowerDemandMegawatts,
        double MassClosureKilogramsPerSecond,
        double FluidWorkOwnershipResidualWatts,
        double PositiveShaftEfficiencyResidualWatts);
}
