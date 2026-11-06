using Verse;

namespace ColonyOS.Data
{
    public sealed partial class ColonySample : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref MapCount, "mapCount");
            Scribe_Values.Look(ref WealthTotal, "wealthTotal");
            Scribe_Values.Look(ref WealthItems, "wealthItems");
            Scribe_Values.Look(ref WealthBuildings, "wealthBuildings");
            Scribe_Values.Look(ref WealthPawns, "wealthPawns");
            Scribe_Values.Look(ref PowerGeneration, "powerGeneration");
            Scribe_Values.Look(ref PowerConsumption, "powerConsumption");
            Scribe_Values.Look(ref PotentialGeneration, "potentialGeneration");
            Scribe_Values.Look(ref BatteryCharge, "batteryCharge");
            Scribe_Values.Look(ref BatteryCapacity, "batteryCapacity");
            Scribe_Values.Look(ref BatterySelfDischarge, "batterySelfDischarge");
            Scribe_Values.Look(ref FoodNutrition, "foodNutrition");
            Scribe_Values.Look(ref StorageCellCount, "storageCellCount");
            Scribe_Values.Look(ref StorageFreeSlotEstimate, "storageFreeSlotEstimate");
            Scribe_Values.Look(ref Population, "population");
            Scribe_Values.Look(ref AvailableWorkforce, "availableWorkforce");
            Scribe_Values.Look(ref IdlePawns, "idlePawns");
            Scribe_Values.Look(ref TotalWorkbenches, "totalWorkbenches");
            Scribe_Values.Look(ref UsableWorkbenches, "usableWorkbenches");
            Scribe_Values.Look(ref MannedWorkbenches, "mannedWorkbenches");
            Scribe_Values.Look(ref ActiveBills, "activeBills");
            Scribe_Values.Look(ref ProductionIterationsLastDay, "productionIterationsLastDay");
            Scribe_Collections.Look(ref Resources, "resources", LookMode.Deep);
            Scribe_Collections.Look(ref PowerDevices, "powerDevices", LookMode.Deep);
            Scribe_Collections.Look(ref PowerNetworks, "powerNetworks", LookMode.Deep);
            Scribe_Collections.Look(ref Workbenches, "workbenches", LookMode.Deep);
            Scribe_Collections.Look(ref StorageZones, "storageZones", LookMode.Deep);
            Scribe_Collections.Look(ref Skills, "skills", LookMode.Deep);
            Scribe_Collections.Look(ref Workforce, "workforce", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Resources = Resources ?? new System.Collections.Generic.List<ResourceSnapshot>();
                PowerDevices = PowerDevices ?? new System.Collections.Generic.List<PowerDeviceSnapshot>();
                PowerNetworks = PowerNetworks ?? new System.Collections.Generic.List<PowerNetworkSnapshot>();
                Workbenches = Workbenches ?? new System.Collections.Generic.List<WorkbenchSnapshot>();
                StorageZones = StorageZones ?? new System.Collections.Generic.List<StorageZoneSnapshot>();
                Skills = Skills ?? new System.Collections.Generic.List<SkillSnapshot>();
                Workforce = Workforce ?? new System.Collections.Generic.List<WorkforceSnapshot>();
            }
        }
    }

    public sealed partial class ProductionEventRecord : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref MapId, "mapId");
            Scribe_Values.Look(ref RecipeDefName, "recipeDefName");
            Scribe_Values.Look(ref RecipeLabel, "recipeLabel");
            Scribe_Values.Look(ref WorkbenchLabel, "workbenchLabel");
        }
    }

    public sealed partial class TradeEligibilityRecord : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref AvailableCount, "availableCount");
            Scribe_Values.Look(ref BaseMarketValue, "baseMarketValue");
        }
    }

    public sealed partial class TraderVisitRecord : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref TraderKey, "traderKey");
            Scribe_Values.Look(ref TraderName, "traderName");
            Scribe_Values.Look(ref TraderKindDefName, "traderKindDefName");
            Scribe_Values.Look(ref TraderKindLabel, "traderKindLabel");
            Scribe_Values.Look(ref FactionName, "factionName");
            Scribe_Values.Look(ref Currency, "currency");
            Scribe_Values.Look(ref MapId, "mapId");
            Scribe_Values.Look(ref MapLabel, "mapLabel");
            Scribe_Values.Look(ref FirstSeenTick, "firstSeenTick");
            Scribe_Values.Look(ref LastSeenTick, "lastSeenTick");
            Scribe_Values.Look(ref PurchasingCapacity, "purchasingCapacity");
            Scribe_Values.Look(ref Active, "active");
            Scribe_Collections.Look(ref EligibleGoods, "eligibleGoods", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EligibleGoods = EligibleGoods ?? new System.Collections.Generic.List<TradeEligibilityRecord>();
                Active = false;
            }
        }
    }

    public sealed partial class TradeTransactionRecord : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref TraderKey, "traderKey");
            Scribe_Values.Look(ref TraderName, "traderName");
            Scribe_Values.Look(ref TraderKindDefName, "traderKindDefName");
            Scribe_Values.Look(ref TraderKindLabel, "traderKindLabel");
            Scribe_Values.Look(ref FactionName, "factionName");
            Scribe_Values.Look(ref Currency, "currency");
            Scribe_Values.Look(ref Direction, "direction");
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Count, "count");
            Scribe_Values.Look(ref UnitPrice, "unitPrice");
            Scribe_Values.Look(ref TotalValue, "totalValue");
        }
    }
}
