using System.IO;
using System.Numerics;
using ImGuiNET;

namespace Microdancer
{
    public class NewFileTemplate : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            if (!ImGui.TreeNode("New File Template (optional)"))
                return true;

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("Microdancer supports a custom template for new micro files. The file must be named .template.micro.");

            if (!Directory.Exists(Config.LibraryPath))
            {
                ImGui.TextColored(
                    new(1.0f, 0.0f, 0.0f, 1.0f),
                    "Please create or select a library before making a template.");
                return true;
            }

            var templatePath = Path.Combine(Config.LibraryPath, ".template.micro");
            if (!File.Exists(templatePath))
            {
                ImGui.TextColored(
                    new(1.0f, 1.0f, 0.0f, 1.0f),
                    "A template file has not been created. Create one now?");
                if (ImGui.Button("Create Default Template"))
                {
                    File.WriteAllText(templatePath, DefaultTemplate);
                }
            }
            else
            {
                ImGui.Text("A template file currently exists. Press the button below to open the template in your default editor.");
                if (ImGui.Button("Edit Template"))
                {
                    Open(templatePath);
                }
            }
            if (ImGui.TreeNode("Tokens"))
            {
                ImGui.TextColored(
                    new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "[fileName] - micro file name (without extension)");
                ImGui.TextColored(
                    new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "[year] - the current year");
                ImGui.TextColored(
                    new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "[playerName] - the player's full name");
                ImGui.TextColored(
                    new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "[playerWorld] - the player's world");
                ImGui.TreePop();
            }

            ImGui.TreePop();
            return true;
        }

        private const string DefaultTemplate = $@"# =====================================================
# [fileName]
# Choreography © [year] by [playerName]@[playerWorld]
# =====================================================

/autocountdown <wait.0>
/autobusy <wait.0>

#region :Before
/target <me> <wait.0>
/bm off <wait.0>
/snapchange ""[My Gear Set]"" <wait.2>
#endregion

#region Verse 1
#endregion

#region Chorus 1
#endregion

#region Verse 2
#endregion

#region Chorus 2
#endregion

#region Bridge
#endregion

#region Outro
#endregion";
    }
}
