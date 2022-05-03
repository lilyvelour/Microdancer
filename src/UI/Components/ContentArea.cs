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
        private readonly Breadcrumb _breadcrumb;
        private readonly DisplayNode _node;
        private readonly FileContents _fileContents;
        private readonly CreateButtons _createButtons;
        private readonly NodeContextMenu _contextMenu;

        private MicroInfo? _info;

        private float _lastColumnWidth;

        public ContentArea()
        {
            _breadcrumb = new Breadcrumb();
            _node = new DisplayNode("content-area", grid: true);
            _fileContents = new FileContents();
            _createButtons = new CreateButtons();
            _contextMenu = new NodeContextMenu("content-area-context-menu", allowSelectRenameDelete: false);
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

            _breadcrumb.Draw(node);

            ImGui.Spacing();

            if (micro != null)
            {
                var inCombat = Condition[ConditionFlag.InCombat];

                var regions = _info!.AllRegions;
                var regionButtonSize = Vector2.Zero;

                float? newColumnWidth = null;

                ImGui.Columns(2);

                _fileContents.Draw(micro);

                ImGui.NextColumn();

                ImGui.BeginChildFrame(
                    20,
                    new(
                        -1,
                        ImGui.GetContentRegionAvail().Y
                            - Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize)
                            + ImGuiHelpers.GlobalScale
                    ),
                    ImGuiWindowFlags.NoBackground
                );

                if (regions.Length == 0 || regions.All(r => r.IsDefaultRegion))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    if (micro.IsReadOnly)
                    {
                        ImGui.TextWrapped("- No regions -");
                    }
                    else
                    {
                        ImGui.TextWrapped(
                            "Add a region to your file (using #region [name] and #endregion) to have it show up here.\n\nRegions starting with \":\" will show up as special buttons that are not part of the timeline."
                        );
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    regions = regions.Where(r => !r.IsDefaultRegion).ToArray();

                    for (int i = 0; i < regions.Length; i++)
                    {
                        var sz = ImGui.CalcTextSize($"{i + 1}");
                        if (sz.X >= regionButtonSize.X)
                        {
                            regionButtonSize = sz;
                        }
                    }

                    regionButtonSize.X += 80 * ImGuiHelpers.GlobalScale;
                    regionButtonSize.Y += 40 * ImGuiHelpers.GlobalScale;

                    var col = 0;
                    var regionNumber = 1;

                    var columnWidth = ImGui.GetColumnWidth();
                    if (Math.Abs(columnWidth - _lastColumnWidth) > 1.0f)
                    {
                        var w = columnWidth;
                        var gridWidth =
                            regionButtonSize.X
                            + ImGui.GetStyle().ItemSpacing.X * 2.0f
                            + ImGui.GetStyle().ColumnsMinSpacing * 2.0f
                            + ImGui.GetStyle().FramePadding.X * 2.0f;

                        if (w % gridWidth < gridWidth * 0.5f)
                            w -= w % gridWidth;
                        else
                            w += gridWidth - w % gridWidth;

                        newColumnWidth = Math.Max(w, gridWidth);
                    }

                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (regions[i].IsDefaultRegion)
                        {
                            continue;
                        }

                        col++;

                        var region = regions[i];
                        var isNamedRegion = false;
                        var size = regionButtonSize;

                        if (region.IsNamedRegion)
                        {
                            while (
                                size.X + regionButtonSize.X
                                < ImGui.GetContentRegionAvail().X
                                    + Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).X
                            )
                            {
                                size.X += regionButtonSize.X + ImGui.GetStyle().ItemSpacing.X;
                            }
                            isNamedRegion = true;
                        }

                        var currentRegion = MicroManager.Current?.CurrentCommand?.Region;

                        if (
                            !isNamedRegion
                            && MicroManager.Current?.Micro == micro
                            && MicroManager.PlaybackState != PlaybackState.Stopped
                            && currentRegion == region
                        )
                        {
                            ImGui.ProgressBar(currentRegion.GetProgress(), size, $"{regionNumber++}");
                        }
                        else if (ImGui.Button(isNamedRegion ? region.Name : $"{regionNumber++}", size))
                        {
                            if (!inCombat)
                            {
                                MicroManager.StartMicro(micro, region.Name);
                            }
                        }

                        ImGuiExt.TextTooltip(region.Name);

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGui.Selectable($"Copy run command"))
                            {
                                ImGui.SetClipboardText(
                                    $"/runmicro {micro.Id} \"{(region.IsNamedRegion ? ":" : string.Empty)}{region.Name}\""
                                );
                            }

                            ImGui.EndPopup();
                        }

                        if (
                            i < regions.Length - 1
                            && !isNamedRegion
                            && ImGui.GetContentRegionAvail().X - ((size.X + ImGui.GetStyle().ItemSpacing.X) * col)
                                > size.X
                        )
                        {
                            var nextRegion = i == regions.Length - 1 ? null : regions[i + 1];

                            if (nextRegion?.IsNamedRegion != true)
                            {
                                ImGui.SameLine();
                            }
                        }
                        else
                        {
                            col = 0;
                        }
                    }
                }

                ImGui.EndChildFrame();

                if (newColumnWidth != null && regions.Length > 0 && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGui.SetColumnOffset(1, Math.Max(ImGui.GetWindowContentRegionWidth() - newColumnWidth.Value, 2));
                    _lastColumnWidth = newColumnWidth.Value;
                }
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

                        ImGui.Selectable("test", false, ImGuiSelectableFlags.Disabled, ImGui.GetContentRegionAvail());

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
