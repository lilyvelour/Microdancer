using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class NodeContextMenu : PluginUiBase, IDrawable<INode>
    {
        private readonly CreateButtons _createButtons;
        private readonly string _id;
        private readonly bool _allowRenameDelete;

        private Guid _deleting;

        public NodeContextMenu(string id, bool allowRenameDelete = true)
        {
            _id = id;
            _createButtons = new CreateButtons(CreateButtons.ButtonStyle.ContextMenu);
            _allowRenameDelete = allowRenameDelete;
        }

        bool IDrawable<INode>.Draw(INode node)
        {
            return Draw(node, showCreateButtons: true, showViewButton: true);
        }

        public bool Draw(INode node, bool showCreateButtons = true, bool showViewButton = true)
        {
            return Draw(node, out var _, showRename: false, showCreateButtons, showViewButton);
        }

        public bool Draw(INode node, out bool rename, bool showCreateButtons = true, bool showViewButton = true)
        {
            return Draw(node, out rename, showRename: true, showCreateButtons, showViewButton);
        }

        private bool Draw(
            INode node,
            out bool rename,
            bool showRename = true,
            bool showCreateButtons = true,
            bool showViewButton = true
        )
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

                if (showViewButton)
                {
                    var isViewing = Config.LibrarySelection == node.Id || Config.OpenWindows.Contains(node.Id);
                    if (ImGui.Selectable(isViewing ? "Select Tab" : "View in New Tab"))
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
                        }
                    }
                }

                if (!node.IsReadOnly && _allowRenameDelete && (showRename || canDelete))
                {
                    ImGui.Separator();

                    if (showRename && ImGui.Selectable("Rename"))
                    {
                        rename = true;
                    }

                    if (canDelete)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
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
                ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(400, 80));
                var windowPosition = ImGui.GetIO().DisplaySize * 0.5f;
                windowPosition.X -= 200 * ImGuiHelpers.GlobalScale;
                windowPosition.Y -= 40 * ImGuiHelpers.GlobalScale;

                ImGui.SetNextWindowPos(windowPosition);
                var modalOpen = true;

                // HACK: Bit hacky, we force disable scrolling here
                var drawModal = ImGui.Begin(
                    $"Delete {node.GetType().Name}?##{_id}-{_deleting}",
                    ref modalOpen,
                    ImGuiWindowFlags.Modal
                        | ImGuiWindowFlags.NoSavedSettings
                        | ImGuiWindowFlags.NoScrollbar
                        | ImGuiWindowFlags.NoScrollWithMouse
                        | ImGuiWindowFlags.NoResize
                        | ImGuiWindowFlags.NoDocking
                );

                if (drawModal)
                {
                    ImGui.Text($"Are you sure you want to delete {node.Name}?");

                    if (ImGui.Button("Move to Recycle Bin"))
                    {
                        DeleteNode(node);
                        _deleting = Guid.Empty;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        _deleting = Guid.Empty;
                    }

                    if (!ImGui.IsWindowFocused())
                    {
                        _deleting = Guid.Empty;
                    }
                }

                ImGui.End();

                if (!modalOpen)
                {
                    _deleting = Guid.Empty;
                }

                return true;
            }

            return open;
        }
    }
}
