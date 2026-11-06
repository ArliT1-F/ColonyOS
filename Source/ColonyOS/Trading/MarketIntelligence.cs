using System;
using System.Collections.Generic;
using ColonyOS.Data;

namespace ColonyOS.Trading
{
    /// <summary>Demand rankings with explicit evidence classes; no hidden-preference assumptions.</summary>
    public static class MarketIntelligence
    {
        public static List<DemandOpportunity> Confirmed(IReadOnlyList<TraderVisitRecord> visits)
        {
            Dictionary<string, DemandAccumulator> totals = new Dictionary<string, DemandAccumulator>();
            if (visits != null)
            {
                for (int i = 0; i < visits.Count; i++)
                {
                    TraderVisitRecord visit = visits[i];
                    if (!visit.Active || visit.EligibleGoods == null)
                        continue;
                    for (int g = 0; g < visit.EligibleGoods.Count; g++)
                    {
                        TradeEligibilityRecord good = visit.EligibleGoods[g];
                        DemandAccumulator total = Get(totals, good.DefName, good.Label);
                        if (total.BuyerKeys.Add(visit.TraderKey))
                            total.ConfirmedBuyerCount++;
                        total.AvailableCount = Math.Max(total.AvailableCount, good.AvailableCount);
                        total.EstimatedUnitValue = good.BaseMarketValue;
                    }
                }
            }

            List<DemandOpportunity> result = Finalize(totals, EvidenceKind.Confirmed);
            for (int i = 0; i < result.Count; i++)
            {
                DemandOpportunity item = result[i];
                item.Score = item.ConfirmedBuyerCount * 50f + (float)Math.Log10(item.AvailableCount + 1) * 12f;
                item.Confidence = Clamp(0.55f + item.ConfirmedBuyerCount * 0.1f, 0.55f, 0.95f);
                item.Explanation = "Confirmed by the current ITrader purchasing rules for " + item.ConfirmedBuyerCount
                    + " observed trader(s). Availability is the maximum observed stock, not summed across buyers.";
            }
            return Sort(result);
        }

        public static List<DemandOpportunity> Historical(IReadOnlyList<TradeTransactionRecord> transactions, int nowTick, int days = 90)
        {
            Dictionary<string, DemandAccumulator> totals = new Dictionary<string, DemandAccumulator>();
            int cutoff = nowTick - days * TrendTicks.PerDay;
            if (transactions != null)
            {
                for (int i = 0; i < transactions.Count; i++)
                {
                    TradeTransactionRecord transaction = transactions[i];
                    if (transaction.Direction != TradeDirection.PlayerSold || transaction.Tick < cutoff)
                        continue;
                    DemandAccumulator total = Get(totals, transaction.DefName, transaction.Label);
                    total.HistoricalQuantity += transaction.Count;
                    total.HistoricalTransactionCount++;
                    total.HistoricalRevenue += transaction.TotalValue;
                    total.BuyerKeys.Add(transaction.TraderKey);
                }
            }

            List<DemandOpportunity> result = Finalize(totals, EvidenceKind.Historical);
            for (int i = 0; i < result.Count; i++)
            {
                DemandOpportunity item = result[i];
                item.Score = (float)Math.Log(1 + item.HistoricalQuantity) * 15f
                    + item.HistoricalTransactionCount * 12f + (float)Math.Log(1 + item.HistoricalRevenue) * 4f;
                item.Confidence = Clamp(0.2f + item.HistoricalTransactionCount * 0.05f + item.ConfirmedBuyerCount * 0.03f, 0.2f, 0.75f);
                item.Explanation = "Recorded from " + item.HistoricalTransactionCount + " completed sale transaction(s) in the last "
                    + days + " in-game days. This is history, not a promise of future eligibility.";
            }
            return Sort(result);
        }

        public static List<DemandOpportunity> Estimated(
            IReadOnlyList<TraderVisitRecord> visits,
            IReadOnlyList<TradeTransactionRecord> transactions,
            int nowTick)
        {
            List<DemandOpportunity> confirmed = Confirmed(visits);
            List<DemandOpportunity> historical = Historical(transactions, nowTick);
            Dictionary<string, DemandOpportunity> union = new Dictionary<string, DemandOpportunity>();
            Merge(union, confirmed, true);
            Merge(union, historical, false);

            List<DemandOpportunity> result = new List<DemandOpportunity>();
            foreach (DemandOpportunity item in union.Values)
            {
                bool hasConfirmed = item.ConfirmedBuyerCount > 0;
                bool hasHistory = item.HistoricalTransactionCount > 0;
                item.Evidence = EvidenceKind.Estimated;
                float confirmedEvidence = hasConfirmed ? 55f + item.ConfirmedBuyerCount * 12f : 0f;
                float historyEvidence = hasHistory
                    ? (float)Math.Log(1 + item.HistoricalQuantity) * 16f + item.HistoricalTransactionCount * 10f
                    : 0f;
                item.Score = hasConfirmed && hasHistory
                    ? confirmedEvidence * 0.55f + historyEvidence * 0.45f
                    : (hasConfirmed ? confirmedEvidence * 0.65f : historyEvidence * 0.65f);
                item.Confidence = hasConfirmed && hasHistory
                    ? Clamp(0.45f + item.HistoricalTransactionCount * 0.04f + item.ConfirmedBuyerCount * 0.06f, 0.45f, 0.85f)
                    : (hasConfirmed ? 0.4f : Clamp(0.15f + item.HistoricalTransactionCount * 0.035f, 0.15f, 0.5f));
                item.Explanation = "Estimated ranking only. Inputs: "
                    + (hasConfirmed ? item.ConfirmedBuyerCount + " current eligible buyer(s)" : "no current eligible buyer")
                    + " and " + (hasHistory ? item.HistoricalTransactionCount + " historical sale(s)" : "no recorded sales")
                    + ". It does not infer unstated merchant preferences.";
                result.Add(item);
            }
            return Sort(result);
        }

        private static void Merge(Dictionary<string, DemandOpportunity> union, List<DemandOpportunity> source, bool confirmed)
        {
            for (int i = 0; i < source.Count; i++)
            {
                DemandOpportunity item = source[i];
                if (!union.TryGetValue(item.DefName, out DemandOpportunity target))
                {
                    target = new DemandOpportunity { DefName = item.DefName, Label = item.Label };
                    union.Add(item.DefName, target);
                }
                if (confirmed)
                {
                    target.ConfirmedBuyerCount = item.ConfirmedBuyerCount;
                    target.AvailableCount = item.AvailableCount;
                    if (target.EstimatedUnitValue <= 0f)
                        target.EstimatedUnitValue = item.EstimatedUnitValue;
                }
                else
                {
                    target.HistoricalQuantity = item.HistoricalQuantity;
                    target.HistoricalTransactionCount = item.HistoricalTransactionCount;
                    target.HistoricalRevenue = item.HistoricalRevenue;
                    if (item.HistoricalQuantity > 0)
                        target.EstimatedUnitValue = item.HistoricalRevenue / item.HistoricalQuantity;
                }
            }
        }

        private static DemandAccumulator Get(Dictionary<string, DemandAccumulator> totals, string key, string label)
        {
            if (!totals.TryGetValue(key, out DemandAccumulator value))
            {
                value = new DemandAccumulator { DefName = key, Label = label };
                totals.Add(key, value);
            }
            return value;
        }

        private static List<DemandOpportunity> Finalize(Dictionary<string, DemandAccumulator> totals, EvidenceKind evidence)
        {
            List<DemandOpportunity> result = new List<DemandOpportunity>();
            foreach (DemandAccumulator value in totals.Values)
            {
                result.Add(new DemandOpportunity
                {
                    DefName = value.DefName,
                    Label = value.Label,
                    Evidence = evidence,
                    ConfirmedBuyerCount = evidence == EvidenceKind.Confirmed ? value.ConfirmedBuyerCount : value.BuyerKeys.Count,
                    AvailableCount = value.AvailableCount,
                    HistoricalQuantity = value.HistoricalQuantity,
                    HistoricalTransactionCount = value.HistoricalTransactionCount,
                    HistoricalRevenue = value.HistoricalRevenue,
                    EstimatedUnitValue = value.HistoricalQuantity > 0
                        ? value.HistoricalRevenue / value.HistoricalQuantity
                        : value.EstimatedUnitValue
                });
            }
            return result;
        }

        private static List<DemandOpportunity> Sort(List<DemandOpportunity> items)
        {
            items.Sort((a, b) => b.Score.CompareTo(a.Score));
            return items;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private sealed class DemandAccumulator
        {
            public string DefName;
            public string Label;
            public int ConfirmedBuyerCount;
            public int AvailableCount;
            public int HistoricalQuantity;
            public int HistoricalTransactionCount;
            public float HistoricalRevenue;
            public float EstimatedUnitValue;
            public readonly HashSet<string> BuyerKeys = new HashSet<string>();
        }
    }

    internal static class TrendTicks
    {
        public const int PerDay = 60000;
    }
}
