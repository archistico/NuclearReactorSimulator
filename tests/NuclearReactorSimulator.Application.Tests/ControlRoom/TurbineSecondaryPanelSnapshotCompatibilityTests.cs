using System.Text.Json.Serialization;
using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class TurbineSecondaryPanelSnapshotCompatibilityTests
{
    [Fact]
    public void EffectiveTurbineSteamFlow_RemainsPresentationOnlyForFingerprintV1Compatibility()
    {
        var property = typeof(TurbineSecondaryPanelSnapshot)
            .GetProperty(nameof(TurbineSecondaryPanelSnapshot.EffectiveTurbineSteamFlow))
            ?? throw new InvalidOperationException("Effective turbine steam-flow presentation property is missing.");

        Assert.NotNull(Attribute.GetCustomAttribute(property, typeof(JsonIgnoreAttribute)));
    }
}
