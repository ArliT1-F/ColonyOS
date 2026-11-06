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
- `ColonyOSWindow` is a custom full-window application shell. Rendering has no game-state queries or analytical scans.

## Measurement boundaries

- Power values are current measured `CompPowerTrader.PowerOutput` and battery values on actual `PowerNet` instances. Building output is not described as a per-building energy meter over time.
- Production output means completed bill iterations recorded by `Notify_IterationCompleted`; variable recipe yields are not guessed.
- Workbench utilization means currently manned/usable workbenches at sample time, not a fabricated uptime statistic.
- Storage pressure uses the same stockpile-cell/free-slot estimate as `Zone_Stockpile`; it is not an exact universal item-capacity calculation and does not include every modded container.
- Resource amounts come from RimWorld's counted resource readout (eligible storage), not every item anywhere on a map.
- Food projections require a significant measured downward trend in stored human-edible nutrition. ColonyOS does not assume a universal meals-per-colonist constant.
- Market eligibility comes from `ITrader.ColonyThingsWillingToBuy` and actual trade session tradeables. Sale prices in history are captured before a successful `TradeDeal` resets.
- Forecast rankings are explicitly estimated and require current eligibility and/or transaction history. There are no invented hidden preferences.

## Persistence bounds

Defaults retain 120 in-game days and cap samples/events independently. UI charts downsample to a fixed point budget. Changing retention prunes on the next collection/save cycle.

## Version builds

Shared source builds to `net472` + Harmony 2 for RimWorld 1.6 and `net10.0` + Harmony 3 for RimWorld 1.7. `Krafs.Rimworld.Ref` reference assemblies make both CI builds independent of a local game installation. Version-specific code uses `RW_1_7` only where necessary.
