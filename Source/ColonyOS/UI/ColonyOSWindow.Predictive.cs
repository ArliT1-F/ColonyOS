using System;
using ColonyOS.Analytics;
using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public sealed partial class ColonyOSWindow
    {
        private void DrawPredictivePage(DashboardViewModel view, Rect rect)
        {
            ThemePalette theme = ColonyTheme.Current;
            float y = rect.y;
            int critical = 0, moderate = 0, opportunities = 0;
            for (int i = 0; i < view.Predictions.Count; i++)
            {
                Prediction prediction = view.Predictions[i];
                if (prediction.Severity >= PredictionSeverity.High) critical++;
                else if (prediction.Severity == PredictionSeverity.Moderate) moderate++;
                if (prediction.Severity == PredictionSeverity.Informational) opportunities++;
            }
            int columns = rect.width > 700f ? 3 : 1;
            Charts.Kpi(UiDraw.GridCell(rect, columns, 0, 110f), "High-priority signals", critical.ToString(), "High + critical", "Deduplicated", critical > 0 ? theme.Danger : theme.Good, Department.Predictive, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 1, 110f), "Moderate warnings", moderate.ToString(), "Evidence-linked", "Recalculated on schedule", moderate > 0 ? theme.Warning : theme.Accent, Department.Predictive, null, null);
            Charts.Kpi(UiDraw.GridCell(rect, columns, 2, 110f), "Information & opportunities", opportunities.ToString(), view.Predictions.Count + " total signals", "Maximum 24", theme.Accent2, Department.Predictive, null, null);
            y += Mathf.CeilToInt(3f / columns) * 122f + 8f;

            Prediction selected = null;
            for (int i = 0; i < view.Predictions.Count; i++)
            {
                if (view.Predictions[i].Id == selectedPredictionId) { selected = view.Predictions[i]; break; }
            }
            if (selected == null && view.Predictions.Count > 0)
                selected = view.Predictions[0];

            bool compact = rect.width < 700f;
            float leftWidth = compact ? rect.width : rect.width * 0.58f - 7f;
            if (view.Predictions.Count == 0)
            {
                Rect empty = new Rect(rect.x, y, leftWidth, 480f);
                UiDraw.EmptyState(empty, "No evidence-backed predictions", "ColonyOS needs current measurements and, for trend forecasts, enough historical coverage. No generic warnings are inserted to fill this page.");
            }
            else
            {
                for (int i = 0; i < view.Predictions.Count; i++)
                    DrawPredictionCard(new Rect(rect.x, y + i * 122f, leftWidth, 112f), view.Predictions[i], selected?.Id == view.Predictions[i].Id);
            }

            float listHeight = view.Predictions.Count == 0 ? 480f : view.Predictions.Count * 122f;
            float detailY = compact ? y + listHeight + 14f : y;
            Rect detail = compact
                ? new Rect(rect.x, detailY, rect.width, 544f)
                : new Rect(rect.x + leftWidth + 14f, detailY, rect.xMax - rect.x - leftWidth - 14f, 544f);
            Rect detailInner = UiDraw.Card(detail, "Prediction evidence", selected == null ? "Select a signal" : selected.Id);
            DrawPredictionDetail(detailInner, selected);
            Rect method = new Rect(detail.x, detail.yMax + 14f, detail.width, 274f);
            Rect methodInner = UiDraw.Card(method, "Forecast methodology", "What confidence does — and does not — mean");
            UiDraw.Label(new Rect(methodInner.x, methodInner.y + 2f, methodInner.width, 66f), "CONFIRMED signals use current game rules or direct measurements. HISTORICAL signals summarize recorded observations. ESTIMATED signals project a measured trend or combine current eligibility with sale history.", theme.Muted, GameFont.Tiny);
            UiDraw.Divider(new Rect(methodInner.x, methodInner.y + 78f, methodInner.width, 1f));
            UiDraw.Label(new Rect(methodInner.x, methodInner.y + 90f, methodInner.width, 70f), "Trend forecasts use least-squares regression, require at least six samples and adequate time coverage, reject poor fits, and reduce confidence for estimates such as stockpile slots.", theme.Muted, GameFont.Tiny);
            UiDraw.Label(new Rect(methodInner.x, methodInner.y + 169f, methodInner.width, 48f), "Confidence is not a probability of occurrence. It reflects evidence type, sample support, trend fit and measurement limitations.", theme.Faint, GameFont.Tiny);
        }

        private void DrawPredictionCard(Rect rect, Prediction prediction, bool selected)
        {
            ThemePalette theme = ColonyTheme.Current;
            Color severity = theme.Severity(prediction.Severity);
            UiDraw.Rounded(rect, selected ? severity : Mouse.IsOver(rect) ? theme.Accent : theme.Border);
            UiDraw.Rounded(rect.ContractedBy(1f), selected ? theme.PanelRaised : theme.Panel);
            UiDraw.Rounded(new Rect(rect.x + 13f, rect.y + 14f, 4f, rect.height - 28f), severity);
            UiDraw.Label(new Rect(rect.x + 28f, rect.y + 10f, rect.width - 192f, 24f), prediction.Title, theme.Text, GameFont.Small, TextAnchor.MiddleLeft, true, false);
            UiDraw.Pill(new Rect(rect.xMax - 151f, rect.y + 12f, 64f, 21f), prediction.Severity.ToString(), severity);
            UiDraw.Pill(new Rect(rect.xMax - 80f, rect.y + 12f, 66f, 21f), prediction.Confidence.ToString("P0"), theme.Evidence(prediction.Evidence));
            UiDraw.Label(new Rect(rect.x + 28f, rect.y + 38f, rect.width - 42f, 39f), prediction.Explanation, theme.Muted, GameFont.Tiny);
            UiDraw.Label(new Rect(rect.x + 28f, rect.yMax - 27f, rect.width * 0.42f, 20f), ShortDepartmentName(prediction.Department), theme.Evidence(prediction.Evidence), GameFont.Tiny, TextAnchor.MiddleLeft, true, false);
            UiDraw.Label(new Rect(rect.x + rect.width * 0.46f, rect.yMax - 27f, rect.width * 0.49f, 20f), UiFormat.Duration(prediction.TimeframeTicks) + "  •  " + prediction.Evidence, theme.Faint, GameFont.Tiny, TextAnchor.MiddleRight, false, false);
            if (Widgets.ButtonInvisible(rect))
                selectedPredictionId = prediction.Id;
        }

        private void DrawPredictionDetail(Rect rect, Prediction prediction)
        {
            if (prediction == null)
            {
                UiDraw.EmptyState(rect, "No prediction selected", "Signals include their evidence and suggested action here.");
                return;
            }
            selectedPredictionId = prediction.Id;
            ThemePalette theme = ColonyTheme.Current;
            UiDraw.Pill(new Rect(rect.x, rect.y + 2f, 98f, 24f), prediction.Evidence.ToString(), theme.Evidence(prediction.Evidence));
            UiDraw.Pill(new Rect(rect.x + 107f, rect.y + 2f, 92f, 24f), prediction.Severity.ToString(), theme.Severity(prediction.Severity));
            UiDraw.Label(new Rect(rect.x, rect.y + 39f, rect.width, 55f), prediction.Title, theme.Text, GameFont.Medium, TextAnchor.UpperLeft, true);
            DrawStatRow(new Rect(rect.x, rect.y + 103f, rect.width, 26f), "Estimated timeframe", UiFormat.Duration(prediction.TimeframeTicks), theme.Text);
            DrawStatRow(new Rect(rect.x, rect.y + 131f, rect.width, 26f), "Evidence confidence", prediction.Confidence.ToString("P0"), theme.Evidence(prediction.Evidence));
            DrawStatRow(new Rect(rect.x, rect.y + 159f, rect.width, 26f), "Affected department", ShortDepartmentName(prediction.Department), theme.Accent2);
            UiDraw.Label(new Rect(rect.x, rect.y + 197f, rect.width, 66f), prediction.Explanation, theme.Muted, GameFont.Tiny);
            UiDraw.Label(new Rect(rect.x, rect.y + 270f, rect.width, 22f), "Supporting evidence", theme.Text, GameFont.Tiny, TextAnchor.UpperLeft, true, false);
            int evidenceCount = Math.Min(5, prediction.SupportingEvidence.Count);
            for (int i = 0; i < evidenceCount; i++)
            {
                float y = rect.y + 298f + i * 28f;
                UiDraw.Rounded(new Rect(rect.x + 2f, y + 8f, 5f, 5f), theme.Accent2);
                UiDraw.Label(new Rect(rect.x + 15f, y, rect.width - 15f, 27f), prediction.SupportingEvidence[i], theme.Muted, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            }
            float actionY = rect.yMax - 104f;
            UiDraw.Label(new Rect(rect.x, actionY, rect.width, 20f), "Suggested player action", theme.Text, GameFont.Tiny, TextAnchor.UpperLeft, true, false);
            UiDraw.Label(new Rect(rect.x, actionY + 23f, rect.width, 48f), prediction.SuggestedAction, theme.Muted, GameFont.Tiny);
            if (prediction.Department != Department.Predictive && UiDraw.Button(new Rect(rect.x, rect.yMax - 27f, rect.width, 27f), "Open " + ShortDepartmentName(prediction.Department) + " →", null, true))
                Navigate(prediction.Department);
        }
    }
}
