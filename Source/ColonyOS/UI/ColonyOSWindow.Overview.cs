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
        private void DrawOverviewPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null)
            {
                UiDraw.EmptyState(rect, "No colony snapshot yet", "ColonyOS will display measured data after the first scheduled collection.");
                return;
            }

            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            Prediction priority = null;
            for (int i = 0; i < view.Predictions.Count; i++)
            {
                if (view.Predictions[i].Severity >= PredictionSeverity.High)
                {
                    priority = view.Predictions[i];
                    break;
                }
            }
            if (priority != null)
            {
                Rect alert = new Rect(rect.x, y, rect.width, 66f);
                UiDraw.Panel(alert, true, Mouse.IsOver(alert));
                Color severity = theme.Severity(priority.Severity);
                UiDraw.Rounded(new Rect(alert.x + 15f, alert.y + 15f, 36f, 36f), ColonyTheme.Alpha(severity, 0.18f));
                UiDraw.Label(new Rect(alert.x + 15f, alert.y + 15f, 36f, 36f), "!", severity, GameFont.Medium, TextAnchor.MiddleCenter, true, false);
                UiDraw.Label(new Rect(alert.x + 63f, alert.y + 10f, alert.width - 200f, 24f), priority.Title, theme.Text, GameFont.Small, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(alert.x + 63f, alert.y + 34f, alert.width - 200f, 20f), priority.SuggestedAction, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Pill(new Rect(alert.xMax - 112f, alert.y + 21f, 92f, 24f), "Inspect →", severity);
                if (Widgets.ButtonInvisible(alert))
                    OpenPrediction(priority);
                y += 78f;
            }

            int columns = rect.width >= 1050f ? 4 : rect.width >= 700f ? 3 : 2;
            int index = 0;
            DrawOverviewKpi(view, rect, columns, index++, "Total wealth", UiFormat.Compact(current.WealthTotal), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.Wealth))), "Items + buildings + pawns", theme.Accent2, Department.Economy, view.GetSeries(SampleMetric.Wealth), ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Power generation", UiFormat.Compact(current.PowerGeneration, " W"), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.PowerGeneration))), "Measured active output", theme.Good, Department.Power, view.GetSeries(SampleMetric.PowerGeneration), ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Power consumption", UiFormat.Compact(current.PowerConsumption, " W"), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.PowerConsumption))), current.NetPower >= 0f ? "Surplus " + UiFormat.Compact(current.NetPower, " W") : "Deficit " + UiFormat.Compact(-current.NetPower, " W"), current.NetPower >= 0f ? theme.Accent : theme.Danger, Department.Power, view.GetSeries(SampleMetric.PowerConsumption), ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Battery charge", current.BatteryCapacity <= 0f ? "No batteries" : current.BatteryPct.ToString("P0"), UiFormat.Compact(current.BatteryCharge, " Wd"), "of " + UiFormat.Compact(current.BatteryCapacity, " Wd"), current.BatteryPct < ColonyOSMod.Settings.BatteryWarningPct ? theme.Warning : theme.Accent, Department.Power, view.GetSeries(SampleMetric.BatteryCharge), ref y);
            if (ColonyOSMod.Settings.ShowFoodWidget)
                DrawOverviewKpi(view, rect, columns, index++, "Food reserves", UiFormat.Compact(current.FoodNutrition), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.FoodNutrition))), "Counted nutrition", current.FoodNutrition <= 0f ? theme.Danger : theme.Good, Department.Logistics, view.GetSeries(SampleMetric.FoodNutrition), ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Population", current.Population.ToString(), current.IdlePawns + " idle", current.AvailableWorkforce + " non-downed", theme.Purple, Department.Population, view.GetSeries(SampleMetric.Population), ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Production output", current.ProductionIterationsLastDay + "/day", current.ActiveBills + " active bills", current.MannedWorkbenches + "/" + current.TotalWorkbenches + " manned", theme.Accent2, Department.Production, view.ProductionSeries, ref y);
            DrawOverviewKpi(view, rect, columns, index++, "Storage pressure", current.StorageCellCount <= 0 ? "No zones" : current.StorageUtilization.ToString("P0"), current.StorageFreeSlotEstimate + " free slots", "Stockpile-cell estimate", current.StorageUtilization >= ColonyOSMod.Settings.StorageWarningPct ? theme.Warning : theme.Accent, Department.Logistics, view.GetSeries(SampleMetric.StorageUtilization), ref y);
            y += Mathf.CeilToInt((float)index / columns) * 140f + 4f;

            if (ColonyOSMod.Settings.ShowPowerWidget)
            {
                float leftWidth = rect.width * 0.64f - 7f;
                Rect powerCard = new Rect(rect.x, y, leftWidth, 278f);
                Rect powerInner = UiDraw.Card(powerCard, "Generation versus consumption", "Measured watts across collected snapshots");
                List<ChartSeriesModel> power = new List<ChartSeriesModel>
                {
                    new ChartSeriesModel("Generation", view.GetSeries(SampleMetric.PowerGeneration), theme.Good),
                    new ChartSeriesModel("Consumption", view.GetSeries(SampleMetric.PowerConsumption), theme.Danger)
                };
                Charts.LineChart(powerInner, power, " W");
                if (Widgets.ButtonInvisible(powerCard))
                    Navigate(Department.Power);

                Rect warnings = new Rect(powerCard.xMax + 14f, y, rect.xMax - powerCard.xMax - 14f, 278f);
                Rect warningsInner = UiDraw.Card(warnings, "Operational intelligence", view.Predictions.Count + " active signal(s)");
                DrawPredictionRows(warningsInner, view.Predictions, 3);
                y += 292f;
            }

            if (ColonyOSMod.Settings.ShowWealthWidget)
            {
                Rect wealth = new Rect(rect.x, y, rect.width * 0.58f - 7f, 278f);
                Rect wealthInner = UiDraw.Card(wealth, "Colony wealth trajectory", "Actual RimWorld wealth watcher totals");
                Charts.LineChart(wealthInner, OneSeries(view, SampleMetric.Wealth, "Total wealth", theme.Accent2), " silver", false);
                if (Widgets.ButtonInvisible(wealth))
                    Navigate(Department.Economy);
                Rect resources = new Rect(wealth.xMax + 14f, y, rect.xMax - wealth.xMax - 14f, 278f);
                Rect resourcesInner = UiDraw.Card(resources, "Resource value distribution", "Base market value × counted quantity");
                Charts.Donut(resourcesInner, ResourceValues(current, 6), "base value");
                if (Widgets.ButtonInvisible(resources))
                    Navigate(Department.Logistics);
                y += 292f;
            }

            if (ColonyOSMod.Settings.ShowProductionWidget || ColonyOSMod.Settings.ShowWorkforceWidget)
            {
                float left = ColonyOSMod.Settings.ShowProductionWidget ? rect.width * 0.56f - 7f : 0f;
                if (ColonyOSMod.Settings.ShowProductionWidget)
                {
                    Rect production = new Rect(rect.x, y, ColonyOSMod.Settings.ShowWorkforceWidget ? left : rect.width, 278f);
                    Rect inner = UiDraw.Card(production, "Production activity", "Recorded bill iterations — not estimated item yields");
                    Charts.VerticalBars(inner, view.ProductionSeries, theme.Accent2);
                    if (Widgets.ButtonInvisible(production))
                        Navigate(Department.Production);
                }
                if (ColonyOSMod.Settings.ShowWorkforceWidget)
                {
                    float x = ColonyOSMod.Settings.ShowProductionWidget ? rect.x + left + 14f : rect.x;
                    Rect workforce = new Rect(x, y, rect.xMax - x, 278f);
                    Rect inner = UiDraw.Card(workforce, "Workforce allocation", "Colonists with each work type enabled");
                    List<NamedValue> work = new List<NamedValue>();
                    for (int i = 0; i < Math.Min(7, current.Workforce.Count); i++)
                        work.Add(new NamedValue(current.Workforce[i].DefName, current.Workforce[i].Label, current.Workforce[i].EnabledPawns));
                    Charts.RankingBars(inner, work, "No work assignments", "Work allocation appears when initialized colonists have enabled work types.");
                    if (Widgets.ButtonInvisible(workforce))
                        Navigate(Department.Population);
                }
                y += 292f;
            }

            if (ColonyOSMod.Settings.ShowMarketWidget)
            {
                Rect market = new Rect(rect.x, y, rect.width, 270f);
                Rect inner = UiDraw.Card(market, "Regional merchant demand", "Estimated ranking grounded in current eligibility and recorded sales");
                List<DemandOpportunity> demand = view.EstimatedDemand;
                if (demand.Count == 0)
                {
                    UiDraw.EmptyState(inner, "No merchant evidence yet", "Open a trade session or wait for an observed trader. ColonyOS does not invent regional demand.");
                }
                else
                {
                    int count = Math.Min(5, demand.Count);
                    float rowHeight = inner.height / count;
                    for (int i = 0; i < count; i++)
                    {
                        DemandOpportunity item = demand[i];
                        Rect row = new Rect(inner.x, inner.y + i * rowHeight, inner.width, rowHeight - 4f);
                        UiDraw.Label(new Rect(row.x, row.y, row.width * 0.32f, row.height), item.Label, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                        UiDraw.Progress(new Rect(row.x + row.width * 0.34f, row.y + row.height * 0.34f, row.width * 0.34f, 7f), demand[0].Score <= 0f ? 0f : item.Score / demand[0].Score, theme.Series(i));
                        UiDraw.Label(new Rect(row.x + row.width * 0.71f, row.y, row.width * 0.15f, row.height), item.ConfirmedBuyerCount + " current", theme.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                        UiDraw.Pill(new Rect(row.xMax - 62f, row.y + row.height * 0.2f, 60f, 20f), item.Confidence.ToString("P0"), theme.Purple);
                    }
                }
                if (Widgets.ButtonInvisible(market))
                    Navigate(Department.RegionalMarket);
            }
        }

        private void DrawOverviewKpi(DashboardViewModel view, Rect rect, int columns, int index, string title, string value, string delta, string status, Color color, Department target, IReadOnlyList<MetricPoint> series, ref float y)
        {
            Rect cell = UiDraw.GridCell(new Rect(rect.x, y, rect.width, rect.height), columns, index, 128f, 12f);
            Charts.Kpi(cell, title, value, delta, status, color, target, series, () => Navigate(target));
        }
    }
}
