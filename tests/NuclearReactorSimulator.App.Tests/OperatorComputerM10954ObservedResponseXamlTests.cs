using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorComputerM10954ObservedResponseXamlTests
{
    [Fact]
    public void CommandsPage_ContainsDistinctPostDispatchObservedResponseEvidencePanel()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));

        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "OBSERVED RESPONSE — POST-DISPATCH EVIDENCE");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "{Binding LastCommandObservedResponseText}");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && ((string?)element.Attribute("Text"))?.Contains("not proof of causality", StringComparison.Ordinal) == true);
    }
}
