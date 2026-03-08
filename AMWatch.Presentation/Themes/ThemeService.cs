using MudBlazor;

namespace AMWatch.Presentation.Themes;

public static class ThemeService
{
    public static MudTheme DarkTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Background = "#0F172A",
            Surface = "#1E293B",
            Primary = "#22C55E",
            TextPrimary = "#E5E7EB"
        }
    };
}
