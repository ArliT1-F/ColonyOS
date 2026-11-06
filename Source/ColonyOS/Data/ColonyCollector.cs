using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyOS.Data
{
    /// <summary>
    /// The only scheduled whole-map reader. It creates plain read models and never runs from UI drawing.
    /// </summary>
    public sealed class ColonyCollector
    {
        private readonly Dictionary<string, ResourceSnapshot> resources = new Dictionary<string, ResourceSnapshot>();
        private readonly Dictionary<string, SkillAccumulator> skills = new Dictionary<string, SkillAccumulator>();
        private readonly Dictionary<string, WorkforceAccumulator> workforce = new Dictionary<string, WorkforceAccumulator>();
        private readonly HashSet<Thing> mannedWorkbenches = new HashSet<Thing>();

        public ColonySample Collect(HistoryStore history, int tick)
        {
            ColonySample sample = new ColonySample { Tick = tick };
            resources.Clear();
            skills.Clear();
            workforce.Clear();
            mannedWorkbenches.Clear();

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !map.IsPlayerHome)
                    continue;

                sample.MapCount++;
                CollectWealth(map, sample);
                CollectPower(map, sample);
                CollectResources(map, sample);
                CollectStorage(map, sample);
                CollectPopulationAndProduction(map, sample);
            }

            sample.ProductionIterationsLastDay = history.ProductionIterationsSince(tick - HistoryStore.TicksPerDay);
            sample.Resources.AddRange(resources.Values);
            sample.Resources.Sort((a, b) => b.Count.CompareTo(a.Count));

            foreach (SkillAccumulator value in skills.Values)
            {
                sample.Skills.Add(new SkillSnapshot
                {
                    DefName = value.DefName,
                    Label = value.Label,
                    AverageLevel = value.PawnCount == 0 ? 0f : (float)value.TotalLevel / value.PawnCount,
                    HighestLevel = value.HighestLevel,
                    PawnsAtEightPlus = value.AtEightPlus
                });
            }
            sample.Skills.Sort((a, b) => b.AverageLevel.CompareTo(a.AverageLevel));

            foreach (WorkforceAccumulator value in workforce.Values)
            {
                sample.Workforce.Add(new WorkforceSnapshot
                {
                    DefName = value.DefName,
                    Label = value.Label,
                    EnabledPawns = value.Enabled,
                    AvailablePawns = value.Available,
                    PriorityScore = value.PriorityScore
                });
            }
            sample.Workforce.Sort((a, b) => b.EnabledPawns.CompareTo(a.EnabledPawns));

            history.MarkAllTradersAbsent();
            ObserveRegionalTraders(history, maps, tick);
            return sample;
        }

        private static void CollectWealth(Map map, ColonySample sample)
        {
            sample.WealthTotal += map.wealthWatcher.WealthTotal;
            sample.WealthItems += map.wealthWatcher.WealthItems;
            sample.WealthBuildings += map.wealthWatcher.WealthBuildings;
            sample.WealthPawns += map.wealthWatcher.WealthPawns;
            sample.FoodNutrition += map.resourceCounter.TotalHumanEdibleNutrition;
        }

        private void CollectPower(Map map, ColonySample sample)
        {
            List<PowerNet> nets = map.powerNetManager.AllNetsListForReading;
            for (int n = 0; n < nets.Count; n++)
            {
                PowerNet net = nets[n];
                PowerNetworkSnapshot network = new PowerNetworkSnapshot { NetworkId = sample.PowerNetworks.Count };
                for (int i = 0; i < net.powerComps.Count; i++)
                {
                    CompPowerTrader power = net.powerComps[i];
                    float output = power.PowerOn ? power.PowerOutput : 0f;
                    float potential = power.PowerOutput;
                    if (output > 0f)
                    {
                        sample.PowerGeneration += output;
                        network.Generation += output;
                    }
                    else if (output < 0f)
                    {
                        sample.PowerConsumption -= output;
                        network.Consumption -= output;
                    }
                    if (potential > 0f)
                    {
                        sample.PotentialGeneration += potential;
                        network.PotentialGeneration += potential;
                    }

                    sample.PowerDevices.Add(new PowerDeviceSnapshot
                    {
                        BuildingDefName = power.parent.def.defName,
                        Label = power.parent.LabelShort,
                        NetworkId = network.NetworkId,
                        Output = output,
                        PotentialOutput = potential,
                        Active = power.PowerOn
                    });
                }

                for (int i = 0; i < net.batteryComps.Count; i++)
                {
                    CompPowerBattery battery = net.batteryComps[i];
                    sample.BatteryCharge += battery.StoredEnergy;
                    sample.BatteryCapacity += battery.Props.storedEnergyMax;
                    network.BatteryCharge += battery.StoredEnergy;
                    network.BatteryCapacity += battery.Props.storedEnergyMax;
                    if (battery.StoredEnergy > 0f)
                    {
                        sample.BatterySelfDischarge += 5f;
                        network.BatterySelfDischarge += 5f;
                    }
                }
                sample.PowerNetworks.Add(network);
            }
        }

        private void CollectResources(Map map, ColonySample sample)
        {
            string mapLabel = MapLabel(map);
            foreach (KeyValuePair<ThingDef, int> pair in map.resourceCounter.AllCountedAmounts)
            {
                if (pair.Key == null || pair.Value <= 0)
                    continue;

                string key = pair.Key.defName;
                if (!resources.TryGetValue(key, out ResourceSnapshot resource))
                {
                    resource = new ResourceSnapshot
                    {
                        DefName = key,
                        Label = pair.Key.label,
                        Category = pair.Key.FirstThingCategory == null ? "Other" : pair.Key.FirstThingCategory.label,
                        BaseMarketValue = pair.Key.BaseMarketValue
                    };
                    resources.Add(key, resource);
                }
                resource.Count += pair.Value;
                if (!resource.MapLabels.Contains(mapLabel))
                    resource.MapLabels.Add(mapLabel);
            }
        }

        private static void CollectStorage(Map map, ColonySample sample)
        {
            List<Zone> zones = map.zoneManager.AllZones;
            string mapLabel = MapLabel(map);
            for (int i = 0; i < zones.Count; i++)
            {
                if (!(zones[i] is Zone_Stockpile stockpile))
                    continue;
                int free = Math.Max(0, stockpile.SpaceRemaining);
                sample.StorageCellCount += stockpile.CellCount;
                sample.StorageFreeSlotEstimate += free;
                sample.StorageZones.Add(new StorageZoneSnapshot
                {
                    MapLabel = mapLabel,
                    Label = stockpile.label,
                    Cells = stockpile.CellCount,
                    FreeSlots = free
                });
            }
            sample.StorageZones.Sort((a, b) => b.Utilization.CompareTo(a.Utilization));
        }

        private void CollectPopulationAndProduction(Map map, ColonySample sample)
        {
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            sample.Population += colonists.Count;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (!pawn.Downed)
                    sample.AvailableWorkforce++;
                if (pawn.mindState != null && pawn.mindState.IsIdle)
                    sample.IdlePawns++;

                if (pawn.CurJob != null)
                {
                    if (pawn.CurJob.targetA.Thing != null)
                        mannedWorkbenches.Add(pawn.CurJob.targetA.Thing);
                    if (pawn.CurJob.targetB.Thing != null)
                        mannedWorkbenches.Add(pawn.CurJob.targetB.Thing);
                }

                CollectPawnSkills(pawn);
                CollectPawnWork(pawn);
            }

            IEnumerable<Building_WorkTable> tables = map.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>();
            foreach (Building_WorkTable table in tables)
            {
                BillStack stack = table.BillStack;
                bool usable = table.CurrentlyUsableForBills();
                bool manned = mannedWorkbenches.Contains(table);
                sample.TotalWorkbenches++;
                if (usable)
                    sample.UsableWorkbenches++;
                if (manned)
                    sample.MannedWorkbenches++;

                WorkbenchSnapshot model = new WorkbenchSnapshot
                {
                    Label = table.LabelShort,
                    BuildingDefName = table.def.defName,
                    BillCount = stack.Count,
                    Usable = usable,
                    Manned = manned
                };

                Bill firstActive = null;
                for (int i = 0; i < stack.Count; i++)
                {
                    Bill bill = stack[i];
                    if (!bill.ShouldDoNow())
                        continue;
                    model.ActiveBillCount++;
                    sample.ActiveBills++;
                    if (firstActive == null)
                        firstActive = bill;
                }

                if (firstActive != null)
                {
                    model.CurrentRecipe = firstActive.LabelCap;
                    CollectRecipeRequirements(firstActive, colonists, model);
                    CollectIngredients(map, firstActive, model);
                }
                sample.Workbenches.Add(model);
            }
        }

        private void CollectPawnSkills(Pawn pawn)
        {
            if (pawn.skills == null || pawn.skills.skills == null)
                return;
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                SkillRecord record = pawn.skills.skills[i];
                if (record.def == null || record.TotallyDisabled)
                    continue;
                if (!skills.TryGetValue(record.def.defName, out SkillAccumulator total))
                {
                    total = new SkillAccumulator { DefName = record.def.defName, Label = record.def.label };
                    skills.Add(record.def.defName, total);
                }
                total.TotalLevel += record.Level;
                total.PawnCount++;
                total.HighestLevel = Math.Max(total.HighestLevel, record.Level);
                if (record.Level >= 8)
                    total.AtEightPlus++;
            }
        }

        private void CollectPawnWork(Pawn pawn)
        {
            if (pawn.workSettings == null || !pawn.workSettings.EverWork)
                return;
            List<WorkTypeDef> workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            for (int i = 0; i < workTypes.Count; i++)
            {
                WorkTypeDef work = workTypes[i];
                int priority = pawn.workSettings.GetPriority(work);
                if (priority <= 0)
                    continue;
                if (!workforce.TryGetValue(work.defName, out WorkforceAccumulator total))
                {
                    total = new WorkforceAccumulator { DefName = work.defName, Label = work.labelShort };
                    workforce.Add(work.defName, total);
                }
                total.Enabled++;
                if (!pawn.Downed)
                    total.Available++;
                total.PriorityScore += 5 - Math.Min(4, priority);
            }
        }

        private static void CollectRecipeRequirements(Bill bill, List<Pawn> colonists, WorkbenchSnapshot model)
        {
            RecipeDef recipe = bill.recipe;
            if (recipe == null)
                return;
            if (recipe.workSkill != null)
                model.RequiredSkill = recipe.workSkill.label;
            if (recipe.skillRequirements != null)
            {
                for (int i = 0; i < recipe.skillRequirements.Count; i++)
                {
                    SkillRequirement requirement = recipe.skillRequirements[i];
                    if (requirement.minLevel > model.MinimumSkillLevel)
                    {
                        model.MinimumSkillLevel = requirement.minLevel;
                        model.RequiredSkill = requirement.skill.label;
                    }
                }
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.Downed || pawn.workSettings == null || !pawn.workSettings.EverWork)
                    continue;
                if (recipe.requiredGiverWorkType != null && !pawn.workSettings.WorkIsActive(recipe.requiredGiverWorkType))
                    continue;
                if (!recipe.PawnSatisfiesSkillRequirements(pawn))
                    continue;
                model.QualifiedAvailablePawns++;
            }
        }

        private static void CollectIngredients(Map map, Bill bill, WorkbenchSnapshot model)
        {
            RecipeDef recipe = bill.recipe;
            if (recipe?.ingredients == null)
                return;
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipe.ingredients[i];
                IngredientSnapshot result = new IngredientSnapshot { Label = ingredient.filter.Summary };
                if (ingredient.IsFixedIngredient)
                {
                    ThingDef def = ingredient.FixedIngredient;
                    result.DefName = def.defName;
                    result.Label = def.label;
                    result.Required = ingredient.CountRequiredOfFor(def, recipe, bill);
                    if (def.CountAsResource)
                    {
                        result.Available = map.resourceCounter.GetCount(def);
                        result.Missing = result.Available < result.Required;
                        result.ExactQuantity = true;
                    }
                }
                else
                {
                    float available = 0f;
                    bool anyCounted = false;
                    foreach (ThingDef def in ingredient.filter.AllowedThingDefs)
                    {
                        if (!def.CountAsResource)
                            continue;
                        anyCounted = true;
                        available += map.resourceCounter.GetCount(def);
                    }
                    if (anyCounted)
                    {
                        result.Available = available;
                        result.Missing = available <= 0f;
                    }
                    result.Label = "Flexible: " + result.Label;
                }
                model.Ingredients.Add(result);
            }
        }

        private static void ObserveRegionalTraders(HistoryStore history, List<Map> maps, int tick)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !map.IsPlayerHome)
                    continue;
                Pawn negotiator = BestNegotiator(map);
                if (negotiator == null)
                    continue;

                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int p = 0; p < pawns.Count; p++)
                {
                    Pawn pawn = pawns[p];
                    if (pawn.trader != null && pawn.CanTradeNow)
                        RecordTrader(history, pawn, negotiator, map, tick, "pawn:" + pawn.thingIDNumber);
                }

                List<PassingShip> ships = map.passingShipManager.passingShips;
                for (int s = 0; s < ships.Count; s++)
                {
                    if (ships[s] is TradeShip ship && ship.CanTradeNow)
                        RecordTrader(history, ship, negotiator, map, tick, "ship:" + ((ILoadReferenceable)ship).GetUniqueLoadID());
                }
            }
        }

        internal static void RecordTrader(HistoryStore history, ITrader trader, Pawn negotiator, Map map, int tick, string traderKey)
        {
            if (history == null || trader?.TraderKind == null || negotiator == null)
                return;

            TraderVisitRecord visit = new TraderVisitRecord
            {
                TraderKey = traderKey,
                TraderName = trader.TraderName,
                TraderKindDefName = trader.TraderKind.defName,
                TraderKindLabel = trader.TraderKind.label,
                FactionName = trader.Faction == null ? "Independent" : trader.Faction.Name,
                Currency = trader.TradeCurrency.ToString(),
                MapId = map?.uniqueID ?? -1,
                MapLabel = map == null ? "Caravan" : MapLabel(map),
                FirstSeenTick = tick,
                LastSeenTick = tick,
                Active = true
            };

            Dictionary<string, EligibilityAccumulator> goods = new Dictionary<string, EligibilityAccumulator>();
            foreach (Thing thing in trader.ColonyThingsWillingToBuy(negotiator))
            {
                if (thing?.def == null || thing.stackCount <= 0)
                    continue;
                string key = thing.def.defName;
                if (!goods.TryGetValue(key, out EligibilityAccumulator value))
                {
                    value = new EligibilityAccumulator { DefName = key, Label = thing.def.label };
                    goods.Add(key, value);
                }
                value.Count += thing.stackCount;
                value.MarketValue += thing.MarketValue * thing.stackCount;
            }

            foreach (Thing good in trader.Goods)
            {
                if (good?.def == ThingDefOf.Silver)
                    visit.PurchasingCapacity += good.stackCount;
            }

            foreach (EligibilityAccumulator value in goods.Values)
            {
                visit.EligibleGoods.Add(new TradeEligibilityRecord
                {
                    DefName = value.DefName,
                    Label = value.Label,
                    AvailableCount = value.Count,
                    BaseMarketValue = value.Count <= 0 ? 0f : value.MarketValue / value.Count
                });
            }
            if (map != null)
            {
                foreach (KeyValuePair<ThingDef, int> pair in map.resourceCounter.AllCountedAmounts)
                {
                    if (pair.Key == null || pair.Value <= 0 || goods.ContainsKey(pair.Key.defName))
                        continue;
                    visit.IneligibleGoods.Add(new TradeEligibilityRecord
                    {
                        DefName = pair.Key.defName,
                        Label = pair.Key.label,
                        AvailableCount = pair.Value,
                        BaseMarketValue = pair.Key.BaseMarketValue
                    });
                }
                visit.IneligibleGoods.Sort((a, b) => b.AvailableCount.CompareTo(a.AvailableCount));
                if (visit.IneligibleGoods.Count > 100)
                    visit.IneligibleGoods.RemoveRange(100, visit.IneligibleGoods.Count - 100);
            }
            visit.EligibleGoods.Sort((a, b) => b.AvailableCount.CompareTo(a.AvailableCount));
            if (visit.EligibleGoods.Count > 100)
                visit.EligibleGoods.RemoveRange(100, visit.EligibleGoods.Count - 100);
            history.RecordTraderVisit(visit);
        }

        private static Pawn BestNegotiator(Map map)
        {
            Pawn best = null;
            float bestValue = float.MinValue;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.Downed || pawn.skills == null)
                    continue;
                float value = pawn.GetStatValue(StatDefOf.TradePriceImprovement);
                if (value > bestValue)
                {
                    best = pawn;
                    bestValue = value;
                }
            }
            return best;
        }

        internal static string MapLabel(Map map)
        {
            if (map.Parent != null && !string.IsNullOrEmpty(map.Parent.Label))
                return map.Parent.Label;
            return "Map " + map.uniqueID;
        }

        private sealed class SkillAccumulator
        {
            public string DefName;
            public string Label;
            public int TotalLevel;
            public int PawnCount;
            public int HighestLevel;
            public int AtEightPlus;
        }

        private sealed class WorkforceAccumulator
        {
            public string DefName;
            public string Label;
            public int Enabled;
            public int Available;
            public int PriorityScore;
        }

        private sealed class EligibilityAccumulator
        {
            public string DefName;
            public string Label;
            public int Count;
            public float MarketValue;
        }
    }
}
