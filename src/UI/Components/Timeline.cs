using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Microdancer
{
    public class Timeline : PluginUiBase, IDrawable<INode?>
    {
        private MicroInfo? _currentInfo;
        private MicroInfo? _info;
        private float _lastScrollPosition = -1;
        private bool _resetScroll;

        public bool Draw(INode? node)
        {
            var micro = node as Micro;

            if (micro == null)
            {
                return false;
            }

            _currentInfo = MicroManager.Current;
            if (_currentInfo?.Micro.Id == micro.Id)
            {
                _info = _currentInfo;
                micro = _info.Micro;
            }
            else if (_info?.Micro.Id != micro.Id || _info.CurrentTime > TimeSpan.Zero)
            {
                _info = new MicroInfo(micro);
                _resetScroll = _lastScrollPosition >= 0;
            }

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4 * ImGuiHelpers.GlobalScale);

            var frameSize = ImGui.GetContentRegionAvail();
            frameSize.X -= 9 * ImGuiHelpers.GlobalScale;
            frameSize.Y = ImGuiHelpers.GetButtonSize(string.Empty).Y * 4.0f;

            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Theme.GetColor(ImGuiCol.ScrollbarGrab));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, Theme.GetColor(ImGuiCol.ScrollbarGrab) * 2);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, Theme.GetColor(ImGuiCol.ScrollbarGrabActive) * 2);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, Theme.GetColor(ImGuiCol.ScrollbarGrabHovered) * 2);

            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 20.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 2.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0.0f, 0.0f));

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
            ImGui.BeginChildFrame((uint)_info.Id.GetHashCode(), frameSize, ImGuiWindowFlags.AlwaysHorizontalScrollbar);
            ImGui.PopStyleVar();

            if (_resetScroll)
            {
                ImGui.SetScrollX(_lastScrollPosition);
                _resetScroll = false;
            }

            ImGui.PopStyleColor(4);

            ImGui.PushStyleColor(ImGuiCol.Border, Theme.GetColor(ImGuiCol.ScrollbarGrab));

            ImGui.SetWindowFontScale(0.85f);
            var timecodeWidth = ImGui.CalcTextSize("##:##:##:###").X * 1.25f * ImGuiHelpers.GlobalScale;
            ImGui.SetWindowFontScale(1.0f);

            var duration = Math.Max((float)_info.TotalTime.TotalSeconds, 5.0f);
            if (!Config.TimelineZoomFactor.TryGetValue(_info.Id, out var increment))
            {
                Config.TimelineZoomFactor[_info.Id] = increment = 50.0f;
            }

            var usableWidth = duration / Math.Max(increment, float.Epsilon) * timecodeWidth * 1.5f;
            var usableHeight = frameSize.Y - Theme.GetStyle<float>(ImGuiStyleVar.ScrollbarSize);

            var regionsSize = new Vector2(usableWidth, usableHeight * 0.33f);
            var commandsSize = new Vector2(usableWidth, usableHeight * 0.33f);

            var rulerSize = new Vector2(
                Math.Clamp((float)increment / duration, 0.0f, 1.0f) * usableWidth,
                usableHeight * 0.33f
            );
            var spacingSize = new Vector2(rulerSize.X - timecodeWidth, rulerSize.Y);

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

            ImGui.SetWindowFontScale(0.85f);
            for (var time = 0.0f; time < duration; time += increment)
            {
                if (time >= increment)
                {
                    ImGui.SameLine();
                }

                ImGuiExt.TintButton(
                    $"##timelineCursor_{time}",
                    new Vector2(1.0f, rulerSize.Y),
                    new(0.0f, 0.0f, 0.0f, 1.0f)
                );

                ImGui.SameLine();

                ImGui.InvisibleButton($"ruler_line_{time}", new(4.0f, rulerSize.Y));

                ImGui.SameLine();

                ImGui.Selectable(
                    TimeSpan.FromSeconds(time).ToTimeCode(),
                    false,
                    ImGuiSelectableFlags.Disabled,
                    new Vector2(timecodeWidth - 5.0f, rulerSize.Y)
                );

                if (spacingSize.X > 0)
                {
                    ImGui.SameLine();
                    ImGui.InvisibleButton($"ruler_{time}", spacingSize);
                }
            }
            ImGui.SetWindowFontScale(1.0f);

            var separatorWidth = Math.Max(usableWidth + timecodeWidth * 2.0f, frameSize.X);

            ImGuiExt.TintButton("##blocks-separator-0", new(separatorWidth, 1.0f), new(0.0f, 0.0f, 0.0f, 1.0f));

            if (_info.AllCommands.Length > 0)
            {
                var commandProgress = _currentInfo?.CurrentCommand?.GetProgress() ?? 0.0f;
                var regionProgress = _currentInfo?.CurrentCommand?.Region.GetProgress() ?? 0.0f;

                DrawBlocks(micro, _info, _info.AllRegions, regionProgress, regionsSize);

                ImGuiExt.TintButton("##blocks-separator-1", new(separatorWidth, 1.0f), new(0.25f, 0.25f, 0.25f, 0.5f));

                DrawBlocks(micro, _info, _info.AllCommands, commandProgress, commandsSize);
            }
            else
            {
                ImGuiExt.TintButton(" - Empty - ", new(-1, -1), Vector4.Zero);
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(7);

            ImGui.EndChildFrame();

            ImGui.PushButtonRepeat(true);

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4 * ImGuiHelpers.GlobalScale);

            var zoomFactor = increment;

            if (ImGuiExt.IconButton((FontAwesomeIcon)0xf010, "Zoom Out"))
            {
                zoomFactor += 0.1f;
            }

            ImGui.SameLine();

            if (ImGuiExt.IconButton((FontAwesomeIcon)0xf00e, "Zoom In"))
            {
                zoomFactor -= 0.1f;
            }

            ImGui.PopStyleVar();

            ImGui.PopButtonRepeat();

            Config.TimelineZoomFactor[_info.Id] = Math.Max(
                MathExt.Snap(Math.Min(zoomFactor, duration * 0.1f), 0.05f),
                0.25f
            );

            return true;
        }

        private void DrawBlocks(Micro micro, MicroInfo info, MicroInfoBase[] items, float progress, Vector2 size)
        {
            var scrollX = ImGui.GetScrollX();
            var startMousePos = ImGui.GetCursorScreenPos().X;
            var endMousePos = startMousePos + size.X;
            var hasPlaybackCursor = false;
            var isCurrent = _currentInfo?.Micro == micro;
            var mousePos = ImGui.GetMousePos().X;
            var t = MathExt.InvLerp(startMousePos, endMousePos, mousePos);
            var timecode = MathExt.Lerp(TimeSpan.Zero, info.TotalTime, t).ToTimeCode();

            var firstDraw = false;
            for (var i = 0; i < items.Length; ++i)
            {
                var item = items[i];

                if (item.WaitTime == TimeSpan.Zero)
                    continue;

                if (firstDraw)
                {
                    ImGui.SameLine();
                }
                firstDraw = true;

                var f = (float)item.WaitTime.TotalSeconds / Math.Max((float)info.TotalTime.TotalSeconds, float.Epsilon);
                var barSize = new Vector2(f * size.X, size.Y);

                var label = string.Empty;
                var lineNumber = 0;
                var barColor = Vector4.Zero;

                if (item is MicroRegion region)
                {
                    label = region.IsDefaultRegion ? "(No Region)" : region.Name;
                    lineNumber = region.Commands[0].LineNumber;
                    hasPlaybackCursor =
                        isCurrent && region.Commands.Any(c => c.LineNumber == _currentInfo?.CurrentCommand?.LineNumber);
                    barColor = ImGuiExt.RandomColor(label.GetHashCode());

                    if (info.CurrentTime > TimeSpan.Zero)
                    {
                        var dim = true;
                        foreach (var c in info.Commands)
                        {
                            if (c.Text == "/loop" && !Config.IgnoreLooping)
                            {
                                break;
                            }
                            if (c.Region.CommandStartLineNumber == region.CommandStartLineNumber)
                            {
                                dim = false;
                                break;
                            }
                        }

                        if (dim)
                        {
                            barColor *= 0.5f;
                        }
                    }

                    if (
                        info.CurrentTime > TimeSpan.Zero
                        && info.Regions.All(r => r.CommandStartLineNumber != region.CommandStartLineNumber)
                    )
                    {
                        barColor *= 0.5f;
                    }
                }
                else if (item is MicroCommand command)
                {
                    label = command.Text.Replace(" motion", string.Empty);
                    lineNumber = command.LineNumber;
                    hasPlaybackCursor = isCurrent && _currentInfo?.CurrentCommand?.LineNumber == lineNumber;
                    barColor = ImGuiExt.RandomColor(
                        command.Text.Replace("\"", string.Empty).Replace(" motion", string.Empty).GetHashCode()
                    );

                    if (info.CurrentTime > TimeSpan.Zero)
                    {
                        var dim = true;
                        foreach (var c in info.Commands)
                        {
                            if (c.Text == "/loop" && !Config.IgnoreLooping)
                            {
                                break;
                            }
                            if (c.LineNumber == command.LineNumber)
                            {
                                dim = false;
                                break;
                            }
                        }

                        if (dim)
                        {
                            barColor *= 0.5f;
                        }
                    }
                }
                else
                {
                    hasPlaybackCursor = false;
                }

                var tooltip = $"{timecode}\n{label} <{item.WaitTime.ToSimpleString()}>";

                if (hasPlaybackCursor)
                {
                    tooltip += $"\n{item.CurrentTime.ToSimpleString()}";
                }

                var playbackCursorSize = new Vector2(2.0f, barSize.Y);

                var beforeSize = barSize;
                var afterSize = barSize;

                if (hasPlaybackCursor)
                {
                    beforeSize.X = Math.Max((barSize.X * progress) - 1.0f, 0.0f);
                    afterSize.X = Math.Max(barSize.X - beforeSize.X + 1.0f, 0.0f);
                }
                else
                {
                    beforeSize.X = 0f;
                    playbackCursorSize.X = 0f;
                }

                var cursorColor = hasPlaybackCursor ? Theme.GetColor(ImGuiCol.TextSelectedBg) : barColor;
                if (ImGuiExt.TintButton($"##{i}_before", beforeSize, barColor))
                {
                    MicroManager.StartMicro(micro, lineNumber);
                }
                ImGuiExt.TextTooltip(tooltip);

                ImGui.SameLine();
                if (ImGuiExt.TintButton($"##{i}_cursor", playbackCursorSize, cursorColor))
                {
                    MicroManager.StartMicro(micro, lineNumber);
                }
                ImGuiExt.TextTooltip(tooltip);

                if (hasPlaybackCursor && _currentInfo?.IsPlaying == true && _currentInfo?.IsPaused == false)
                {
                    ImGui.SetScrollHereX();
                }

                _lastScrollPosition = ImGui.GetScrollX();

                ImGui.SameLine();
                if (ImGuiExt.TintButton($"{label}##{i}_after", afterSize, barColor))
                {
                    MicroManager.StartMicro(micro, lineNumber);
                }
                ImGuiExt.TextTooltip(tooltip);
            }
        }
    }
}
