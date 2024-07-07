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
            ImGui.Text("Library Path");

            string libPath = Config.LibraryPath;
            if (ImGui.InputText("##lib-path", ref libPath, 8192, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                Config.LibraryPath = libPath;
                Library.MarkAsDirty();
            }

            var hasLibrary = Directory.Exists(Config.LibraryPath);

            ImGui.SameLine();

            if (ImGui.Button(hasLibrary ? "Open Library" : "Create New Library"))
            {
                Directory.CreateDirectory(Config.LibraryPath);
                Open(Config.LibraryPath);
            }

            if (hasLibrary)
            {
                ImGui.SameLine();
                if (ImGui.Button("Reload Library"))
                {
                    Library.MarkAsDirty(forceReload: true);
                }
            }

            return true;
        }
    }
}
