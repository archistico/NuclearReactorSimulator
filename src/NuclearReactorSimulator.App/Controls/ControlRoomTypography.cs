using Avalonia.Media;

namespace NuclearReactorSimulator.App.Controls;

internal static class ControlRoomTypography
{
    public const string FontFamilyName = "Inter";
    public const string DataFontFamilyName = "Cascadia Mono,Consolas";

    public static FontFamily InterfaceFont { get; } = new(FontFamilyName);
    public static FontFamily DataFont { get; } = new(DataFontFamilyName);
}
