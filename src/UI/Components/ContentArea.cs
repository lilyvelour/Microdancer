using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class ContentArea : PluginUiBase, IDrawable
    {
        private readonly Breadcrumb _breadcrumb;
        private readonly DisplayNode _node;
        private readonly FileContents _fileContents;
        private MicroInfo? _info;

        public ContentArea()
        {
            _breadcrumb = new Breadcrumb();
            _node = new DisplayNode("content-area");
            _fileContents = new FileContents();
        }

        public void Draw()
        {
            INode? node = null;

            if (Config.LibrarySelection != Guid.Empty)
            {
                node = Library.Find<INode>(Config.LibrarySelection);
            }

            ImGui.BeginChildFrame(
                2,
                new(-1, ImGui.GetContentRegionAvail().Y - (216 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            _breadcrumb.Draw(node);

            ImGui.Separator();

            if (node is Micro micro)
            {
                var lines = micro.GetBody().ToArray();

                var inCombat = Condition[ConditionFlag.InCombat];

                if (_info?.Micro != micro)
                {
                    _info = new MicroInfo(micro);
                }

                var regions = lines.Where(l => l.Trim().StartsWith("#region ")).Select(l => l.Trim()[8..]).ToArray();
                var regionButtonSize = Vector2.Zero;

                ImGui.Columns(2);

                _fileContents.Draw(lines);

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

                if (regions.Length == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.TextWrapped(
                        "Add a region to your file (using #region [name] and #endregion) to have it show up here.\n\nRegions starting with \":\" will show up as special buttons that are not part of the timeline."
                    );
                    ImGui.PopStyleColor();
                }
                else
                {
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
                    for (int i = 0; i < regions.Length; i++)
                    {
                        col++;

                        var region = regions[i];
                        var isNamedRegion = false;
                        var size = regionButtonSize;

                        if (region.StartsWith(":"))
                        {
                            size.X =
                                ImGui.GetContentRegionAvail().X - Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).X;
                            isNamedRegion = true;
                        }

                        var currentRegion = MicroManager.Current?.CurrentCommand?.Region;

                        if (
                            !isNamedRegion
                            && MicroManager.Current?.Micro == micro
                            && MicroManager.PlaybackState != PlaybackState.Stopped
                            && currentRegion?.Name == region
                        )
                        {
                            ImGui.ProgressBar(currentRegion.GetProgress(), size, $"{regionNumber++}");
                        }
                        else if (ImGui.Button(isNamedRegion ? region[1..] : $"{regionNumber++}", size))
                        {
                            if (!inCombat)
                            {
                                MicroManager.StartMicro(micro, region);
                            }
                        }

                        ImGuiExt.TextTooltip(isNamedRegion ? region[1..] : region);

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGuiExt.IconButton(FontAwesomeIcon.Copy, $"Copy run command for {region}"))
                            {
                                ImGui.SetClipboardText($"/runmicro {micro.Id} \"{region}\"");
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

                            if (nextRegion?.StartsWith(":") != true)
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
            }
            else
            {
                ImGui.Separator();
                ImGui.Separator();

                var nodes = node?.Children ?? Library.GetNodes().ToList();

                if (nodes.Count > 0)
                {
                    foreach (var child in nodes)
                    {
                        _node.Draw(child);
                    }
                }
                else
                {
                    ImGui.Text("This folder is lonely...let's get started!");
                }

                var basePath = (node as Folder)?.Path ?? Config.LibraryPath;

                ImGui.Separator();

                if (ImGui.Button("Create new Micro"))
                {
                    Directory.CreateDirectory(basePath);
                    File.CreateText(IOUtility.MakeUniqueFile(basePath, "New Micro ({0}).micro", "New Micro.micro"));
                    Library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (ImGui.Button("Create new Folder"))
                {
                    Directory.CreateDirectory(IOUtility.MakeUniqueDir(basePath, "New Folder ({0})", "New Folder"));
                    Library.MarkAsDirty();
                }
            }

            ImGui.EndChildFrame();
        }
    }
}
