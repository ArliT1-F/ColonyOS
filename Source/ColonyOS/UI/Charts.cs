using System;
using System.Collections.Generic;
using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public sealed class ChartSeriesModel
    {
        public string Label;
        public IReadOnlyList<MetricPoint> Points;
        public Color Color;
        public bool Estimated;

        public ChartSeriesModel(string label, IReadOnlyList<MetricPoint> points, Color color, bool estimated = false)
        {
            Label = label;
            Points = points;
            Color = color;
            Estimated = estimated;
        }
    }

    public static class Charts
    {
        public static void Kpi(Rect rect, string title, string value, string delta, string status, Color statusColor, Department department, IReadOnlyList<MetricPoint> sparkline, Action navigate)
        {
            bool hover = Mouse.IsOver(rect);
            UiDraw.Panel(rect, false, hover);
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Label(new Rect(rect.x + 15f, rect.y + 13f, rect.width - 55f, 20f), title, theme.Muted, GameFont.Tiny, TextAnchor.UpperLeft, false, false);
            UiDraw.Icon(new Rect(rect.xMax - 36f, rect.y + 12f, 20f, 20f), UiAssets.Icon(department), ColonyTheme.Alpha(statusColor, 0.9f));
            UiDraw.Label(new Rect(rect.x + 15f, rect.y + 35f, rect.width - 30f, 35f), value, theme.Text, GameFont.Medium, TextAnchor.UpperLeft, true, false);
            if (!string.IsNullOrEmpty(delta))
                UiDraw.Label(new Rect(rect.x + 15f, rect.y + 72f, rect.width * 0.48f, 20f), delta, statusColor, GameFont.Tiny, TextAnchor.UpperLeft, true, false);
            if (!string.IsNullOrEmpty(status))
                UiDraw.Label(new Rect(rect.x + rect.width * 0.46f, rect.y + 72f, rect.width * 0.48f - 12f, 20f), status, theme.Muted, GameFont.Tiny, TextAnchor.UpperRight, false, false);
            if (sparkline != null && sparkline.Count > 1)
                Sparkline(new Rect(rect.x + 15f, rect.yMax - 34f, rect.width - 30f, 23f), sparkline, statusColor);
            else
                UiDraw.Label(new Rect(rect.x + 15f, rect.yMax - 31f, rect.width - 30f, 18f), "Awaiting history", theme.Faint, GameFont.Tiny, TextAnchor.UpperLeft, false, false);
            if (navigate != null)
            {
                TooltipHandler.TipRegion(rect, "Open " + title + " details");
                if (Widgets.ButtonInvisible(rect))
                    navigate();
            }
        }

        public static void Sparkline(Rect rect, IReadOnlyList<MetricPoint> points, Color color)
        {
            if (points == null || points.Count < 2)
                return;
            MinMax(points, false, out float min, out float max);
            float range = Mathf.Max(0.0001f, max - min);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 a = Point(rect, i - 1, points.Count, points[i - 1].Value, min, range);
                Vector2 b = Point(rect, i, points.Count, points[i].Value, min, range);
                Widgets.DrawLine(a, b, ColonyTheme.Alpha(color, 0.85f), 1.5f);
            }
        }

        public static void LineChart(Rect rect, IList<ChartSeriesModel> series, string unit = "", bool includeZero = true)
        {
            if (series == null || series.Count == 0 || !HasSamples(series, 1))
            {
                UiDraw.EmptyState(rect, "No measurements yet", "The chart will use real collected snapshots when they become available.");
                return;
            }

            ThemePalette theme = ColonyTheme.Current;
            Rect plot = new Rect(rect.x + 46f, rect.y + 12f, rect.width - 58f, rect.height - 48f);
            bool multiple = HasSamples(series, 2);
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int s = 0; s < series.Count; s++)
            {
                IReadOnlyList<MetricPoint> points = series[s].Points;
                if (points == null)
                    continue;
                for (int i = 0; i < points.Count; i++)
                {
                    min = Mathf.Min(min, points[i].Value);
                    max = Mathf.Max(max, points[i].Value);
                }
            }
            if (includeZero && min >= 0f)
                min = 0f;
            if (max - min < 0.0001f)
            {
                max += 1f;
                min -= min > 0f ? 0.5f : 1f;
            }
            float padding = (max - min) * 0.08f;
            max += padding;
            if (min < 0f || !includeZero)
                min -= padding;
            float range = Mathf.Max(0.0001f, max - min);

            if (ColonyOS.Core.ColonyOSMod.Settings.ShowChartGrid)
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.y + plot.height * i / 4f;
                    Widgets.DrawLineHorizontal(plot.x, y, plot.width, ColonyTheme.Alpha(theme.Grid, 0.75f));
                    float value = max - range * i / 4f;
                    UiDraw.Label(new Rect(rect.x, y - 9f, 40f, 18f), UiFormat.Axis(value), theme.Faint, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                }
            }

            for (int s = 0; s < series.Count; s++)
            {
                ChartSeriesModel line = series[s];
                if (line.Points == null || line.Points.Count == 0)
                    continue;
                if (line.Points.Count == 1)
                {
                    Vector2 center = Point(plot, 0, 1, line.Points[0].Value, min, range);
                    UiDraw.Rounded(new Rect(center.x - 3f, center.y - 3f, 6f, 6f), line.Color);
                    continue;
                }
                for (int i = 1; i < line.Points.Count; i++)
                {
                    Vector2 a = Point(plot, i - 1, line.Points.Count, line.Points[i - 1].Value, min, range);
                    Vector2 b = Point(plot, i, line.Points.Count, line.Points[i].Value, min, range);
                    if (line.Estimated)
                        DrawDashed(a, b, line.Color);
                    else
                        Widgets.DrawLine(a, b, line.Color, 2f);
                }
            }

            float legendX = plot.x;
            for (int s = 0; s < series.Count; s++)
            {
                float width = Mathf.Min(150f, 24f + series[s].Label.Length * 6.5f);
                UiDraw.Rounded(new Rect(legendX, rect.yMax - 20f, 7f, 7f), series[s].Color);
                UiDraw.Label(new Rect(legendX + 12f, rect.yMax - 27f, width, 22f), series[s].Label, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                legendX += width + 18f;
            }

            if (!multiple)
                UiDraw.Label(new Rect(plot.x + 10f, plot.y + 8f, plot.width - 20f, 20f), "One measured sample — trend unavailable", theme.Muted, GameFont.Tiny, TextAnchor.UpperCenter, false, false);

            if (Mouse.IsOver(plot))
                DrawChartTooltip(plot, series, min, range, unit);
        }

        public static void RankingBars(Rect rect, IReadOnlyList<NamedValue> values, string emptyTitle, string emptyExplanation, string suffix = "")
        {
            if (values == null || values.Count == 0)
            {
                UiDraw.EmptyState(rect, emptyTitle, emptyExplanation);
                return;
            }
            float maximum = 0f;
            for (int i = 0; i < values.Count; i++)
                maximum = Mathf.Max(maximum, values[i].Value);
            if (maximum <= 0f)
                maximum = 1f;
            float rows = Mathf.Min(values.Count, Mathf.Max(1, Mathf.FloorToInt(rect.height / 27f)));
            float rowHeight = rect.height / rows;
            for (int i = 0; i < rows; i++)
            {
                NamedValue value = values[i];
                float y = rect.y + i * rowHeight;
                UiDraw.Label(new Rect(rect.x, y, rect.width * 0.43f - 8f, rowHeight), value.Label, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                Rect bar = new Rect(rect.x + rect.width * 0.43f, y + rowHeight * 0.31f, rect.width * 0.39f, Mathf.Max(5f, rowHeight * 0.28f));
                UiDraw.Progress(bar, value.Value / maximum, ColonyTheme.Current.Series(i));
                UiDraw.Label(new Rect(rect.x + rect.width * 0.84f, y, rect.width * 0.16f, rowHeight), UiFormat.Compact(value.Value, suffix), ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                TooltipHandler.TipRegion(new Rect(rect.x, y, rect.width, rowHeight), value.Label + ": " + value.Value.ToString("0.##") + suffix);
            }
        }

        public static void VerticalBars(Rect rect, IReadOnlyList<MetricPoint> points, Color color, string valueSuffix = "")
        {
            if (points == null || points.Count == 0)
            {
                UiDraw.EmptyState(rect, "No production records", "Completed bill iterations will appear here after they are observed.");
                return;
            }
            float max = 1f;
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                max = Mathf.Max(max, points[i].Value);
                total += points[i].Value;
            }
            Rect plot = new Rect(rect.x + 5f, rect.y + 10f, rect.width - 10f, rect.height - 38f);
            float gap = 3f;
            float width = Mathf.Max(2f, (plot.width - gap * (points.Count - 1)) / points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                float height = plot.height * points[i].Value / max;
                Rect bar = new Rect(plot.x + i * (width + gap), plot.yMax - height, width, height);
                if (height > 0.1f)
                    UiDraw.Rounded(bar, ColonyTheme.Alpha(color, 0.85f));
                TooltipHandler.TipRegion(new Rect(bar.x, plot.y, bar.width, plot.height), UiFormat.TickAge(points[i].Tick, points[points.Count - 1].Tick) + ": " + points[i].Value.ToString("0") + valueSuffix);
            }
            Widgets.DrawLineHorizontal(plot.x, plot.yMax, plot.width, ColonyTheme.Current.Grid);
            UiDraw.Label(new Rect(rect.x, rect.yMax - 23f, rect.width, 20f), total.ToString("0") + " completed bill iteration(s) in this range", ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleCenter, false, false);
        }

        public static void Donut(Rect rect, IReadOnlyList<NamedValue> values, string centerLabel)
        {
            float total = 0f;
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                    total += Mathf.Max(0f, values[i].Value);
            }
            if (total <= 0f)
            {
                UiDraw.EmptyState(rect, "No distribution to display", "A distribution appears only when real non-zero measurements are available.");
                return;
            }

            float diameter = Mathf.Min(rect.height - 10f, rect.width * 0.5f, 170f);
            Vector2 center = new Vector2(rect.x + diameter / 2f + 10f, rect.y + rect.height / 2f);
            float radius = diameter * 0.39f;
            const int segments = 72;
            int valueIndex = 0;
            float cumulative = values[0].Value;
            for (int segment = 0; segment < segments; segment++)
            {
                float fraction = (segment + 0.5f) / segments;
                while (valueIndex < values.Count - 1 && fraction * total > cumulative)
                    cumulative += Mathf.Max(0f, values[++valueIndex].Value);
                float a0 = ((float)segment / segments * 360f - 90f) * Mathf.Deg2Rad;
                float a1 = ((float)(segment + 1) / segments * 360f - 90f) * Mathf.Deg2Rad;
                Vector2 start = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
                Vector2 end = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                Widgets.DrawLine(start, end, ColonyTheme.Current.Series(valueIndex), Mathf.Max(6f, diameter * 0.09f));
            }
            UiDraw.Label(new Rect(center.x - radius * 0.65f, center.y - 19f, radius * 1.3f, 24f), UiFormat.Compact(total), ColonyTheme.Current.Text, GameFont.Small, TextAnchor.MiddleCenter, true, false);
            UiDraw.Label(new Rect(center.x - radius * 0.7f, center.y + 5f, radius * 1.4f, 20f), centerLabel, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleCenter, false, false);

            float legendX = rect.x + diameter + 28f;
            float legendWidth = rect.xMax - legendX;
            int shown = Math.Min(values.Count, 6);
            for (int i = 0; i < shown; i++)
            {
                float y = rect.y + 10f + i * Mathf.Min(29f, (rect.height - 20f) / shown);
                UiDraw.Rounded(new Rect(legendX, y + 7f, 8f, 8f), ColonyTheme.Current.Series(i));
                UiDraw.Label(new Rect(legendX + 15f, y, legendWidth * 0.68f - 15f, 23f), values[i].Label, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(legendX + legendWidth * 0.68f, y, legendWidth * 0.32f, 23f), (values[i].Value / total).ToString("P0"), ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            }
        }

        private static void DrawDashed(Vector2 a, Vector2 b, Color color)
        {
            const int pieces = 12;
            for (int i = 0; i < pieces; i += 2)
            {
                Vector2 start = Vector2.Lerp(a, b, i / (float)pieces);
                Vector2 end = Vector2.Lerp(a, b, (i + 1) / (float)pieces);
                Widgets.DrawLine(start, end, color, 2f);
            }
        }

        private static void DrawChartTooltip(Rect plot, IList<ChartSeriesModel> series, float min, float range, string unit)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            ChartSeriesModel first = null;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i].Points != null && series[i].Points.Count > 0)
                {
                    first = series[i];
                    break;
                }
            }
            if (first == null)
                return;
            float normalized = Mathf.Clamp01((Event.current.mousePosition.x - plot.x) / plot.width);
            int index = Mathf.RoundToInt(normalized * (first.Points.Count - 1));
            string tooltip = string.Empty;
            for (int s = 0; s < series.Count; s++)
            {
                IReadOnlyList<MetricPoint> points = series[s].Points;
                if (points == null || points.Count == 0)
                    continue;
                int i = Mathf.Clamp(Mathf.RoundToInt(normalized * (points.Count - 1)), 0, points.Count - 1);
                if (tooltip.Length > 0)
                    tooltip += "\n";
                tooltip += series[s].Label + ": " + points[i].Value.ToString("0.##") + unit;
                if (series[s].Estimated)
                    tooltip += " (extrapolated, not measured)";
            }
            float x = plot.x + plot.width * index / Mathf.Max(1, first.Points.Count - 1);
            UiDraw.Rounded(new Rect(x, plot.y, 1f, plot.height), ColonyTheme.Alpha(ColonyTheme.Current.Muted, 0.45f));
            Widgets.MouseAttachedLabel(tooltip, 12f, -12f);
        }

        private static Vector2 Point(Rect rect, int index, int count, float value, float min, float range)
        {
            float x = count <= 1 ? rect.center.x : rect.x + rect.width * index / (count - 1);
            float y = rect.yMax - (value - min) / range * rect.height;
            return new Vector2(x, y);
        }

        private static void MinMax(IReadOnlyList<MetricPoint> points, bool includeZero, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                min = Mathf.Min(min, points[i].Value);
                max = Mathf.Max(max, points[i].Value);
            }
            if (includeZero)
            {
                min = Mathf.Min(0f, min);
                max = Mathf.Max(0f, max);
            }
        }

        private static bool HasSamples(IList<ChartSeriesModel> series, int minimum)
        {
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i].Points != null && series[i].Points.Count >= minimum)
                    return true;
            }
            return false;
        }
    }
}
