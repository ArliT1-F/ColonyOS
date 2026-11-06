using System;
using System.Collections.Generic;
using Verse;

namespace ColonyOS.Data
{
    public sealed class HistoryStore : IExposable
    {
        public const int TicksPerDay = 60000;
        public const int AbsoluteMaxSamples = 6000;
        public const int AbsoluteMaxProductionEvents = 6000;
        public const int AbsoluteMaxTransactions = 3000;
        public const int AbsoluteMaxTraderVisits = 1200;

        public List<ColonySample> Samples = new List<ColonySample>();
        public List<ProductionEventRecord> ProductionEvents = new List<ProductionEventRecord>();
        public List<TradeTransactionRecord> Transactions = new List<TradeTransactionRecord>();
        public List<TraderVisitRecord> TraderVisits = new List<TraderVisitRecord>();

        private List<Prediction> predictions = new List<Prediction>();

        public int Revision { get; private set; }
        public ColonySample Current => Samples.Count == 0 ? null : Samples[Samples.Count - 1];
        public IReadOnlyList<Prediction> Predictions => predictions;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Samples, "colonyOSSamples", LookMode.Deep);
            Scribe_Collections.Look(ref ProductionEvents, "colonyOSProductionEvents", LookMode.Deep);
            Scribe_Collections.Look(ref Transactions, "colonyOSTransactions", LookMode.Deep);
            Scribe_Collections.Look(ref TraderVisits, "colonyOSTraderVisits", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Samples = Samples ?? new List<ColonySample>();
                ProductionEvents = ProductionEvents ?? new List<ProductionEventRecord>();
                Transactions = Transactions ?? new List<TradeTransactionRecord>();
                TraderVisits = TraderVisits ?? new List<TraderVisitRecord>();
                Samples.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                ProductionEvents.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                Transactions.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                for (int i = 0; i < TraderVisits.Count; i++)
                    TraderVisits[i].Active = false;
                Revision++;
            }
        }

        public void RecordSample(ColonySample sample)
        {
            if (sample == null)
                return;
            if (Samples.Count > 0 && Samples[Samples.Count - 1].Tick == sample.Tick)
                Samples[Samples.Count - 1] = sample;
            else
                Samples.Add(sample);
            TrimFront(Samples, AbsoluteMaxSamples);
            Revision++;
        }

        public void RecordProduction(ProductionEventRecord record)
        {
            if (record == null)
                return;
            ProductionEvents.Add(record);
            TrimFront(ProductionEvents, AbsoluteMaxProductionEvents);
            Revision++;
        }

        public void RecordTransaction(TradeTransactionRecord record)
        {
            if (record == null || record.Count <= 0)
                return;
            Transactions.Add(record);
            TrimFront(Transactions, AbsoluteMaxTransactions);
            Revision++;
        }

        public void MarkAllTradersAbsent()
        {
            for (int i = 0; i < TraderVisits.Count; i++)
                TraderVisits[i].Active = false;
        }

        public void RecordTraderVisit(TraderVisitRecord observed)
        {
            if (observed == null || string.IsNullOrEmpty(observed.TraderKey))
                return;

            TraderVisitRecord existing = null;
            for (int i = TraderVisits.Count - 1; i >= 0; i--)
            {
                TraderVisitRecord candidate = TraderVisits[i];
                if (candidate.TraderKey == observed.TraderKey && candidate.Active)
                {
                    existing = candidate;
                    break;
                }
            }

            if (existing == null)
            {
                TraderVisits.Add(observed);
            }
            else
            {
                existing.LastSeenTick = observed.LastSeenTick;
                existing.Active = true;
                existing.PurchasingCapacity = observed.PurchasingCapacity;
                existing.EligibleGoods = observed.EligibleGoods;
                existing.MapId = observed.MapId;
                existing.MapLabel = observed.MapLabel;
            }

            TrimFront(TraderVisits, AbsoluteMaxTraderVisits);
            Revision++;
        }

        public void SetPredictions(List<Prediction> next)
        {
            predictions = next ?? new List<Prediction>();
            Revision++;
        }

        public void Prune(int nowTick, int retentionDays)
        {
            int cutoff = nowTick - Math.Max(1, retentionDays) * TicksPerDay;
            Samples.RemoveAll(x => x.Tick < cutoff);
            ProductionEvents.RemoveAll(x => x.Tick < cutoff);
            Transactions.RemoveAll(x => x.Tick < cutoff);
            TraderVisits.RemoveAll(x => x.LastSeenTick < cutoff);
            TrimFront(Samples, AbsoluteMaxSamples);
            TrimFront(ProductionEvents, AbsoluteMaxProductionEvents);
            TrimFront(Transactions, AbsoluteMaxTransactions);
            TrimFront(TraderVisits, AbsoluteMaxTraderVisits);
            Revision++;
        }

        public int ProductionIterationsSince(int tick)
        {
            int count = 0;
            for (int i = ProductionEvents.Count - 1; i >= 0; i--)
            {
                if (ProductionEvents[i].Tick < tick)
                    break;
                count++;
            }
            return count;
        }

        private static void TrimFront<T>(List<T> list, int maximum)
        {
            int excess = list.Count - maximum;
            if (excess > 0)
                list.RemoveRange(0, excess);
        }
    }
}
