using Verse;

namespace ColonyOS.Data
{
    public sealed partial class ResourceSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Category, "category");
            Scribe_Values.Look(ref Count, "count");
            Scribe_Values.Look(ref BaseMarketValue, "baseMarketValue");
            Scribe_Collections.Look(ref MapLabels, "mapLabels", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                MapLabels = MapLabels ?? new System.Collections.Generic.List<string>();
        }
    }

    public sealed partial class PowerDeviceSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref BuildingDefName, "buildingDefName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref NetworkId, "networkId");
            Scribe_Values.Look(ref Output, "output");
            Scribe_Values.Look(ref PotentialOutput, "potentialOutput");
            Scribe_Values.Look(ref Active, "active");
        }
    }

    public sealed partial class PowerNetworkSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref NetworkId, "networkId");
            Scribe_Values.Look(ref Generation, "generation");
            Scribe_Values.Look(ref Consumption, "consumption");
            Scribe_Values.Look(ref PotentialGeneration, "potentialGeneration");
            Scribe_Values.Look(ref BatteryCharge, "batteryCharge");
            Scribe_Values.Look(ref BatteryCapacity, "batteryCapacity");
            Scribe_Values.Look(ref BatterySelfDischarge, "batterySelfDischarge");
        }
    }

    public sealed partial class IngredientSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Required, "required", -1f);
            Scribe_Values.Look(ref Available, "available", -1f);
            Scribe_Values.Look(ref Missing, "missing");
            Scribe_Values.Look(ref ExactQuantity, "exactQuantity");
        }
    }

    public sealed partial class WorkbenchSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref BuildingDefName, "buildingDefName");
            Scribe_Values.Look(ref CurrentRecipe, "currentRecipe");
            Scribe_Values.Look(ref RequiredSkill, "requiredSkill");
            Scribe_Values.Look(ref MinimumSkillLevel, "minimumSkillLevel");
            Scribe_Values.Look(ref QualifiedAvailablePawns, "qualifiedAvailablePawns");
            Scribe_Values.Look(ref BillCount, "billCount");
            Scribe_Values.Look(ref ActiveBillCount, "activeBillCount");
            Scribe_Values.Look(ref Usable, "usable");
            Scribe_Values.Look(ref Manned, "manned");
            Scribe_Collections.Look(ref Ingredients, "ingredients", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Ingredients = Ingredients ?? new System.Collections.Generic.List<IngredientSnapshot>();
        }
    }

    public sealed partial class StorageZoneSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref MapLabel, "mapLabel");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Cells, "cells");
            Scribe_Values.Look(ref FreeSlots, "freeSlots");
        }
    }

    public sealed partial class SkillSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref AverageLevel, "averageLevel");
            Scribe_Values.Look(ref HighestLevel, "highestLevel");
            Scribe_Values.Look(ref PawnsAtEightPlus, "pawnsAtEightPlus");
        }
    }

    public sealed partial class WorkforceSnapshot : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref EnabledPawns, "enabledPawns");
            Scribe_Values.Look(ref AvailablePawns, "availablePawns");
            Scribe_Values.Look(ref PriorityScore, "priorityScore");
        }
    }
}
