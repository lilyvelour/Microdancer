using System;
using ImGuiNET;

namespace Microdancer
{
    public class DisplayNode : PluginUiBase, IDrawable<INode>
    {
        private readonly string _idPrefix;

        private bool _isRenaming;
        private string? _rename;

        public DisplayNode(string idPrefix)
        {
            _idPrefix = idPrefix;
        }

        public void Draw(INode node)
        {
            ImGui.PushID($"{node.Id}");

            bool open;

            if (node.Children.Count == 0)
            {
                var flags = ImGuiTreeNodeFlags.Leaf;
                if (Config.LibrarySelection == node.Id)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
                open = ImGui.TreeNodeEx($"{_idPrefix}{node.Id}", flags, $"{node.Name}");

                if (ImGui.IsItemClicked())
                {
                    Config.LibrarySelection = Config.LibrarySelection == node.Id ? Guid.Empty : node.Id;
                    PluginInterface.SavePluginConfig(Config);
                }
            }
            else
            {
                var flags = ImGuiTreeNodeFlags.None;
                if (_idPrefix != string.Empty)
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }
                open = ImGui.TreeNodeEx($"{_idPrefix}{node.Id}", flags, $"{node.Name}");

                if (_idPrefix.Length == 0 && ImGui.IsItemClicked() && Config.LibrarySelection != node.Id)
                {
                    Config.LibrarySelection = node.Id;
                    PluginInterface.SavePluginConfig(Config);
                }
            }

            if (ImGui.BeginPopupContextItem())
            {
                if (node is Micro micro)
                {
                    if (ImGui.Button("Play##context"))
                    {
                        MicroManager.StartMicro(micro);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Open File##context"))
                    {
                        OpenNode(node);
                    }
                }
                else
                {
                    if (ImGui.Button("Open Folder##context"))
                    {
                        OpenNode(node);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Rename Folder##context"))
                    {
                        _isRenaming = true;
                    }

                    if (_isRenaming)
                    {
                        _rename = node.Name;

                        if (ImGui.InputText("RenameFolder", ref _rename, 256))
                        {
                            _isRenaming = false;
                            // no-op
                        }
                    }
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();

            if (open)
            {
                foreach (var child in node.Children)
                {
                    Draw(child);
                }

                ImGui.TreePop();
            }
        }
    }
}
