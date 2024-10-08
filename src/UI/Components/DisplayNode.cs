using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class DisplayNode : PluginUiBase, IDrawable<INode>, IDrawable<INode, string>
    {
        private readonly string _idPrefix;
        private readonly bool _grid;

        private readonly NodeContextMenu _contextMenu;
        private Guid _renaming = Guid.Empty;
        private bool _shouldSetRenameFocus = false;

        private string _newName = string.Empty;
        private static bool _ignoreInput;

        public DisplayNode(string idPrefix, bool grid = false)
        {
            _idPrefix = idPrefix;
            _grid = grid;
            _contextMenu = new NodeContextMenu($"{_idPrefix}-node-context-menu");
        }

        bool IDrawable<INode>.Draw(INode node)
        {
            return Draw(node);
        }

        public bool Draw(INode node, string? filter = null)
        {
            var shouldDraw = false;
            var result = DrawImpl(node, filter, ref shouldDraw);

            return result;
        }

        private bool DrawImpl(INode node, string? filter, ref bool shouldDraw)
        {
            if (node == null)
            {
                return false;
            }

            // HACK: Update starred display instantly instead of waiting for a refresh
            if (node.Parent is StarredFolderRoot && !Config.StarredItems.Contains(node.Id))
            {
                return false;
            }

            var isShared = false;
            var isFolderRoot = node is LibraryFolderRoot || node is SharedFolderRoot;
            var micro = node as Micro;

            bool open;
            bool rename;
            var flags = GetFlags(node, filter, ref shouldDraw, ref isShared);

            if (!shouldDraw)
            {
                return false;
            }

            ImGui.PushID($"{node.Id}{filter ?? string.Empty}item");

            if (_grid)
            {
                open = false;
                var icon = node switch
                {
                    StarredFolderRoot => FontAwesomeIcon.Star,
                    LibraryFolderRoot => FontAwesomeIcon.Book,
                    SharedFolderRoot => FontAwesomeIcon.UserFriends,
                    Folder => FontAwesomeIcon.Folder,
                    _ => FontAwesomeIcon.FileAlt,
                };
                ImGui.BeginGroup();

                ImGui.SetWindowFontScale(2.0f);
                if (ImGuiExt.IconButton(icon, node.Name, ImGuiHelpers.ScaledVector2(128, 128)))
                {
                    if (!_ignoreInput)
                    {
                        Navigate(node.Parent?.Id ?? Guid.Empty, node.Id);
                    }
                    _ignoreInput = false;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
                {
                    if (!_ignoreInput)
                    {
                        View(node.Id);
                    }
                    _ignoreInput = false;
                }
                ImGui.SetWindowFontScale(1.0f);

                _contextMenu.Draw(node, out rename, showCreateButtons: false);

                if (rename && _renaming != node.Id)
                {
                    _renaming = node.Id;
                    _newName = node.Name;
                    _shouldSetRenameFocus = true;
                }

                if (_renaming != node.Id)
                {
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, Vector4.Zero);
                    ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                    ImGui.BeginChildFrame(
                        (uint)HashCode.Combine(8429234, node),
                        new(128 * ImGuiHelpers.GlobalScale, 30 * ImGuiHelpers.GlobalScale),
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
                    );
                    ImGui.PopStyleVar(3);

                    var label = node.Name;

                    while (
                        label.Length > 3
                        && ImGuiHelpers.GetButtonSize($"{label}...").X > ImGui.GetContentRegionAvail().X
                    )
                    {
                        label = label[..^1];
                    }

                    if (label != node.Name)
                    {
                        label += "...";
                    }

                    if (ImGuiExt.TintButton(label, new(-1, -1), Vector4.Zero))
                    {
                        Navigate(node.Parent?.Id ?? Guid.Empty, node.Id);
                    }

                    ImGui.PopStyleColor(2);

                    ImGui.EndChildFrame();
                }
            }
            else
            {
                if (flags.HasFlag(ImGuiTreeNodeFlags.Bullet))
                {
                    ImGui.SetNextItemOpen(true);
                }

                if (isFolderRoot)
                {
                    flags |= ImGuiTreeNodeFlags.CollapsingHeader;
                    flags &= ~ImGuiTreeNodeFlags.Bullet;
                    flags &= ~ImGuiTreeNodeFlags.Leaf;
                }
                else
                {
                    flags |= ImGuiTreeNodeFlags.FramePadding;
                }

                var label = _renaming == node.Id ? string.Empty : $"{node.Name}";

                if (micro != null && !node.IsReadOnly)
                {
                    label = $"  {label}";

                    var cursorPos = ImGui.GetCursorPosX();
                    DrawStar(micro);

                    ImGui.SameLine();

                    ImGui.SetCursorPosX(cursorPos);
                }

                open = ImGui.TreeNodeEx($"{_idPrefix}{node.Id}", flags, label);
            }

            ImGui.PopID();

            if (_renaming == node.Id)
            {
                if (ImGui.IsItemClicked())
                {
                    _shouldSetRenameFocus = true;
                }

                if (!_grid)
                {
                    ImGui.SameLine();
                }

                ImGui.PushItemWidth(_grid ? 128 * ImGuiHelpers.GlobalScale : 0);
                if (_shouldSetRenameFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _shouldSetRenameFocus = false;
                }
                if (
                    ImGui.InputText(
                        $"##{_idPrefix}{node.Id}rename",
                        ref _newName,
                        1024,
                        ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue
                    )
                )
                {
                    RenameNode(node, _newName);

                    _renaming = Guid.Empty;
                    _newName = string.Empty;
                    _ignoreInput = true;
                }
                ImGui.PopItemWidth();

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && !ImGui.IsItemHovered())
                {
                    RenameNode(node, _newName);

                    _renaming = Guid.Empty;
                    _newName = string.Empty;
                    _ignoreInput = true;
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    _renaming = Guid.Empty;
                    _newName = string.Empty;
                }
            }
            else if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
            {
                if (!_ignoreInput)
                {
                    View(node);
                }
                _ignoreInput = false;
            }
            else if (!_grid && ImGui.IsItemClicked())
            {
                if (!_ignoreInput)
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        View(node);
                    }
                    else
                    {
                        Select(node);
                    }
                }
                _ignoreInput = false;
            }

            _contextMenu.Draw(node, out rename, showCreateButtons: false);

            if (rename && _renaming != node.Id)
            {
                _renaming = node.Id;
                _newName = node.Name;
                _shouldSetRenameFocus = true;
            }

            if (isShared && _renaming != node.Id && node is not LibraryFolderRoot)
            {
                var cursorPos = ImGui.GetCursorPos();

                if (_grid)
                {
                    ImGui.SetCursorPos(
                        new(cursorPos.X + 100 * ImGuiHelpers.GlobalScale, cursorPos.Y - 64 * ImGuiHelpers.GlobalScale)
                    );
                }
                else
                {
                    ImGui.SameLine();
                }

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushID($"{node.Id}{filter ?? string.Empty}icon");
                ImGui.Text(FontAwesomeIcon.UserFriends.ToIconString());
                ImGui.PopID();
                ImGui.PopFont();

                if (_grid)
                {
                    ImGui.SetCursorPos(cursorPos);
                }
            }

            if (micro != null && _grid && !node.IsReadOnly)
            {
                var cursorPos = ImGui.GetCursorPos();

                ImGui.SetCursorPos(
                    new(cursorPos.X + 2 * ImGuiHelpers.GlobalScale, cursorPos.Y - 68 * ImGuiHelpers.GlobalScale)
                );

                DrawStar(micro);

                ImGui.SetCursorPos(cursorPos);
            }

            if (_grid)
            {
                ImGui.EndGroup();
            }

            if (open && !_grid)
            {
                foreach (var child in node.Children)
                {
                    DrawImpl(child, filter, ref shouldDraw);
                }

                if (!isFolderRoot)
                {
                    ImGui.TreePop();
                }
                else if (node is SharedFolderRoot && node.Children.Count == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Theme.GetColor(ImGuiCol.TextDisabled));
                    ImGui.TreeNodeEx($"{_idPrefix}{node.Id}empty", ImGuiTreeNodeFlags.Leaf, SharedContent.Connected ? "- None -" : "- Disconnected -");
                    ImGui.PopStyleColor();
                    ImGui.TreePop();
                }
            }

            return true;
        }

        private void DrawStar(Micro micro)
        {
            var isStarred = Config.StarredItems.Contains(micro.Id);
            if (!isStarred)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Theme.GetColor(ImGuiCol.TextDisabled) * 0.75f);
            }

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
            ImGui.Button(FontAwesomeIcon.Star.ToIconString());

            if (
                ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax())
                && ImGui.IsMouseClicked(ImGuiMouseButton.Left)
            )
            {
                _ignoreInput = true;

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
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
            ImGui.PopFont();

            if (!isStarred)
            {
                ImGui.PopStyleColor();
            }
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
                    shouldDraw = emptyFilter || matchesFilter;
                }

                if (isSelected)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
            }

            if (matchesFilter || (emptyFilter && node != root && isSelected))
            {
                flags |= ImGuiTreeNodeFlags.Bullet;

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
