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
        private void DrawLogisticsPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null) { UiDraw.EmptyState(rect, "No logistics snapshot", "Counted resources and stockpiles will appear after collection."); return; }
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int columns = rect.width > 850f ? 4 : 2;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 122f), "Stockpile cells", current.StorageCellCount.ToString(), current.StorageZones.Count + " zone(s)", "Zones only", theme.Accent2, Department.Logistics, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 122f), "Estimated free slots", current.StorageFreeSlotEstimate.ToString(), current.StorageUtilization.ToString("P0") + " occupied", "RimWorld zone estimate", current.StorageUtilization >= 0.9f ? theme.Warning : theme.Accent, Department.Logistics, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 122f), "Counted resources", current.Resources.Count.ToString(), "Distinct ThingDefs", "Eligible storage", theme.Purple, Department.Logistics, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 3, 122f), "Human-edible nutrition", UiFormat.Compact(current.FoodNutrition), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.FoodNutrition))), "Counted food only", current.FoodNutrition <= 0f ? theme.Danger : theme.Good, Department.Logistics, view.GetSeries(SampleMetric.FoodNutrition), null);
            y += Mathf.CeilToInt(4f / columns) * 134f + 8f;

            Rect food = new Rect(rect.x, y, rect.width * 0.62f - 7f, 282f);
            Rect foodInner = UiDraw.Card(food, "Food reserve trajectory", "Historical projection is generated only from a significant measured trend");
            Charts.LineChart(foodInner, OneSeries(view, SampleMetric.FoodNutrition, "Counted nutrition", theme.Good));
            Rect pressure = new Rect(food.xMax + 14f, y, rect.xMax - food.xMax - 14f, 282f);
            Rect pressureInner = UiDraw.Card(pressure, "Storage capacity pressure", "Cell utilization — not a universal item-capacity claim");
            UiDraw.Label(new Rect(pressureInner.x, pressureInner.y + 10f, pressureInner.width, 48f), current.StorageUtilization.ToString("P0"), theme.Text, GameFont.Medium, TextAnchor.MiddleCenter, true, false);
            UiDraw.Progress(new Rect(pressureInner.x + 8f, pressureInner.y + 68f, pressureInner.width - 16f, 15f), current.StorageUtilization, current.StorageUtilization >= 0.9f ? theme.Warning : theme.Accent);
            DrawStatRow(new Rect(pressureInner.x, pressureInner.y + 102f, pressureInner.width, 27f), "Estimated occupied", (current.StorageCellCount - current.StorageFreeSlotEstimate).ToString(), theme.Text);
            DrawStatRow(new Rect(pressureInner.x, pressureInner.y + 130f, pressureInner.width, 27f), "Estimated free", current.StorageFreeSlotEstimate.ToString(), theme.Text);
            DrawStatRow(new Rect(pressureInner.x, pressureInner.y + 158f, pressureInner.width, 27f), "Zones tracked", current.StorageZones.Count.ToString(), theme.Muted);
            y += 296f;

            Rect counts = new Rect(rect.x, y, rect.width * 0.45f - 7f, 292f);
            Rect countsInner = UiDraw.Card(counts, "Resource availability", "Largest counted stacks by quantity");
            Charts.RankingBars(countsInner, ResourceCounts(current), "No counted resources", "Resources inside eligible storage will appear here.");
            Rect distribution = new Rect(counts.xMax + 14f, y, rect.xMax - counts.xMax - 14f, 292f);
            Rect distributionInner = UiDraw.Card(distribution, "Resource value distribution", "Base market value × quantity; not an offer price");
            Charts.Donut(distributionInner, ResourceValues(current, 6), "base value");
            y += 306f;

            UiDraw.Label(new Rect(rect.x, y, rect.width, 25f), "Stockpile pressure heatmap", theme.Text, GameFont.Small, TextAnchor.UpperLeft, true, false);
            y += 32f;
            if (current.StorageZones.Count == 0)
            {
                UiDraw.EmptyState(new Rect(rect.x, y, rect.width, 190f), "No stockpile zones", "Create a stockpile zone to enable slot-pressure monitoring. Shelves and modded containers are not included in this estimate.");
                y += 204f;
            }
            else
            {
                int zoneColumns = rect.width > 1050f ? 4 : rect.width > 680f ? 3 : 2;
                int count = Math.Min(24, current.StorageZones.Count);
                for (int i = 0; i < count; i++)
                {
                    StorageZoneSnapshot zone = current.StorageZones[i];
                    Rect cell = UiDraw.GridCell(new Rect(rect.x, y, rect.width, rect.height), zoneColumns, i, 96f, 10f);
                    Color color = zone.Utilization >= 0.95f ? theme.Danger : zone.Utilization >= 0.85f ? theme.Warning : theme.Accent;
                    UiDraw.Rounded(cell, ColonyTheme.Alpha(color, 0.08f + zone.Utilization * 0.12f));
                    UiDraw.Rounded(cell.ContractedBy(1f), ColonyTheme.Alpha(theme.Panel, 0.82f));
                    UiDraw.Label(new Rect(cell.x + 11f, cell.y + 8f, cell.width - 22f, 21f), zone.Label, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                    UiDraw.Label(new Rect(cell.x + 11f, cell.y + 29f, cell.width - 22f, 17f), zone.MapLabel, theme.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                    UiDraw.Progress(new Rect(cell.x + 11f, cell.y + 54f, cell.width - 22f, 8f), zone.Utilization, color);
                    UiDraw.Label(new Rect(cell.x + 11f, cell.y + 66f, cell.width - 22f, 20f), zone.FreeSlots + " free / " + zone.Cells + " cells", color, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                }
                y += Mathf.CeilToInt((float)count / zoneColumns) * 106f;
            }

            UiDraw.Label(new Rect(rect.x, y + 8f, rect.width, 25f), "Resource locations", theme.Text, GameFont.Small, TextAnchor.UpperLeft, true, false);
            y += 40f;
            int locationCount = Math.Min(6, current.Resources.Count);
            for (int i = 0; i < locationCount; i++)
            {
                ResourceSnapshot resource = current.Resources[i];
                Rect row = new Rect(rect.x, y + i * 31f, rect.width, 28f);
                UiDraw.Rounded(row, ColonyTheme.Alpha(theme.Grid, 0.4f));
                UiDraw.Label(new Rect(row.x + 10f, row.y, row.width * 0.28f, row.height), resource.Label, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.3f, row.y, row.width * 0.18f, row.height), resource.Count.ToString(), theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.49f, row.y, row.width * 0.48f, row.height), string.Join(", ", resource.MapLabels), theme.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            }
        }
    }
}
