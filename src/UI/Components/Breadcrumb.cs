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

            var breadcrumb = new Stack<INode>();

            for (var n = node; n != null; n = n.Parent)
            {
                breadcrumb.Push(n);
            }

            if (breadcrumb.Count < 2)
            {
                return false;
            }

            var drawSeparator = false;
            do
            {
                var segment = breadcrumb.Pop();

                if (drawSeparator)
                {
                    ImGui.SameLine();
                    ImGui.Text("»");
                    ImGui.SameLine();
                }
                drawSeparator |= true;

                if (segment is Micro)
                {
                    ImGui.Selectable(segment.Name, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(segment.Name));
                    _contextMenu.Draw(segment, showCreateButtons: false, showViewButton: segment != node);

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
                    _contextMenu.Draw(segment, showCreateButtons: false, showViewButton: segment != node);
                }
            } while (breadcrumb.Count > 0);

            return true;
        }
    }
}
