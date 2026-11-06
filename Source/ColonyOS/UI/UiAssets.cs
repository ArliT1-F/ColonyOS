using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    [StaticConstructorOnStartup]
    public static class UiAssets
    {
        public static readonly Texture2D Rounded = Load("Rounded");
        public static readonly Texture2D Overview = Load("Overview");
        public static readonly Texture2D Power = Load("Power");
        public static readonly Texture2D Production = Load("Production");
        public static readonly Texture2D Logistics = Load("Logistics");
        public static readonly Texture2D Population = Load("Population");
        public static readonly Texture2D Economy = Load("Economy");
        public static readonly Texture2D RegionalMarket = Load("RegionalMarket");
        public static readonly Texture2D Predictive = Load("Predictive");
        public static readonly Texture2D Refresh = Load("Refresh");
        public static readonly Texture2D Search = Load("Search");
        public static readonly Texture2D Settings = Load("Settings");

        public static Texture2D Icon(Department department)
        {
            switch (department)
            {
                case Department.Power: return Power;
                case Department.Production: return Production;
                case Department.Logistics: return Logistics;
                case Department.Population: return Population;
                case Department.Economy: return Economy;
                case Department.RegionalMarket: return RegionalMarket;
                case Department.Predictive: return Predictive;
                default: return Overview;
            }
        }

        private static Texture2D Load(string name) => ContentFinder<Texture2D>.Get("ColonyOS/UI/" + name);
    }
}
