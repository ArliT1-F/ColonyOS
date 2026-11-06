using System.Collections.Generic;
using ColonyOS.Core;
using ColonyOS.Data;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyOS.Compatibility
{
    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Notify_IterationCompleted))]
    internal static class ProductionIterationObservation
    {
        private static void Postfix(Bill_Production __instance, Pawn billDoer)
        {
            if (__instance?.recipe == null || ColonyOSMod.Component == null)
                return;
            Thing workbench = __instance.billStack?.billGiver as Thing;
            ColonyOSMod.Component.History.RecordProduction(new ProductionEventRecord
            {
                Tick = Find.TickManager.TicksGame,
                MapId = __instance.Map?.uniqueID ?? -1,
                RecipeDefName = __instance.recipe.defName,
                RecipeLabel = __instance.recipe.label,
                WorkbenchLabel = workbench == null ? string.Empty : workbench.LabelShort
            });
            ColonyOSMod.Component.MarkDashboardDirty();
        }
    }

    [HarmonyPatch(typeof(TradeSession), nameof(TradeSession.SetupWith))]
    internal static class TradeSessionObservation
    {
        private static void Postfix(ITrader newTrader, Pawn newPlayerNegotiator)
        {
            if (newTrader == null || newPlayerNegotiator == null || ColonyOSMod.Component == null)
                return;
            Map map = newPlayerNegotiator.MapHeld ?? (newTrader as TradeShip)?.Map;
            ColonyCollector.RecordTrader(
                ColonyOSMod.Component.History,
                newTrader,
                newPlayerNegotiator,
                map,
                Find.TickManager.TicksGame,
                TraderIdentity.Key(newTrader));
            ColonyOSMod.Component.MarkDashboardDirty();
        }
    }

    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    internal static class TradeExecutionObservation
    {
        private static void Prefix(TradeDeal __instance, ref TradeExecutionSnapshot __state)
        {
            __state = null;
            if (TradeSession.giftMode || TradeSession.trader == null || __instance?.AllTradeables == null)
                return;

            ITrader trader = TradeSession.trader;
            __state = new TradeExecutionSnapshot
            {
                Tick = Find.TickManager.TicksGame,
                TraderKey = TraderIdentity.Key(trader),
                TraderName = trader.TraderName,
                TraderKindDefName = trader.TraderKind?.defName ?? string.Empty,
                TraderKindLabel = trader.TraderKind?.label ?? string.Empty,
                FactionName = trader.Faction?.Name ?? "Independent",
                Currency = trader.TradeCurrency.ToString()
            };

            List<Tradeable> tradeables = __instance.AllTradeables;
            for (int i = 0; i < tradeables.Count; i++)
            {
                Tradeable tradeable = tradeables[i];
                if (tradeable == null || tradeable.IsCurrency || tradeable.ThingDef == null)
                    continue;
                TradeAction action = tradeable.ActionToDo;
                if (action == TradeAction.None)
                    continue;
                int count = action == TradeAction.PlayerSells
                    ? tradeable.CountToTransferToDestination
                    : tradeable.CountToTransferToSource;
                if (count <= 0)
                    continue;
                float unitPrice = tradeable.GetPriceFor(action);
                __state.Lines.Add(new PendingTradeLine
                {
                    Direction = action == TradeAction.PlayerSells ? TradeDirection.PlayerSold : TradeDirection.PlayerBought,
                    DefName = tradeable.ThingDef.defName,
                    Label = tradeable.ThingDef.label,
                    Count = count,
                    UnitPrice = unitPrice,
                    TotalValue = unitPrice * count
                });
            }
        }

        private static void Postfix(bool __result, bool actuallyTraded, TradeExecutionSnapshot __state)
        {
            if (!__result || !actuallyTraded || __state == null || ColonyOSMod.Component == null)
                return;
            for (int i = 0; i < __state.Lines.Count; i++)
            {
                PendingTradeLine line = __state.Lines[i];
                ColonyOSMod.Component.History.RecordTransaction(new TradeTransactionRecord
                {
                    Tick = __state.Tick,
                    TraderKey = __state.TraderKey,
                    TraderName = __state.TraderName,
                    TraderKindDefName = __state.TraderKindDefName,
                    TraderKindLabel = __state.TraderKindLabel,
                    FactionName = __state.FactionName,
                    Currency = __state.Currency,
                    Direction = line.Direction,
                    DefName = line.DefName,
                    Label = line.Label,
                    Count = line.Count,
                    UnitPrice = line.UnitPrice,
                    TotalValue = line.TotalValue
                });
            }
            ColonyOSMod.Component.MarkDashboardDirty();
        }
    }

    internal static class TraderIdentity
    {
        public static string Key(ITrader trader)
        {
            if (trader is Pawn pawn)
                return "pawn:" + pawn.thingIDNumber;
            if (trader is TradeShip ship)
                return "ship:" + ship.loadID;
            if (trader is ILoadReferenceable referenceable)
                return "trader:" + referenceable.GetUniqueLoadID();
            return "trader:" + trader.TraderName + ":" + trader.TraderKind?.defName;
        }
    }

    internal sealed class TradeExecutionSnapshot
    {
        public int Tick;
        public string TraderKey;
        public string TraderName;
        public string TraderKindDefName;
        public string TraderKindLabel;
        public string FactionName;
        public string Currency;
        public readonly List<PendingTradeLine> Lines = new List<PendingTradeLine>();
    }

    internal sealed class PendingTradeLine
    {
        public TradeDirection Direction;
        public string DefName;
        public string Label;
        public int Count;
        public float UnitPrice;
        public float TotalValue;
    }
}
