using System.IO;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class LibraryPath : PluginUiBase, IDrawable
    {
        public bool Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 4.0f));
            ImGui.BeginChildFrame(7000, new(-1, 60 * ImGuiHelpers.GlobalScale), ImGuiWindowFlags.NoBackground);
            ImGui.PopStyleVar();
            ImGui.Text("Library Path");

            string libPath = Config.LibraryPath;
            if (ImGui.InputText("##lib-path", ref libPath, 8192, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                Config.LibraryPath = libPath;
                PluginInterface.SavePluginConfig(Config);
                Library.MarkAsDirty();
            }

            var hasLibrary = Directory.Exists(Config.LibraryPath);

            ImGui.SameLine();

            if (ImGui.Button(hasLibrary ? "Open Library" : "Create New Library"))
            {
                Directory.CreateDirectory(Config.LibraryPath);
                Open(Config.LibraryPath);
            }
            ImGui.EndChildFrame();

            return true;
        }
    }
}
