using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;

namespace Microdancer
{
    public class ContentArea : PluginUiBase, IDrawable<INode?>
    {
        private readonly DisplayNode _node;
        private readonly FileContents _fileContents;
        private readonly CreateButtons _createButtons;
        private readonly NodeContextMenu _contextMenu;

        private MicroInfo? _info;

        public ContentArea()
        {
            _node = new DisplayNode("content-area", grid: true);
            _fileContents = new FileContents();
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

            ImGui.Spacing();

            if (micro != null)
            {
                _fileContents.Draw(micro);
            }
            else
            {
                ImGui.Spacing();

                var nodes = node?.Children ?? Library.GetNodes().ToList();

                if (nodes.Count > 0)
                {
                    if (node != null)
                    {
                        var cursorPos = ImGui.GetCursorPos();

                        ImGui.Selectable(" ", false, ImGuiSelectableFlags.Disabled, ImGui.GetContentRegionAvail());

                        _contextMenu.Draw(node);

                        ImGui.SetCursorPos(cursorPos);
                    }

                    var usableSpace =
                        ImGui.GetContentRegionAvail()
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.WindowPadding) * 2.0f
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding) * 4.0f;

                    var colCount = Math.Max((int)(usableSpace.X / (128 * ImGuiHelpers.GlobalScale)), 1);
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (i != 0 && i % colCount != 0)
                        {
                            ImGui.SameLine();
                        }
                        var child = nodes[i];
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

                    if (node?.IsReadOnly == false)
                    {
                        var basePath = (node as Folder)?.Path ?? Config.LibraryPath;
                        _createButtons.Draw(basePath);
                    }
                }
            }

            ImGui.EndChildFrame();

            return true;
        }
    }
}
