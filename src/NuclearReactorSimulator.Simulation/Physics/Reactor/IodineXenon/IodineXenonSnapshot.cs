using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;

/// <summary>
/// Immutable diagnostics for the reduced I-135 / Xe-135 inventory model.
/// Rates are expressed in the same normalized inventory units used by the configured definition.
/// </summary>
public sealed record IodineXenonSnapshot(
    string DefinitionId,
    IodineXenonState State,
    double IodineProductionRatePerSecond,
    double IodineDecayRatePerSecond,
    double DirectXenonProductionRatePerSecond,
    double XenonProductionFromIodineRatePerSecond,
    double XenonNaturalDecayRatePerSecond,
    double XenonBurnupRatePerSecond,
    Reactivity XenonReactivity)
{
    public ReactivityContribution ToContribution()
        => new($"xenon/{DefinitionId}", ReactivityContributionKind.Xenon, XenonReactivity);
}
