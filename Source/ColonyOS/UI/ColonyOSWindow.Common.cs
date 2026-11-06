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
        private int workbenchCacheRevision = -1;
        private string workbenchCacheQuery;
        private int workbenchCacheSort;
        private List<WorkbenchSnapshot> workbenchCache = new List<WorkbenchSnapshot>();

        private float ContentHeight(DashboardViewModel view, float width)
        {
            switch (department)
            {
                case Department.Production:
                    int columns = width > 1050f ? 3 : width > 680f ? 2 : 1;
                    int tables = FilteredWorkbenches(view).Count;
                    return 690f + Mathf.CeilToInt((float)Math.Max(1, tables) / columns) * 190f + 70f;
                case Department.Logistics:
                    int zoneColumns = width > 1050f ? 4 : width > 680f ? 3 : 2;
                    int zones = view.Current?.StorageZones.Count ?? 0;
                    return 1030f + Mathf.CeilToInt((float)Math.Max(1, Math.Min(24, zones)) / zoneColumns) * 106f;
                case Department.RegionalMarket: return 1190f;
                case Department.Predictive: return Math.Max(1000f, 180f + view.Predictions.Count * 122f);
                case Department.Economy: return 1390f;
                case Department.Population:
                    int skillColumns = width > 1050f ? 4 : width > 680f ? 3 : 2;
                    return 850f + Mathf.CeilToInt(12f / skillColumns) * 108f;
                case Department.Power: return 1130f;
                default:
                    int kpiColumns = width >= 1050f ? 4 : width >= 700f ? 3 : 2;
                    return 1300f + Mathf.CeilToInt(8f / kpiColumns) * 140f;
            }
        }

        private static float PercentChange(IReadOnlyList<MetricPoint> points)
        {
            return TrendAnalyzer.PercentChange(points);
        }

        private static List<ChartSeriesModel> OneSeries(DashboardViewModel view, SampleMetric metric, string label, Color color)
        {
            return new List<ChartSeriesModel> { new ChartSeriesModel(label, view.GetSeries(metric), color) };
        }

        private static List<NamedValue> ResourceCounts(ColonySample current, int maximum = 8)
        {
            List<NamedValue> values = new List<NamedValue>();
            if (current == null)
                return values;
            int count = Math.Min(maximum, current.Resources.Count);
            for (int i = 0; i < count; i++)
            {
                ResourceSnapshot resource = current.Resources[i];
                values.Add(new NamedValue(resource.DefName, resource.Label, resource.Count));
            }
            return values;
        }

        private static List<NamedValue> ResourceValues(ColonySample current, int maximum = 8)
        {
            List<NamedValue> values = new List<NamedValue>();
            if (current == null)
                return values;
            for (int i = 0; i < current.Resources.Count; i++)
            {
                ResourceSnapshot resource = current.Resources[i];
                float value = resource.Count * resource.BaseMarketValue;
                if (value <= 0f)
                    continue;
                values.Add(new NamedValue(resource.DefName, resource.Label, value));
            }
            values.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (values.Count > maximum)
                values.RemoveRange(maximum, values.Count - maximum);
            return values;
        }

        private static List<NamedValue> WealthComposition(ColonySample current)
        {
            List<NamedValue> values = new List<NamedValue>();
            if (current == null)
                return values;
            values.Add(new NamedValue("items", "Items", current.WealthItems));
            values.Add(new NamedValue("buildings", "Buildings & floors", current.WealthBuildings));
            values.Add(new NamedValue("pawns", "Pawns", current.WealthPawns));
            return values;
        }

        private void DrawPredictionRows(Rect rect, IReadOnlyList<Prediction> predictions, int maximum)
        {
            if (predictions == null || predictions.Count == 0)
            {
                UiDraw.EmptyState(rect, "No active warnings", "The shared forecast engine has not found enough measured evidence for a warning or opportunity.");
                return;
            }
            int count = Math.Min(maximum, predictions.Count);
            float rowHeight = rect.height / count;
            for (int i = 0; i < count; i++)
            {
                Prediction prediction = predictions[i];
                Rect row = new Rect(rect.x, rect.y + i * rowHeight, rect.width, rowHeight - 8f);
                bool hover = Mouse.IsOver(row);
                UiDraw.Rounded(row, hover ? ColonyTheme.Current.PanelRaised : ColonyTheme.Alpha(ColonyTheme.Current.Grid, 0.45f));
                UiDraw.Rounded(new Rect(row.x + 10f, row.y + 12f, 4f, row.height - 24f), ColonyTheme.Current.Severity(prediction.Severity));
                UiDraw.Label(new Rect(row.x + 23f, row.y + 8f, row.width - 115f, 23f), prediction.Title, ColonyTheme.Current.Text, GameFont.Small, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(row.x + 23f, row.y + 32f, row.width - 35f, row.height - 38f), prediction.Explanation, ColonyTheme.Current.Muted, GameFont.Tiny);
                UiDraw.Pill(new Rect(row.xMax - 84f, row.y + 10f, 72f, 20f), prediction.Severity.ToString(), ColonyTheme.Current.Severity(prediction.Severity));
                if (Widgets.ButtonInvisible(row))
                    OpenPrediction(prediction);
            }
        }

        private static void DrawStatRow(Rect rect, string label, string value, Color valueColor)
        {
            UiDraw.Label(new Rect(rect.x, rect.y, rect.width * 0.62f, rect.height), label, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            UiDraw.Label(new Rect(rect.x + rect.width * 0.62f, rect.y, rect.width * 0.38f, rect.height), value, valueColor, GameFont.Tiny, TextAnchor.MiddleRight, true, false);
        }

        private List<WorkbenchSnapshot> FilteredWorkbenches(DashboardViewModel view)
        {
            string query = productionSearch?.Trim() ?? string.Empty;
            if (workbenchCacheRevision == view.Revision && workbenchCacheQuery == query && workbenchCacheSort == productionSort)
                return workbenchCache;
            List<WorkbenchSnapshot> result = new List<WorkbenchSnapshot>();
            if (view.Current == null)
                return result;
            for (int i = 0; i < view.Current.Workbenches.Count; i++)
            {
                WorkbenchSnapshot table = view.Current.Workbenches[i];
                if (query.Length > 0 && (table.Label?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) < 0
                    && (table.CurrentRecipe?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                    continue;
                result.Add(table);
            }
            result.Sort((a, b) =>
            {
                if (productionSort == 1)
                    return b.ActiveBillCount.CompareTo(a.ActiveBillCount);
                if (productionSort == 2)
                    return a.Manned.CompareTo(b.Manned);
                return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
            });
            workbenchCache = result;
            workbenchCacheRevision = view.Revision;
            workbenchCacheQuery = query;
            workbenchCacheSort = productionSort;
            return workbenchCache;
        }

        private static string ProductionStatus(WorkbenchSnapshot table)
        {
            if (!table.Usable) return "Unavailable";
            if (table.Manned) return "Manned now";
            if (table.ActiveBillCount > 0) return "Awaiting worker";
            return "No active bill";
        }
    }
}
