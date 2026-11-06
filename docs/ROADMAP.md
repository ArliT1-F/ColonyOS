# ColonyOS future roadmap

These are proposed additions, not implemented features or release commitments. Every addition must preserve ColonyOS's core principles: real measured evidence, explicit estimates, bounded resource use, local-only operation, and player control over every colony decision.

## Recommended sequence

1. Complete the runtime release gate.
2. Add measured production telemetry.
3. Add prediction lifecycle tracking and backtesting.
4. Add per-map and caravan perspectives.
5. Add player-directed trade planning.
6. Expand workforce diagnostics, accessibility, and the extension API.

## 0. Runtime reliability first

Before expanding the feature surface, complete the checks in [TESTING.md](TESTING.md) on RimWorld 1.6 and 1.7 with Core and Harmony only:

- New and existing saves, save/restart/load cycles, and removal on a disposable save.
- Production and transaction accuracy against the actual game interface.
- UI interaction and readability at 100%, 125%, 150%, and 200% UI scales.
- Large-colony collection, read-model, rendering, allocation, and save-size measurements.
- Compatibility with representative UI and trading mods.
- Actual in-game screenshots for every department and compact layouts.

Compilation and deterministic tests remain necessary but are not substitutes for this gate.

## 1. Measured production telemetry

- Observe actual work start/stop events to calculate workbench utilization instead of relying only on manned-at-snapshot observations.
- Build per-recipe throughput distributions from completed bill iterations.
- Add completion estimates only with sufficient measured history; show sample count, variability, and a range rather than false precision.
- Aggregate requirements across queued bills to identify upcoming ingredient constraints.
- Distinguish missing ingredients, unavailable labour, unusable workstations, and stations that are simply between jobs.
- Keep observation hooks narrow and avoid per-tick workstation scans.

## 2. Prediction lifecycle and backtesting

- Add new, acknowledged, snoozed, escalated, and resolved prediction states.
- Preserve useful forecast outcomes without unbounded history growth.
- Record what was predicted and compare it with the eventual measurement.
- Show historical hit/miss outcomes and use sufficient evidence to calibrate confidence.
- Explain why a warning appeared, changed, or disappeared.
- Add per-department severity thresholds, notification preferences, and quiet periods.
- Improve duplicate suppression using material condition changes rather than prediction IDs alone.

## 3. Per-map and caravan perspectives

- Add All maps and individual-home-map filtering.
- Compare power, food, storage, workforce, and production between settlements.
- Keep map-local constraints separate from colony-wide aggregate totals.
- Add caravan inventory, location, and trade information where supported by reliable APIs.
- Avoid implying that resources on one map are immediately available to another.

## 4. Player-directed trade planning

- Let players create sale lists without executing trades automatically.
- Show counted stock, reserved stock, current confirmed buyers, and historical actual sale prices.
- Add price history by good, trader type, and faction with evidence age and sample counts.
- Compare actual historical sale prices with current base market value.
- Estimate eligible-stock revenue only; do not call it accounting profit without a valid cost basis.
- Let players reserve goods so they are excluded from sale planning.
- Retain the Confirmed / Historical / Estimated distinction throughout every new view.

## 5. Workforce diagnostics

- Add a skill-coverage matrix showing primary, backup, and missing specialists.
- Detect work-priority inversions supported directly by current work settings.
- Highlight active production chains with only one qualified worker.
- Show schedule-related availability only when real timetable data supports it.
- Add deeper workload-conflict and overburden analysis only when it can be measured rather than inferred from cosmetic indicators.
- Never change work assignments automatically.

## 6. Operations feed and notification controls

- Add a compact chronological feed for important measured changes.
- Include completed production runs, trader arrivals/departures, executed trades, threshold crossings, and resolved warnings.
- Make feed entries link to their underlying evidence.
- Cap and persist the feed using the same bounded-retention principles as other history.

## 7. UI and accessibility

- Saved dashboard layouts and pinned widgets.
- A global command/search palette for goods, traders, workstations, maps, and predictions.
- Keyboard navigation and visible focus states.
- Colour-blind-safe status palettes and a high-contrast theme.
- Compact and comfortable density preferences.
- Optional CSV export of measured history, clearly separated from estimated values.
- Onboarding that explains evidence classes, snapshot cadence, and empty-history states.

## 8. Architecture, performance, and compatibility

- Add a small public provider API so other mods can supply metrics without changing ColonyOS UI code.
- Load optional compatibility providers only when their parent mod is present; keep them out of the Core-only requirement.
- Expose diagnostic counters for collection duration, read-model rebuild time, draw time, and retained-record counts.
- Add benchmark fixtures for large colonies, long histories, many traders, and hundreds of workstations.
- Consider streaming or ring-buffer metric storage if real save measurements justify it; do not restore full-colony object graphs for every historical sample.
- Add automated runtime smoke and screenshot workflows only in an environment with licensed game installations.

## Explicit non-goals

- Automatic bill, work, storage, or trading decisions.
- Opaque machine-learning demand predictions without inspectable evidence.
- Mandatory cloud services, accounts, analytics telemetry, or online operation.
- Broad Harmony patches where a narrow event or supported game API is available.
- Fake precision, fabricated historical data, or confidence presented as a guaranteed probability.
