using System;
using System.Collections.Generic;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class Breadcrumb : PluginUiBase, IDrawable<INode?>
    {
        private readonly NodeContextMenu _contextMenu;

        public Breadcrumb()
        {
            _contextMenu = new NodeContextMenu("breadcrumb-context-menu", allowRenameDelete: false);
        }

        public bool Draw(INode? node)
        {
            if (node == null)
            {
                return false;
            }

            if (ImGui.Selectable("Home", false, ImGuiSelectableFlags.None, ImGui.CalcTextSize("Home")))
            {
                Navigate(node.Id, Guid.Empty);
            }

            var breadCrumb = new Stack<INode>();

            for (var n = node; n != null; n = n.Parent)
            {
                breadCrumb.Push(n);
            }

            while (breadCrumb.Count > 0)
            {
                var segment = breadCrumb.Pop();

                ImGui.SameLine();
                ImGui.Text("»");
                ImGui.SameLine();

                if (segment is Micro)
                {
                    ImGui.Selectable(segment.Name, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(segment.Name));
                    _contextMenu.Draw(segment, false);

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
                    var canNavigate = ImGui.Selectable(
                        segment.Name,
                        false,
                        ImGuiSelectableFlags.None,
                        ImGui.CalcTextSize(segment.Name)
                    );

                    if (canNavigate)
                    {
                        Navigate(node.Id, segment.Id);
                    }
                    _contextMenu.Draw(segment, false);
                }
            }

            return true;
        }
    }
}
