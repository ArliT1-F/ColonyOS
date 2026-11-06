using System;
using System.Collections.Generic;
using ColonyOS.Data;

namespace ColonyOS.Analytics
{
    public static class TrendAnalyzer
    {
        public const int TicksPerDay = 60000;

        public static ForecastLine ProjectToThreshold(
            IReadOnlyList<MetricPoint> points,
            float threshold,
            bool requireFalling,
            int minimumSamples = 6,
            int minimumSpanTicks = TicksPerDay / 2)
        {
            ForecastLine result = new ForecastLine();
            if (points == null || points.Count < minimumSamples)
            {
                result.Reason = "At least " + minimumSamples + " historical samples are required.";
                return result;
            }

            int count = points.Count;
            double span = points[count - 1].Tick - points[0].Tick;
            if (span < minimumSpanTicks)
            {
                result.Reason = "The samples do not cover enough time yet.";
                return result;
            }

            double origin = points[0].Tick;
            double meanX = 0;
            double meanY = 0;
            for (int i = 0; i < count; i++)
            {
                meanX += (points[i].Tick - origin) / TicksPerDay;
                meanY += points[i].Value;
            }
            meanX /= count;
            meanY /= count;

            double covariance = 0;
            double varianceX = 0;
            for (int i = 0; i < count; i++)
            {
                double x = (points[i].Tick - origin) / TicksPerDay;
                double dx = x - meanX;
                covariance += dx * (points[i].Value - meanY);
                varianceX += dx * dx;
            }
            if (varianceX < 0.0000001)
            {
                result.Reason = "The timeline has insufficient variation.";
                return result;
            }

            double slope = covariance / varianceX;
            if ((requireFalling && slope >= -0.0000001) || (!requireFalling && slope <= 0.0000001))
            {
                result.Reason = requireFalling
                    ? "No sustained downward trend is measured."
                    : "No sustained upward trend is measured.";
                return result;
            }

            double intercept = meanY - slope * meanX;
            double thresholdDay = (threshold - intercept) / slope;
            double targetTick = origin + thresholdDay * TicksPerDay;
            if (targetTick < points[count - 1].Tick || double.IsNaN(targetTick) || double.IsInfinity(targetTick))
            {
                result.Reason = "The measured trend does not cross the threshold in the future.";
                return result;
            }

            double totalSquares = 0;
            double residualSquares = 0;
            for (int i = 0; i < count; i++)
            {
                double x = (points[i].Tick - origin) / TicksPerDay;
                double actual = points[i].Value;
                double expected = intercept + slope * x;
                totalSquares += Math.Pow(actual - meanY, 2);
                residualSquares += Math.Pow(actual - expected, 2);
            }
            double rSquared = totalSquares < 0.0000001
                ? (residualSquares < 0.0000001 ? 1.0 : 0.0)
                : Math.Max(0.0, 1.0 - residualSquares / totalSquares);
            if (rSquared < 0.2)
            {
                result.Reason = "The trend is too noisy for a responsible forecast.";
                return result;
            }

            double coverage = Math.Min(1.0, span / (TicksPerDay * 3.0));
            double sampleFactor = Math.Min(1.0, (count - minimumSamples + 1) / 12.0);
            result.Available = true;
            result.SlopePerDay = (float)slope;
            result.TargetAtTick = (float)targetTick;
            result.RSquared = (float)rSquared;
            result.Confidence = Clamp(0.25f + (float)rSquared * 0.4f + (float)coverage * 0.15f + (float)sampleFactor * 0.1f, 0.2f, 0.95f);
            result.Reason = "Least-squares trend; R² " + rSquared.ToString("0.00") + " from " + count + " samples.";
            return result;
        }

        public static float PercentChange(IReadOnlyList<MetricPoint> points)
        {
            if (points == null || points.Count < 2)
                return 0f;
            float first = points[0].Value;
            float last = points[points.Count - 1].Value;
            if (Math.Abs(first) < 0.0001f)
                return 0f;
            return (last - first) / Math.Abs(first);
        }

        public static TrendDirection Direction(float delta, float tolerance = 0.001f)
        {
            if (delta > tolerance) return TrendDirection.Rising;
            return delta < -tolerance ? TrendDirection.Falling : TrendDirection.Steady;
        }

        public static List<MetricPoint> Downsample(IReadOnlyList<MetricPoint> source, int maximumPoints)
        {
            List<MetricPoint> result = new List<MetricPoint>();
            if (source == null || maximumPoints <= 0)
                return result;
            if (source.Count <= maximumPoints)
            {
                for (int i = 0; i < source.Count; i++)
                    result.Add(source[i]);
                return result;
            }

            for (int bucket = 0; bucket < maximumPoints; bucket++)
            {
                int start = bucket * source.Count / maximumPoints;
                int end = (bucket + 1) * source.Count / maximumPoints;
                if (end <= start)
                    end = start + 1;
                double sum = 0;
                for (int i = start; i < end && i < source.Count; i++)
                    sum += source[i].Value;
                int actualEnd = Math.Min(end, source.Count);
                result.Add(new MetricPoint(source[actualEnd - 1].Tick, (float)(sum / (actualEnd - start))));
            }
            return result;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
