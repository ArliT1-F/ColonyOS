using System.Collections.Generic;

namespace ColonyOS.Data
{
    // Pure test double for the read-model factory. Runtime persistence stays in Source/Data/HistoryStore.
    public sealed class HistoryStore
    {
        public int Revision { get; set; } = 1;
        public ColonySample Current { get; set; }
        public List<ColonySample> Samples { get; } = new List<ColonySample>();
        public List<ProductionEventRecord> ProductionEvents { get; } = new List<ProductionEventRecord>();
        public List<TradeTransactionRecord> Transactions { get; } = new List<TradeTransactionRecord>();
        public List<TraderVisitRecord> TraderVisits { get; } = new List<TraderVisitRecord>();
        public IReadOnlyList<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
