using System.IO;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class ServerCredentials : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            ImGui.Text("Server Credentials (optional)");

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("Microdancer can optionally be run with a server backend.");
            ImGui.Text("This lets users quickly share micro files between each other when connected to the same server.");

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("Server URI");
            ImGui.SameLine();
            var serverUri = Config.ServerUri;
            if (ImGui.InputTextWithHint("##server-uri", "https://example.com/share", ref serverUri,
                8192, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                Config.ServerUri = serverUri;
            }

            ImGui.Text("Server Password");
            ImGui.SameLine();
            var serverPassword = Config.ServerPassword;
            if (ImGui.InputText("##server-password", ref serverPassword, 8192,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.Password))
            {
                Config.ServerPassword = serverPassword;
            }

            return true;
        }
    }
}
