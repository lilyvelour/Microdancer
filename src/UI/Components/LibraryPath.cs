using System.IO;
using ImGuiNET;

namespace Microdancer
{
    public class LibraryPath : PluginUiBase, IDrawable
    {
        public void Draw()
        {
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
                OpenNode(null);
            }
        }
    }
}
