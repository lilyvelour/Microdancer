using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class FileContents : PluginUiBase, IDrawable<Micro>
    {
        private MicroInfo? _info;

        public bool Draw(Micro micro)
        {
            if (_info?.Micro != micro)
            {
                _info = new MicroInfo(micro);
            }

            ImGui.InvisibleButton("file-contents-spacer", new(-1, 0.0f));

            var framePadding = ImGui.GetStyle().FramePadding;
            var fileContentsSize = ImGui.GetContentRegionAvail();
            fileContentsSize.X -= framePadding.X;

            var lines = micro.GetBody().ToArray();

            if (lines.Length > 0)
            {
                var isRunning =
                    MicroManager.Current?.Micro == micro && MicroManager.PlaybackState != PlaybackState.Stopped;

                fileContentsSize.Y -= Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize) * 4.0f;
                fileContentsSize.Y = Math.Max(fileContentsSize.Y, 200 * ImGuiHelpers.GlobalScale);

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(8, 8));

                if (isRunning)
                {
                    var normal = Theme.GetColor(ImGuiCol.FrameBg);
                    var active = Theme.GetColor(ImGuiCol.FrameBgActive);
                    var hovered = Theme.GetColor(ImGuiCol.FrameBgHovered);
                    normal.X *= 1.25f;
                    active.X *= 1.25f;
                    hovered.X *= 1.25f;
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, normal);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, active);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, hovered);
                }

                ImGui.BeginChildFrame(10, fileContentsSize, ImGuiWindowFlags.HorizontalScrollbar);

                if (isRunning)
                {
                    ImGui.PopStyleColor(3);
                }

                ImGui.PopStyleVar();

                var len = lines.Length;
                var maxChars = len.ToString().Length;

                for (var i = 0; i < len; ++i)
                {
                    var lineNumber = i + 1;
                    var command = _info.AllCommands.FirstOrDefault(c => c.LineNumber == lineNumber);

                    var prefixColor = Vector4.Zero;
                    var textColor = Theme.GetColor(ImGuiCol.Text) * 0.75f;
                    textColor.W = 1.0f;
                    var currentLineNumber = MicroManager.Current?.CurrentCommand?.LineNumber ?? 0;

                    if (isRunning && currentLineNumber == lineNumber)
                    {
                        prefixColor = Theme.GetColor(ImGuiCol.TitleBgActive);
                        textColor = Theme.GetColor(ImGuiCol.Text);

                        var linePos = ImGui.GetCursorPosY();

                        if (MicroManager.PlaybackState == PlaybackState.Playing)
                        {
                            if (
                                linePos > ImGui.GetScrollY() + fileContentsSize.Y - ImGui.GetTextLineHeightWithSpacing()
                            )
                            {
                                ImGui.SetScrollHereY(0.85f);
                            }
                            else if (linePos < ImGui.GetScrollY())
                            {
                                ImGui.SetScrollHereY(0.15f);
                            }
                        }
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(8.0f, 0.0f));

                    ImGui.PushStyleColor(ImGuiCol.Text, prefixColor);
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Text(FontAwesomeIcon.CaretRight.ToIconString());
                    ImGui.PopFont();
                    ImGui.PopStyleColor();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.MonoFont);

                    ImGui.PushStyleColor(ImGuiCol.Text, textColor * 0.75f);
                    ImGui.Text($"{lineNumber.ToString().PadLeft(maxChars)}");
                    ImGui.PopStyleColor();

                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                    if (ImGui.Selectable($"{lines[i]}##{i}", currentLineNumber == lineNumber))
                    {
                        MicroManager.StartMicro(micro, i + 1);
                    }
                    ImGui.PopStyleColor();

                    ImGui.PopFont();

                    ImGui.PopStyleVar();

                    if (command != null)
                    {
                        var startTimeInRegionMs = command.Region.Commands
                            .Where(c => c.LineNumber < command.LineNumber)
                            .Sum(c => (int)c.WaitTime.TotalMilliseconds);
                        var startTimeInRegion = TimeSpan.FromMilliseconds(startTimeInRegionMs);

                        var tooltip =
                            $"{command.Text.Replace(" motion", string.Empty)} <{command.WaitTime.ToSimpleString()}>";

                        if (!command.Region.IsDefaultRegion)
                        {
                            tooltip += $"\n- In {command.Region.Name}: {startTimeInRegion.ToSecondsString()}";
                        }

                        if (!command.Region.IsNamedRegion)
                        {
                            var startTimeInMicroMs = _info.Commands
                                .Where(c => c.LineNumber < command.LineNumber)
                                .Sum(c => (int)c.WaitTime.TotalMilliseconds);
                            var startTimeInMicro = TimeSpan.FromMilliseconds(startTimeInMicroMs);

                            tooltip += $"\n- In {_info.Micro.Name}: {startTimeInMicro.ToSecondsString()}";
                        }

                        ImGuiExt.TextTooltip(tooltip);
                    }
                    else
                    {
                        var region = _info.AllRegions.FirstOrDefault(
                            r => r.StartLineNumber == lineNumber || r.EndLineNumber == lineNumber
                        );

                        if (region != null)
                        {
                            var tooltip = $"{region.Name} <{region.WaitTime.ToSimpleString()}>";

                            if (!region.IsNamedRegion)
                            {
                                var startTimeInMicroMs = _info.Commands
                                    .Where(c => c.LineNumber < lineNumber)
                                    .Sum(c => (int)c.WaitTime.TotalMilliseconds);
                                var startTimeInMicro = TimeSpan.FromMilliseconds(startTimeInMicroMs);

                                tooltip += $"\n- In {_info.Micro.Name}: {startTimeInMicro.ToSecondsString()}";
                            }

                            ImGuiExt.TextTooltip(tooltip);
                        }
                    }
                }
                ImGui.EndChildFrame();
            }

            return true;
        }
    }
}
