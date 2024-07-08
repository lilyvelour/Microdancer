using System.IO;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class LibraryPath : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            ImGui.Text("Microdancer requires a library folder in order to store micro files.");
            ImGui.Text("This can be anywhere on your system, but it is recommended to place the folder close to a root drive.");

            ImGui.Spacing();
            ImGui.Spacing();


            ImGui.BeginChild("Path", new Vector2(-1, 25 * ImGuiHelpers.GlobalScale));
            ImGui.Columns(2, "Path", false);
            ImGui.SetColumnWidth(0, 100 * ImGuiHelpers.GlobalScale);
            ImGui.Spacing();
            ImGui.Text("Library Path");
            ImGui.NextColumn();
            string libPath = Config.LibraryPath;
            if (ImGui.InputTextWithHint("##lib-path", "C:\\FFXIV\\Microdancer",
                ref libPath, 8192, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                Config.LibraryPath = libPath;
                Library.MarkAsDirty();
            }

            var hasLibrary = Directory.Exists(Config.LibraryPath);

            if (hasLibrary)
            {
                ImGui.SameLine();

                if (ImGui.Button("Open Library"))
                {
                    Open(Config.LibraryPath);
                }

                ImGui.SameLine();
                if (ImGui.Button("Reload Library"))
                {
                    Library.MarkAsDirty(forceReload: true);
                }
            }
            ImGui.EndChild();

            return true;
        }
    }
}