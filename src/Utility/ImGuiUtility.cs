using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public static class ImGuiExt
    {
        public static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null)
                TextTooltip(tooltip);

            return result;
        }

        public static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }

        public static bool TintButton(string label, Vector4 color)
        {
            return TintButtonImpl(() => ImGui.Button(label), color);
        }

        public static bool TintButton(string label, Vector2 size, Vector4 color)
        {
            return TintButtonImpl(() => ImGui.Button(label, size), color);
        }

        private static bool TintButtonImpl(Func<bool> button, Vector4 color)
        {
            var activeColor = new Vector4(color.X * 1.5f, color.Y * 1.5f, color.Z * 1.5f, color.W);
            var hoveredColor = new Vector4(color.X * 1.25f, color.Y * 1.25f, color.Z * 1.25f, color.W);
            var lightText = color + new Vector4(0.8f);
            var darkText = color - new Vector4(0.8f);

            var darkDiff = Vector4.DistanceSquared(darkText, Vector4.Min(activeColor, hoveredColor) * 0.67f);
            var lightDiff = Vector4.DistanceSquared(lightText, Vector4.Max(activeColor, hoveredColor));

            lightText = Vector4.Min(lightText, Vector4.One);
            darkText = Vector4.Max(darkText, Vector4.Zero);
            lightText.W = color.W;
            darkText.W = color.W;

            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.Text, darkDiff > lightDiff ? darkText : lightText);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor);

            var pressed = button();

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            return pressed;
        }
    }
}
