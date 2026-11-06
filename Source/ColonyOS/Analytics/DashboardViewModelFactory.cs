using System;
using System.Collections.Generic;
using ColonyOS.Data;
using ColonyOS.Trading;

namespace ColonyOS.Analytics
{
    /// <summary>Runs on scheduled game ticks. The UI only consumes these completed models.</summary>
    public static class DashboardViewModelFactory
    {
        private const int DayTicks = 60000;

        public static DashboardCache Build(HistoryStore history)
        {
            DashboardCache cache = new DashboardCache { Revision = history.Revision };
            List<DemandOpportunity> confirmed = MarketIntelligence.Confirmed(history.TraderVisits);
            List<DemandOpportunity> historical = MarketIntelligence.Historical(history.Transactions, Now(history));
            List<DemandOpportunity> estimated = MarketIntelligence.Estimated(history.TraderVisits, history.Transactions, Now(history));
            List<TraderVisitRecord> traders = new List<TraderVisitRecord>(history.TraderVisits);
            traders.Sort((a, b) => b.LastSeenTick.CompareTo(a.LastSeenTick));
            if (traders.Count > 200)
                traders.RemoveRange(200, traders.Count - 200);
            BuildCurrentRankings(history.Current,
                out List<NamedValue> resourceCounts,
                out List<NamedValue> resourceValues,
                out List<NamedValue> generators,
                out List<PowerDeviceSnapshot> powerExceptions,
                out List<WorkbenchSnapshot> labourConstraints);

            foreach (DashboardTimeRange range in Enum.GetValues(typeof(DashboardTimeRange)))
            {
                DashboardViewModel view = new DashboardViewModel
                {
                    Range = range,
                    Revision = history.Revision,
                    Current = history.Current,
                    CutoffTick = Cutoff(history, range),
                    Predictions = history.Predictions,
                    ConfirmedDemand = confirmed,
                    HistoricalDemand = historical,
                    EstimatedDemand = estimated,
                    TraderDirectory = traders,
                    HistorySampleCount = history.Samples.Count,
                    ResourceCountRanking = resourceCounts,
                    ResourceValueRanking = resourceValues,
                    GeneratorContributionRanking = generators,
                    PowerExceptionRanking = powerExceptions,
                    LabourConstraintRanking = labourConstraints
                };
                BuildSeries(view, history.Samples);
                view.FoodProjection = TrendAnalyzer.ProjectToThreshold(view.GetSeries(SampleMetric.FoodNutrition), 0f, true);
                view.StorageProjection = TrendAnalyzer.ProjectToThreshold(view.GetSeries(SampleMetric.StorageUtilization), 100f, false);
                BuildProduction(view, history.ProductionEvents);
                BuildTransactions(view, history.Transactions);
                cache.Views.Add(range, view);
            }
            return cache;
        }

        private static void BuildCurrentRankings(ColonySample current,
            out List<NamedValue> resourceCounts,
            out List<NamedValue> resourceValues,
            out List<NamedValue> generators,
            out List<PowerDeviceSnapshot> powerExceptions,
            out List<WorkbenchSnapshot> labourConstraints)
        {
            resourceCounts = new List<NamedValue>();
            resourceValues = new List<NamedValue>();
            generators = new List<NamedValue>();
            powerExceptions = new List<PowerDeviceSnapshot>();
            labourConstraints = new List<WorkbenchSnapshot>();
            if (current == null)
                return;

            for (int i = 0; i < current.Resources.Count; i++)
            {
                ResourceSnapshot resource = current.Resources[i];
                resourceCounts.Add(new NamedValue(resource.DefName, resource.Label, resource.Count));
                float value = resource.Count * resource.BaseMarketValue;
                if (value > 0f)
                    resourceValues.Add(new NamedValue(resource.DefName, resource.Label, value));
            }
            resourceCounts.Sort((a, b) => b.Value.CompareTo(a.Value));
            resourceValues.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (resourceCounts.Count > 8) resourceCounts.RemoveRange(8, resourceCounts.Count - 8);
            if (resourceValues.Count > 6) resourceValues.RemoveRange(6, resourceValues.Count - 6);

            for (int i = 0; i < current.PowerDevices.Count; i++)
            {
                PowerDeviceSnapshot device = current.PowerDevices[i];
                if (device.Output > 0f)
                    generators.Add(new NamedValue(device.BuildingDefName + i, device.Label, device.Output));
                powerExceptions.Add(device);
            }
            generators.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (generators.Count > 8) generators.RemoveRange(8, generators.Count - 8);
            powerExceptions.Sort((a, b) =>
            {
                int offline = (b.PotentialOutput > 0f && !b.Active).CompareTo(a.PotentialOutput > 0f && !a.Active);
                return offline != 0 ? offline : a.Output.CompareTo(b.Output);
            });
            if (powerExceptions.Count > 8) powerExceptions.RemoveRange(8, powerExceptions.Count - 8);

            for (int i = 0; i < current.Workbenches.Count; i++)
            {
                WorkbenchSnapshot table = current.Workbenches[i];
                if (table.ActiveBillCount > 0 && !string.IsNullOrEmpty(table.CurrentRecipe))
                    labourConstraints.Add(table);
            }
            labourConstraints.Sort((a, b) => a.QualifiedAvailablePawns.CompareTo(b.QualifiedAvailablePawns));
            if (labourConstraints.Count > 5) labourConstraints.RemoveRange(5, labourConstraints.Count - 5);
        }

        private static void BuildSeries(DashboardViewModel view, IReadOnlyList<ColonySample> samples)
        {
            foreach (SampleMetric metric in Enum.GetValues(typeof(SampleMetric)))
            {
                List<MetricPoint> points = new List<MetricPoint>();
                for (int i = 0; i < samples.Count; i++)
                {
                    ColonySample sample = samples[i];
                    if (sample.Tick < view.CutoffTick)
                        continue;
                    points.Add(new MetricPoint(sample.Tick, Value(sample, metric)));
                }
                view.SetSeries(metric, TrendAnalyzer.Downsample(points, 240));
            }
        }

        private static void BuildProduction(DashboardViewModel view, IReadOnlyList<ProductionEventRecord> events)
        {
            int now = view.Current?.Tick ?? 0;
            int bucketCount;
            int bucketTicks;
            int start;
            switch (view.Range)
            {
                case DashboardTimeRange.Day:
                    bucketCount = 12;
                    bucketTicks = 5000;
                    start = now - DayTicks;
                    break;
                case DashboardTimeRange.Week:
                    bucketCount = 7;
                    bucketTicks = DayTicks;
                    start = now - 7 * DayTicks;
                    break;
                case DashboardTimeRange.Month:
                    bucketCount = 30;
                    bucketTicks = DayTicks;
                    start = now - 30 * DayTicks;
                    break;
                default:
                    bucketCount = 30;
                    int oldest = now - DayTicks;
                    for (int i = 0; i < events.Count; i++)
                        oldest = Math.Min(oldest, events[i].Tick);
                    int span = Math.Max(DayTicks, now - oldest);
                    bucketTicks = Math.Max(2500, span / bucketCount + 1);
                    start = now - bucketTicks * bucketCount;
                    break;
            }

            int[] counts = new int[bucketCount];
            Dictionary<string, NamedValue> recipes = new Dictionary<string, NamedValue>();
            for (int i = 0; i < events.Count; i++)
            {
                ProductionEventRecord record = events[i];
                if (record.Tick < view.CutoffTick)
                    continue;
                NamedValue recipe;
                if (!recipes.TryGetValue(record.RecipeDefName, out recipe))
                {
                    recipe = new NamedValue(record.RecipeDefName, record.RecipeLabel, 0f);
                    recipes.Add(record.RecipeDefName, recipe);
                }
                recipe.Value++;
                if (record.Tick <= start || record.Tick > now)
                    continue;
                int bucket = (record.Tick - start - 1) / bucketTicks;
                if (bucket >= 0 && bucket < bucketCount)
                    counts[bucket]++;
            }
            for (int i = 0; i < bucketCount; i++)
                view.ProductionSeries.Add(new MetricPoint(start + (i + 1) * bucketTicks, counts[i]));
            view.RecipeLeaderboard.AddRange(recipes.Values);
            view.RecipeLeaderboard.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (view.RecipeLeaderboard.Count > 8)
                view.RecipeLeaderboard.RemoveRange(8, view.RecipeLeaderboard.Count - 8);
        }

        private static void BuildTransactions(DashboardViewModel view, IReadOnlyList<TradeTransactionRecord> transactions)
        {
            for (int i = transactions.Count - 1; i >= 0; i--)
            {
                TradeTransactionRecord transaction = transactions[i];
                if (transaction.Tick < view.CutoffTick)
                    continue;
                if (view.Transactions.Count < 250)
                    view.Transactions.Add(transaction);
                if (transaction.Direction == TradeDirection.PlayerSold)
                    view.SalesTotal += transaction.TotalValue;
                else
                    view.PurchasesTotal += transaction.TotalValue;
            }
        }

        private static int Cutoff(HistoryStore history, DashboardTimeRange range)
        {
            int now = Now(history);
            switch (range)
            {
                case DashboardTimeRange.Day: return now - DayTicks;
                case DashboardTimeRange.Week: return now - 7 * DayTicks;
                case DashboardTimeRange.Month: return now - 30 * DayTicks;
                default: return int.MinValue;
            }
        }

        private static int Now(HistoryStore history) => history.Current?.Tick ?? 0;

        private static float Value(ColonySample sample, SampleMetric metric)
        {
            switch (metric)
            {
                case SampleMetric.Wealth: return sample.WealthTotal;
                case SampleMetric.PowerGeneration: return sample.PowerGeneration;
                case SampleMetric.PowerConsumption: return sample.PowerConsumption;
                case SampleMetric.PowerBalance: return sample.GridBalanceAfterBatteryLosses;
                case SampleMetric.BatteryCharge: return sample.BatteryCharge;
                case SampleMetric.FoodNutrition: return sample.FoodNutrition;
                case SampleMetric.Population: return sample.Population;
                case SampleMetric.StorageUtilization: return sample.StorageUtilization * 100f;
                default: return 0f;
            }
        }
    }
}
