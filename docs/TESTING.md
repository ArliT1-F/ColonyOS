# Test and release checklist

## Automated gate

- Build RimWorld 1.6 (`net472`) and 1.7 (`net10.0`) with warnings as errors.
- Run deterministic analytics/market/forecast tests.
- Parse all XML and verify keyed localization references.
- Verify no third-party DLL is bundled and both mod load folders resolve.

## Required in-game gate (not yet performed)

Test both 1.6 and 1.7, Core-only, with Harmony loaded before ColonyOS:

1. Start a new colony and load an existing save created without ColonyOS.
2. Confirm one scheduled snapshot, no per-tick map scans, and no errors in the log.
3. Open every department; check 100%, 125%, 150%, and 200% UI scales and narrow windows.
4. Confirm empty-history charts say that history is unavailable instead of drawing invented values.
5. Complete production bills; save, restart, load, and verify event history.
6. Open ground and orbital trade sessions; compare confirmed eligible goods with the trade dialog.
7. Execute purchases and sales; verify counts, prices, transaction direction, and merchant history after reload.
8. Verify forecasts against measured trends and inspect their evidence/confidence panels.
9. Save/load repeatedly, add/remove the mod on a disposable save, and check save integrity.
10. Profile a large colony with the dashboard closed and open. Check snapshot allocations and draw cost.
11. Test alongside at least one other UI mod and one trading mod; report conflicts before release.

No milestone or release is considered runtime-complete until the applicable checks and real screenshots exist.
