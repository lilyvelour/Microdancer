using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class NodeContextMenu : PluginUiBase, IDrawable<INode>
    {
        private readonly CreateButtons _createButtons;
        private readonly string _id;
        private readonly bool _allowSelectRenameDelete;

        private Guid _deleting;

        public NodeContextMenu(string id, bool allowSelectRenameDelete = true)
        {
            _id = id;
            _createButtons = new CreateButtons(CreateButtons.ButtonStyle.ContextMenu);
            _allowSelectRenameDelete = allowSelectRenameDelete;
        }

        bool IDrawable<INode>.Draw(INode node)
        {
            return Draw(node, true);
        }

        public bool Draw(INode node, bool showCreateButtons = true)
        {
            return Draw(node, out var _, showRename: false, showCreateButtons);
        }

        public bool Draw(INode node, out bool rename, bool showCreateButtons = true)
        {
            return Draw(node, out rename, showRename: true, showCreateButtons);
        }

        private bool Draw(INode node, out bool rename, bool showRename = true, bool showCreateButtons = true)
        {
            rename = false;

            var open = ImGui.BeginPopupContextItem($"{_id}{node.Id}");
            var canDelete = true;

            if (node is LibraryFolderRoot || node is SharedFolderRoot || node.Parent is StarredFolderRoot)
            {
                showRename = false;
                canDelete = false;

                if (node is SharedFolderRoot || node is StarredFolderRoot || node.Parent is StarredFolderRoot)
                {
                    showCreateButtons = false;
                }
            }

            if (open)
            {
                if (showCreateButtons && !node.IsReadOnly && node is Folder folder)
                {
                    _createButtons.Draw(folder.Path);

                    ImGui.Separator();
                }

                if (_allowSelectRenameDelete)
                {
                    if (ImGui.Selectable("Select"))
                    {
                        Select(node);
                    }

                    if (node is Micro && ImGui.Selectable("View in New Window"))
                    {
                        View(node);
                    }
                }

                if (!node.IsReadOnly)
                {
                    if (ImGui.Selectable("Open"))
                    {
                        OpenNode(node);
                    }

                    if (ImGui.Selectable("Reveal in File Explorer"))
                    {
                        RevealNode(node);
                    }
                }

                if (node is Micro micro)
                {
                    ImGui.Separator();

                    if (ImGui.Selectable("Play"))
                    {
                        Select(micro);
                        MicroManager.StartMicro(micro);
                    }

                    if (ImGui.Selectable($"Copy run command"))
                    {
                        ImGui.SetClipboardText($"/runmicro {micro.Id}");
                    }

                    if (!node.IsReadOnly)
                    {
                        ImGui.Separator();

                        var isStarred = Config.StarredItems.Contains(micro.Id);
                        if (ImGui.Selectable(isStarred ? "Unstar" : "Star"))
                        {
                            if (isStarred)
                            {
                                Config.Unstar(micro.Id);
                            }
                            else
                            {
                                Config.Star(micro.Id);
                            }

                            PluginInterface.SavePluginConfig(Config);

                            Library.MarkAsDirty();
                        }

                        var isShared = Config.SharedItems.Contains(micro.Id);
                        if (ImGui.Selectable(isShared ? "Stop sharing" : "Share"))
                        {
                            if (isShared)
                            {
                                Config.Unshare(micro.Id);
                            }
                            else
                            {
                                Config.Share(micro.Id);
                            }

                            PluginInterface.SavePluginConfig(Config);
                        }
                    }
                }

                if (!node.IsReadOnly && _allowSelectRenameDelete && (showRename || canDelete))
                {
                    ImGui.Separator();

                    if (showRename && ImGui.Selectable("Rename"))
                    {
                        rename = true;
                    }

                    if (canDelete)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f));
                        if (ImGui.Selectable("Delete"))
                        {
                            _deleting = node.Id;
                        }
                        ImGui.PopStyleColor();
                    }
                }

                ImGui.EndPopup();
            }

            if (_deleting == node.Id && canDelete)
            {
                ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(300, 80));
                var windowPosition = ImGui.GetIO().DisplaySize * 0.5f;
                windowPosition.X -= 150 * ImGuiHelpers.GlobalScale;
                windowPosition.Y -= 40 * ImGuiHelpers.GlobalScale;

                ImGui.SetNextWindowPos(windowPosition);
                var _ = true;
                var drawModal = ImGui.Begin($"Delete {node.GetType().Name}?", ref _, ImGuiWindowFlags.Modal);

                if (drawModal)
                {
                    ImGui.TextWrapped($"Are you sure you want to delete {node.Name}?");

                    if (ImGui.Button("Move to Recycle Bin"))
                    {
                        DeleteNode(node);
                        _deleting = Guid.Empty;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel") || !ImGui.IsWindowFocused())
                    {
                        _deleting = Guid.Empty;
                    }
                }
                ImGui.End();

                return true;
            }

            return open;
        }
    }
}
