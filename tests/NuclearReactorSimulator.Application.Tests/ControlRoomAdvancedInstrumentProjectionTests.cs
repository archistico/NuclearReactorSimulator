using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomAdvancedInstrumentProjectionTests
{
    [Fact]
    public void ColdShutdownProjection_PublishesCanonicalGaugeSemanticsWithoutUiThresholdOwnership()
    {
        var snapshot = new ColdShutdownInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.Equal(ControlRoomInstrumentProvenance.Measured, snapshot.ReactorCore.ReactorThermalPower.Provenance);
        Assert.NotNull(snapshot.ReactorCore.ReactorThermalPower.InstrumentScale);
        Assert.Equal(ControlRoomInstrumentProvenance.Model, snapshot.ReactorCore.AverageRodWithdrawal.Provenance);
        Assert.Equal(0d, snapshot.ReactorCore.AverageRodWithdrawal.InstrumentScale!.Minimum);
        Assert.Equal(100d, snapshot.ReactorCore.AverageRodWithdrawal.InstrumentScale.Maximum);

        Assert.NotEmpty(snapshot.PrimaryCircuit.SteamDrums);
        Assert.All(snapshot.PrimaryCircuit.SteamDrums, static drum =>
        {
            Assert.Equal(ControlRoomInstrumentProvenance.Measured, drum.Pressure.Provenance);
            var pressureScale = Assert.IsType<ControlRoomInstrumentScaleSnapshot>(drum.Pressure.InstrumentScale);
            Assert.Contains(pressureScale.OperatingBands, static band =>
                band.Kind == ControlRoomInstrumentBandKind.Warning
                && band.Minimum == 20d
                && band.Maximum == 25d);
            Assert.Contains(pressureScale.ProtectionLimits, static limit =>
                limit.Direction == ControlRoomLimitDirection.High
                && limit.Threshold == 25d);
            Assert.Contains("STEAM-DRUM PRESSURE HIGH 20–25 MPa", drum.Pressure.ScaleSemanticsText, StringComparison.Ordinal);
            Assert.Contains("VERY-HIGH-PRESSURE ≥25 MPa", drum.Pressure.ScaleSemanticsText, StringComparison.Ordinal);
            Assert.NotNull(drum.Level.InstrumentScale);
            Assert.Equal(0d, drum.Level.InstrumentScale!.Minimum);
            Assert.Equal(100d, drum.Level.InstrumentScale.Maximum);
        });

        Assert.NotEmpty(snapshot.TurbineSecondary.Rotors);
        Assert.All(snapshot.TurbineSecondary.Rotors, static rotor =>
        {
            var scale = Assert.IsType<ControlRoomInstrumentScaleSnapshot>(rotor.Speed.InstrumentScale);
            Assert.Contains(scale.ProtectionLimits, static limit =>
                limit.Direction == ControlRoomLimitDirection.High
                && limit.Label.Contains("OVERSPEED", StringComparison.Ordinal));
        });

        Assert.NotEmpty(snapshot.Electrical.Generators);
        Assert.All(snapshot.Electrical.Generators, static generator =>
        {
            Assert.NotNull(generator.Frequency.InstrumentScale?.TargetBand);
            Assert.NotNull(generator.TerminalVoltage.InstrumentScale?.TargetBand);
            Assert.NotNull(generator.PhaseDifference.InstrumentScale?.TargetBand);
            Assert.Equal(0d, generator.PhaseDifference.InstrumentScale!.Setpoint!.Value);
            Assert.NotNull(generator.ElectricalOutput.InstrumentScale);
        });
        Assert.NotNull(snapshot.Electrical.GrossElectricalOutput.InstrumentScale);
    }

    [Fact]
    public void ValueSnapshot_ReportsOffScaleExplicitlyAndKeepsScaleDistinctFromSafetyState()
    {
        var value = new ControlRoomValueSnapshot("120", "%", 120d, ControlRoomVisualState.Normal)
        {
            InstrumentScale = new ControlRoomInstrumentScaleSnapshot(0d, 100d),
            Provenance = ControlRoomInstrumentProvenance.Measured,
            Quality = ControlRoomInstrumentQuality.Good,
        };

        Assert.True(value.IsAboveScale);
        Assert.True(value.IsOffScale);
        Assert.Equal("> 100 %", value.ScaleStatusText);
        Assert.Equal(ControlRoomVisualState.Normal, value.State);
        Assert.Equal("MEASURED", value.ProvenanceText);
        Assert.Equal("NO CANONICAL OPERATING/TARGET/PROTECTION BAND", value.ScaleSemanticsText);
    }
}
