using System;
using Dalamud.Interface;
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
                ImGui.Text("Home");
            }
            else if (node != null)
            {
                if (ImGui.Selectable("Home", false, ImGuiSelectableFlags.None, ImGui.CalcTextSize("Home")))
                {
                    DeselectAll();
                }

                ImGui.SameLine();
                ImGui.Text("»");
                ImGui.SameLine();
                string label = node switch
                {
                    SharedFolderRoot => "Shared with Me",
                    StarredFolderRoot => "Starred",
                    _ => "Library",
                };
                if (ImGui.Selectable(label, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(label)))
                {
                    Select(Library.Find<Node>(label)?.Id ?? Guid.Empty);
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

                        if (node is Micro && segment.EndsWith(".micro"))
                        {
                            ImGui.Text(segment[..^6]);
                            var isShared = Config.SharedItems.Contains(node.Id);
                            if (isShared)
                            {
                                ImGui.SameLine();

                                ImGui.PushFont(UiBuilder.IconFont);
                                ImGui.Text(FontAwesomeIcon.UserFriends.ToIconString());
                                ImGui.PopFont();
                            }
                        }
                        else
                        {
                            if (
                                ImGui.Selectable(segment, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(segment))
                                && parent != null
                            )
                            {
                                Select(parent);
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
