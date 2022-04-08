using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public static class ImGuiExt
    {
        public static bool IconButton(FontAwesomeIcon icon, Vector2 size)
        {
            return IconButton(icon, null, size);
        }

        public static bool IconButton(string icon, Vector2 size)
        {
            return IconButton(icon, null, size);
        }

        public static bool IconButton(FontAwesomeIcon icon, string? tooltip = null, Vector2 size = default)
        {
            return IconButton(icon.ToIconString(), tooltip, size);
        }

        public static bool IconButton(string icon, string? tooltip = null, Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon}##{icon}-{tooltip}", size);
            ImGui.PopFont();

            if (tooltip != null)
            {
                TextTooltip(tooltip);
            }

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

        public static void PushDisableButtonBg()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        }

        public static void PopDisableButtonBg()
        {
            ImGui.PopStyleColor(3);
        }

        public static bool BeginCursorPopup(string name)
        {
            ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos());

            bool _ = false;
            var open = ImGui.Begin(
                name,
                ref _,
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Popup
            );

            if (open && !ImGui.IsWindowFocused())
            {
                return false;
            }

            return open;
        }

        public static void EndCursorPopup()
        {
            ImGui.End();
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
            lightText.W = 1.0f;
            darkText.W = 1.0f;

            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.Text, darkDiff > lightDiff ? darkText : lightText);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor);

            var pressed = button();

            ImGui.PopStyleColor(4);

            return pressed;
        }

        public static Vector4 RandomColor(int seed)
        {
            var random = new Random(seed);

            static float NextClamped(Random rand)
            {
                return MathExt.Lerp(0.25f, 0.75f, (float)rand.NextDouble());
            }

            return new Vector4(NextClamped(random), NextClamped(random), NextClamped(random), 1.0f);
        }
    }
}
