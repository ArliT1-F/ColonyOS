# Research and framework decision (2026-11-06)

## Game API evidence

The implementation was checked against public RimWorld decompiled API source and is compiled in CI against `Krafs.Rimworld.Ref` reference assemblies generated from actual game assemblies. The primary APIs used are:

- `PowerNetManager.AllNetsListForReading`, `PowerNet.powerComps`, `PowerNet.batteryComps`, `CompPowerTrader.PowerOutput`, `CompPowerBattery.StoredEnergy` and `Props.storedEnergyMax`.
- `Map.wealthWatcher` and its total/item/building/pawn values.
- `ResourceCounter.AllCountedAmounts` and `TotalHumanEdibleNutrition`.
- `Building_WorkTable.BillStack`, `Bill_Production`, `RecipeDef` ingredients and skill requirements.
- `Zone_Stockpile.CellCount` and `SpaceRemaining`.
- `Pawn_WorkSettings.GetPriority`, `Pawn_SkillTracker.skills`, and `Pawn_MindState.IsIdle`.
- `ITrader.ColonyThingsWillingToBuy`, `Tradeable.GetPriceFor`, `TradeDeal.TryExecute`, and `ILoadReferenceable.GetUniqueLoadID` for stable orbital-ship identity.

No private member is publicized and no guessed reflection target is used. The three patch targets are strongly typed method signatures. CI compilation caught and replaced an inaccessible `PassingShip.loadID`, a line-drawing overload mismatch, and an invalid property-by-reference settings call before these fixes were retained.

## UI candidates

| Candidate | Observed evidence | Decision |
|---|---|---|
| RimUI Framework 0.4 / core 0.9.3 | Documents 1.6/1.7, grid, themes, 60+ components, no Harmony. Repository contains binaries/docs but no source or license file was visible, so third-party redistribution and API auditing are not suitable. | Not selected. |
| Cosmere.Lightweave | MIT source, composable retained-node layout, typography, navigation and charts. Current metadata observed during evaluation lists 1.6 and additional Concord/RimLogging dependencies; the repository has a large runtime/rendering surface and explicitly permits breaking primitive APIs. | Technically capable, but too much dependency/version surface for this initial release. |
| RUIF | MIT source and extensive components/charts; current metadata lists 1.6 only and fixed design tokens with theming removed. | Does not meet both version/theming needs without another layer. |
| Small internal renderer | Uses verified `Widgets`, `GUI`, and static PNG assets; custom navigation, responsive grids, rounded cards, line/bar/donut charts, filters, tooltips, and themes. | Selected: smallest dependency set and direct control over visual quality. Analytics remains UI-independent. |

This is not a vanilla `Listing_Standard` interface. Only low-level drawing/input primitives come from the game; the application shell, design system, charts, and interaction model are ColonyOS code.

## Validation status

Compilation targets both game versions. Actual in-game visual/performance testing still requires a licensed RimWorld installation and is not claimed by repository-only CI. See `docs/TESTING.md` for the release gate.
