using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class PlaybackControls : PluginUiBase, IDrawable<INode?>
    {
        private MicroInfo? _info;
        private MicroInfo? _current;

        public bool Draw(INode? node)
        {
            var micro = node as Micro;
            _current = MicroManager.Current;

            if (micro != null)
            {
                if (_current?.Micro == micro)
                {
                    _info = _current;
                }
                else if (_info?.Micro != micro || _info.CurrentTime > TimeSpan.Zero)
                {
                    _info = new MicroInfo(micro);
                }
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.BeginChildFrame(3, new(-1, ImGui.GetContentRegionAvail().Y));
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0, 0, 0, 1));
            ImGui.Separator();
            ImGui.PopStyleColor();

            DrawLabel(micro);

            ImGui.Spacing();

            DrawButtons(micro);

            DrawTimecode();

            ImGui.EndChildFrame();

            return true;
        }

        private void DrawButtons(Micro? micro)
        {
            var playPauseLabel = $"{FontAwesomeIcon.Play.ToIconString()}##PlayPause";
            var playPauseTooltip = MicroManager.PlaybackState == PlaybackState.Paused ? "Resume" : "Play";
            var highlightColor = Vector4.One;
            var dimColor = new Vector4(1, 1, 1, 0.25f);

            var controlButtonColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

            if (MicroManager.PlaybackState == PlaybackState.Playing)
            {
                playPauseLabel = $"{FontAwesomeIcon.Pause.ToIconString()}##PlayPause";
                playPauseTooltip = "Pause";
            }

            var buttonSize = ImGuiHelpers.ScaledVector2(38, 38);
            const float buttonCount = 6;

            var spacer = ImGui.GetContentRegionAvail();
            spacer.X -= buttonSize.X * buttonCount;
            spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X * (buttonCount + 1);
            spacer.X /= 2.0f;
            spacer.Y = buttonSize.Y;

            if (spacer.X > 0)
            {
                ImGui.InvisibleButton("controls-before", spacer);
                ImGui.SameLine();
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, buttonSize.X * 0.5f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
            ImGuiExt.PushDisableButtonBg();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Config.IgnoreAutoCountdown ? dimColor : highlightColor);
            if (ImGuiExt.IconButton(FontAwesomeIcon.Clock, buttonSize))
            {
                Config.IgnoreAutoCountdown ^= true;
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            ImGuiExt.TextTooltip($"{(Config.IgnoreAutoCountdown ? "Allow" : "Ignore")} Automatic Countdowns");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushButtonRepeat(true);
            if (ImGuiExt.IconButton(FontAwesomeIcon.FastBackward, buttonSize))
            {
                if (_current?.IsPlaying == true && _current.Commands.Length > 0)
                {
                    var command = _current.CurrentCommand ?? _current.Commands[0];
                    if (command != null)
                    {
                        var resolution = TimeSpan.FromMilliseconds(100);
                        var previousCommand = _current.AllCommands.LastOrDefault(
                            c => c.LineNumber < command.LineNumber && c.WaitTime > resolution // HACK
                        );
                        var lineNumber = previousCommand?.LineNumber ?? command.LineNumber;
                        var minimumWaitTime = _current.CurrentCommand?.WaitTime ?? resolution;

                        if (!_current.IsPlaying || _current.CurrentTime > minimumWaitTime)
                        {
                            MicroManager.StartMicro(_current.Micro, lineNumber);
                        }
                    }
                }
            }
            ImGui.PopButtonRepeat();
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Previous Line");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(playPauseLabel, buttonSize, controlButtonColor))
            {
                if (MicroManager.PlaybackState == PlaybackState.Playing)
                {
                    _current?.Pause();
                }
                else if (MicroManager.PlaybackState == PlaybackState.Paused)
                {
                    _current?.Resume();
                }
                else if (micro != null)
                {
                    MicroManager.StartMicro(micro);
                }
            }
            ImGui.PopFont();

            ImGuiExt.TextTooltip(playPauseTooltip);

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(FontAwesomeIcon.Stop.ToIconString(), buttonSize, controlButtonColor))
            {
                _current?.Stop();
            }
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Stop");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushButtonRepeat(true);
            if (ImGuiExt.IconButton(FontAwesomeIcon.FastForward, buttonSize))
            {
                if (_current?.IsPlaying == true)
                {
                    var command = _current.CurrentCommand;
                    if (command != null)
                    {
                        var resolution = TimeSpan.FromMilliseconds(100);
                        var nextCommand = _current.AllCommands.FirstOrDefault(c => c.LineNumber > command.LineNumber);
                        var lastCommand = _current.AllCommands.LastOrDefault();

                        if (nextCommand != null && (nextCommand != lastCommand || lastCommand.WaitTime > resolution))
                        {
                            MicroManager.StartMicro(_current.Micro, nextCommand.LineNumber);
                        }
                    }
                }
                else if (micro != null)
                {
                    MicroManager.StartMicro(micro);
                }
            }
            ImGui.PopButtonRepeat();
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Next Line");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Config.IgnoreLooping ? dimColor : highlightColor);
            if (ImGuiExt.IconButton((FontAwesomeIcon)0xf01e, buttonSize))
            {
                Config.IgnoreLooping ^= true;
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            ImGuiExt.TextTooltip($"{(Config.IgnoreLooping ? "Allow" : "Ignore")} Looping");

            if (spacer.X > 0)
            {
                ImGui.SameLine();
                ImGui.InvisibleButton("controls-after", spacer);
            }

            ImGuiExt.PopDisableButtonBg();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }

        private void DrawLabel(Micro? micro)
        {
            var labelSize = new Vector2(-1, ImGuiHelpers.GetButtonSize(string.Empty).Y);

            var label = micro?.Name ?? "------";
            var playing = true;
            if (_current != null && MicroManager.PlaybackState != PlaybackState.Stopped)
            {
                label = _current.Micro.Name ?? "<Unknown Micro>";
            }
            else
            {
                playing = false;
            }

            if (!playing)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Theme.GetColor(ImGuiCol.TextDisabled));
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
            if (ImGuiExt.TintButton(label, labelSize, new(0, 0, 0, 0)))
            {
                if (_current != null)
                {
                    Select(_current.Micro);
                }
            }
            ImGui.PopStyleVar(2);

            if (!playing)
            {
                ImGui.PopStyleColor();
            }
        }

        private void DrawTimecode()
        {
            var progressSize = new Vector2(-100, ImGui.GetTextLineHeightWithSpacing());

            var time = TimeSpan.Zero.ToTimeCode();
            var duration = time;
            var progress = 0.0f;
            var playing = true;
            if (_current != null && MicroManager.PlaybackState != PlaybackState.Stopped)
            {
                time = _current.CurrentTime.ToTimeCode();
                duration = _current.WaitTime.ToTimeCode();
                progress = _current.GetProgress();
            }
            else
            {
                playing = false;
            }

            if (!playing)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Theme.GetColor(ImGuiCol.TextDisabled));
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 100);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            ImGuiExt.TintButton(time, new(100, 0), new(0, 0, 0, 0));

            ImGui.SameLine();

            ImGui.BeginChildFrame(8439, progressSize, ImGuiWindowFlags.NoBackground);
            var barHeight = 4 * ImGuiHelpers.GlobalScale;
            var spacerSize = new Vector2(-1, Math.Max((progressSize.Y * 0.5f) - (barHeight * 0.5f), 1.0f));

            ImGuiHelpers.ScaledDummy(spacerSize);
            ImGui.ProgressBar(progress, new Vector2(-1, barHeight), string.Empty);
            ImGuiHelpers.ScaledDummy(spacerSize);

            ImGui.EndChildFrame();

            ImGui.SameLine();

            ImGuiExt.TintButton(duration, new(100, 0), new(0, 0, 0, 0));

            ImGui.PopStyleVar(3);

            if (!playing)
            {
                ImGui.PopStyleColor();
            }
        }
    }
}
