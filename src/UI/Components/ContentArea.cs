using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class ContentArea : PluginUiBase, IDrawable<INode?>
    {
        private readonly DisplayNode _node;
        private readonly FileContents _fileContents;
        private readonly Breadcrumb _breadcrumb;
        private readonly CreateButtons _createButtons;
        private readonly NodeContextMenu _contextMenu;

        private MicroInfo? _info;

        public ContentArea()
        {
            _node = new DisplayNode("content-area", grid: true);
            _fileContents = new FileContents();
            _breadcrumb = new Breadcrumb();
            _createButtons = new CreateButtons();
            _contextMenu = new NodeContextMenu("content-area-context-menu", allowRenameDelete: false);
        }

        public bool Draw(INode? node)
        {
            var micro = node as Micro;

            if (micro != null)
            {
                if (MicroManager.Current?.Micro == micro)
                {
                    _info = MicroManager.Current;
                }
                else if (_info?.Micro != micro || _info.CurrentTime > TimeSpan.Zero)
                {
                    _info = new MicroInfo(micro);
                }
            }

            var frameSize = new Vector2(-1, -1);

            if (micro != null)
            {
                frameSize.Y = -134 * ImGuiHelpers.GlobalScale;
            }

            ImGui.BeginChildFrame(2, frameSize, ImGuiWindowFlags.NoBackground);

            if (micro == null && node?.IsReadOnly == false)
            {
                var basePath = (node as Folder)?.Path ?? Config.LibraryPath;
                _createButtons.Draw(basePath);
            }

            ImGui.Spacing();
            ImGui.Spacing();

            if (!_breadcrumb.Draw(node))
            {
                ImGui.Text(" ");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            if (micro != null)
            {
                _fileContents.Draw(micro);
            }
            else
            {
                var nodes = node?.Children ?? Library.GetNodes();

                if (nodes.Count > 0)
                {
                    if (node != null)
                    {
                        var cursorPos = ImGui.GetCursorPos();

                        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
                        ImGuiExt.TintButton(" ", ImGui.GetContentRegionAvail(), Vector4.Zero);
                        ImGui.PopStyleVar();

                        _contextMenu.Draw(node, !node.IsReadOnly, false);

                        ImGui.SetCursorPos(cursorPos);
                    }

                    var usableSpace = ImGui.GetContentRegionAvail();

                    var colCount = Math.Max(
                        (int)(
                            usableSpace.X
                            / (
                                (128 * ImGuiHelpers.GlobalScale)
                                + ImGui.GetStyle().ItemSpacing.X
                                - ImGui.GetStyle().FrameBorderSize
                            )
                        ),
                        1
                    );

                    if (node?.Parent != null)
                    {
                        ImGui.SetItemAllowOverlap();

                        ImGui.BeginGroup();
                        ImGui.SetWindowFontScale(2.0f);
                        if (
                            ImGuiExt.IconButton(
                                FontAwesomeIcon.LevelUpAlt,
                                "Go back",
                                ImGuiHelpers.ScaledVector2(128, 128)
                            )
                        )
                        {
                            Navigate(node.Id, node.Parent.Id);
                        }
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
                        {
                            View(node.Parent.Id);
                        }
                        ImGui.SetWindowFontScale(1.0f);

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
                        if (ImGuiExt.TintButton("...", new(-1, -1), Vector4.Zero))
                        {
                            Navigate(node.Id, node.Parent.Id);
                        }

                        ImGui.PopStyleColor(2);

                        ImGui.EndChildFrame();

                        ImGui.EndGroup();

                        ImGui.SameLine();
                    }

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        // HACK: Math is hard so let's do it manually
                        if (node?.Parent != null)
                        {
                            if (i < colCount - 1)
                            {
                                ImGui.SameLine();
                            }
                            else if (i == colCount - 1)
                            {
                                /* no-op */
                            }
                            else if (i < (colCount * 2 - 2))
                            {
                                ImGui.SameLine();
                            }
                            else
                            {
                                if ((i + 1) % colCount != 0)
                                {
                                    ImGui.SameLine();
                                }
                            }
                        }
                        else if (i > 0 && i % colCount != 0)
                        {
                            ImGui.SameLine();
                        }

                        var child = nodes[i];

                        ImGui.SetItemAllowOverlap();
                        _node.Draw(child);
                    }
                }
                else if (node is SharedFolderRoot)
                {
                    ImGui.Text("No shared content available.");
                }
                else
                {
                    ImGui.Text("This folder is lonely...let's get started!");

                    ImGui.Spacing();
                }
            }

            ImGui.EndChildFrame();

            return true;
        }
    }
}
