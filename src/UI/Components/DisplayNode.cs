using System;
using ImGuiNET;

namespace Microdancer
{
    public class DisplayNode : PluginUiBase, IDrawable<INode>, IDrawable<INode, string>
    {
        private readonly string _idPrefix;

        public DisplayNode(string idPrefix)
        {
            _idPrefix = idPrefix;
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
            var flags = GetFlags(node, filter, ref shouldDraw);
            if (!shouldDraw)
            {
                return false;
            }

            ImGui.PushID($"{node.Id}{filter ?? string.Empty}");

            var open = ImGui.TreeNodeEx($"{_idPrefix}{node.Id}", flags, $"{node.Name}");

            if (ImGui.IsItemClicked() && (node.Children.Count == 0 || _idPrefix == "library"))
            {
                Config.LibrarySelection = node.Id;
                PluginInterface.SavePluginConfig(Config);
            }

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Selectable("Select"))
                {
                    Config.LibrarySelection = node.Id;
                    PluginInterface.SavePluginConfig(Config);
                }

                if (ImGui.Selectable("Open"))
                {
                    OpenNode(node);
                }

                if (ImGui.Selectable("Reveal in File Explorer"))
                {
                    RevealNode(node);
                }

                if (node is Micro micro)
                {
                    if (ImGui.Selectable("Play"))
                    {
                        Config.LibrarySelection = micro.Id;
                        PluginInterface.SavePluginConfig(Config);
                        MicroManager.StartMicro(micro);
                    }

                    if (ImGui.Selectable($"Copy run command"))
                    {
                        ImGui.SetClipboardText($"/runmicro {micro.Id}");
                    }
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();

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

        private ImGuiTreeNodeFlags GetFlags(INode root, string? filter, ref bool shouldDraw)
        {
            return GetFlags(root, root, filter, ref shouldDraw, ImGuiTreeNodeFlags.SpanAvailWidth);
        }

        private ImGuiTreeNodeFlags GetFlags(
            INode root,
            INode node,
            string? filter,
            ref bool shouldDraw,
            ImGuiTreeNodeFlags flags
        )
        {
            var emptyFilter = string.IsNullOrWhiteSpace(filter);
            var matchesFilter = !emptyFilter && node.Name.Contains(filter!, StringComparison.CurrentCultureIgnoreCase);
            shouldDraw |= emptyFilter || matchesFilter;

            var hasChildren = node.Children.Count > 0;
            var isSelected = Config.LibrarySelection == node.Id;

            if (node == root && !hasChildren)
            {
                flags = ImGuiTreeNodeFlags.Leaf;
                if (isSelected)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
            }

            if (matchesFilter || (node != root && isSelected))
            {
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Bullet;

                if (node == root)
                {
                    flags &= ~ImGuiTreeNodeFlags.Bullet;
                }
            }

            foreach (var child in node.Children)
            {
                flags |= GetFlags(root, child, filter, ref shouldDraw, flags);
            }

            return flags;
        }
    }
}
