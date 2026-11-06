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
        private void DrawEconomyPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null) { UiDraw.EmptyState(rect, "No economic snapshot", "Wealth and trade history will appear after collection."); return; }
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int columns = rect.width > 850f ? 4 : 2;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 122f), "Total colony wealth", UiFormat.Compact(current.WealthTotal), UiFormat.SignedPercent(PercentChange(view.GetSeries(SampleMetric.Wealth))), "RimWorld wealth watcher", theme.Accent2, Department.Economy, view.GetSeries(SampleMetric.Wealth), null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 122f), "Inventory wealth", UiFormat.Compact(current.WealthItems), current.WealthTotal <= 0f ? "—" : (current.WealthItems / current.WealthTotal).ToString("P0") + " of total", "Actual game value", theme.Purple, Department.Economy, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 122f), "Sales revenue", UiFormat.Compact(view.SalesTotal), "Selected time range", "Actual transaction value", theme.Good, Department.Economy, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 3, 122f), "Purchase spending", UiFormat.Compact(view.PurchasesTotal), "Selected time range", "Actual transaction value", theme.Warning, Department.Economy, null, null);
            y += Mathf.CeilToInt(4f / columns) * 134f + 8f;

            y = SplitPanels(rect, y, 282f, 0.64f, out Rect history, out Rect composition);
            Rect historyInner = UiDraw.Card(history, "Colony wealth history", "Measured total wealth, including items, buildings, floors and pawns");
            Charts.LineChart(historyInner, OneSeries(view, SampleMetric.Wealth, "Total wealth", theme.Accent2), " silver", false);
            Rect compositionInner = UiDraw.Card(composition, "Wealth composition", "Current WealthWatcher components");
            Charts.Donut(compositionInner, WealthComposition(current), "total wealth");

            Rect trade = new Rect(rect.x, y, rect.width, 236f);
            Rect tradeInner = UiDraw.Card(trade, "Trade flow comparison", "Executed transaction prices — never substituted with market value");
            DrawTradeComparison(tradeInner, view);
            y += 250f;

            y = SplitPanels(rect, y, 266f, 0.46f, out Rect values, out Rect transactionCard);
            Rect valuesInner = UiDraw.Card(values, "Valuable counted resources", "Base market value × quantity; not merchant offers");
            Charts.RankingBars(valuesInner, view.ResourceValueRanking, "No valued resources", "Counted resources with positive base market value will appear here.");
            Rect transactionInner = UiDraw.Card(transactionCard, "Economic evidence", "How to interpret values on this page");
            UiDraw.Label(new Rect(transactionInner.x, transactionInner.y + 5f, transactionInner.width, 54f), "Market value is the game's item valuation. Transaction totals use the actual unit price captured immediately before a successful trade resets. Demand estimates are kept in Regional Market Intelligence.", theme.Muted, GameFont.Tiny);
            DrawStatRow(new Rect(transactionInner.x, transactionInner.y + 78f, transactionInner.width, 28f), "Buildings & floors", UiFormat.Money(current.WealthBuildings), theme.Text);
            DrawStatRow(new Rect(transactionInner.x, transactionInner.y + 108f, transactionInner.width, 28f), "Pawn wealth", UiFormat.Money(current.WealthPawns), theme.Text);
            DrawStatRow(new Rect(transactionInner.x, transactionInner.y + 138f, transactionInner.width, 28f), "Net trade flow", (view.SalesTotal - view.PurchasesTotal >= 0f ? "+" : "") + UiFormat.Money(view.SalesTotal - view.PurchasesTotal), view.SalesTotal >= view.PurchasesTotal ? theme.Good : theme.Warning);

            Rect transactions = new Rect(rect.x, y, rect.width, 420f);
            Rect transactionsInner = UiDraw.Card(transactions, "Transaction history", "Newest executed trades in the selected range");
            string[] filters = { "All", "Sales", "Purchases" };
            for (int i = 0; i < filters.Length; i++)
            {
                if (UiDraw.Button(new Rect(transactionsInner.xMax - (3 - i) * 82f, transactionsInner.y - 35f, 76f, 27f), filters[i], null, transactionFilter == i))
                    transactionFilter = i;
            }
            DrawTransactions(transactionsInner, view.Transactions);
        }

        private static void DrawTradeComparison(Rect rect, DashboardViewModel view)
        {
            float maximum = Math.Max(1f, Math.Max(view.SalesTotal, view.PurchasesTotal));
            DrawStatRow(new Rect(rect.x, rect.y + 6f, rect.width, 25f), "Sales to merchants", "+" + UiFormat.Money(view.SalesTotal), ColonyTheme.Current.Good);
            UiDraw.Progress(new Rect(rect.x, rect.y + 34f, rect.width, 14f), view.SalesTotal / maximum, ColonyTheme.Current.Good);
            DrawStatRow(new Rect(rect.x, rect.y + 65f, rect.width, 25f), "Purchases from merchants", "−" + UiFormat.Money(view.PurchasesTotal), ColonyTheme.Current.Warning);
            UiDraw.Progress(new Rect(rect.x, rect.y + 93f, rect.width, 14f), view.PurchasesTotal / maximum, ColonyTheme.Current.Warning);
            float net = view.SalesTotal - view.PurchasesTotal;
            UiDraw.Label(new Rect(rect.x, rect.y + 127f, rect.width, 32f), (net >= 0f ? "Net inflow  +" : "Net outflow  ") + UiFormat.Money(net), net >= 0f ? ColonyTheme.Current.Good : ColonyTheme.Current.Warning, GameFont.Small, TextAnchor.MiddleCenter, true, false);
        }

        private void DrawTransactions(Rect rect, IReadOnlyList<TradeTransactionRecord> transactions)
        {
            List<TradeTransactionRecord> filtered = new List<TradeTransactionRecord>();
            for (int i = 0; i < transactions.Count; i++)
            {
                TradeTransactionRecord transaction = transactions[i];
                if (transactionFilter == 1 && transaction.Direction != TradeDirection.PlayerSold) continue;
                if (transactionFilter == 2 && transaction.Direction != TradeDirection.PlayerBought) continue;
                filtered.Add(transaction);
                if (filtered.Count >= 10) break;
            }
            if (filtered.Count == 0)
            {
                UiDraw.EmptyState(rect, "No matching transactions", "Executed purchases and sales will appear here with actual captured prices.");
                return;
            }
            float rowHeight = Math.Min(33f, rect.height / filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                TradeTransactionRecord item = filtered[i];
                Rect row = new Rect(rect.x, rect.y + i * rowHeight, rect.width, rowHeight - 3f);
                UiDraw.Rounded(row, ColonyTheme.Alpha(ColonyTheme.Current.Grid, 0.38f));
                Color color = item.Direction == TradeDirection.PlayerSold ? ColonyTheme.Current.Good : ColonyTheme.Current.Warning;
                UiDraw.Label(new Rect(row.x + 9f, row.y, 48f, row.height), item.Direction == TradeDirection.PlayerSold ? "SELL" : "BUY", color, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(row.x + 61f, row.y, row.width * 0.29f, row.height), item.Label + " ×" + item.Count, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.38f, row.y, row.width * 0.25f, row.height), item.TraderName, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.64f, row.y, row.width * 0.18f, row.height), item.UnitPrice.ToString("0.##") + " / unit", ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.84f, row.y, row.width * 0.14f - 8f, row.height), UiFormat.Compact(item.TotalValue), color, GameFont.Tiny, TextAnchor.MiddleRight, true, false);
            }
        }
    }
}
