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

    [Fact]
    public void CondenserPhaseChangeDiagnostics_RemainPresentationOnlyForFingerprintV1Compatibility()
    {
        var propertyNames = new[]
        {
            nameof(CondenserPresentationSnapshot.CondensateSpecificInternalEnergy),
            nameof(CondenserPresentationSnapshot.SpecificCondensationEnergyDrop),
            nameof(CondenserPresentationSnapshot.CondensationLimitStatus),
            nameof(CondenserPresentationSnapshot.InstalledCoolingCapacity),
            nameof(CondenserPresentationSnapshot.AvailableCoolingCapacity),
            nameof(CondenserPresentationSnapshot.SurfaceHeatTransferLimit),
            nameof(CondenserPresentationSnapshot.HeatRejectionLimitStatus),
        };

        foreach (var propertyName in propertyNames)
        {
            var property = typeof(CondenserPresentationSnapshot).GetProperty(propertyName)
                ?? throw new InvalidOperationException($"Condenser presentation property '{propertyName}' is missing.");

            Assert.NotNull(Attribute.GetCustomAttribute(property, typeof(JsonIgnoreAttribute)));
        }
    }
}
