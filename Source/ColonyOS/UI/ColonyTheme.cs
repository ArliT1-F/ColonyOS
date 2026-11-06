using ColonyOS.Core;
using ColonyOS.Data;
using UnityEngine;

namespace ColonyOS.UI
{
    public sealed class ThemePalette
    {
        public Color Window;
        public Color Sidebar;
        public Color Panel;
        public Color PanelRaised;
        public Color Border;
        public Color Text;
        public Color Muted;
        public Color Faint;
        public Color Accent;
        public Color Accent2;
        public Color Good;
        public Color Warning;
        public Color Danger;
        public Color Critical;
        public Color Purple;
        public Color Grid;

        public Color Series(int index)
        {
            switch (index % 6)
            {
                case 0: return Accent;
                case 1: return Accent2;
                case 2: return Purple;
                case 3: return Good;
                case 4: return Warning;
                default: return Danger;
            }
        }

        public Color Severity(PredictionSeverity severity)
        {
            switch (severity)
            {
                case PredictionSeverity.Critical: return Critical;
                case PredictionSeverity.High: return Danger;
                case PredictionSeverity.Moderate: return Warning;
                case PredictionSeverity.Low: return Accent2;
                default: return Muted;
            }
        }

        public Color Evidence(EvidenceKind evidence)
        {
            switch (evidence)
            {
                case EvidenceKind.Confirmed: return Good;
                case EvidenceKind.Historical: return Accent2;
                default: return Purple;
            }
        }
    }

    public static class ColonyTheme
    {
        public static ThemePalette Current { get; private set; } = Build(DashboardTheme.DarkIndustrial, 0);
        private static DashboardTheme loadedTheme = DashboardTheme.DarkIndustrial;
        private static int loadedAccent;

        public static void Configure(ColonyOSSettings settings)
        {
            if (settings == null)
                return;
            if (Current == null || loadedTheme != settings.Theme || loadedAccent != settings.AccentPalette)
            {
                loadedTheme = settings.Theme;
                loadedAccent = settings.AccentPalette;
                Current = Build(settings.Theme, settings.AccentPalette);
            }
        }

        private static ThemePalette Build(DashboardTheme theme, int accent)
        {
            ThemePalette palette = new ThemePalette();
            switch (theme)
            {
                case DashboardTheme.Midnight:
                    palette.Window = Rgb(0x070B16);
                    palette.Sidebar = Rgb(0x0A1020);
                    palette.Panel = Rgb(0x10182A);
                    palette.PanelRaised = Rgb(0x162139);
                    palette.Border = Rgb(0x293654);
                    palette.Text = Rgb(0xEEF0FF);
                    palette.Muted = Rgb(0x959DBA);
                    palette.Faint = Rgb(0x66708F);
                    palette.Grid = Rgb(0x26314B);
                    break;
                case DashboardTheme.NeutralProfessional:
                    palette.Window = Rgb(0xEEF1F4);
                    palette.Sidebar = Rgb(0xE3E8ED);
                    palette.Panel = Rgb(0xFAFBFC);
                    palette.PanelRaised = Rgb(0xFFFFFF);
                    palette.Border = Rgb(0xCDD5DD);
                    palette.Text = Rgb(0x1D2935);
                    palette.Muted = Rgb(0x5D6B78);
                    palette.Faint = Rgb(0x86929D);
                    palette.Grid = Rgb(0xDCE2E7);
                    break;
                default:
                    palette.Window = Rgb(0x0B1016);
                    palette.Sidebar = Rgb(0x0E151E);
                    palette.Panel = Rgb(0x121B26);
                    palette.PanelRaised = Rgb(0x182431);
                    palette.Border = Rgb(0x2A3949);
                    palette.Text = Rgb(0xE8EEF4);
                    palette.Muted = Rgb(0x91A0B1);
                    palette.Faint = Rgb(0x637284);
                    palette.Grid = Rgb(0x273543);
                    break;
            }

            palette.Accent = accent == 1 ? Rgb(0x55A7FF) : accent == 2 ? Rgb(0xF1B85B) : accent == 3 ? Rgb(0xB493FF) : Rgb(0x43D6B2);
            palette.Accent2 = theme == DashboardTheme.NeutralProfessional ? Rgb(0x287CC1) : Rgb(0x55A7FF);
            palette.Good = theme == DashboardTheme.NeutralProfessional ? Rgb(0x21875B) : Rgb(0x47D184);
            palette.Warning = Rgb(0xF1B85B);
            palette.Danger = Rgb(0xF16C7B);
            palette.Critical = Rgb(0xFF4385);
            palette.Purple = theme == DashboardTheme.NeutralProfessional ? Rgb(0x7956B3) : Rgb(0xB493FF);
            return palette;
        }

        public static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static Color Rgb(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
