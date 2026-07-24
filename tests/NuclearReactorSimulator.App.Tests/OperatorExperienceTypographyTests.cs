using System.Xml.Linq;
using System.Text;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorExperienceTypographyTests
{
    [Fact]
    public void Interface_DoesNotEmbedFontFiles()
    {
        var assembly = typeof(NuclearReactorSimulator.App.App).Assembly;
        using var stream = assembly.GetManifestResourceStream("!AvaloniaResources");
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var resourceCatalog = Encoding.UTF8.GetString(buffer.ToArray());

        Assert.DoesNotContain("Assets/Fonts/", resourceCatalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Interface_UsesInterForInterfaceAndMonospaceOnlyForData()
    {
        var app = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "App.axaml"));
        var fontResources = app.Descendants()
            .Where(element => element.Name.LocalName == "FontFamily")
            .ToDictionary(
                element => (string)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))!,
                element => element.Value,
                StringComparer.Ordinal);

        Assert.Equal("Inter", fontResources["InterfaceFont"]);
        Assert.Equal("Cascadia Mono,Consolas", fontResources["DataFont"]);

        foreach (var fileName in new[] { "MainWindow.axaml", "ControlRoomComputerControl.axaml" })
        {
            var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, fileName));
            Assert.DoesNotContain("Courier New", content, StringComparison.Ordinal);
        }
    }
}
