using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ColonyOS.Core
{
    public sealed class ColonyOSMod : Mod
    {
        public static ColonyOSMod Instance { get; private set; }
        public static ColonyOSSettings Settings => Instance?.settings;
        public static ColonyOSGameComponent Component => Current.Game == null ? null : Current.Game.GetComponent<ColonyOSGameComponent>();

        private readonly ColonyOSSettings settings;
        private string collectionBuffer;
        private string forecastBuffer;
        private string retentionBuffer;
        private Vector2 settingsScroll;

        public ColonyOSMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<ColonyOSSettings>();
            settings.Clamp();
            try
            {
                Harmony harmony = new Harmony("ArliT1F.ColonyOS");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[ColonyOS] Initialized with narrowly scoped production and trading observers.");
            }
            catch (Exception ex)
            {
                Log.Error("[ColonyOS] Harmony initialization failed. Scheduled analytics remain available; event history is disabled for this session. " + ex);
            }
        }

        public override string SettingsCategory() => "ColonyOS_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.Clamp();
            collectionBuffer = collectionBuffer ?? settings.CollectionIntervalTicks.ToString();
            forecastBuffer = forecastBuffer ?? settings.ForecastIntervalTicks.ToString();
            retentionBuffer = retentionBuffer ?? settings.RetentionDays.ToString();

            Rect view = new Rect(0f, 0f, inRect.width - 20f, 850f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);
            listing.Label("ColonyOS_Tagline".Translate());
            listing.GapLine();
            listing.Label("ColonyOS_DataCollection".Translate());
            listing.TextFieldNumericLabeled("ColonyOS_CollectionInterval".Translate(), ref settings.CollectionIntervalTicks, ref collectionBuffer, 600, 60000);
            listing.TextFieldNumericLabeled("ColonyOS_ForecastInterval".Translate(), ref settings.ForecastIntervalTicks, ref forecastBuffer, 600, 60000);
            listing.TextFieldNumericLabeled("ColonyOS_RetentionDays".Translate(), ref settings.RetentionDays, ref retentionBuffer, 7, 365);
            if (listing.ButtonText("ColonyOS_Refresh".Translate()))
                Component?.RequestRefresh();
            listing.CheckboxLabeled("ColonyOS_Notifications".Translate(), ref settings.CriticalNotifications);
            listing.GapLine();
            listing.Label("ColonyOS_Appearance".Translate());
            if (listing.ButtonTextLabeled("ColonyOS_Theme".Translate(), ThemeLabel(settings.Theme)))
                settings.Theme = (DashboardTheme)(((int)settings.Theme + 1) % 3);
            if (listing.ButtonTextLabeled("Accent", "Palette " + (settings.AccentPalette + 1)))
                settings.AccentPalette = (settings.AccentPalette + 1) % 4;
            listing.CheckboxLabeled("ColonyOS_Animations".Translate(), ref settings.Animations);
            listing.CheckboxLabeled("Chart grid", ref settings.ShowChartGrid);
            listing.GapLine();
            listing.Label("Warning thresholds");
            settings.BatteryWarningPct = listing.SliderLabeled("Battery reserve: " + settings.BatteryWarningPct.ToString("P0"), settings.BatteryWarningPct, 0.05f, 0.75f);
            settings.StorageWarningPct = listing.SliderLabeled("Storage pressure: " + settings.StorageWarningPct.ToString("P0"), settings.StorageWarningPct, 0.5f, 1f);
            settings.ForecastHorizonHours = Mathf.RoundToInt(listing.SliderLabeled("Forecast horizon: " + settings.ForecastHorizonHours + " h", settings.ForecastHorizonHours, 6, 720));
            listing.GapLine();
            listing.Label("Overview widgets");
            listing.CheckboxLabeled("Power", ref settings.ShowPowerWidget);
            listing.CheckboxLabeled("Wealth", ref settings.ShowWealthWidget);
            listing.CheckboxLabeled("Production", ref settings.ShowProductionWidget);
            listing.CheckboxLabeled("Regional market", ref settings.ShowMarketWidget);
            listing.CheckboxLabeled("Workforce", ref settings.ShowWorkforceWidget);
            listing.CheckboxLabeled("Food", ref settings.ShowFoodWidget);
            listing.GapLine();
            if (listing.ButtonText("ColonyOS_Reset".Translate()))
            {
                settings.ResetToDefaults();
                collectionBuffer = null;
                forecastBuffer = null;
                retentionBuffer = null;
            }
            listing.End();
            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            settings.Clamp();
            base.WriteSettings();
        }

        private static string ThemeLabel(DashboardTheme theme)
        {
            switch (theme)
            {
                case DashboardTheme.Midnight: return "Midnight";
                case DashboardTheme.NeutralProfessional: return "Neutral Professional";
                default: return "Dark Industrial";
            }
        }
    }
}
