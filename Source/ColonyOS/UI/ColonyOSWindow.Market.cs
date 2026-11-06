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
        private int demandCacheRevision = -1;
        private EvidenceKind demandCacheTab;
        private string demandCacheQuery;
        private int demandCacheSort;
        private List<DemandOpportunity> demandCache = new List<DemandOpportunity>();
        private int traderGoodsFilter;

        private void DrawMarketPage(DashboardViewModel view, Rect rect)
        {
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            string[] tabs = { "Confirmed", "Historical", "Estimated" };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (UiDraw.Button(new Rect(rect.x + i * 116f, y, 108f, 34f), tabs[i], null, (int)marketTab == i))
                    marketTab = (EvidenceKind)i;
            }
            bool compact = rect.width < 700f;
            float controlY = compact ? y + 44f : y;
            Rect searchRect = compact ? new Rect(rect.x, controlY, rect.width * 0.52f - 6f, 34f) : new Rect(rect.xMax - 356f, controlY, 190f, 34f);
            Rect sortRect = compact ? new Rect(rect.x + rect.width * 0.52f + 6f, controlY, rect.width * 0.48f - 6f, 34f) : new Rect(rect.xMax - 158f, controlY, 158f, 34f);
            marketSearch = UiDraw.Search(searchRect, marketSearch, "Search goods");
            if (UiDraw.Button(sortRect, "Sort: " + (marketSort == 0 ? "rank" : marketSort == 1 ? "confidence" : "stock")))
                marketSort = (marketSort + 1) % 3;
            y += compact ? 92f : 46f;

            Rect evidenceBanner = new Rect(rect.x, y, rect.width, 58f);
            Color evidenceColor = theme.Evidence(marketTab);
            UiDraw.Rounded(evidenceBanner, ColonyTheme.Alpha(evidenceColor, 0.12f));
            UiDraw.Pill(new Rect(evidenceBanner.x + 14f, evidenceBanner.y + 16f, 96f, 25f), tabs[(int)marketTab], evidenceColor);
            string evidenceText = marketTab == EvidenceKind.Confirmed
                ? "Directly established by current ITrader purchasing rules and reachable colony goods."
                : marketTab == EvidenceKind.Historical
                    ? "Recorded from completed sales to merchants. History does not confirm a future purchase."
                    : "A ranking model using only confirmed and historical inputs. Never a claim of hidden merchant intent.";
            UiDraw.Label(new Rect(evidenceBanner.x + 124f, evidenceBanner.y + 8f, evidenceBanner.width - 140f, 42f), evidenceText, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft);
            y += 72f;

            List<DemandOpportunity> demand = FilteredDemand(view);
            y = SplitPanels(rect, y, 414f, 0.57f, out Rect ranking, out Rect detail);
            Rect rankingInner = UiDraw.Card(ranking, "Merchant demand ranking", demand.Count + " matching good(s) • select for evidence");
            DrawDemandRanking(rankingInner, demand);
            DemandOpportunity selected = SelectedGood(demand);
            Rect detailInner = UiDraw.Card(detail, "Demand evidence", selected?.Label ?? "No good selected");
            DrawDemandDetail(detailInner, selected);

            TraderVisitRecord trader = SelectedTrader(view);
            y = SplitPanels(rect, y, 526f, 0.41f, out Rect directory, out Rect profile);
            Rect directoryInner = UiDraw.Card(directory, "Merchant directory", "Active visitors and previously observed trader identities");
            DrawTraderDirectory(directoryInner, view.TraderDirectory);
            Rect profileInner = UiDraw.Card(profile, "Merchant profile", trader?.TraderName ?? "No trader selected");
            DrawTraderProfile(profileInner, trader, view.Transactions);
            UiDraw.Label(new Rect(rect.x, y, rect.width, 48f), "Purchasing eligibility and purchasing capacity are not inferred from what a merchant sells. Confirmed goods come from ColonyThingsWillingToBuy/tradeables; silver capacity is summed from current trader goods where exposed. Forecast confidence is deliberately reduced with sparse evidence.", theme.Faint, GameFont.Tiny);
        }

        private List<DemandOpportunity> FilteredDemand(DashboardViewModel view)
        {
            string query = marketSearch?.Trim() ?? string.Empty;
            if (demandCacheRevision == view.Revision && demandCacheTab == marketTab && demandCacheQuery == query && demandCacheSort == marketSort)
                return demandCache;
            IReadOnlyList<DemandOpportunity> source = marketTab == EvidenceKind.Confirmed ? view.ConfirmedDemand : marketTab == EvidenceKind.Historical ? view.HistoricalDemand : view.EstimatedDemand;
            demandCache = new List<DemandOpportunity>();
            for (int i = 0; i < source.Count; i++)
            {
                if (query.Length > 0 && (source[i].Label?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) < 0 && (source[i].DefName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                    continue;
                demandCache.Add(source[i]);
            }
            demandCache.Sort((a, b) => marketSort == 1 ? b.Confidence.CompareTo(a.Confidence) : marketSort == 2 ? b.AvailableCount.CompareTo(a.AvailableCount) : b.Score.CompareTo(a.Score));
            demandCacheRevision = view.Revision;
            demandCacheTab = marketTab;
            demandCacheQuery = query;
            demandCacheSort = marketSort;
            return demandCache;
        }

        private void DrawDemandRanking(Rect rect, IReadOnlyList<DemandOpportunity> demand)
        {
            if (demand.Count == 0)
            {
                string title = marketTab == EvidenceKind.Confirmed ? "No currently confirmed buyers" : marketTab == EvidenceKind.Historical ? "No matching sale history" : "No evidence for an estimate";
                UiDraw.EmptyState(rect, title, "Observe a trader, complete a sale, or change the search. Empty evidence is shown instead of fabricated demand.");
                return;
            }
            int count = Math.Min(6, demand.Count);
            float rowHeight = rect.height / count;
            float maximum = Math.Max(0.001f, demand[0].Score);
            for (int i = 0; i < count; i++)
            {
                DemandOpportunity item = demand[i];
                Rect row = new Rect(rect.x, rect.y + i * rowHeight, rect.width, rowHeight - 6f);
                bool selected = item.DefName == selectedGoodDef;
                UiDraw.Rounded(row, selected ? ColonyTheme.Alpha(ColonyTheme.Current.Accent, 0.16f) : Mouse.IsOver(row) ? ColonyTheme.Current.PanelRaised : ColonyTheme.Alpha(ColonyTheme.Current.Grid, 0.38f));
                UiDraw.Label(new Rect(row.x + 11f, row.y + 5f, row.width * 0.48f, 22f), item.Label, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                UiDraw.Progress(new Rect(row.x + 11f, row.y + 31f, row.width * 0.46f, 6f), item.Score / maximum, ColonyTheme.Current.Series(i));
                UiDraw.Label(new Rect(row.x + row.width * 0.52f, row.y + 4f, row.width * 0.24f, 23f), item.ConfirmedBuyerCount + " buyer(s)", ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(row.x + row.width * 0.52f, row.y + 27f, row.width * 0.24f, 20f), item.HistoricalQuantity + " sold", ColonyTheme.Current.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Pill(new Rect(row.xMax - 76f, row.y + row.height * 0.26f, 65f, 23f), item.Confidence.ToString("P0"), ColonyTheme.Current.Evidence(item.Evidence));
                if (Widgets.ButtonInvisible(row))
                    selectedGoodDef = item.DefName;
            }
        }

        private DemandOpportunity SelectedGood(IReadOnlyList<DemandOpportunity> demand)
        {
            if (demand.Count == 0)
                return null;
            for (int i = 0; i < demand.Count; i++)
                if (demand[i].DefName == selectedGoodDef)
                    return demand[i];
            return demand[0];
        }

        private void DrawDemandDetail(Rect rect, DemandOpportunity item)
        {
            if (item == null)
            {
                UiDraw.EmptyState(rect, "Select a good", "Its eligibility, history, confidence and model explanation will appear here.");
                return;
            }
            // Keep selection stable when a tab or filter changes.
            selectedGoodDef = item.DefName;
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Pill(new Rect(rect.x, rect.y + 4f, 104f, 25f), item.Evidence.ToString(), theme.Evidence(item.Evidence));
            UiDraw.Pill(new Rect(rect.x + 114f, rect.y + 4f, 110f, 25f), item.Confidence.ToString("P0") + " confidence", theme.Accent2);
            UiDraw.Label(new Rect(rect.x, rect.y + 45f, rect.width, 36f), item.Label, theme.Text, GameFont.Medium, TextAnchor.UpperLeft, true, false);
            DrawStatRow(new Rect(rect.x, rect.y + 91f, rect.width, 27f), "Current confirmed buyers", item.ConfirmedBuyerCount.ToString(), theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 120f, rect.width, 27f), "Available counted stock", item.AvailableCount.ToString(), theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 149f, rect.width, 27f), "Historical units sold", item.HistoricalQuantity.ToString(), theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 178f, rect.width, 27f), "Historical sale revenue", UiFormat.Money(item.HistoricalRevenue), theme.Text);
            string valueLabel = item.HistoricalQuantity > 0 ? "Avg. actual sale price" : "Observed base value";
            DrawStatRow(new Rect(rect.x, rect.y + 207f, rect.width, 27f), valueLabel, UiFormat.Money(item.EstimatedUnitValue), theme.Muted);
            UiDraw.Divider(new Rect(rect.x, rect.y + 244f, rect.width, 1f));
            UiDraw.Label(new Rect(rect.x, rect.y + 255f, rect.width, rect.height - 255f), item.Explanation, theme.Muted, GameFont.Tiny);
        }

        private TraderVisitRecord SelectedTrader(DashboardViewModel view)
        {
            TraderVisitRecord fallback = null;
            for (int i = 0; i < view.TraderDirectory.Count; i++)
            {
                TraderVisitRecord trader = view.TraderDirectory[i];
                if (trader.TraderKey == selectedTraderKey)
                    return trader;
                if (fallback == null || (trader.Active && !fallback.Active))
                    fallback = trader;
            }
            return fallback;
        }

        private void DrawTraderDirectory(Rect rect, IReadOnlyList<TraderVisitRecord> traders)
        {
            if (traders.Count == 0)
            {
                UiDraw.EmptyState(rect, "No traders observed", "Ground visitors, orbital ships and trade sessions will create merchant profiles.");
                return;
            }
            int count = Math.Min(8, traders.Count);
            float rowHeight = rect.height / count;
            for (int i = 0; i < count; i++)
            {
                TraderVisitRecord trader = traders[i];
                Rect row = new Rect(rect.x, rect.y + i * rowHeight, rect.width, rowHeight - 5f);
                bool selected = trader.TraderKey == selectedTraderKey;
                UiDraw.Rounded(row, selected ? ColonyTheme.Alpha(ColonyTheme.Current.Accent, 0.15f) : ColonyTheme.Alpha(ColonyTheme.Current.Grid, 0.35f));
                UiDraw.Rounded(new Rect(row.x + 11f, row.y + 13f, 7f, 7f), trader.Active ? ColonyTheme.Current.Good : ColonyTheme.Current.Faint);
                UiDraw.Label(new Rect(row.x + 27f, row.y + 5f, row.width - 42f, 23f), trader.TraderName, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(row.x + 27f, row.y + 28f, row.width - 42f, 18f), trader.TraderKindLabel + "  •  " + trader.FactionName, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                if (Widgets.ButtonInvisible(row))
                    selectedTraderKey = trader.TraderKey;
            }
        }

        private void DrawTraderProfile(Rect rect, TraderVisitRecord trader, IReadOnlyList<TradeTransactionRecord> transactions)
        {
            if (trader == null)
            {
                UiDraw.EmptyState(rect, "No merchant selected", "Trader-specific eligibility and transactions appear here.");
                return;
            }
            selectedTraderKey = trader.TraderKey;
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Pill(new Rect(rect.x, rect.y, 92f, 24f), trader.Active ? "Active now" : "Historical", trader.Active ? theme.Good : theme.Accent2);
            UiDraw.Label(new Rect(rect.x + 104f, rect.y, rect.width - 104f, 24f), trader.Currency + "  •  " + trader.MapLabel, theme.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            DrawStatRow(new Rect(rect.x, rect.y + 37f, rect.width, 25f), "Purchasing capacity", trader.PurchasingCapacity > 0f ? UiFormat.Compact(trader.PurchasingCapacity) + " silver" : "Not exposed / none", theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 64f, rect.width, 25f), "First observed", UiFormat.TickAge(trader.FirstSeenTick, trader.LastSeenTick), theme.Muted);
            UiDraw.Divider(new Rect(rect.x, rect.y + 99f, rect.width, 1f));
            string goodsTitle = traderGoodsFilter == 0
                ? (trader.Active ? "Confirmed current purchasing eligibility" : "Eligibility at last observation (historical)")
                : (trader.Active ? "Counted goods not currently eligible" : "Counted goods not eligible at last observation");
            UiDraw.Label(new Rect(rect.x, rect.y + 109f, Mathf.Max(80f, rect.width - 154f), 22f), goodsTitle, theme.Text, GameFont.Tiny, TextAnchor.UpperLeft, true, false);
            if (UiDraw.Button(new Rect(rect.xMax - 148f, rect.y + 106f, 70f, 25f), "Can buy", null, traderGoodsFilter == 0))
                traderGoodsFilter = 0;
            if (UiDraw.Button(new Rect(rect.xMax - 72f, rect.y + 106f, 70f, 25f), "Cannot", null, traderGoodsFilter == 1))
                traderGoodsFilter = 1;
            IReadOnlyList<TradeEligibilityRecord> goodsSource = traderGoodsFilter == 0 ? trader.EligibleGoods : trader.IneligibleGoods;
            int goods = Math.Min(6, goodsSource.Count);
            for (int i = 0; i < goods; i++)
            {
                TradeEligibilityRecord good = goodsSource[i];
                float y = rect.y + 136f + i * 25f;
                UiDraw.Label(new Rect(rect.x + 6f, y, rect.width * 0.7f, 23f), good.Label, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.72f, y, rect.width * 0.26f, 23f), "×" + good.AvailableCount, theme.Text, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            }
            if (goods == 0)
                UiDraw.Label(new Rect(rect.x + 6f, rect.y + 139f, rect.width - 12f, 44f), traderGoodsFilter == 0 ? "No eligible colony goods were exposed at the latest observation." : "No complementary counted goods were recorded. Items outside the resource readout are not classified.", theme.Faint, GameFont.Tiny);
            float historyY = rect.y + 299f;
            UiDraw.Divider(new Rect(rect.x, historyY, rect.width, 1f));
            UiDraw.Label(new Rect(rect.x, historyY + 10f, rect.width, 22f), "Recent transactions with this trader", theme.Text, GameFont.Tiny, TextAnchor.UpperLeft, true, false);
            int shown = 0;
            for (int i = 0; i < transactions.Count && shown < 4; i++)
            {
                TradeTransactionRecord transaction = transactions[i];
                if (transaction.TraderKey != trader.TraderKey)
                    continue;
                float y = historyY + 38f + shown * 27f;
                Color color = transaction.Direction == TradeDirection.PlayerSold ? theme.Good : theme.Warning;
                UiDraw.Label(new Rect(rect.x + 6f, y, rect.width * 0.64f, 24f), (transaction.Direction == TradeDirection.PlayerSold ? "Sold " : "Bought ") + transaction.Label + " ×" + transaction.Count, theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.67f, y, rect.width * 0.31f, 24f), UiFormat.Compact(transaction.TotalValue), color, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                shown++;
            }
            if (shown == 0)
                UiDraw.Label(new Rect(rect.x + 6f, historyY + 41f, rect.width - 12f, 40f), "No transactions in the selected time range.", theme.Faint, GameFont.Tiny);
        }
    }
}
