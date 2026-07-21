using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>M5.4 top-level automatic-control snapshot over the unchanged M4.7 physical full-plant snapshot.</summary>
public sealed record IntegratedAutomaticControlSnapshot(
    FullPlantSnapshot FullPlant,
    ReactorPrimaryControlSnapshot ReactorPrimary,
    TurbineSecondaryControlSnapshot TurbineSecondary);
