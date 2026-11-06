using System;
using System.Collections.Generic;
using ColonyOS.Data;
using ColonyOS.Analytics;
using ColonyOS.Forecasting;
using RimWorld;
using Verse;

namespace ColonyOS.Core
{
    public sealed class ColonyOSGameComponent : GameComponent
    {
        private readonly ColonyCollector collector = new ColonyCollector();
        private HistoryStore history = new HistoryStore();
        private int nextCollectionTick;
        private int nextForecastTick;
        private bool refreshRequested;
        private bool dashboardDirty = true;
        private DashboardCache dashboard = new DashboardCache();
        private readonly HashSet<string> notifiedPredictions = new HashSet<string>();
        private List<string> notifiedPredictionIds = new List<string>();

        public HistoryStore History => history;
        public bool RefreshQueued => refreshRequested;
        public DashboardCache Dashboard => dashboard;

        public ColonyOSGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref history, "colonyOSHistory");
            Scribe_Collections.Look(ref notifiedPredictionIds, "colonyOSNotifiedPredictions", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (history == null)
                    history = new HistoryStore();
                notifiedPredictionIds = notifiedPredictionIds ?? new List<string>();
                notifiedPredictions.Clear();
                for (int i = 0; i < notifiedPredictionIds.Count; i++)
                    notifiedPredictions.Add(notifiedPredictionIds[i]);
            }
        }

        public override void StartedNewGame()
        {
            refreshRequested = true;
        }

        public override void LoadedGame()
        {
            refreshRequested = true;
        }

        public override void FinalizeInit()
        {
            refreshRequested = true;
            nextCollectionTick = 0;
            nextForecastTick = 0;
        }

        public override void GameComponentTick()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.TickManager == null)
                return;
            int tick = Find.TickManager.TicksGame;
            if (refreshRequested || nextCollectionTick <= 0 || tick >= nextCollectionTick)
                CollectScheduled(tick);
            if (nextForecastTick <= 0 || tick >= nextForecastTick)
                ForecastScheduled(tick);
            if (dashboardDirty)
                RebuildDashboard();
        }

        public void RequestRefresh()
        {
            refreshRequested = true;
        }

        public void MarkDashboardDirty()
        {
            dashboardDirty = true;
        }

        private void RebuildDashboard()
        {
            dashboardDirty = false;
            try
            {
                dashboard = DashboardViewModelFactory.Build(history);
            }
            catch (Exception ex)
            {
                Log.Error("[ColonyOS] Dashboard read-model update failed. " + ex);
            }
        }

        private void CollectScheduled(int tick)
        {
            refreshRequested = false;
            try
            {
                history.Prune(tick, ColonyOSMod.Settings.RetentionDays);
                ColonySample sample = collector.Collect(history, tick);
                history.RecordSample(sample);
                dashboardDirty = true;
                nextCollectionTick = tick + ColonyOSMod.Settings.CollectionIntervalTicks;
            }
            catch (Exception ex)
            {
                nextCollectionTick = tick + 2500;
                Log.Error("[ColonyOS] Scheduled collection failed; collection will retry later. " + ex);
            }
        }

        private void ForecastScheduled(int tick)
        {
            try
            {
                ForecastOptions options = new ForecastOptions
                {
                    BatteryWarningPct = ColonyOSMod.Settings.BatteryWarningPct,
                    StorageWarningPct = ColonyOSMod.Settings.StorageWarningPct,
                    HorizonHours = ColonyOSMod.Settings.ForecastHorizonHours,
                    RetentionDays = ColonyOSMod.Settings.RetentionDays
                };
                List<Prediction> next = ForecastEngine.Generate(history.Current, history.Samples, history.TraderVisits, history.Transactions, options, tick);
                history.SetPredictions(next);
                dashboardDirty = true;
                NotifyNewCritical(next);
                nextForecastTick = tick + ColonyOSMod.Settings.ForecastIntervalTicks;
            }
            catch (Exception ex)
            {
                nextForecastTick = tick + 2500;
                Log.Error("[ColonyOS] Forecast update failed; forecasting will retry later. " + ex);
            }
        }

        private void NotifyNewCritical(List<Prediction> predictions)
        {
            if (!ColonyOSMod.Settings.CriticalNotifications)
                return;
            HashSet<string> current = new HashSet<string>();
            for (int i = 0; i < predictions.Count; i++)
            {
                Prediction prediction = predictions[i];
                current.Add(prediction.Id);
                if (prediction.Severity == PredictionSeverity.Critical && notifiedPredictions.Add(prediction.Id))
                {
                    Messages.Message("ColonyOS: " + prediction.Title, MessageTypeDefOf.NegativeEvent, false);
                }
            }
            notifiedPredictions.RemoveWhere(id => !current.Contains(id));
            notifiedPredictionIds.Clear();
            notifiedPredictionIds.AddRange(notifiedPredictions);
        }
    }
}
