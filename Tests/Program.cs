using System;
using System.Collections.Generic;
using ColonyOS.Analytics;
using ColonyOS.Data;
using ColonyOS.Forecasting;
using ColonyOS.Trading;

internal static class Program
{
    private static int passed;

    private static int Main()
    {
        Run("Sparse history does not forecast", SparseHistory);
        Run("Measured food trend reaches correct threshold", FoodTrend);
        Run("Rising utilization reaches full threshold", StorageTrend);
        Run("Noisy trend is rejected", NoisyTrend);
        Run("Downsampling is bounded", DownsampleBounds);
        Run("Confirmed demand counts buyers without summing stock", ConfirmedDemand);
        Run("Historical demand uses actual sale totals", HistoricalDemand);
        Run("Unsupported market estimates are empty", EmptyMarket);
        Run("Battery depletion uses measured deficit", BatteryForecast);
        Run("Food forecast consumes shared samples", FoodForecast);
        Run("Missing ingredients remain traceable", IngredientWarning);
        Console.WriteLine("PASS: " + passed + " analytical tests");
        return 0;
    }

    private static void SparseHistory()
    {
        List<MetricPoint> points = Points(100f, 90f, 80f);
        ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 0f, true);
        Assert(!forecast.Available, "Three samples must not produce a trend forecast.");
    }

    private static void FoodTrend()
    {
        List<MetricPoint> points = Points(120f, 100f, 80f, 60f, 40f, 20f);
        ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 0f, true);
        Assert(forecast.Available, forecast.Reason);
        Near(-20f, forecast.SlopePerDay, 0.01f, "slope");
        Near(360000f, forecast.TargetAtTick, 1f, "target tick");
        Assert(forecast.RSquared > 0.99f, "Linear data should have a high fit.");
    }

    private static void StorageTrend()
    {
        List<MetricPoint> points = Points(0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f);
        ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 1f, false);
        Assert(forecast.Available, forecast.Reason);
        Near(0.1f, forecast.SlopePerDay, 0.0001f, "storage slope");
        Near(240000f, forecast.TargetAtTick, 1f, "storage target");
    }

    private static void NoisyTrend()
    {
        List<MetricPoint> points = Points(100f, 1f, 99f, 2f, 98f, 3f, 97f);
        ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 0f, true);
        Assert(!forecast.Available, "Alternating noise must not become a confident depletion forecast.");
    }

    private static void DownsampleBounds()
    {
        List<MetricPoint> points = new List<MetricPoint>();
        for (int i = 0; i < 1000; i++) points.Add(new MetricPoint(i, i));
        Assert(TrendAnalyzer.Downsample(points, 37).Count == 37, "Downsample count must be exact.");
        Assert(TrendAnalyzer.Downsample(points, 2000).Count == 1000, "Small series must not be duplicated.");
    }

    private static void ConfirmedDemand()
    {
        List<TraderVisitRecord> visits = new List<TraderVisitRecord>
        {
            Visit("a", 10),
            Visit("b", 20)
        };
        List<DemandOpportunity> rankings = MarketIntelligence.Confirmed(visits);
        Assert(rankings.Count == 1, "One shared ThingDef should rank once.");
        Assert(rankings[0].ConfirmedBuyerCount == 2, "Distinct current buyers should be counted.");
        Assert(rankings[0].AvailableCount == 20, "Stock must be the maximum observation, not 30 summed across buyers.");
        Assert(rankings[0].Evidence == EvidenceKind.Confirmed, "Evidence class changed.");
    }

    private static void HistoricalDemand()
    {
        List<TradeTransactionRecord> transactions = new List<TradeTransactionRecord>
        {
            new TradeTransactionRecord { Tick = 100, TraderKey = "a", Direction = TradeDirection.PlayerSold, DefName = "Cloth", Label = "Cloth", Count = 10, UnitPrice = 5f, TotalValue = 50f },
            new TradeTransactionRecord { Tick = 101, TraderKey = "a", Direction = TradeDirection.PlayerBought, DefName = "Cloth", Label = "Cloth", Count = 3, UnitPrice = 8f, TotalValue = 24f }
        };
        List<DemandOpportunity> rankings = MarketIntelligence.Historical(transactions, 1000);
        Assert(rankings.Count == 1, "Only player sales belong in purchasing-demand history.");
        Assert(rankings[0].HistoricalQuantity == 10, "Purchase quantity leaked into sales history.");
        Near(50f, rankings[0].HistoricalRevenue, 0.001f, "revenue");
        Near(5f, rankings[0].EstimatedUnitValue, 0.001f, "actual average sale price");
        List<DemandOpportunity> estimated = MarketIntelligence.Estimated(new List<TraderVisitRecord>(), transactions, 1000);
        Assert(estimated[0].Confidence <= 0.5f, "History-only estimates must remain low confidence.");
    }

    private static void EmptyMarket()
    {
        Assert(MarketIntelligence.Confirmed(null).Count == 0, "No visits should mean no confirmed demand.");
        Assert(MarketIntelligence.Historical(null, 0).Count == 0, "No transactions should mean no historical demand.");
        Assert(MarketIntelligence.Estimated(null, null, 0).Count == 0, "No evidence should mean no estimate.");
    }

    private static void BatteryForecast()
    {
        ColonySample current = new ColonySample { Tick = 100000, PowerGeneration = 50f, PowerConsumption = 100f, BatteryCharge = 25f, BatteryCapacity = 100f };
        List<Prediction> predictions = ForecastEngine.Generate(current, new List<ColonySample> { current }, new List<TraderVisitRecord>(), new List<TradeTransactionRecord>(), new ForecastOptions(), current.Tick);
        Prediction prediction = Find(predictions, "power.battery-depletion");
        Near(30000f, prediction.TimeframeTicks, 1f, "battery timeframe");
        Assert(prediction.Evidence == EvidenceKind.Estimated, "Battery projection must remain estimated.");
        Assert(prediction.SupportingEvidence.Count >= 3, "Battery evidence is missing.");
    }

    private static void FoodForecast()
    {
        List<ColonySample> samples = new List<ColonySample>();
        float[] values = { 120f, 100f, 80f, 60f, 40f, 20f };
        for (int i = 0; i < values.Length; i++)
            samples.Add(new ColonySample { Tick = i * 60000, FoodNutrition = values[i], Population = 5 });
        ForecastOptions options = new ForecastOptions { HorizonHours = 72, RetentionDays = 120 };
        List<Prediction> predictions = ForecastEngine.Generate(samples[samples.Count - 1], samples, new List<TraderVisitRecord>(), new List<TradeTransactionRecord>(), options, 300000);
        Prediction prediction = Find(predictions, "food.projected-depletion");
        Near(60000f, prediction.TimeframeTicks, 1f, "food timeframe");
        Assert(prediction.Confidence > 0.5f, "A well-supported linear trend should have useful confidence.");
    }

    private static void IngredientWarning()
    {
        ColonySample current = new ColonySample { Tick = 1 };
        current.Workbenches.Add(new WorkbenchSnapshot
        {
            Label = "Bench",
            CurrentRecipe = "Make jacket",
            ActiveBillCount = 1,
            QualifiedAvailablePawns = 2,
            Usable = true,
            Ingredients = new List<IngredientSnapshot>
            {
                new IngredientSnapshot { Label = "Cloth", Missing = true, ExactQuantity = true, Required = 50, Available = 10 }
            }
        });
        List<Prediction> predictions = ForecastEngine.Generate(current, new List<ColonySample> { current }, new List<TraderVisitRecord>(), new List<TradeTransactionRecord>(), new ForecastOptions(), 1);
        Prediction prediction = Find(predictions, "production.ingredients");
        Assert(prediction.Evidence == EvidenceKind.Confirmed, "Exact missing ingredients should be confirmed.");
        Assert(prediction.SupportingEvidence[0].Contains("Cloth"), "Affected ingredient is not traceable.");
    }

    private static TraderVisitRecord Visit(string key, int count)
    {
        return new TraderVisitRecord
        {
            TraderKey = key,
            Active = true,
            EligibleGoods = new List<TradeEligibilityRecord>
            {
                new TradeEligibilityRecord { DefName = "Cloth", Label = "Cloth", AvailableCount = count, BaseMarketValue = 2f }
            }
        };
    }

    private static List<MetricPoint> Points(params float[] values)
    {
        List<MetricPoint> points = new List<MetricPoint>();
        for (int i = 0; i < values.Length; i++) points.Add(new MetricPoint(i * 60000, values[i]));
        return points;
    }

    private static Prediction Find(List<Prediction> predictions, string id)
    {
        for (int i = 0; i < predictions.Count; i++) if (predictions[i].Id == id) return predictions[i];
        throw new Exception("Missing prediction: " + id + ". Found " + predictions.Count);
    }

    private static void Near(float expected, float actual, float tolerance, string name)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine("  ✓ " + name);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("  ✗ " + name + "\n" + ex);
            Environment.Exit(1);
        }
    }
}
