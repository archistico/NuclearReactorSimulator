using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorComputerM10953ContextInspectorXamlTests
{
    [Fact]
    public void CommandsPage_ContainsProgressiveDisclosureContextInspectorAndCanonicalMimicFocus()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));

        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "CONTEXT INSPECTOR — AUTHORED CONSEQUENCE / DEPENDENCY EVIDENCE");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "{Binding SelectedCommandContextSummaryText}");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "ListBox"
            && (string?)element.Attribute("ItemsSource") == "{Binding SelectedCommandDependencySteps}"
            && (string?)element.Attribute("SelectedItem") == "{Binding SelectedCommandDependencyStep, Mode=TwoWay}");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "ControlRoomPlantMimicControl"
            && (string?)element.Attribute("Snapshot") == "{Binding CommandContextPlantMimic}"
            && (string?)element.Attribute("SelectedElementId") == "{Binding SelectedCommandSchematicElementId, Mode=OneWay}"
            && (string?)element.Attribute("IsHitTestVisible") == "False");
    }

    [Fact]
    public void CommandsPage_PreservesExistingExplicitExecuteBoundaryAndNoFreeFormInput()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));

        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "Button"
            && (string?)element.Attribute("Command") == "{Binding ExecuteSelectedCommandCommand}"
            && (string?)element.Attribute("Content") == "EXECUTE [ENTER]");
        Assert.DoesNotContain(document.Descendants(), static element =>
            element.Name.LocalName is "TextBox" or "AutoCompleteBox");
    }
}
