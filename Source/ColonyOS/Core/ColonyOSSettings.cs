using Verse;

namespace ColonyOS.Core
{
    public enum DashboardTheme
    {
        DarkIndustrial,
        Midnight,
        NeutralProfessional
    }

    public sealed class ColonyOSSettings : ModSettings
    {
        public int CollectionIntervalTicks = 2500;
        public int ForecastIntervalTicks = 2500;
        public int RetentionDays = 120;
        public DashboardTheme Theme = DashboardTheme.DarkIndustrial;
        public int AccentPalette;
        public bool Animations = true;
        bool showChartGrid = true;
        public bool CriticalNotifications = true;
        public float BatteryWarningPct = 0.2f;
        public float StorageWarningPct = 0.9f;
        public int ForecastHorizonHours = 72;
        public bool ShowPowerWidget = true;
        public bool ShowWealthWidget = true;
        public bool ShowProductionWidget = true;
        public bool ShowMarketWidget = true;
        public bool ShowWorkforceWidget = true;
        public bool ShowFoodWidget = true;

        public bool ShowChartGrid
        {
            get => showChartGrid;
            set => showChartGrid = value;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref CollectionIntervalTicks, "collectionIntervalTicks", 2500);
            Scribe_Values.Look(ref ForecastIntervalTicks, "forecastIntervalTicks", 2500);
            Scribe_Values.Look(ref RetentionDays, "retentionDays", 120);
            Scribe_Values.Look(ref Theme, "theme", DashboardTheme.DarkIndustrial);
            Scribe_Values.Look(ref AccentPalette, "accentPalette", 0);
            Scribe_Values.Look(ref Animations, "animations", true);
            Scribe_Values.Look(ref showChartGrid, "showChartGrid", true);
            Scribe_Values.Look(ref CriticalNotifications, "criticalNotifications", true);
            Scribe_Values.Look(ref BatteryWarningPct, "batteryWarningPct", 0.2f);
            Scribe_Values.Look(ref StorageWarningPct, "storageWarningPct", 0.9f);
            Scribe_Values.Look(ref ForecastHorizonHours, "forecastHorizonHours", 72);
            Scribe_Values.Look(ref ShowPowerWidget, "showPowerWidget", true);
            Scribe_Values.Look(ref ShowWealthWidget, "showWealthWidget", true);
            Scribe_Values.Look(ref ShowProductionWidget, "showProductionWidget", true);
            Scribe_Values.Look(ref ShowMarketWidget, "showMarketWidget", true);
            Scribe_Values.Look(ref ShowWorkforceWidget, "showWorkforceWidget", true);
            Scribe_Values.Look(ref ShowFoodWidget, "showFoodWidget", true);
            Clamp();
        }

        public void Clamp()
        {
            CollectionIntervalTicks = ClampInt(CollectionIntervalTicks, 600, 60000);
            ForecastIntervalTicks = ClampInt(ForecastIntervalTicks, 600, 60000);
            RetentionDays = ClampInt(RetentionDays, 7, 365);
            AccentPalette = ClampInt(AccentPalette, 0, 3);
            ForecastHorizonHours = ClampInt(ForecastHorizonHours, 6, 720);
            if (BatteryWarningPct < 0.01f) BatteryWarningPct = 0.01f;
            if (BatteryWarningPct > 0.95f) BatteryWarningPct = 0.95f;
            if (StorageWarningPct < 0.25f) StorageWarningPct = 0.25f;
            if (StorageWarningPct > 1f) StorageWarningPct = 1f;
        }

        public void ResetToDefaults()
        {
            CollectionIntervalTicks = 2500;
            ForecastIntervalTicks = 2500;
            RetentionDays = 120;
            Theme = DashboardTheme.DarkIndustrial;
            AccentPalette = 0;
            Animations = true;
            ShowChartGrid = true;
            CriticalNotifications = true;
            BatteryWarningPct = 0.2f;
            StorageWarningPct = 0.9f;
            ForecastHorizonHours = 72;
            ShowPowerWidget = true;
            ShowWealthWidget = true;
            ShowProductionWidget = true;
            ShowMarketWidget = true;
            ShowWorkforceWidget = true;
            ShowFoodWidget = true;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
