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
                new(-1, ImGui.GetContentRegionAvail().Y - (120 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            _breadcrumb.Draw(node);

            if (node is Micro micro)
            {
                var lines = micro.GetBody().ToArray();

                var inCombat = Condition[ConditionFlag.InCombat];

                if (_info?.Micro != micro)
                {
                    _info = new MicroInfo(micro);
                }

                if (inCombat)
                {
                    ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "All Micros paused while in combat!");
                }
                else
                {
                    DrawPlayPause(micro, lines, _info);

                    ImGui.Separator();
                }

                var regions = lines.Where(l => l.Trim().StartsWith("#region ")).Select(l => l.Trim()[8..]).ToArray();
                var regionButtonSize = Vector2.Zero;

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

                    regionButtonSize.X += 40 * ImGuiHelpers.GlobalScale;
                    regionButtonSize.Y += 20 * ImGuiHelpers.GlobalScale;

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
                            size.X = -1;
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

                _fileContents.Draw(lines);
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

        private void DrawPlayPause(Micro micro, string[] lines, MicroInfo displayInfo)
        {
            if (lines.Length > 0 && displayInfo.Commands.Length > 0)
            {
                var controlButtonColor = new Vector4(0.44705883f, 0.44705883f, 0.44705883f, 1.0f);
                var stopButtonColor = new Vector4(0.44705883f, 0.0f, 0.0f, 1.0f);
                var playPauseLabel = $"{FontAwesomeIcon.Play.ToIconString()}##PlayPause";
                var playPauseTooltip = MicroManager.PlaybackState == PlaybackState.Paused ? "Resume" : "Play";
                var playPauseColor = new Vector4(0.0f, 0.44705883f, 0.0f, 1.0f);

                var isCurrent = micro == MicroManager.Current?.Micro;

                if (isCurrent && MicroManager.PlaybackState == PlaybackState.Playing)
                {
                    playPauseLabel = $"{FontAwesomeIcon.Pause.ToIconString()}##PlayPause";
                    playPauseTooltip = "Pause";
                    playPauseColor = controlButtonColor;
                }

                var buttonSize = ImGuiHelpers.ScaledVector2(96, 48);

                ImGui.BeginChildFrame(9008, new(-1, buttonSize.Y + (8 * ImGuiHelpers.GlobalScale)));

                var spacer = ImGui.GetContentRegionAvail();
                spacer.X -= buttonSize.X * 2.0f;
                spacer.X -= Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize) * 4.0f;
                spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;
                spacer.X /= 2.0f;
                spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;

                if (spacer.X > 0)
                {
                    ImGui.InvisibleButton("controls-before", spacer);
                    ImGui.SameLine();
                }

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0.0f);

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGuiExt.TintButton(playPauseLabel, buttonSize, playPauseColor))
                {
                    if (isCurrent && MicroManager.PlaybackState == PlaybackState.Playing)
                    {
                        MicroManager.Current?.Pause();
                    }
                    else if (isCurrent && MicroManager.PlaybackState == PlaybackState.Paused)
                    {
                        MicroManager.Current?.Resume();
                    }
                    else
                    {
                        MicroManager.StartMicro(micro);
                    }
                }
                ImGui.PopFont();

                ImGuiExt.TextTooltip(playPauseTooltip);

                ImGui.SameLine();

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGuiExt.TintButton(FontAwesomeIcon.Stop.ToIconString(), buttonSize, stopButtonColor))
                {
                    MicroManager.Current?.Stop();
                }
                ImGui.PopFont();

                ImGuiExt.TextTooltip("Stop");

                if (spacer.X > 0)
                {
                    ImGui.SameLine();
                    ImGui.InvisibleButton("controls-after", spacer);
                }

                ImGui.PopStyleVar();

                ImGui.EndChildFrame();
            }
        }
    }
}
