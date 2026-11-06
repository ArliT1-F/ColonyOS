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
        private void DrawPowerPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null) { UiDraw.EmptyState(rect, "No power snapshot", "Power networks will appear after collection."); return; }
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int columns = rect.width > 850f ? 4 : 2;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 122f), "Active generation", UiFormat.Compact(current.PowerGeneration, " W"), current.PotentialGeneration > current.PowerGeneration ? UiFormat.Compact(current.PotentialGeneration - current.PowerGeneration, " W") + " offline potential" : "All measured output active", "CompPowerTrader", theme.Good, Department.Power, view.GetSeries(SampleMetric.PowerGeneration), null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 122f), "Active consumption", UiFormat.Compact(current.PowerConsumption, " W"), current.PowerDevices.Count + " connected device(s)", "Measured draw", theme.Danger, Department.Power, view.GetSeries(SampleMetric.PowerConsumption), null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 122f), "Net grid balance", (current.NetPower >= 0f ? "+" : "") + UiFormat.Compact(current.NetPower, " W"), current.BatterySelfDischarge > 0f ? "-" + UiFormat.Compact(current.BatterySelfDischarge, " W") + " battery loss" : "No battery loss", current.GridBalanceAfterBatteryLosses >= 0f ? "Balanced" : "Discharging", current.GridBalanceAfterBatteryLosses >= 0f ? theme.Accent : theme.Warning, Department.Power, view.GetSeries(SampleMetric.PowerBalance), null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 3, 122f), "Battery reserve", current.BatteryCapacity <= 0f ? "—" : current.BatteryPct.ToString("P1"), UiFormat.Compact(current.BatteryCharge, " Wd"), UiFormat.Compact(current.BatteryCapacity, " Wd") + " capacity", theme.Accent, Department.Power, view.GetSeries(SampleMetric.BatteryCharge), null);
            y += Mathf.CeilToInt(4f / columns) * 134f + 8f;

            Rect trend = new Rect(rect.x, y, rect.width * 0.66f - 7f, 282f);
            Rect trendInner = UiDraw.Card(trend, "Grid output and load", "Measured instantaneous values at each snapshot");
            Charts.LineChart(trendInner, new List<ChartSeriesModel>
            {
                new ChartSeriesModel("Generation", view.GetSeries(SampleMetric.PowerGeneration), theme.Good),
                new ChartSeriesModel("Consumption", view.GetSeries(SampleMetric.PowerConsumption), theme.Danger)
            }, " W");
            Rect reserve = new Rect(trend.xMax + 14f, y, rect.xMax - trend.xMax - 14f, 282f);
            Rect reserveInner = UiDraw.Card(reserve, "Reserve intelligence", "Watt-days and current depletion estimate");
            DrawReservePanel(reserveInner, view, current);
            y += 296f;

            Rect battery = new Rect(rect.x, y, rect.width * 0.48f - 7f, 282f);
            Rect batteryInner = UiDraw.Card(battery, "Battery charge history", "No forecast curve is drawn without a measured trend");
            Charts.LineChart(batteryInner, OneSeries(view, SampleMetric.BatteryCharge, "Stored energy", theme.Accent), " Wd");
            Rect contribution = new Rect(battery.xMax + 14f, y, rect.xMax - battery.xMax - 14f, 282f);
            Rect contributionInner = UiDraw.Card(contribution, "Generator contribution", "Active output by building at the latest snapshot");
            Charts.RankingBars(contributionInner, PowerDeviceValues(current, true), "No active generation", "No building is currently reporting positive power output.", " W");
            y += 296f;

            Rect networks = new Rect(rect.x, y, rect.width * 0.52f - 7f, 340f);
            Rect networksInner = UiDraw.Card(networks, "Power network status", current.PowerNetworks.Count + " independently measured network(s)");
            DrawNetworks(networksInner, current.PowerNetworks);
            Rect consumers = new Rect(networks.xMax + 14f, y, rect.xMax - networks.xMax - 14f, 340f);
            Rect consumersInner = UiDraw.Card(consumers, "Consumption & generator exceptions", "Largest current loads and offline positive-output buildings");
            DrawPowerExceptions(consumersInner, current);
            y += 354f;
            UiDraw.Label(new Rect(rect.x, y, rect.width, 42f), "Measurement boundary: building values are current CompPowerTrader outputs, not per-building energy meters or historical utilization. Potential output is the component's current output setting while offline.", theme.Faint, GameFont.Tiny);
        }

        private static void DrawReservePanel(Rect rect, DashboardViewModel view, ColonySample current)
        {
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Label(new Rect(rect.x, rect.y + 8f, rect.width, 48f), current.BatteryCapacity <= 0f ? "No battery capacity" : current.BatteryPct.ToString("P0"), theme.Text, GameFont.Medium, TextAnchor.MiddleCenter, true, false);
            UiDraw.Progress(new Rect(rect.x + 8f, rect.y + 65f, rect.width - 16f, 13f), current.BatteryPct, current.BatteryPct < 0.2f ? theme.Warning : theme.Accent);
            DrawStatRow(new Rect(rect.x, rect.y + 94f, rect.width, 26f), "Stored", UiFormat.Compact(current.BatteryCharge, " Wd"), theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 122f, rect.width, 26f), "Capacity", UiFormat.Compact(current.BatteryCapacity, " Wd"), theme.Muted);
            Prediction depletion = null;
            for (int i = 0; i < view.Predictions.Count; i++)
            {
                if (view.Predictions[i].Id == "power.battery-depletion") { depletion = view.Predictions[i]; break; }
            }
            if (depletion != null)
            {
                UiDraw.Pill(new Rect(rect.x, rect.yMax - 38f, rect.width, 26f), UiFormat.Duration(depletion.TimeframeTicks) + "  •  " + depletion.Confidence.ToString("P0"), theme.Severity(depletion.Severity));
            }
            else
            {
                UiDraw.Label(new Rect(rect.x, rect.yMax - 43f, rect.width, 38f), current.GridBalanceAfterBatteryLosses >= 0f ? "The current grid is not discharging reserves." : "Insufficient information for a depletion estimate.", theme.Muted, GameFont.Tiny, TextAnchor.MiddleCenter);
            }
        }

        private static List<NamedValue> PowerDeviceValues(ColonySample current, bool generators)
        {
            List<NamedValue> values = new List<NamedValue>();
            for (int i = 0; i < current.PowerDevices.Count; i++)
            {
                PowerDeviceSnapshot device = current.PowerDevices[i];
                float value = generators ? device.Output : -device.Output;
                if (value <= 0f)
                    continue;
                values.Add(new NamedValue(device.BuildingDefName + i, device.Label, value));
            }
            values.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (values.Count > 8) values.RemoveRange(8, values.Count - 8);
            return values;
        }

        private static void DrawNetworks(Rect rect, IReadOnlyList<PowerNetworkSnapshot> networks)
        {
            if (networks.Count == 0) { UiDraw.EmptyState(rect, "No power networks", "Build or connect a power network to begin monitoring."); return; }
            int count = Math.Min(8, networks.Count);
            float rowHeight = Math.Min(39f, rect.height / count);
            for (int i = 0; i < count; i++)
            {
                PowerNetworkSnapshot net = networks[i];
                Rect row = new Rect(rect.x, rect.y + i * rowHeight, rect.width, rowHeight - 4f);
                UiDraw.Rounded(row, ColonyTheme.Alpha(ColonyTheme.Current.Grid, 0.45f));
                UiDraw.Label(new Rect(row.x + 10f, row.y, 76f, row.height), "Grid " + (net.NetworkId + 1), ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(row.x + 91f, row.y, row.width * 0.36f, row.height), "+" + UiFormat.Compact(net.Generation) + " / −" + UiFormat.Compact(net.Consumption) + " W", ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                Color color = net.BalanceAfterBatteryLosses >= 0f ? ColonyTheme.Current.Good : ColonyTheme.Current.Danger;
                UiDraw.Pill(new Rect(row.xMax - 78f, row.y + 6f, 68f, row.height - 12f), (net.BalanceAfterBatteryLosses >= 0f ? "+" : "") + UiFormat.Compact(net.BalanceAfterBatteryLosses) + " W", color);
            }
        }

        private static void DrawPowerExceptions(Rect rect, ColonySample current)
        {
            List<PowerDeviceSnapshot> devices = new List<PowerDeviceSnapshot>(current.PowerDevices);
            devices.Sort((a, b) =>
            {
                int offline = (b.PotentialOutput > 0f && !b.Active).CompareTo(a.PotentialOutput > 0f && !a.Active);
                return offline != 0 ? offline : a.Output.CompareTo(b.Output);
            });
            int count = Math.Min(8, devices.Count);
            if (count == 0) { UiDraw.EmptyState(rect, "No power devices", "No building-level power components were found."); return; }
            float rowHeight = Math.Min(39f, rect.height / count);
            for (int i = 0; i < count; i++)
            {
                PowerDeviceSnapshot device = devices[i];
                float y = rect.y + i * rowHeight;
                string status = device.Active ? "online" : device.PotentialOutput > 0f ? "offline generator" : "offline";
                Color color = device.PotentialOutput > 0f && !device.Active ? ColonyTheme.Current.Warning : device.Output < 0f ? ColonyTheme.Current.Danger : ColonyTheme.Current.Good;
                UiDraw.Label(new Rect(rect.x, y, rect.width * 0.51f, rowHeight), device.Label, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.52f, y, rect.width * 0.25f, rowHeight), status, color, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.78f, y, rect.width * 0.22f, rowHeight), UiFormat.Compact(device.Output, " W"), ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                TooltipHandler.TipRegion(new Rect(rect.x, y, rect.width, rowHeight), "Grid " + (device.NetworkId + 1) + "\nCurrent output: " + device.Output + " W\nCurrent offline output setting: " + device.PotentialOutput + " W");
            }
        }
    }
}
