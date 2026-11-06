using System;
using System.Collections.Generic;
using ColonyOS.Data;

namespace ColonyOS.Analytics
{
    public enum DashboardTimeRange
    {
        Day,
        Week,
        Month,
        All
    }

    public enum SampleMetric
    {
        Wealth,
        PowerGeneration,
        PowerConsumption,
        PowerBalance,
        BatteryCharge,
        FoodNutrition,
        Population,
        StorageUtilization
    }

    public sealed class DashboardCache
    {
        public int Revision = -1;
        public readonly Dictionary<DashboardTimeRange, DashboardViewModel> Views = new Dictionary<DashboardTimeRange, DashboardViewModel>();

        public DashboardViewModel Get(DashboardTimeRange range)
        {
            return Views.TryGetValue(range, out DashboardViewModel view) ? view : null;
        }
    }

    public sealed class DashboardViewModel
    {
        public DashboardTimeRange Range;
        public int Revision;
        public int CutoffTick;
        public ColonySample Current;
        public IReadOnlyList<Prediction> Predictions = new List<Prediction>();
        public List<DemandOpportunity> ConfirmedDemand = new List<DemandOpportunity>();
        public List<DemandOpportunity> HistoricalDemand = new List<DemandOpportunity>();
        public List<DemandOpportunity> EstimatedDemand = new List<DemandOpportunity>();
        public List<TraderVisitRecord> TraderDirectory = new List<TraderVisitRecord>();
        public List<TradeTransactionRecord> Transactions = new List<TradeTransactionRecord>();
        public List<MetricPoint> ProductionSeries = new List<MetricPoint>();
        public List<NamedValue> RecipeLeaderboard = new List<NamedValue>();
        public float SalesTotal;
        public float PurchasesTotal;
        public int HistorySampleCount;

        private readonly Dictionary<SampleMetric, List<MetricPoint>> series = new Dictionary<SampleMetric, List<MetricPoint>>();

        public IReadOnlyList<MetricPoint> GetSeries(SampleMetric metric)
        {
            return series.TryGetValue(metric, out List<MetricPoint> points) ? points : EmptyPoints;
        }

        public void SetSeries(SampleMetric metric, List<MetricPoint> points) => series[metric] = points;

        private static readonly List<MetricPoint> EmptyPoints = new List<MetricPoint>();
    }
}
