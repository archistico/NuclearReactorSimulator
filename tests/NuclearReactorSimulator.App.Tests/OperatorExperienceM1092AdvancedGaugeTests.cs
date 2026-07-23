using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorExperienceM1092AdvancedGaugeTests
{
    [Fact]
    public void Workspaces_UseLinearAndCircularGaugesForSelectedOperationalQuantities()
    {
        var document = LoadMainWindow();
        var linear = document.Descendants().Where(static element => element.Name.LocalName == "ControlRoomLinearGauge").ToArray();
        var circular = document.Descendants().Where(static element => element.Name.LocalName == "ControlRoomCircularGauge").ToArray();

        Assert.True(linear.Length >= 8, $"Expected at least 8 linear gauges, found {linear.Length}.");
        Assert.Equal(2, circular.Length);

        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding Pressure}");
        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding Level}");
        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding Electrical.GrossElectricalOutput}");
        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding Frequency}");
        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding TerminalVoltage}");
        Assert.Contains(linear, static element => (string?)element.Attribute("Snapshot") == "{Binding PhaseDifference}");
        Assert.Contains(circular, static element => (string?)element.Attribute("Snapshot") == "{Binding ReactorCore.ReactorThermalPower}");
        Assert.Contains(circular, static element => (string?)element.Attribute("Snapshot") == "{Binding Speed}");
    }

    [Fact]
    public void HmiLegend_ExplainsGaugeSemanticsAndRejectsInScaleEqualsSafeShortcut()
    {
        var document = LoadMainWindow();
        var literalText = document.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .Select(static text => text!)
            .ToArray();

        Assert.Contains(literalText, static text => text.Contains("cyan band = target window", StringComparison.Ordinal));
        Assert.Contains(literalText, static text => text.Contains("red marker = protection limit", StringComparison.Ordinal));
        Assert.Contains(literalText, static text => text.Contains("In-scale never means automatically safe", StringComparison.Ordinal));
    }

    [Fact]
    public void TurbineShaftPower_RemainsNumericUntilACanonicalScaleExists()
    {
        var document = LoadMainWindow();
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName.EndsWith("Gauge", StringComparison.Ordinal)
                && (string?)element.Attribute("Snapshot") == "{Binding TurbineSecondary.TotalTurbineShaftPower}");
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
}
