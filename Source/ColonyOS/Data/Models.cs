using System;
using System.Collections.Generic;

namespace ColonyOS.Data
{
    public enum Department
    {
        Overview = 0,
        Power = 1,
        Production = 2,
        Logistics = 3,
        Population = 4,
        Economy = 5,
        RegionalMarket = 6,
        Predictive = 7
    }

    public enum EvidenceKind
    {
        Confirmed,
        Historical,
        Estimated
    }

    public enum PredictionSeverity
    {
        Informational = 0,
        Low = 1,
        Moderate = 2,
        High = 3,
        Critical = 4
    }

    public enum TradeDirection
    {
        PlayerSold,
        PlayerBought
    }

    public enum TrendDirection
    {
        Steady,
        Rising,
        Falling
    }

    public sealed class NamedValue
    {
        public string Key = string.Empty;
        public string Label = string.Empty;
        public float Value;
        public float SecondaryValue;

        public NamedValue() { }
        public NamedValue(string key, string label, float value, float secondaryValue = 0f)
        {
            Key = key;
            Label = label;
            Value = value;
            SecondaryValue = secondaryValue;
        }
    }

    public sealed class MetricPoint
    {
        public int Tick;
        public float Value;

        public MetricPoint() { }

        public MetricPoint(int tick, float value)
        {
            Tick = tick;
            Value = value;
        }
    }

    public sealed partial class ResourceSnapshot
    {
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public string Category = string.Empty;
        public int Count;
        public float BaseMarketValue;
        public List<string> MapLabels = new List<string>();
    }

    public sealed partial class PowerDeviceSnapshot
    {
        public string BuildingDefName = string.Empty;
        public string Label = string.Empty;
        public int NetworkId;
        public float Output;
        public float PotentialOutput;
        public bool Active;
    }

    public sealed partial class PowerNetworkSnapshot
    {
        public int NetworkId;
        public float Generation;
        public float Consumption;
        public float PotentialGeneration;
        public float BatteryCharge;
        public float BatteryCapacity;
        public float BatterySelfDischarge;

        public float BalanceAfterBatteryLosses => Generation - Consumption - BatterySelfDischarge;
    }

    public sealed partial class IngredientSnapshot
    {
        public string Label = string.Empty;
        public string DefName = string.Empty;
        public float Required = -1f;
        public float Available = -1f;
        public bool Missing;
        public bool ExactQuantity;
    }

    public sealed partial class WorkbenchSnapshot
    {
        public string Label = string.Empty;
        public string BuildingDefName = string.Empty;
        public string CurrentRecipe = string.Empty;
        public string RequiredSkill = string.Empty;
        public int MinimumSkillLevel;
        public int QualifiedAvailablePawns;
        public int BillCount;
        public int ActiveBillCount;
        public bool Usable;
        public bool Manned;
        public List<IngredientSnapshot> Ingredients = new List<IngredientSnapshot>();
    }

    public sealed partial class StorageZoneSnapshot
    {
        public string MapLabel = string.Empty;
        public string Label = string.Empty;
        public int Cells;
        public int FreeSlots;

        public float Utilization => Cells <= 0 ? 0f : 1f - (float)FreeSlots / Cells;
    }

    public sealed partial class SkillSnapshot
    {
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public float AverageLevel;
        public int HighestLevel;
        public int PawnsAtEightPlus;
    }

    public sealed partial class WorkforceSnapshot
    {
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public int EnabledPawns;
        public int AvailablePawns;
        public int PriorityScore;
    }

    public sealed partial class ColonySample
    {
        public int Tick;
        public int MapCount;

        public float WealthTotal;
        public float WealthItems;
        public float WealthBuildings;
        public float WealthPawns;

        public float PowerGeneration;
        public float PowerConsumption;
        public float PotentialGeneration;
        public float BatteryCharge;
        public float BatteryCapacity;
        public float BatterySelfDischarge;

        public float FoodNutrition;
        public int StorageCellCount;
        public int StorageFreeSlotEstimate;

        public int Population;
        public int AvailableWorkforce;
        public int IdlePawns;

        public int TotalWorkbenches;
        public int UsableWorkbenches;
        public int MannedWorkbenches;
        public int ActiveBills;
        public int ProductionIterationsLastDay;

        public List<ResourceSnapshot> Resources = new List<ResourceSnapshot>();
        public List<PowerDeviceSnapshot> PowerDevices = new List<PowerDeviceSnapshot>();
        public List<PowerNetworkSnapshot> PowerNetworks = new List<PowerNetworkSnapshot>();
        public List<WorkbenchSnapshot> Workbenches = new List<WorkbenchSnapshot>();
        public List<StorageZoneSnapshot> StorageZones = new List<StorageZoneSnapshot>();
        public List<SkillSnapshot> Skills = new List<SkillSnapshot>();
        public List<WorkforceSnapshot> Workforce = new List<WorkforceSnapshot>();

        public float NetPower => PowerGeneration - PowerConsumption;
        public float GridBalanceAfterBatteryLosses => NetPower - BatterySelfDischarge;
        public float BatteryPct => BatteryCapacity <= 0f ? 0f : BatteryCharge / BatteryCapacity;
        public float StorageUtilization => StorageCellCount <= 0 ? 0f : 1f - (float)StorageFreeSlotEstimate / StorageCellCount;

        /// <summary>Older history needs scalar measurements, not another copy of the whole colony.</summary>
        public void ReduceToSummary()
        {
            PowerDevices.Clear();
            PowerNetworks.Clear();
            Resources.Clear();
            StorageZones.Clear();
            Workbenches.Clear();
            Skills.Clear();
            Workforce.Clear();
        }
    }

    public sealed partial class ProductionEventRecord
    {
        public int Tick;
        public int MapId;
        public string RecipeDefName = string.Empty;
        public string RecipeLabel = string.Empty;
        public string WorkbenchLabel = string.Empty;
    }

    public sealed partial class TradeEligibilityRecord
    {
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public int AvailableCount;
        public float BaseMarketValue;
    }

    public sealed partial class TraderVisitRecord
    {
        public string TraderKey = string.Empty;
        public string TraderName = string.Empty;
        public string TraderKindDefName = string.Empty;
        public string TraderKindLabel = string.Empty;
        public string FactionName = string.Empty;
        public string Currency = "Silver";
        public int MapId;
        public string MapLabel = string.Empty;
        public int FirstSeenTick;
        public int LastSeenTick;
        public float PurchasingCapacity;
        public bool Active;
        public List<TradeEligibilityRecord> EligibleGoods = new List<TradeEligibilityRecord>();
        public List<TradeEligibilityRecord> IneligibleGoods = new List<TradeEligibilityRecord>();
    }

    public sealed partial class TradeTransactionRecord
    {
        public int Tick;
        public string TraderKey = string.Empty;
        public string TraderName = string.Empty;
        public string TraderKindDefName = string.Empty;
        public string TraderKindLabel = string.Empty;
        public string FactionName = string.Empty;
        public string Currency = "Silver";
        public TradeDirection Direction;
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public int Count;
        public float UnitPrice;
        public float TotalValue;
    }

    public sealed class DemandOpportunity
    {
        public string DefName = string.Empty;
        public string Label = string.Empty;
        public EvidenceKind Evidence;
        public float Score;
        public float Confidence;
        public int ConfirmedBuyerCount;
        public int AvailableCount;
        public int HistoricalQuantity;
        public int HistoricalTransactionCount;
        public float HistoricalRevenue;
        public float EstimatedUnitValue;
        public string Explanation = string.Empty;
    }

    public sealed class Prediction
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Explanation = string.Empty;
        public Department Department;
        public PredictionSeverity Severity;
        public EvidenceKind Evidence;
        public int TimeframeTicks = -1;
        public float Confidence;
        public string SuggestedAction = string.Empty;
        public List<string> SupportingEvidence = new List<string>();
    }

    public sealed class ForecastLine
    {
        public bool Available;
        public float SlopePerDay;
        public float TargetAtTick;
        public float RSquared;
        public float Confidence;
        public string Reason = string.Empty;
    }
}
