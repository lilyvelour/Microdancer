using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class ServerCredentials : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            if (!ImGui.TreeNode("Server Credentials (optional)"))
                return true;

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("Microdancer can optionally be run with a server backend.");
            ImGui.Text("This lets users quickly share micro files between each other when connected to the same server.");
            ImGui.TextColored(new(1.0f, 1.0f, 0.0f, 1.0f), "Only connect to Microdancer servers that you trust!");
            ImGui.TextColored(new(1.0f, 1.0f, 0.0f, 1.0f), "Servers can see your player name, world, party members, nearby players, and your shared micro files.");
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.BeginChild("Server URI", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            ImGui.Columns(2, "Server URI", false);
            ImGui.SetColumnWidth(0, 100 * ImGuiHelpers.GlobalScale);
            ImGui.Spacing();
            ImGui.Text("Server URI");
            ImGui.NextColumn();
            var serverUri = Config.ServerUri;
            if (ImGui.InputTextWithHint("##server-uri", "https://example.com/share", ref serverUri, 8192))
            {
                Config.ServerUri = serverUri;
            }
            ImGui.EndChild();

            ImGui.BeginChild("Username", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            ImGui.Columns(2, "Username", false);
            ImGui.SetColumnWidth(0, 100 * ImGuiHelpers.GlobalScale);
            ImGui.Spacing();
            ImGui.Text("Username");
            ImGui.NextColumn();
            var serverUsername = Config.ServerUsername;
            if (ImGui.InputTextWithHint("##server-username", "username", ref serverUsername, 8192))
            {
                Config.ServerUsername = serverUsername;
            }
            ImGui.EndChild();

            ImGui.BeginChild("Password", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            ImGui.Columns(2, "Password", false);
            ImGui.SetColumnWidth(0, 100 * ImGuiHelpers.GlobalScale);
            ImGui.Spacing();
            ImGui.Text("Password");
            ImGui.NextColumn();
            var serverPassword = Config.ServerPasswordPlaceholder;
            if (ImGui.InputTextWithHint("##server-password", "password", ref serverPassword, 256, ImGuiInputTextFlags.Password))
            {
                var hash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(serverPassword))).Replace("-", "");
                Config.ServerPasswordHash = hash;
                Config.ServerPasswordPlaceholder = hash[..serverPassword.Length];
            }
            ImGui.EndChild();

            ImGui.Spacing();

            ImGui.BeginChild("Connection Status", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            ImGui.Columns(2, "Connection Status", false);
            ImGui.SetColumnWidth(0, 100 * ImGuiHelpers.GlobalScale);
            ImGui.NextColumn();
            if (SharedContent.Connected)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "Connected");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(SharedContent.LastError))
                {
                    ImGui.TextColored(
                        new Vector4(1, 0, 0, 1),
                        "Disconnected");
                }
                else
                {
                    ImGui.TextColored(
                        new Vector4(1, 0, 0, 1),
                        $"Disconnected ({SharedContent.LastError})");
                }
            }
            ImGui.EndChild();

            ImGui.TreePop();
            return true;
        }
    }
}
