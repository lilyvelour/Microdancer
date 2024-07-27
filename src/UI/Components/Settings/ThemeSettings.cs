using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class ThemeSettings : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            if (!ImGui.TreeNode("Theme Settings"))
                return true;

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.BeginChild("UI Accent Color", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            var uiColor = Config.UiColor;
            if (ImGui.ColorEdit3("UI Accent Color", ref uiColor))
            {
                Config.UiColor = uiColor;
                PluginInterface.SaveConfiguration();
            }
            ImGui.SameLine();
            if (ImGui.Button("Reset"))
            {
                Config.UiColor = new Vector3(0.89411765f, 0.0f, 0.06666667f);
                PluginInterface.SaveConfiguration();
            }
            ImGui.EndChild();

            ImGui.TreePop();
            return true;
        }
    }
}
