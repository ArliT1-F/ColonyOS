using System;
using System.Collections.Generic;
using ColonyOS.Analytics;
using ColonyOS.Core;
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
            bool compact = width < 700f;
            switch (department)
            {
                case Department.Production:
                    int columns = width > 1050f ? 3 : width > 680f ? 2 : 1;
                    int tables = FilteredWorkbenches(view).Count;
                    return 760f + Mathf.CeilToInt((float)Math.Max(1, tables) / columns) * 190f + (compact ? 300f : 0f);
                case Department.Logistics:
                    int zoneColumns = width > 1050f ? 4 : width > 680f ? 3 : 2;
                    int zones = view.Current?.StorageZones.Count ?? 0;
                    return 1030f + Mathf.CeilToInt((float)Math.Max(1, Math.Min(24, zones)) / zoneColumns) * 106f + (compact ? 600f : 0f);
                case Department.RegionalMarket: return 1190f + (compact ? 1100f : 0f);
                case Department.Predictive:
                    int predictionListHeight = view.Predictions.Count == 0 ? 480 : view.Predictions.Count * 122;
                    return compact ? 1230f + predictionListHeight : Math.Max(1000f, 180f + view.Predictions.Count * 122f);
                case Department.Economy: return 1390f + (compact ? 580f : 0f);
                case Department.Population:
                    int skillColumns = width > 1050f ? 4 : width > 680f ? 3 : 2;
                    return 850f + Mathf.CeilToInt(12f / skillColumns) * 108f + (compact ? 320f : 0f);
                case Department.Power: return 1130f + (compact ? 900f : 0f);
                default:
                    int kpiColumns = width >= 1050f ? 4 : width >= 700f ? 3 : 2;
                    return 1600f + Mathf.CeilToInt(8f / kpiColumns) * 140f + (compact ? 1250f : 0f);
            }
        }

        private static float SplitPanels(Rect content, float y, float height, float leftShare, out Rect left, out Rect right)
        {
            if (content.width < 700f)
            {
                left = new Rect(content.x, y, content.width, height);
                right = new Rect(content.x, y + height + 14f, content.width, height);
                return y + (height + 14f) * 2f;
            }
            left = new Rect(content.x, y, content.width * leftShare - 7f, height);
            right = new Rect(left.xMax + 14f, y, content.xMax - left.xMax - 14f, height);
            return y + height + 14f;
        }

        private static float PercentChange(IReadOnlyList<MetricPoint> points)
        {
            return TrendAnalyzer.PercentChange(points);
        }

        private static List<ChartSeriesModel> OneSeries(DashboardViewModel view, SampleMetric metric, string label, Color color)
        {
            return new List<ChartSeriesModel> { new ChartSeriesModel(label, view.GetSeries(metric), color) };
        }

        private static List<ChartSeriesModel> ProjectionSeries(DashboardViewModel view, SampleMetric metric, ForecastLine forecast, float threshold, string label, Color color)
        {
            IReadOnlyList<MetricPoint> history = view.GetSeries(metric);
            List<ChartSeriesModel> result = new List<ChartSeriesModel> { new ChartSeriesModel(label, history, color) };
            if (forecast == null || !forecast.Available || history.Count == 0)
                return result;
            float delta = forecast.TargetAtTick - history[history.Count - 1].Tick;
            if (delta <= 0f || delta > ColonyOSMod.Settings.ForecastHorizonHours * 2500f || forecast.TargetAtTick >= int.MaxValue)
                return result;
            List<MetricPoint> projection = new List<MetricPoint>
            {
                history[history.Count - 1],
                new MetricPoint((int)forecast.TargetAtTick, threshold)
            };
            string title = "Estimated • " + UiFormat.Duration((int)delta) + " • R² " + forecast.RSquared.ToString("0.00");
            result.Add(new ChartSeriesModel(title, projection, ColonyTheme.Alpha(color, 0.8f), true));
            return result;
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
