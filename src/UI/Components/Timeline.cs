using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class Timeline : PluginUiBase, IDrawable
    {
        private MicroInfo? _info;

        public bool Draw()
        {
            Micro? micro = null;

            if (Config.LibrarySelection != Guid.Empty)
            {
                micro = Library.Find<Micro>(Config.LibrarySelection);
            }

            if (micro == null)
            {
                return false;
            }

            if (_info?.Micro != micro)
            {
                _info = new MicroInfo(micro);
            }

            var frameSize = ImGui.GetContentRegionAvail();
            frameSize.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).X * 2.0f;
            frameSize.X -= Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize) * 2.0f;
            frameSize.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.WindowPadding).X * 2.0f;
            frameSize.Y = ImGuiHelpers.GetButtonSize(string.Empty).Y * 4.0f;

            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Theme.GetColor(ImGuiCol.ScrollbarGrab));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, Theme.GetColor(ImGuiCol.ScrollbarGrab) * 2);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, Theme.GetColor(ImGuiCol.ScrollbarGrabActive) * 2);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, Theme.GetColor(ImGuiCol.ScrollbarGrabHovered) * 2);

            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 20.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0.0f, 0.0f));

            ImGui.BeginChildFrame(
                (uint)(_info?.Id.GetHashCode() ?? 9999),
                frameSize,
                ImGuiWindowFlags.AlwaysHorizontalScrollbar
            );

            ImGui.PopStyleColor(4);

            ImGui.PushStyleColor(ImGuiCol.Border, Theme.GetColor(ImGuiCol.ScrollbarGrab));

            var timecodeWidth = ImGui.CalcTextSize("##:##:##:###").X * 2.0f * ImGuiHelpers.GlobalScale;

            var duration = _info?.Commands.Length > 0 ? (float)_info.WaitTime.TotalSeconds : 5.0f;

            var usableWidth = Math.Max(duration * timecodeWidth * 2.0f, frameSize.X);
            var usableHeight = frameSize.Y - Theme.GetStyle<float>(ImGuiStyleVar.ScrollbarSize);

            var regionsSize = new Vector2(usableWidth, usableHeight * 0.33f);
            var commandsSize = new Vector2(usableWidth, usableHeight * 0.33f);

            const float increment = 0.5f;

            var rulerSize = new Vector2(
                Math.Clamp((float)increment / duration, 0.0f, 1.0f) * usableWidth,
                usableHeight * 0.33f
            );
            var spacingSize = new Vector2(rulerSize.X - timecodeWidth, rulerSize.Y);

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

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

            ImGuiExt.TintButton(
                "##blocks-separator-0",
                new(usableWidth + timecodeWidth, 1.0f),
                new(0.0f, 0.0f, 0.0f, 1.0f)
            );

            if (_info?.Commands.Length > 0)
            {
                var commandProgress = MicroManager.Current?.CurrentCommand?.GetProgress() ?? 0.0f;
                var regionProgress = MicroManager.Current?.CurrentCommand?.Region.GetProgress() ?? 0.0f;

                DrawBlocks(micro, _info, _info.Regions, regionProgress, regionsSize);

                ImGuiExt.TintButton(
                    "##blocks-separator-1",
                    new(usableWidth + timecodeWidth, 1.0f),
                    new(0.25f, 0.25f, 0.25f, 0.5f)
                );

                DrawBlocks(micro, _info, _info.Commands, commandProgress, commandsSize);
            }
            else
            {
                ImGuiExt.TintButton(" - Empty - ", new(-1, -1), Vector4.Zero);
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(7);

            ImGui.EndChildFrame();

            return true;
        }

        private void DrawBlocks(Micro micro, MicroInfo info, MicroInfoBase[] items, float progress, Vector2 size)
        {
            var scrollX = ImGui.GetScrollX();
            var startMousePos = ImGui.GetCursorScreenPos().X;
            var endMousePos = startMousePos + size.X;
            var hasPlaybackCursor = false;
            var isCurrent = MicroManager.Current?.Micro == micro;
            var mousePos = ImGui.GetMousePos().X;
            var t = MathExt.InvLerp(startMousePos, endMousePos, mousePos);
            var timecode = MathExt.Lerp(TimeSpan.Zero, info.WaitTime, t).ToTimeCode();

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

                var f = (float)item.WaitTime.TotalSeconds / Math.Max((float)info.WaitTime.TotalSeconds, float.Epsilon);
                var barSize = new Vector2(f * size.X, size.Y);

                var label = string.Empty;
                var lineNumber = 0;
                var barColor = Vector4.Zero;

                if (item is MicroRegion region)
                {
                    label = region.IsDefaultRegion ? "(No Region)" : region.Name;
                    lineNumber = region.Commands[0].LineNumber;
                    hasPlaybackCursor =
                        isCurrent
                        && region.Commands.Any(c => c.LineNumber == MicroManager.Current?.CurrentCommand?.LineNumber);
                    barColor = ImGuiExt.RandomColor(label.GetHashCode());
                }
                else if (item is MicroCommand command)
                {
                    label = command.Text.Replace(" motion", string.Empty);
                    lineNumber = command.LineNumber;
                    hasPlaybackCursor = isCurrent && MicroManager.Current?.CurrentCommand?.LineNumber == lineNumber;
                    barColor = ImGuiExt.RandomColor(
                        command.Text.Replace("\"", string.Empty).Replace(" motion", string.Empty).GetHashCode()
                    );
                }
                else
                {
                    hasPlaybackCursor = false;
                }

                var tooltip = $"{label} <{item.WaitTime.ToSimpleString()}> [{timecode}]";

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
                    beforeSize.X = 0.0f;
                    playbackCursorSize.X = 0.0f;
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

                if (hasPlaybackCursor && MicroManager.PlaybackState != PlaybackState.Paused)
                {
                    ImGui.SetScrollHereX();
                }

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
