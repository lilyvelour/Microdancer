using System;
using ImGuiNET;

namespace Microdancer
{
    public class Breadcrumb : PluginUiBase, IDrawable<INode?>
    {
        public bool Draw(INode? node)
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

                ImGui.Text("Home");
            }
            else if (node != null)
            {
                if (ImGui.Selectable("Home", false, ImGuiSelectableFlags.None, ImGui.CalcTextSize("Home")))
                {
                    Config.LibrarySelection = Guid.Empty;
                    PluginInterface.SavePluginConfig(Config);
                }

                ImGui.SameLine();
                ImGui.Text("»");
                ImGui.SameLine();

                var label = node.IsReadOnly ? "Shared With Me" : "Library";
                if (ImGui.Selectable(label, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(label)))
                {
                    Config.LibrarySelection = Library.Find<Node>(label)?.Id ?? Guid.Empty;
                    PluginInterface.SavePluginConfig(Config);
                }

                var basePath = node.IsReadOnly ? PluginInterface.SharedFolderPath() : Config.LibraryPath;
                var first = basePath.Length + 1;
                if (first < node.Path.Length)
                {
                    var relativePath = node.Path[first..];
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

                        var parent = node.Parent;

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

            return true;
        }
    }
}
