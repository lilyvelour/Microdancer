using System;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace Microdancer
{
    public class Breadcrumb : PluginUiBase, IDrawable<INode?>
    {
        public void Draw(INode? node)
        {
            ImGui.Spacing();
            ImGui.SameLine();

            if (node == null)
            {
                if (Config.LibrarySelection != Guid.Empty)
                {
                    Config.LibrarySelection = Guid.Empty;
                    PluginInterface.SavePluginConfig(Config);
                }

                ImGui.Text("Library");
            }
            else if (node != null)
            {
                if (ImGui.Selectable("Library", false, ImGuiSelectableFlags.None, ImGui.CalcTextSize("Library")))
                {
                    Config.LibrarySelection = Guid.Empty;
                    PluginInterface.SavePluginConfig(Config);
                }

                var relativePath = node.Path[(Config.LibraryPath.Length + 1)..];
                var breadCrumb = relativePath.Split(new[] { '/', '\\' });

                var currentPath = string.Empty;
                foreach (var segment in breadCrumb)
                {
                    if (string.IsNullOrWhiteSpace(segment))
                    {
                        continue;
                    }

                    ImGui.SameLine();
                    ImGui.Text("»");
                    ImGui.SameLine();

                    currentPath += $"/{segment}";

                    var parent = Library.Find<Folder>(Path.GetFullPath(Config.LibraryPath + currentPath));

                    if (segment.EndsWith(".micro"))
                    {
                        ImGui.Text(segment[..^6]);
                    }
                    else
                    {
                        if (
                            ImGui.Selectable(segment, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(segment))
                            && parent != null
                        )
                        {
                            Config.LibrarySelection = parent.Id;
                            PluginInterface.SavePluginConfig(Config);
                        }
                    }
                }
            }
        }
    }
}
