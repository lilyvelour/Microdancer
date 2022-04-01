using System;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class DisplayNode : PluginUiBase, IDrawable<INode>, IDrawable<INode, string>
    {
        private readonly string _idPrefix;
        private readonly NodeContextMenu _contextMenu;

        public DisplayNode(string idPrefix)
        {
            _idPrefix = idPrefix;
            _contextMenu = new NodeContextMenu();
        }

        bool IDrawable<INode>.Draw(INode node)
        {
            return Draw(node);
        }

        public bool Draw(INode node, string? filter = null)
        {
            var shouldDraw = false;
            return DrawImpl(node, filter, ref shouldDraw);
        }

        private bool DrawImpl(INode node, string? filter, ref bool shouldDraw)
        {
            var isShared = false;

            var flags = GetFlags(node, filter, ref shouldDraw, ref isShared);
            if (!shouldDraw)
            {
                return false;
            }

            ImGui.PushID($"{node.Id}{filter ?? string.Empty}item");

            var open = ImGui.TreeNodeEx($"{_idPrefix}{node.Id}", flags, $"{node.Name}");

            ImGui.PopID();

            if (ImGui.IsItemClicked() && (node.Children.Count == 0 || _idPrefix == "library"))
            {
                Config.LibrarySelection = node.Id;
                PluginInterface.SavePluginConfig(Config);
            }

            _contextMenu.Draw(node);

            if (isShared && node is not LibraryFolderRoot)
            {
                ImGui.SameLine();

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushID($"{node.Id}{filter ?? string.Empty}icon");
                ImGui.Text(FontAwesomeIcon.UserFriends.ToIconString());
                ImGui.PopID();
                ImGui.PopFont();
            }

            if (open)
            {
                foreach (var child in node.Children)
                {
                    DrawImpl(child, filter, ref shouldDraw);
                }

                ImGui.TreePop();
            }

            return true;
        }

        private ImGuiTreeNodeFlags GetFlags(INode root, string? filter, ref bool shouldDraw, ref bool isShared)
        {
            var flags = GetFlags(root, root, filter, ref shouldDraw, ref isShared, ImGuiTreeNodeFlags.SpanAvailWidth);

            if (
                root.Children.Count > 0
                && !string.IsNullOrWhiteSpace(filter)
                && !flags.HasFlag(ImGuiTreeNodeFlags.Bullet)
            )
            {
                shouldDraw = false;
            }

            return flags;
        }

        private ImGuiTreeNodeFlags GetFlags(
            INode root,
            INode node,
            string? filter,
            ref bool shouldDraw,
            ref bool isShared,
            ImGuiTreeNodeFlags flags
        )
        {
            var hasChildren = node.Children.Count > 0;
            var isSelected = Config.LibrarySelection == node.Id;

            var emptyFilter = string.IsNullOrWhiteSpace(filter);
            var matchesFilter = !emptyFilter && node.Path.Contains(filter!, StringComparison.CurrentCultureIgnoreCase);
            shouldDraw |= emptyFilter || matchesFilter;

            if (node == root)
            {
                if (node is Micro)
                {
                    isShared = Config.SharedItems.Contains(node.Id);
                }

                if (!hasChildren)
                {
                    flags = ImGuiTreeNodeFlags.Leaf;
                    if (isSelected)
                    {
                        flags |= ImGuiTreeNodeFlags.Selected;
                    }

                    shouldDraw = emptyFilter || matchesFilter;
                }
            }

            if (matchesFilter || (node != root && isSelected))
            {
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Bullet;

                if (node == root && node.Parent != null)
                {
                    flags &= ~ImGuiTreeNodeFlags.Bullet;
                }
            }

            foreach (var child in node.Children)
            {
                flags |= GetFlags(root, child, filter, ref shouldDraw, ref isShared, flags);
                if (child is Micro)
                {
                    isShared |= Config.SharedItems.Contains(child.Id);
                }
            }

            return flags;
        }
    }
}
