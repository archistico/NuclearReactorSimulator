using System.Globalization;
using NuclearReactorSimulator.App.Presentation;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Presentation;

public sealed class EngineeringNumberFormatterTests
{
    [Fact]
    public void Compact_RemainsInvariantOnItalianHostCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it-IT");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("it-IT");

            Assert.Equal("1234.5", EngineeringNumberFormatter.Compact(1234.5d));
            Assert.Equal("-0.125", EngineeringNumberFormatter.Compact(-0.125d));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
