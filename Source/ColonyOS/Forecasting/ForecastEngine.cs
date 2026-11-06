using System;
using System.Collections.Generic;
using ColonyOS.Analytics;
using ColonyOS.Data;
using ColonyOS.Trading;

namespace ColonyOS.Forecasting
{
    public sealed class ForecastOptions
    {
        public float BatteryWarningPct = 0.2f;
        public float StorageWarningPct = 0.9f;
        public int HorizonHours = 72;
        public int RetentionDays = 120;
    }

    public static class ForecastEngine
    {
        private const int TicksPerDay = 60000;

        public static List<Prediction> Generate(
            ColonySample current,
            IReadOnlyList<ColonySample> samples,
            IReadOnlyList<TraderVisitRecord> visits,
            IReadOnlyList<TradeTransactionRecord> transactions,
            ForecastOptions options,
            int nowTick)
        {
            List<Prediction> result = new List<Prediction>();
            if (current == null)
                return result;

            AddPowerForecasts(result, current, samples, options, nowTick);
            AddFoodForecast(result, current, samples, options, nowTick);
            AddStorageForecasts(result, current, samples, options, nowTick);
            AddProductionIntelligence(result, current);
            AddWealthTrend(result, samples, nowTick);
            AddTradeOpportunity(result, visits, transactions, nowTick);

            result.Sort((a, b) =>
            {
                int severity = b.Severity.CompareTo(a.Severity);
                return severity != 0 ? severity : b.Confidence.CompareTo(a.Confidence);
            });
            if (result.Count > 24)
                result.RemoveRange(24, result.Count - 24);
            return result;
        }

        private static void AddPowerForecasts(List<Prediction> result, ColonySample current, IReadOnlyList<ColonySample> samples, ForecastOptions options, int nowTick)
        {
            float balance = current.GridBalanceAfterBatteryLosses;
            if (balance < -0.1f)
            {
                float deficit = -balance;
                if (current.BatteryCharge > 0.1f)
                {
                    float days = current.BatteryCharge / deficit;
                    int ticks = Math.Max(1, (int)(days * TicksPerDay));
                    float hours = days * 24f;
                    result.Add(new Prediction
                    {
                        Id = "power.battery-depletion",
                        Title = "Battery depletion projected in " + FormatDuration(ticks),
                        Department = Department.Power,
                        Severity = hours <= 1f ? PredictionSeverity.Critical : hours <= 6f ? PredictionSeverity.High : PredictionSeverity.Moderate,
                        Evidence = EvidenceKind.Estimated,
                        TimeframeTicks = ticks,
                        Confidence = 0.82f,
                        Explanation = "If the currently measured grid deficit persists, stored energy reaches zero in approximately " + hours.ToString("0.0") + " hours.",
                        SuggestedAction = "Reduce nonessential draw or bring additional generation online; inspect the contributing power networks.",
                        SupportingEvidence = Evidence(
                            "Generation: " + current.PowerGeneration.ToString("0") + " W",
                            "Consumption: " + current.PowerConsumption.ToString("0") + " W",
                            "Battery self-discharge: " + current.BatterySelfDischarge.ToString("0") + " W",
                            "Stored: " + current.BatteryCharge.ToString("0") + " Wd")
                    });
                }
                else
                {
                    result.Add(new Prediction
                    {
                        Id = "power.current-deficit",
                        Title = "Power demand exceeds generation now",
                        Department = Department.Power,
                        Severity = PredictionSeverity.High,
                        Evidence = EvidenceKind.Confirmed,
                        TimeframeTicks = 0,
                        Confidence = 1f,
                        Explanation = "Measured consumption exceeds generation and no meaningful battery reserve remains.",
                        SuggestedAction = "Inspect offline generators and high-draw buildings; reduce load or add generation.",
                        SupportingEvidence = Evidence("Current net grid balance: " + balance.ToString("0") + " W")
                    });
                }
            }
            else if (current.BatteryCapacity > 0f && current.BatteryPct < options.BatteryWarningPct)
            {
                result.Add(new Prediction
                {
                    Id = "power.low-reserve",
                    Title = "Battery reserve is below the configured threshold",
                    Department = Department.Power,
                    Severity = PredictionSeverity.Low,
                    Evidence = EvidenceKind.Confirmed,
                    TimeframeTicks = 0,
                    Confidence = 1f,
                    Explanation = "The grid currently balances, but reserve energy is " + current.BatteryPct.ToString("P0") + ".",
                    SuggestedAction = "Consider additional storage or generation before demand rises.",
                    SupportingEvidence = Evidence("Threshold: " + options.BatteryWarningPct.ToString("P0"), "Charge: " + current.BatteryCharge.ToString("0") + " / " + current.BatteryCapacity.ToString("0") + " Wd")
                });
            }

            int recurring = 0;
            int cutoff = nowTick - 7 * TicksPerDay;
            if (samples != null)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    ColonySample sample = samples[i];
                    if (sample.Tick >= cutoff && sample.GridBalanceAfterBatteryLosses < 0f && sample.BatteryPct < 0.1f)
                        recurring++;
                }
            }
            if (recurring >= 3 && balance >= 0f)
            {
                result.Add(new Prediction
                {
                    Id = "power.recurring-shortage",
                    Title = "Recurring low-reserve power deficits detected",
                    Department = Department.Power,
                    Severity = PredictionSeverity.Moderate,
                    Evidence = EvidenceKind.Historical,
                    TimeframeTicks = -1,
                    Confidence = Math.Min(0.85f, 0.4f + recurring * 0.05f),
                    Explanation = recurring + " snapshots in the last seven in-game days combined a grid deficit with less than 10% battery charge.",
                    SuggestedAction = "Compare generation and consumption history to identify when the deficits recur.",
                    SupportingEvidence = Evidence("Observations are historical; the current grid is balanced.")
                });
            }
        }

        private static void AddFoodForecast(List<Prediction> result, ColonySample current, IReadOnlyList<ColonySample> samples, ForecastOptions options, int nowTick)
        {
            if (current.Population > 0 && current.FoodNutrition <= 0.01f)
            {
                result.Add(new Prediction
                {
                    Id = "food.empty",
                    Title = "No counted human-edible food remains",
                    Department = Department.Logistics,
                    Severity = PredictionSeverity.Critical,
                    Evidence = EvidenceKind.Confirmed,
                    TimeframeTicks = 0,
                    Confidence = 1f,
                    Explanation = "RimWorld's resource readout reports no eligible stored nutrition for " + current.Population + " colonists.",
                    SuggestedAction = "Secure food immediately; check whether meals are outside counted storage before treating this as a colony-wide total.",
                    SupportingEvidence = Evidence("Counted nutrition: 0", "Population: " + current.Population)
                });
                return;
            }

            List<MetricPoint> points = Series(samples, nowTick - options.RetentionDays * TicksPerDay, x => x.FoodNutrition);
            ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 0f, true);
            int ticks = forecast.Available ? (int)(forecast.TargetAtTick - nowTick) : -1;
            if (forecast.Available && ticks >= 0 && ticks <= options.HorizonHours * 2500)
            {
                result.Add(new Prediction
                {
                    Id = "food.projected-depletion",
                    Title = "Food reserves may run out in " + FormatDuration(ticks),
                    Department = Department.Logistics,
                    Severity = ticks <= 6 * 2500 ? PredictionSeverity.High : PredictionSeverity.Moderate,
                    Evidence = EvidenceKind.Estimated,
                    TimeframeTicks = ticks,
                    Confidence = forecast.Confidence,
                    Explanation = "A measured downward trend in counted human-edible nutrition reaches zero within the configured forecast horizon.",
                    SuggestedAction = "Review food production and reserves; account for meals outside the counted resource readout.",
                    SupportingEvidence = Evidence(
                        "Current counted nutrition: " + current.FoodNutrition.ToString("0.0"),
                        "Trend: " + forecast.SlopePerDay.ToString("0.0") + " nutrition/day",
                        forecast.Reason)
                });
            }
        }

        private static void AddStorageForecasts(List<Prediction> result, ColonySample current, IReadOnlyList<ColonySample> samples, ForecastOptions options, int nowTick)
        {
            if (current.StorageCellCount > 0 && current.StorageUtilization >= options.StorageWarningPct)
            {
                result.Add(new Prediction
                {
                    Id = "storage.pressure",
                    Title = "Stockpile slots are under pressure",
                    Department = Department.Logistics,
                    Severity = current.StorageUtilization >= 0.98f ? PredictionSeverity.High : PredictionSeverity.Moderate,
                    Evidence = EvidenceKind.Confirmed,
                    TimeframeTicks = 0,
                    Confidence = 0.9f,
                    Explanation = "RimWorld's stockpile free-slot estimate is above the configured utilization threshold.",
                    SuggestedAction = "Expand or redistribute stockpiles; inspect shelves and modded containers separately.",
                    SupportingEvidence = Evidence("Estimated free slots: " + current.StorageFreeSlotEstimate + " / " + current.StorageCellCount, "This is a stockpile-cell estimate, not exact item capacity.")
                });
                return;
            }

            List<MetricPoint> points = Series(samples, nowTick - options.RetentionDays * TicksPerDay, x => x.StorageUtilization);
            ForecastLine forecast = TrendAnalyzer.ProjectToThreshold(points, 1f, false);
            int ticks = forecast.Available ? (int)(forecast.TargetAtTick - nowTick) : -1;
            if (current.StorageCellCount > 0 && forecast.Available && ticks >= 0 && ticks <= options.HorizonHours * 2500)
            {
                result.Add(new Prediction
                {
                    Id = "storage.projected-full",
                    Title = "Stockpile slots may fill in " + FormatDuration(ticks),
                    Department = Department.Logistics,
                    Severity = PredictionSeverity.Moderate,
                    Evidence = EvidenceKind.Estimated,
                    TimeframeTicks = ticks,
                    Confidence = forecast.Confidence * 0.85f,
                    Explanation = "The measured stockpile-slot utilization trend reaches full estimated occupancy inside the forecast horizon.",
                    SuggestedAction = "Review storage growth and the highest-pressure zones before adding capacity.",
                    SupportingEvidence = Evidence("Current slot pressure: " + current.StorageUtilization.ToString("P0"), forecast.Reason, "Confidence is reduced because free slots are an estimate.")
                });
            }
        }

        private static void AddProductionIntelligence(List<Prediction> result, ColonySample current)
        {
            List<string> blocked = new List<string>();
            List<string> missingEvidence = new List<string>();
            List<string> understaffed = new List<string>();
            int unmanned = 0;
            bool allExact = true;
            for (int i = 0; i < current.Workbenches.Count; i++)
            {
                WorkbenchSnapshot table = current.Workbenches[i];
                if (table.ActiveBillCount > 0 && table.Usable && !table.Manned)
                    unmanned++;
                if (table.ActiveBillCount > 0 && !string.IsNullOrEmpty(table.CurrentRecipe) && table.QualifiedAvailablePawns == 0 && !string.IsNullOrEmpty(table.RequiredSkill))
                    understaffed.Add(table.CurrentRecipe);
                for (int g = 0; g < table.Ingredients.Count; g++)
                {
                    IngredientSnapshot ingredient = table.Ingredients[g];
                    if (!ingredient.Missing)
                        continue;
                    if (!blocked.Contains(table.CurrentRecipe))
                        blocked.Add(table.CurrentRecipe);
                    allExact = allExact && ingredient.ExactQuantity;
                    missingEvidence.Add(table.Label + ": " + ingredient.Label + " — counted " + ingredient.Available.ToString("0.#") + ", required " + (ingredient.ExactQuantity ? ingredient.Required.ToString("0.#") : "quantity varies by allowed ingredient"));
                }
            }

            if (blocked.Count > 0)
            {
                result.Add(new Prediction
                {
                    Id = "production.ingredients",
                    Title = blocked.Count + " active production chain(s) lack counted ingredients",
                    Department = Department.Production,
                    Severity = PredictionSeverity.Moderate,
                    Evidence = allExact ? EvidenceKind.Confirmed : EvidenceKind.Estimated,
                    TimeframeTicks = -1,
                    Confidence = allExact ? 0.95f : 0.65f,
                    Explanation = "The first active bill on affected workbenches has missing fixed ingredients or no counted flexible ingredients. Items outside the resource readout may change availability.",
                    SuggestedAction = "Inspect the affected bills and ingredient filters before producing or hauling more material.",
                    SupportingEvidence = Limit(missingEvidence, 6)
                });
            }

            if (understaffed.Count > 0)
            {
                result.Add(new Prediction
                {
                    Id = "population.production-skill",
                    Title = "Active bills have no qualified available worker",
                    Department = Department.Population,
                    Severity = PredictionSeverity.Moderate,
                    Evidence = EvidenceKind.Confirmed,
                    TimeframeTicks = -1,
                    Confidence = 0.9f,
                    Explanation = "No non-downed colonist with the relevant work enabled currently satisfies the recipe skill requirements.",
                    SuggestedAction = "Review work restrictions and skill coverage manually; ColonyOS does not change assignments.",
                    SupportingEvidence = Limit(understaffed, 6)
                });
            }
            else if (unmanned >= 2)
            {
                result.Add(new Prediction
                {
                    Id = "production.unmanned",
                    Title = unmanned + " usable production stations are currently unmanned",
                    Department = Department.Production,
                    Severity = PredictionSeverity.Informational,
                    Evidence = EvidenceKind.Confirmed,
                    TimeframeTicks = 0,
                    Confidence = 0.85f,
                    Explanation = "These stations have active bills and are usable, but no colonist job targeted them at snapshot time. This is not an uptime estimate.",
                    SuggestedAction = "Inspect labour allocation and ingredient status; no automatic reassignment is recommended by default.",
                    SupportingEvidence = Evidence("Snapshot-based observation only.")
                });
            }
        }

        private static void AddWealthTrend(List<Prediction> result, IReadOnlyList<ColonySample> samples, int nowTick)
        {
            List<MetricPoint> points = Series(samples, nowTick - 7 * TicksPerDay, x => x.WealthTotal);
            if (points.Count < 6)
                return;
            float change = TrendAnalyzer.PercentChange(points);
            ForecastLine trend = TrendAnalyzer.ProjectToThreshold(points, 0f, true, 6, TicksPerDay);
            if (change <= -0.08f && trend.Available)
            {
                result.Add(new Prediction
                {
                    Id = "economy.decline",
                    Title = "Colony wealth shows a sustained decline",
                    Department = Department.Economy,
                    Severity = change <= -0.2f ? PredictionSeverity.Moderate : PredictionSeverity.Low,
                    Evidence = EvidenceKind.Historical,
                    TimeframeTicks = -1,
                    Confidence = trend.Confidence,
                    Explanation = "Measured total wealth declined " + change.ToString("P0") + " across the available seven-day series. This describes history, not certain future losses.",
                    SuggestedAction = "Inspect item, building, and pawn wealth components and recent trade transactions.",
                    SupportingEvidence = Evidence("Trend: " + trend.SlopePerDay.ToString("0") + " silver/day", trend.Reason)
                });
            }
        }

        private static void AddTradeOpportunity(List<Prediction> result, IReadOnlyList<TraderVisitRecord> visits, IReadOnlyList<TradeTransactionRecord> transactions, int nowTick)
        {
            List<DemandOpportunity> rankings = MarketIntelligence.Estimated(visits, transactions, nowTick);
            if (rankings.Count == 0)
                return;
            DemandOpportunity best = rankings[0];
            result.Add(new Prediction
            {
                Id = "market.opportunity." + best.DefName,
                Title = "Potential sale opportunity: " + best.Label,
                Department = Department.RegionalMarket,
                Severity = PredictionSeverity.Informational,
                Evidence = EvidenceKind.Estimated,
                TimeframeTicks = -1,
                Confidence = best.Confidence,
                Explanation = best.Explanation,
                SuggestedAction = "Open Regional Market Intelligence and verify current confirmed buyer eligibility before committing resources.",
                SupportingEvidence = Evidence("Historical quantity sold: " + best.HistoricalQuantity, "Current confirmed buyers: " + best.ConfirmedBuyerCount, "Available counted stock: " + best.AvailableCount)
            });
        }

        internal static List<MetricPoint> Series(IReadOnlyList<ColonySample> samples, int cutoff, Func<ColonySample, float> selector)
        {
            List<MetricPoint> points = new List<MetricPoint>();
            if (samples == null)
                return points;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Tick >= cutoff)
                    points.Add(new MetricPoint(samples[i].Tick, selector(samples[i])));
            }
            return points;
        }

        private static List<string> Evidence(params string[] lines) => new List<string>(lines);

        private static List<string> Limit(List<string> source, int maximum)
        {
            if (source.Count <= maximum)
                return new List<string>(source);
            List<string> result = source.GetRange(0, maximum);
            result.Add("+ " + (source.Count - maximum) + " more");
            return result;
        }

        public static string FormatDuration(int ticks)
        {
            if (ticks <= 0) return "now";
            float hours = ticks / 2500f;
            if (hours < 1f) return Math.Max(1, (int)(hours * 60f)) + " min";
            if (hours < 48f) return hours.ToString("0.0") + " h";
            return (hours / 24f).ToString("0.0") + " days";
        }
    }
}
