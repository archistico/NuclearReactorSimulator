using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Canonical source catalog used to validate and resolve M5.1 instrument channels.</summary>
public sealed class InstrumentSignalSourceCatalog
{
    public InstrumentSignalSourceCatalog(IEnumerable<InstrumentSignalSource> sources)
        : this(null, sources)
    {
    }

    private InstrumentSignalSourceCatalog(
        IntegratedSecondaryCycleDefinition? fullPlantDefinition,
        IEnumerable<InstrumentSignalSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var canonical = sources
            .Select(source => source ?? throw new ArgumentException("Signal-source collections cannot contain null entries.", nameof(sources)))
            .OrderBy(static source => source.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("A signal-source catalog must contain at least one source.", nameof(sources));
        }

        if (canonical.Select(static source => source.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Signal-source ids must be unique.", nameof(sources));
        }

        FullPlantDefinition = fullPlantDefinition;
        Sources = new ReadOnlyCollection<InstrumentSignalSource>(canonical);
    }

    public IntegratedSecondaryCycleDefinition? FullPlantDefinition { get; }

    public IReadOnlyList<InstrumentSignalSource> Sources { get; }

    public InstrumentSignalSource GetSource(string id)
        => Sources.FirstOrDefault(source => string.Equals(source.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown instrument signal source '{id}'.");

    public void Validate(InstrumentationSystemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        foreach (var channel in definition.Channels)
        {
            var source = GetSource(channel.SourceId);
            if (!string.Equals(source.EngineeringUnitSymbol, channel.EngineeringUnitSymbol, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Instrument channel '{channel.Id}' expects unit '{channel.EngineeringUnitSymbol}', but source '{source.Id}' provides '{source.EngineeringUnitSymbol}'.",
                    nameof(definition));
            }
        }
    }

    /// <summary>
    /// Creates the canonical M5.1 plant source catalog. IDs are stable semantic seams; later UI/controllers consume measured
    /// channel IDs rather than traversing <c>FullPlantSnapshot</c> directly.
    /// </summary>
    public static InstrumentSignalSourceCatalog CreateFullPlantCatalog(IntegratedSecondaryCycleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var sources = new List<InstrumentSignalSource>
        {
            new("plant/reactor/thermal-power", "W", static snapshot => snapshot.ReactorThermalPower.Watts),
            new("plant/generator/gross-electrical-output", "W", static snapshot => snapshot.GrossElectricalOutputPower.Watts),
            new("plant/primary/total-mass", "kg", static snapshot => snapshot.IntegratedCycle.PrimaryCircuit.TotalPlantMass.Kilograms),
            new("plant/turbine/total-shaft-power", "W", static snapshot => snapshot.IntegratedCycle.TurbineExpansion.TotalShaftPower.Watts),
            new("plant/condenser/total-heat-rejection", "W", static snapshot => snapshot.IntegratedCycle.Condenser.TotalHeatRejectionPower.Watts)
        };

        foreach (var loop in definition.PrimaryCircuit.MainCirculationSystem.Loops)
        {
            var loopId = loop.Id;
            sources.Add(new InstrumentSignalSource(
                $"main-circulation-loop/{loopId}/total-pump-flow",
                "kg/s",
                snapshot => snapshot.IntegratedCycle.PrimaryCircuit.MainCirculation.GetLoop(loopId).TotalPumpMassFlowRate.KilogramsPerSecond));
            sources.Add(new InstrumentSignalSource(
                $"main-circulation-loop/{loopId}/header-pressure-rise",
                "Pa",
                snapshot => snapshot.IntegratedCycle.PrimaryCircuit.MainCirculation.GetLoop(loopId).HeaderPressureRise.Pascals));
        }

        foreach (var drum in definition.PrimaryCircuit.SteamDrumSystem.Drums)
        {
            var drumId = drum.Id;
            sources.Add(new InstrumentSignalSource(
                $"steam-drum/{drumId}/pressure",
                "Pa",
                snapshot => snapshot.IntegratedCycle.PrimaryCircuit.SteamDrums.GetDrum(drumId).Pressure.Pascals));
            sources.Add(new InstrumentSignalSource(
                $"steam-drum/{drumId}/level",
                "fraction",
                snapshot => snapshot.IntegratedCycle.PrimaryCircuit.SteamDrums.GetDrum(drumId).LiquidLevelFraction.Fraction));
        }

        foreach (var rotor in definition.TurbineExpansionSystem.Rotors)
        {
            var rotorId = rotor.Id;
            sources.Add(new InstrumentSignalSource(
                $"turbine-rotor/{rotorId}/speed",
                "rpm",
                snapshot => snapshot.IntegratedCycle.TurbineExpansion.GetRotor(rotorId).FinalAngularSpeed.RevolutionsPerMinute));
        }

        foreach (var condenser in definition.CondensateFeedwaterSystem.CondenserSystem.Condensers)
        {
            var condenserId = condenser.Id;
            sources.Add(new InstrumentSignalSource(
                $"condenser/{condenserId}/pressure",
                "Pa",
                snapshot => snapshot.IntegratedCycle.Condenser.GetCondenser(condenserId).FinalSteamSpacePressure.Pascals));
            sources.Add(new InstrumentSignalSource(
                $"condenser/{condenserId}/vacuum",
                "Pa",
                snapshot => snapshot.IntegratedCycle.Condenser.GetCondenser(condenserId).FinalVacuumBelowAtmosphere.Pascals));
            sources.Add(new InstrumentSignalSource(
                $"condenser/{condenserId}/hotwell-mass",
                "kg",
                snapshot => snapshot.IntegratedCycle.Condenser.GetCondenser(condenserId).FinalHotwellMass.Kilograms));
        }

        foreach (var generator in definition.GeneratorGridSystem.Generators)
        {
            var generatorId = generator.Id;
            sources.Add(new InstrumentSignalSource(
                $"generator/{generatorId}/frequency",
                "Hz",
                snapshot => snapshot.IntegratedCycle.GeneratorGrid.Generators
                    .First(item => string.Equals(item.GeneratorId, generatorId, StringComparison.Ordinal))
                    .FinalElectricalFrequency.Hertz));
            sources.Add(new InstrumentSignalSource(
                $"generator/{generatorId}/electrical-output",
                "W",
                snapshot => snapshot.IntegratedCycle.GeneratorGrid.Generators
                    .First(item => string.Equals(item.GeneratorId, generatorId, StringComparison.Ordinal))
                    .ElectricalOutputPower.Watts));
        }

        return new InstrumentSignalSourceCatalog(definition, sources);
    }
}
