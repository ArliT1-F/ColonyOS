using System;
using UnityEngine;

namespace ColonyOS.UI
{
    public static class UiFormat
    {
        public static string Compact(float value, string suffix = "")
        {
            float absolute = Mathf.Abs(value);
            if (absolute >= 1000000f) return (value / 1000000f).ToString("0.00") + "M" + suffix;
            if (absolute >= 1000f) return (value / 1000f).ToString("0.0") + "k" + suffix;
            return value.ToString(absolute < 10f && value != Mathf.Round(value) ? "0.0" : "0") + suffix;
        }

        public static string Money(float value) => Compact(value) + " silver";

        public static string Axis(float value)
        {
            float absolute = Mathf.Abs(value);
            if (absolute >= 1000000f) return (value / 1000000f).ToString("0.#") + "M";
            if (absolute >= 1000f) return (value / 1000f).ToString("0.#") + "k";
            return value.ToString("0.#");
        }

        public static string SignedPercent(float value) => (value >= 0f ? "+" : "") + value.ToString("P1");

        public static string TickAge(int tick, int currentTick)
        {
            int delta = Math.Max(0, currentTick - tick);
            if (delta < 2500) return delta + " ticks ago";
            float hours = delta / 2500f;
            if (hours < 48f) return hours.ToString("0.0") + " h ago";
            return (hours / 24f).ToString("0.0") + " days ago";
        }

        public static string Duration(int ticks)
        {
            if (ticks < 0) return "Timeframe not established";
            if (ticks == 0) return "Now";
            float hours = ticks / 2500f;
            if (hours < 1f) return Math.Max(1, Mathf.RoundToInt(hours * 60f)) + " min";
            if (hours < 48f) return hours.ToString("0.0") + " hours";
            return (hours / 24f).ToString("0.0") + " days";
        }
    }
}
