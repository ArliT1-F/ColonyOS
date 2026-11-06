# ColonyOS architecture

## Dependency direction

`RimWorld collectors / Harmony events -> history store -> analytics + market + forecasting -> read models -> UI`

The model, analytics, market-ranking, and forecasting files do not import `Verse`, `RimWorld`, `UnityEngine`, or a UI framework. Game API access is confined to `Core`, `Data/ColonyCollector`, `Compatibility`, and the persistence adapters. The UI reads the latest completed snapshot; it never scans a map and never mutates colony state.

## Runtime

- `ColonyOSGameComponent` is the save-local service. It schedules collection and owns bounded history.
- `ColonyCollector` reads player-home maps at a configurable interval (default 2,500 ticks), not every tick.
- Narrow Harmony hooks record bill iterations, trader visits/eligibility, and executed transactions. There are no transpilers.
- `HistoryStore` uses RimWorld `Scribe` collections and defensive post-load initialization. Adding or removing the mod does not alter vanilla records.
- `ForecastEngine` recalculates from shared snapshots and events. Predictions have evidence, confidence, timeframe, severity, and a department link.
- `DashboardViewModelFactory` runs on scheduled game ticks. It downsamples every time range, precomputes rankings and food/storage projections, caps visible ledger rows without truncating totals, and shares immutable-in-practice results with all pages.
- `ColonyOSWindow` is a custom full-window application shell. Rendering has no map scans or game-state mutations. Search/sort caches are revision-keyed; compact layouts collapse navigation and stack detail panels.

## Measurement boundaries

- Power values are current measured `CompPowerTrader.PowerOutput` and battery values on actual `PowerNet` instances. Building output is not described as a per-building energy meter over time.
- Production output means completed bill iterations recorded by `Notify_IterationCompleted`; variable recipe yields are not guessed.
- Workbench utilization means currently manned/usable workbenches at sample time, not a fabricated uptime statistic.
- Storage pressure uses the same stockpile-cell/free-slot estimate as `Zone_Stockpile`; it is not an exact universal item-capacity calculation and does not include every modded container.
- Resource amounts come from RimWorld's counted resource readout (eligible storage), not every item anywhere on a map.
- Food projections require a significant measured downward trend in stored human-edible nutrition. ColonyOS does not assume a universal meals-per-colonist constant.
- Market eligibility comes from `ITrader.ColonyThingsWillingToBuy` and actual trade session tradeables. The “Cannot” profile view is only the complement among counted resources; uncounted items are not classified. Sale prices in history are captured before a successful `TradeDeal` resets.
- Forecast rankings are explicitly estimated and require current eligibility and/or transaction history. There are no invented hidden preferences.
- Dashed food/storage lines are linear extrapolations of measured series. They are labelled estimated and are never inserted into measured chart data. No uncertainty band is drawn because the available model does not justify one.
- Net trade flow is revenue minus spending, not accounting profit: RimWorld does not provide a universal cost basis for produced or acquired items.

## Persistence bounds

Defaults retain 120 in-game days and cap samples/events independently: 6,000 snapshots, 6,000 production events, 3,000 transactions, and 1,200 trader visits. Only the newest snapshot retains detailed buildings, resources, skills, and workforce; older snapshots are reduced to scalar metrics before another sample is added and during post-load migration. Trader identities are reused within an active visit instead of being duplicated at each collection.

UI charts downsample to at most 240 points and the visible transaction ledger to 250 rows; aggregate totals still use every transaction in the selected range. Changing retention prunes on the next collection/save cycle. These are hard safety bounds, not a claim that every detailed metric is retained for every historical tick.

## Version builds

Shared source builds to `net472` + Harmony 2 for RimWorld 1.6 and `net10.0` + Harmony 3 for RimWorld 1.7. `Krafs.Rimworld.Ref` reference assemblies make both CI builds independent of a local game installation. Version-specific code uses `RW_1_7` only where necessary.
