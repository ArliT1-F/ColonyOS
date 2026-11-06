using System;
using ColonyOS.Analytics;
using ColonyOS.Core;
using ColonyOS.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public sealed partial class ColonyOSWindow : MainTabWindow
    {
        private Department department = Department.Overview;
        private DashboardTimeRange timeRange = DashboardTimeRange.Week;
        private Vector2 scroll;
        private float navigationStarted;
        private string selectedPredictionId;
        private string selectedGoodDef;
        private string selectedTraderKey;
        private EvidenceKind marketTab = EvidenceKind.Confirmed;
        private string marketSearch = string.Empty;
        private int marketSort;
        private string productionSearch = string.Empty;
        private int productionSort;
        private int transactionFilter;

        public override Vector2 RequestedTabSize => new Vector2(1380f, 840f);
        protected override float Margin => 0f;

        public ColonyOSWindow()
        {
            doWindowBackground = false;
            doCloseButton = false;
            doCloseX = false;
            preventCameraMotion = false;
            navigationStarted = Time.realtimeSinceStartup;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                ColonyTheme.Configure(ColonyOSMod.Settings);
                ThemePalette theme = ColonyTheme.Current;
                GUI.color = theme.Window;
                GUI.DrawTexture(inRect, BaseContent.WhiteTex);
                GUI.color = oldColor;

                float sidebarWidth = inRect.width < 1050f ? 176f : 208f;
                DrawSidebar(new Rect(inRect.x, inRect.y, sidebarWidth, inRect.height));
                Rect header = new Rect(inRect.x + sidebarWidth, inRect.y, inRect.width - sidebarWidth, 96f);
                DrawHeader(header);

                Rect workspace = new Rect(header.x + 24f, header.yMax + 2f, header.width - 48f, inRect.yMax - header.yMax - 22f);
                DashboardViewModel view = ColonyOSMod.Component?.Dashboard.Get(timeRange);
                if (view == null)
                {
                    UiDraw.EmptyState(workspace, "Preparing colony intelligence", "The first scheduled snapshot is being prepared. No placeholder metrics are shown.");
                    return;
                }

                float contentWidth = workspace.width - 18f;
                float contentHeight = ContentHeight(view, contentWidth);
                Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(workspace.height, contentHeight));
                float elapsed = Time.realtimeSinceStartup - navigationStarted;
                float transition = ColonyOSMod.Settings.Animations ? Mathf.Clamp01(elapsed / 0.18f) : 1f;
                viewRect.y = (1f - transition) * 7f;
                viewRect.height += 8f;
                Widgets.BeginScrollView(workspace, ref scroll, viewRect);
                DrawPage(view, viewRect);
                Widgets.EndScrollView();
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
            }
        }

        private void DrawSidebar(Rect rect)
        {
            ThemePalette theme = ColonyTheme.Current;
            GUI.color = theme.Sidebar;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            Widgets.DrawLineVertical(rect.xMax - 1f, rect.y, rect.height, theme.Border);

            UiDraw.Rounded(new Rect(rect.x + 18f, rect.y + 22f, 38f, 38f), theme.Accent);
            UiDraw.Label(new Rect(rect.x + 18f, rect.y + 22f, 38f, 38f), "C", theme.Window, GameFont.Medium, TextAnchor.MiddleCenter, true, false);
            UiDraw.Label(new Rect(rect.x + 66f, rect.y + 20f, rect.width - 78f, 25f), "COLONY OS", theme.Text, GameFont.Small, TextAnchor.MiddleLeft, true, false);
            UiDraw.Label(new Rect(rect.x + 66f, rect.y + 44f, rect.width - 78f, 18f), "INTELLIGENCE", theme.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Divider(new Rect(rect.x + 18f, rect.y + 80f, rect.width - 36f, 1f));
            UiDraw.Label(new Rect(rect.x + 18f, rect.y + 98f, rect.width - 36f, 18f), "COMMAND CENTRE", theme.Faint, GameFont.Tiny, TextAnchor.UpperLeft, true, false);

            float y = rect.y + 126f;
            foreach (Department item in Enum.GetValues(typeof(Department)))
            {
                Rect nav = new Rect(rect.x + 10f, y, rect.width - 20f, 44f);
                if (UiDraw.NavButton(nav, ShortDepartmentName(item), UiAssets.Icon(item), department == item))
                    Navigate(item);
                y += 48f;
            }

            int critical = 0;
            DashboardViewModel view = ColonyOSMod.Component?.Dashboard.Get(timeRange);
            if (view != null)
            {
                for (int i = 0; i < view.Predictions.Count; i++)
                {
                    if (view.Predictions[i].Severity >= PredictionSeverity.High)
                        critical++;
                }
            }
            if (critical > 0)
            {
                UiDraw.Pill(new Rect(rect.x + 18f, y + 6f, rect.width - 36f, 25f), critical + " high-priority signal(s)", theme.Danger);
                y += 40f;
            }

            ColonySample current = view?.Current;
            float footerY = rect.yMax - 82f;
            UiDraw.Divider(new Rect(rect.x + 18f, footerY - 12f, rect.width - 36f, 1f));
            UiDraw.Rounded(new Rect(rect.x + 18f, footerY + 3f, 7f, 7f), current == null ? theme.Warning : theme.Good);
            UiDraw.Label(new Rect(rect.x + 33f, footerY - 5f, rect.width - 48f, 22f), current == null ? "Waiting for snapshot" : "Collection active", theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Label(new Rect(rect.x + 18f, footerY + 22f, rect.width - 36f, 20f), "v0.1.0  •  LOCAL DATA", theme.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Label(new Rect(rect.x + 18f, footerY + 43f, rect.width - 36f, 20f), current == null ? "No save history yet" : current.MapCount + " player home map(s)", theme.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
        }

        private void DrawHeader(Rect rect)
        {
            ThemePalette theme = ColonyTheme.Current;
            GUI.color = theme.Panel;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width, theme.Border);
            UiDraw.Label(new Rect(rect.x + 25f, rect.y + 17f, rect.width * 0.45f, 31f), DepartmentTitle(department), theme.Text, GameFont.Medium, TextAnchor.UpperLeft, true, false);
            UiDraw.Label(new Rect(rect.x + 26f, rect.y + 51f, rect.width * 0.46f, 22f), DepartmentSubtitle(department), theme.Muted, GameFont.Tiny, TextAnchor.UpperLeft, false, false);

            float right = rect.xMax - 24f;
            Rect themeButton = new Rect(right - 36f, rect.y + 22f, 36f, 36f);
            if (UiDraw.Button(themeButton, string.Empty, UiAssets.Settings, false, true, "Cycle dashboard theme"))
            {
                ColonyOSMod.Settings.Theme = (DashboardTheme)(((int)ColonyOSMod.Settings.Theme + 1) % 3);
                ColonyOSMod.Settings.Write();
            }
            right -= 46f;
            Rect refresh = new Rect(right - 36f, rect.y + 22f, 36f, 36f);
            if (UiDraw.Button(refresh, string.Empty, UiAssets.Refresh, false, true, "Queue a fresh snapshot on the next game tick"))
                ColonyOSMod.Component?.RequestRefresh();
            right -= 54f;

            string[] labels = { "24h", "7d", "30d", "All" };
            for (int i = 3; i >= 0; i--)
            {
                Rect range = new Rect(right - 44f, rect.y + 24f, 44f, 32f);
                if (UiDraw.Button(range, labels[i], null, (int)timeRange == i))
                {
                    timeRange = (DashboardTimeRange)i;
                    scroll = Vector2.zero;
                }
                right -= 48f;
            }
            DashboardViewModel view = ColonyOSMod.Component?.Dashboard.Get(timeRange);
            if (view?.Current != null)
                UiDraw.Label(new Rect(right - 180f, rect.y + 29f, 170f, 22f), "Snapshot tick " + view.Current.Tick, theme.Faint, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
        }

        private void DrawPage(DashboardViewModel view, Rect rect)
        {
            switch (department)
            {
                case Department.Power: DrawPowerPage(view, rect); break;
                case Department.Production: DrawProductionPage(view, rect); break;
                case Department.Logistics: DrawLogisticsPage(view, rect); break;
                case Department.Population: DrawPopulationPage(view, rect); break;
                case Department.Economy: DrawEconomyPage(view, rect); break;
                case Department.RegionalMarket: DrawMarketPage(view, rect); break;
                case Department.Predictive: DrawPredictivePage(view, rect); break;
                default: DrawOverviewPage(view, rect); break;
            }
        }

        private void Navigate(Department next)
        {
            if (department == next)
                return;
            department = next;
            scroll = Vector2.zero;
            navigationStarted = Time.realtimeSinceStartup;
        }

        private void OpenPrediction(Prediction prediction)
        {
            selectedPredictionId = prediction?.Id;
            Navigate(Department.Predictive);
        }

        private static string ShortDepartmentName(Department value)
        {
            switch (value)
            {
                case Department.Overview: return "Overview";
                case Department.Power: return "Power";
                case Department.Production: return "Production";
                case Department.Logistics: return "Logistics";
                case Department.Population: return "Population";
                case Department.Economy: return "Economy";
                case Department.RegionalMarket: return "Regional market";
                case Department.Predictive: return "Predictive";
                default: return value.ToString();
            }
        }

        private static string DepartmentTitle(Department value)
        {
            return value == Department.Overview ? "Colony command centre" : ShortDepartmentName(value) + " intelligence";
        }

        private static string DepartmentSubtitle(Department value)
        {
            switch (value)
            {
                case Department.Overview: return "Live operational picture, trends and priorities across every department.";
                case Department.Power: return "Measured grid output, load, storage and network-level performance.";
                case Department.Production: return "Bills, workstations, ingredients and recorded completion activity.";
                case Department.Logistics: return "Counted resources, food reserves and stockpile-slot pressure.";
                case Department.Population: return "Skills, work priorities and production labour availability.";
                case Department.Economy: return "Wealth composition, measured trends and actual trade transactions.";
                case Department.RegionalMarket: return "What observed merchants can buy — confirmed, historical and estimated.";
                case Department.Predictive: return "Traceable warnings and opportunities generated from shared evidence.";
                default: return string.Empty;
            }
        }
    }
}
