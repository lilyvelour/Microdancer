using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace Microdancer
{
    public class FileContents : PluginUiBase, IDrawable<Micro>
    {
        private MicroInfo? _info;

        public bool Draw(Micro micro)
        {
            _info = new MicroInfo(micro);

            ImGui.InvisibleButton($"file-contents-spacer-{micro.Id}", new(-1, 0.0f));

            var framePadding = ImGui.GetStyle().FramePadding;
            var fileContentsSize = ImGui.GetContentRegionAvail();
            fileContentsSize.X -= framePadding.X;

            var lines = micro.GetBody().ToArray();

            if (lines.Length > 0)
            {
                var isRunning =
                    MicroManager.Current?.Micro == micro && MicroManager.PlaybackState != PlaybackState.Stopped;
                var currentLineNumber = MicroManager.Current?.CurrentCommand?.LineNumber ?? 0;

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

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
                ImGui.BeginChildFrame(10, fileContentsSize, ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.PopStyleVar();

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

                    if (command != null)
                    {
                        switch (command.Status)
                        {
                            case MicroCommand.NoteStatus.Debug:
                                textColor = new Vector4(0.0f, 0.8f, 0.8f, 1.0f);
                                break;
                            case MicroCommand.NoteStatus.Info:
                                break;
                            case MicroCommand.NoteStatus.Warning:
                                textColor = new Vector4(0.8f, 0.8f, 0.0f, 1.0f);
                                break;
                            case MicroCommand.NoteStatus.Error:
                                textColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                                break;
                        }
                    }

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
                    if (ImGui.Selectable($"{lines[i]}##{micro.Id}-{i}", isRunning && currentLineNumber == lineNumber))
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

                        var tooltip = string.Empty;

                        if (!string.IsNullOrWhiteSpace(command.Note))
                        {
                            var tooltipNoteColor = Theme.GetColor(ImGuiCol.Text);

                            switch (command.Status)
                            {
                                case MicroCommand.NoteStatus.Debug:
                                    tooltip += $"DEBUG: {command.Note}\n\n";
                                    break;
                                case MicroCommand.NoteStatus.Info:
                                    tooltip += $"INFO: {command.Note}\n\n";
                                    break;
                                case MicroCommand.NoteStatus.Warning:
                                    tooltip += $"WARNING: {command.Note}\n\n";
                                    break;
                                case MicroCommand.NoteStatus.Error:
                                    tooltip += $"ERROR: {command.Note}\n\n";
                                    break;
                            }

                        }

                        tooltip +=
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
