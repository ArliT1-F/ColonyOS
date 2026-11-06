using System;
using ColonyOS.Data;
using UnityEngine;
using Verse;

namespace ColonyOS.UI
{
    public static class UiDraw
    {
        public static void Rounded(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, UiAssets.Rounded, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        public static void Panel(Rect rect, bool raised = false, bool hover = false)
        {
            ThemePalette theme = ColonyTheme.Current;
            Color border = hover ? ColonyTheme.Alpha(theme.Accent, 0.75f) : theme.Border;
            Rounded(rect, border);
            Rounded(rect.ContractedBy(1f), hover ? theme.PanelRaised : raised ? theme.PanelRaised : theme.Panel);
        }

        public static Rect Card(Rect rect, string title, string subtitle = null, bool hover = false)
        {
            Panel(rect, false, hover);
            float y = rect.y + 15f;
            if (!string.IsNullOrEmpty(title))
            {
                Label(new Rect(rect.x + 16f, y, rect.width - 32f, 22f), title, ColonyTheme.Current.Text, GameFont.Small, TextAnchor.UpperLeft, true, false);
                y += 23f;
            }
            if (!string.IsNullOrEmpty(subtitle))
            {
                Label(new Rect(rect.x + 16f, y, rect.width - 32f, 18f), subtitle, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.UpperLeft, false, false);
                y += 20f;
            }
            return new Rect(rect.x + 16f, y + 5f, rect.width - 32f, rect.yMax - y - 16f);
        }

        public static void Label(Rect rect, string text, Color color, GameFont font = GameFont.Small, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, bool wrap = true)
        {
            if (string.IsNullOrEmpty(text) || rect.width <= 0f || rect.height <= 0f)
                return;
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWrap = Text.WordWrap;
            FontStyle oldStyle = GUI.skin.label.fontStyle;
            GUI.color = color;
            Text.Font = font;
            Text.Anchor = anchor;
            Text.WordWrap = wrap;
            GUI.skin.label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (wrap)
                Widgets.Label(rect, text);
            else
                Widgets.LabelEllipses(rect, text);
            GUI.skin.label.fontStyle = oldStyle;
            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWrap;
        }

        public static void Icon(Rect rect, Texture2D icon, Color color)
        {
            if (icon == null)
                return;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        public static bool Button(Rect rect, string label, Texture2D icon = null, bool primary = false, bool active = true, string tooltip = null)
        {
            ThemePalette theme = ColonyTheme.Current;
            bool hover = Mouse.IsOver(rect) && active;
            Color background = primary ? hover ? theme.Accent : ColonyTheme.Alpha(theme.Accent, 0.88f) : hover ? theme.PanelRaised : theme.Panel;
            Rounded(rect, hover ? theme.Accent : theme.Border);
            Rounded(rect.ContractedBy(1f), background);
            Color foreground = primary ? theme.Window : active ? theme.Text : theme.Faint;
            float textX = rect.x + 10f;
            if (icon != null)
            {
                Icon(new Rect(rect.x + 9f, rect.y + (rect.height - 16f) / 2f, 16f, 16f), icon, foreground);
                textX += 21f;
            }
            Label(new Rect(textX, rect.y, rect.xMax - textX - 8f, rect.height), label, foreground, GameFont.Tiny, TextAnchor.MiddleCenter, primary, false);
            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(rect, tooltip);
            return active && Widgets.ButtonInvisible(rect);
        }

        public static bool NavButton(Rect rect, string label, Texture2D icon, bool selected)
        {
            ThemePalette theme = ColonyTheme.Current;
            bool hover = Mouse.IsOver(rect);
            if (selected || hover)
                Rounded(rect, selected ? ColonyTheme.Alpha(theme.Accent, 0.16f) : ColonyTheme.Alpha(theme.PanelRaised, 0.75f));
            if (selected)
                Rounded(new Rect(rect.x, rect.y + 8f, 3f, rect.height - 16f), theme.Accent);
            Color color = selected ? theme.Accent : hover ? theme.Text : theme.Muted;
            Icon(new Rect(rect.x + 15f, rect.y + 12f, 20f, 20f), icon, color);
            Label(new Rect(rect.x + 46f, rect.y, rect.width - 54f, rect.height), label, color, GameFont.Small, TextAnchor.MiddleLeft, selected, false);
            return Widgets.ButtonInvisible(rect);
        }

        public static void Pill(Rect rect, string text, Color color, bool filled = false)
        {
            Rounded(rect, filled ? color : ColonyTheme.Alpha(color, 0.14f));
            if (!filled)
                Rounded(rect.ContractedBy(1f), ColonyTheme.Alpha(color, 0.12f));
            Label(rect, text, filled ? ColonyTheme.Current.Window : color, GameFont.Tiny, TextAnchor.MiddleCenter, true, false);
        }

        public static void Progress(Rect rect, float fraction, Color color, string label = null)
        {
            Rounded(rect, ColonyTheme.Current.Grid);
            float width = Mathf.Clamp01(fraction) * rect.width;
            if (width > 0.5f)
                Rounded(new Rect(rect.x, rect.y, width, rect.height), color);
            if (!string.IsNullOrEmpty(label))
                Label(rect, label, ColonyTheme.Current.Text, GameFont.Tiny, TextAnchor.MiddleCenter, false, false);
        }

        public static void Divider(Rect rect)
        {
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width, ColonyTheme.Current.Border);
        }

        public static void EmptyState(Rect rect, string title, string explanation)
        {
            Panel(rect);
            float size = Mathf.Min(90f, rect.height * 0.3f);
            Icon(new Rect(rect.center.x - size / 2f, rect.y + 22f, size, size), UiAssets.Predictive, ColonyTheme.Alpha(ColonyTheme.Current.Accent, 0.45f));
            Label(new Rect(rect.x + 24f, rect.y + 32f + size, rect.width - 48f, 28f), title, ColonyTheme.Current.Text, GameFont.Small, TextAnchor.MiddleCenter, true);
            Label(new Rect(rect.x + 32f, rect.y + 66f + size, rect.width - 64f, Mathf.Max(30f, rect.yMax - rect.y - size - 76f)), explanation, ColonyTheme.Current.Muted, GameFont.Tiny, TextAnchor.UpperCenter);
        }

        public static string Search(Rect rect, string value, string placeholder = "Search")
        {
            Panel(rect);
            Icon(new Rect(rect.x + 10f, rect.y + 8f, 16f, 16f), UiAssets.Search, ColonyTheme.Current.Muted);
            if (string.IsNullOrEmpty(value))
                Label(new Rect(rect.x + 34f, rect.y, rect.width - 42f, rect.height), placeholder, ColonyTheme.Current.Faint, GameFont.Tiny, TextAnchor.MiddleLeft, false, false);
            Color old = GUI.color;
            GUI.color = ColonyTheme.Current.Text;
            string next = Widgets.TextField(new Rect(rect.x + 32f, rect.y + 3f, rect.width - 40f, rect.height - 6f), value ?? string.Empty);
            GUI.color = old;
            return next;
        }

        public static Rect GridCell(Rect area, int columns, int index, float height, float gap = 14f)
        {
            float width = (area.width - gap * (columns - 1)) / columns;
            int row = index / columns;
            int column = index % columns;
            return new Rect(area.x + column * (width + gap), area.y + row * (height + gap), width, height);
        }
    }
}
