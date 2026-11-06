using System;
using System.Collections.Generic;
using ColonyOS.Analytics;
using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public sealed partial class ColonyOSWindow
    {
        private void DrawPopulationPage(DashboardViewModel view, Rect rect)
        {
            ColonySample current = view.Current;
            if (current == null) { UiDraw.EmptyState(rect, "No population snapshot", "Colonist skills and work settings will appear after collection."); return; }
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int columns = rect.width > 850f ? 4 : 2;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 122f), "Free colonists", current.Population.ToString(), current.MapCount + " home map(s)", "Humanlike workforce", theme.Purple, Department.Population, view.GetSeries(SampleMetric.Population), null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 122f), "Non-downed workforce", current.AvailableWorkforce.ToString(), current.Population - current.AvailableWorkforce + " unavailable", "Current health state", theme.Good, Department.Population, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 122f), "Idle at snapshot", current.IdlePawns.ToString(), current.Population <= 0 ? "—" : ((float)current.IdlePawns / current.Population).ToString("P0"), "Pawn_MindState.IsIdle", current.IdlePawns > 0 ? theme.Warning : theme.Accent, Department.Population, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 3, 122f), "Tracked skills", current.Skills.Count.ToString(), current.Workforce.Count + " enabled work types", "Non-disabled records", theme.Accent2, Department.Population, null, null);
            y += Mathf.CeilToInt(4f / columns) * 134f + 8f;

            y = SplitPanels(rect, y, 302f, 0.48f, out Rect skills, out Rect work);
            Rect skillsInner = UiDraw.Card(skills, "Skill distribution", "Average level across non-disabled colonist skill records");
            List<NamedValue> skillValues = new List<NamedValue>();
            for (int i = 0; i < Math.Min(8, current.Skills.Count); i++)
                skillValues.Add(new NamedValue(current.Skills[i].DefName, current.Skills[i].Label, current.Skills[i].AverageLevel, current.Skills[i].HighestLevel));
            Charts.RankingBars(skillsInner, skillValues, "No skill records", "Skill records appear after colonists are collected.");
            Rect workInner = UiDraw.Card(work, "Workforce allocation", "Enabled assignments and non-downed availability");
            DrawWorkforceAllocation(workInner, current.Workforce);

            UiDraw.Label(new Rect(rect.x, y, rect.width, 25f), "Skill depth and coverage", theme.Text, GameFont.Small, TextAnchor.UpperLeft, true, false);
            y += 34f;
            int skillColumns = rect.width > 1050f ? 4 : rect.width > 680f ? 3 : 2;
            int skillCount = Math.Min(12, current.Skills.Count);
            for (int i = 0; i < skillCount; i++)
            {
                SkillSnapshot skill = current.Skills[i];
                Rect card = UiDraw.GridCell(new Rect(rect.x, y, rect.width, rect.height), skillColumns, i, 98f, 10f);
                UiDraw.Panel(card);
                UiDraw.Label(new Rect(card.x + 12f, card.y + 9f, card.width - 24f, 21f), skill.Label, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(card.x + 12f, card.y + 31f, card.width * 0.48f, 30f), skill.AverageLevel.ToString("0.0"), theme.Accent2, GameFont.Medium, TextAnchor.MiddleLeft, true, false);
                UiDraw.Label(new Rect(card.x + card.width * 0.5f, card.y + 37f, card.width * 0.43f, 20f), "avg level", theme.Faint, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                UiDraw.Progress(new Rect(card.x + 12f, card.y + 67f, card.width - 24f, 7f), skill.AverageLevel / 20f, theme.Series(i));
                UiDraw.Label(new Rect(card.x + 12f, card.y + 77f, card.width - 24f, 17f), "Peak " + skill.HighestLevel + "  •  " + skill.PawnsAtEightPlus + " at 8+", theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            }
            y += Mathf.CeilToInt((float)Math.Max(1, skillCount) / skillColumns) * 108f + 8f;

            Rect labour = new Rect(rect.x, y, rect.width, 240f);
            Rect labourInner = UiDraw.Card(labour, "Production labour coverage", "Qualified, non-downed colonists with relevant work enabled");
            IReadOnlyList<WorkbenchSnapshot> constrained = view.LabourConstraintRanking;
            if (constrained.Count == 0)
            {
                UiDraw.EmptyState(labourInner, "No active recipe requirements", "Skill coverage appears when production bills are active.");
            }
            else
            {
                int count = Math.Min(5, constrained.Count);
                float rowHeight = labourInner.height / count;
                for (int i = 0; i < count; i++)
                {
                    WorkbenchSnapshot table = constrained[i];
                    Rect row = new Rect(labourInner.x, labourInner.y + i * rowHeight, labourInner.width, rowHeight);
                    Color color = table.QualifiedAvailablePawns == 0 ? theme.Danger : table.QualifiedAvailablePawns == 1 ? theme.Warning : theme.Good;
                    UiDraw.Label(new Rect(row.x, row.y, row.width * 0.42f, row.height), table.CurrentRecipe, theme.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                    UiDraw.Label(new Rect(row.x + row.width * 0.44f, row.y, row.width * 0.30f, row.height), string.IsNullOrEmpty(table.RequiredSkill) ? "No specific skill" : table.RequiredSkill + (table.MinimumSkillLevel > 0 ? " " + table.MinimumSkillLevel + "+" : ""), theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                    UiDraw.Pill(new Rect(row.xMax - 105f, row.y + rowHeight * 0.22f, 100f, 22f), table.QualifiedAvailablePawns + " qualified", color);
                }
            }
        }

        private static void DrawWorkforceAllocation(Rect rect, IReadOnlyList<WorkforceSnapshot> workforce)
        {
            if (workforce.Count == 0) { UiDraw.EmptyState(rect, "No enabled work types", "Initialize colonist work settings to see allocation."); return; }
            int count = Math.Min(6, workforce.Count);
            int maximum = 1;
            for (int i = 0; i < count; i++) maximum = Math.Max(maximum, workforce[i].EnabledPawns);
            float rowHeight = rect.height / count;
            for (int i = 0; i < count; i++)
            {
                WorkforceSnapshot work = workforce[i];
                float y = rect.y + i * rowHeight;
                UiDraw.Label(new Rect(rect.x, y, rect.width * 0.34f, rowHeight), work.Label, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
                Rect bar = new Rect(rect.x + rect.width * 0.36f, y + rowHeight * 0.28f, rect.width * 0.43f, 9f);
                UiDraw.Progress(bar, (float)work.EnabledPawns / maximum, ColonyTheme.Alpha(ColonyTheme.Current.Accent2, 0.35f));
                UiDraw.Progress(bar, (float)work.AvailablePawns / maximum, ColonyTheme.Current.Accent2);
                UiDraw.Label(new Rect(rect.x + rect.width * 0.82f, y, rect.width * 0.18f, rowHeight), work.AvailablePawns + "/" + work.EnabledPawns, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
                TooltipHandler.TipRegion(new Rect(rect.x, y, rect.width, rowHeight), work.Label + "\nEnabled: " + work.EnabledPawns + "\nNon-downed: " + work.AvailablePawns + "\nPriority weight: " + work.PriorityScore);
            }
        }
    }
}
