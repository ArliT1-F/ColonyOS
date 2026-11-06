using System;
using System.Collections.Generic;
using ColonyOS.Analytics;
using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public sealed partial class ColonyOSWindow
    {
        private void DrawProductionPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null) { UiDraw.EmptyState(rect, "No production snapshot", "Workstations and bills will appear after collection."); return; }
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int columns = rect.width > 850f ? 4 : 2;
            int unmanned = 0;
            for (int i = 0; i < current.Workbenches.Count; i++)
                if (current.Workbenches[i].Usable && !current.Workbenches[i].Manned && current.Workbenches[i].ActiveBillCount > 0) unmanned++;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 122f), "Active bills", current.ActiveBills.ToString(), current.TotalWorkbenches + " workstation(s)", "Current queues", theme.Accent2, Department.Production, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 122f), "Usable stations", current.UsableWorkbenches + "/" + current.TotalWorkbenches.ToString(), current.MannedWorkbenches + " manned now", "Point-in-time status", theme.Good, Department.Production, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 122f), "Awaiting a worker", unmanned.ToString(), "Usable + active bills", "Not an uptime metric", unmanned > 0 ? theme.Warning : theme.Accent, Department.Production, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 3, 122f), "Completed iterations", current.ProductionIterationsLastDay + "/day", "Actual observed events", "Rolling 24 in-game hours", theme.Accent, Department.Production, view.ProductionSeries, null);
            y += Mathf.CeilToInt(4f / columns) * 134f + 8f;

            y = SplitPanels(rect, y, 286f, 0.61f, out Rect output, out Rect recipes);
            Rect outputInner = UiDraw.Card(output, "Production output history", "One event per completed bill iteration; variable item yields are not guessed");
            Charts.VerticalBars(outputInner, view.ProductionSeries, theme.Accent2);
            Rect recipesInner = UiDraw.Card(recipes, "Completion leaderboard", "Most frequently completed recipes in the selected range");
            Charts.RankingBars(recipesInner, view.RecipeLeaderboard, "No completions recorded", "Finish a production bill with ColonyOS active to begin this history.");
            y += 14f;

            Rect controls = new Rect(rect.x, y, rect.width, 40f);
            productionSearch = UiDraw.Search(new Rect(controls.x, controls.y, Mathf.Min(390f, controls.width * 0.52f), 34f), productionSearch, "Search workstations or recipes");
            float buttonX = controls.xMax - 168f;
            if (UiDraw.Button(new Rect(buttonX, controls.y, 168f, 34f), "Sort: " + (productionSort == 0 ? "name" : productionSort == 1 ? "active bills" : "unmanned first")))
                productionSort = (productionSort + 1) % 3;
            y += 50f;

            List<WorkbenchSnapshot> tables = FilteredWorkbenches(view);
            if (tables.Count == 0)
            {
                UiDraw.EmptyState(new Rect(rect.x, y, rect.width, 260f), "No matching workstations", "Change the search or build a production workstation.");
                return;
            }
            int cardColumns = rect.width > 1050f ? 3 : rect.width > 680f ? 2 : 1;
            for (int i = 0; i < tables.Count; i++)
            {
                Rect card = UiDraw.GridCell(new Rect(rect.x, y, rect.width, rect.height), cardColumns, i, 178f, 12f);
                DrawWorkbenchCard(card, tables[i]);
            }
            y += Mathf.CeilToInt((float)tables.Count / cardColumns) * 190f;
            UiDraw.Label(new Rect(rect.x, y + 4f, rect.width, 42f), "Ingredient quantities use RimWorld's counted resource readout. Flexible filters show counted presence, not an exact equivalent-unit requirement. ‘Awaiting worker’ is a snapshot observation, not historical utilization.", theme.Faint, GameFont.Tiny);
        }

        private static void DrawWorkbenchCard(Rect rect, WorkbenchSnapshot table)
        {
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Panel(rect, false, Mouse.IsOver(rect));
            Color statusColor = !table.Usable ? theme.Danger : table.Manned ? theme.Good : table.ActiveBillCount > 0 ? theme.Warning : theme.Muted;
            UiDraw.Rounded(new Rect(rect.x + 14f, rect.y + 14f, 34f, 34f), ColonyTheme.Alpha(statusColor, 0.15f));
            UiDraw.Icon(new Rect(rect.x + 21f, rect.y + 21f, 20f, 20f), UiAssets.Production, statusColor);
            UiDraw.Label(new Rect(rect.x + 58f, rect.y + 11f, rect.width - 72f, 23f), table.Label, theme.Text, GameFont.Small, TextAnchor.MiddleLeft, true, false);
            UiDraw.Label(new Rect(rect.x + 58f, rect.y + 34f, rect.width - 72f, 18f), ProductionStatus(table), statusColor, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Divider(new Rect(rect.x + 14f, rect.y + 59f, rect.width - 28f, 1f));
            string recipe = string.IsNullOrEmpty(table.CurrentRecipe) ? "No active production bill" : table.CurrentRecipe;
            UiDraw.Label(new Rect(rect.x + 14f, rect.y + 67f, rect.width - 28f, 21f), recipe, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Label(new Rect(rect.x + 14f, rect.y + 89f, rect.width - 28f, 18f), table.ActiveBillCount + "/" + table.BillCount + " bills active  •  " + table.QualifiedAvailablePawns + " qualified", theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);

            int shown = Math.Min(2, table.Ingredients.Count);
            for (int i = 0; i < shown; i++)
            {
                IngredientSnapshot ingredient = table.Ingredients[i];
                float y = rect.y + 114f + i * 24f;
                Color color = ingredient.Missing ? theme.Danger : ingredient.Available >= 0f ? theme.Good : theme.Muted;
                UiDraw.Rounded(new Rect(rect.x + 14f, y + 7f, 6f, 6f), color);
                string quantity = ingredient.Available < 0f ? "not in counted readout" : ingredient.ExactQuantity
                    ? UiFormat.Compact(ingredient.Available) + " / " + UiFormat.Compact(ingredient.Required)
                    : UiFormat.Compact(ingredient.Available) + " counted (flexible)";
                UiDraw.Label(new Rect(rect.x + 27f, y, rect.width * 0.55f - 27f, 20f), ingredient.Label, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.56f, y, rect.width * 0.4f - 12f, 20f), quantity, color, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            }
            if (table.Ingredients.Count > 2)
                UiDraw.Label(new Rect(rect.x + 14f, rect.yMax - 21f, rect.width - 28f, 17f), "+ " + (table.Ingredients.Count - 2) + " more ingredient filter(s)", theme.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
        }
    }
}
