using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class PlaybackControls : PluginUiBase, IDrawable
    {
        private MicroInfo? _info;

        public bool Draw()
        {
            Micro? micro = null;

            if (Config.LibrarySelection != Guid.Empty)
            {
                micro = Library.Find<Micro>(Config.LibrarySelection);
            }

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

            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.BeginChildFrame(3, new(-1, -1));
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

            var controlButtonColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

            if (MicroManager.PlaybackState == PlaybackState.Playing)
            {
                playPauseLabel = $"{FontAwesomeIcon.Pause.ToIconString()}##PlayPause";
                playPauseTooltip = "Pause";
            }

            var buttonSize = ImGuiHelpers.ScaledVector2(38, 38);
            const float buttonCount = 4;

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

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(FontAwesomeIcon.FastBackward.ToIconString(), buttonSize, Vector4.Zero))
            {
                var current = MicroManager.Current;
                if (current != null && _info != null && current.Commands.Length > 0)
                {
                    var command = current.CurrentCommand ?? current.Commands.FirstOrDefault();
                    if (command != null)
                    {
                        var lineNumber = Math.Max(
                            _info.Commands
                                .LastOrDefault(
                                    c =>
                                        c.LineNumber < command.LineNumber && c.WaitTime > TimeSpan.FromMilliseconds(100)
                                )
                                ?.LineNumber ?? 0,
                            0
                        );
                        MicroManager.StartMicro(current.Micro, lineNumber);
                    }
                    else
                    {
                        MicroManager.StartMicro(current.Micro);
                    }
                }
                else if (micro != null)
                {
                    MicroManager.StartMicro(micro);
                }
            }
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Previous Line");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(playPauseLabel, buttonSize, controlButtonColor))
            {
                if (MicroManager.PlaybackState == PlaybackState.Playing)
                {
                    MicroManager.Current?.Pause();
                }
                else if (MicroManager.PlaybackState == PlaybackState.Paused)
                {
                    MicroManager.Current?.Resume();
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
                MicroManager.Current?.Stop();
            }
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Stop");

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(FontAwesomeIcon.FastForward.ToIconString(), buttonSize, Vector4.Zero))
            {
                var current = MicroManager.Current;
                if (current != null)
                {
                    var command = current.CurrentCommand ?? current.Commands.FirstOrDefault();
                    if (command != null)
                    {
                        MicroManager.StartMicro(
                            current.Micro,
                            Math.Clamp(command.LineNumber + 1, 0, current.Commands.Last().LineNumber)
                        );
                    }
                    else
                    {
                        MicroManager.StartMicro(current.Micro);
                    }
                }
                else if (micro != null)
                {
                    MicroManager.StartMicro(micro);
                }
            }
            ImGui.PopFont();

            ImGuiExt.TextTooltip("Next Line");

            if (spacer.X > 0)
            {
                ImGui.SameLine();
                ImGui.InvisibleButton("controls-after", spacer);
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }

        private void DrawLabel(Micro? micro)
        {
            var labelSize = new Vector2(-1, ImGuiHelpers.GetButtonSize(string.Empty).Y);

            var label = micro?.Name ?? "------";
            var playing = true;
            if (MicroManager.Current != null && MicroManager.PlaybackState != PlaybackState.Stopped)
            {
                label = MicroManager.Current.Micro.Name ?? "<Unknown Micro>";
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
                if (MicroManager.Current != null)
                {
                    Config.LibrarySelection = MicroManager.Current.Micro.Id;
                    PluginInterface.SavePluginConfig(Config);
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
            if (MicroManager.Current != null && MicroManager.PlaybackState != PlaybackState.Stopped)
            {
                time = MicroManager.Current.CurrentTime.ToTimeCode();
                duration = MicroManager.Current.WaitTime.ToTimeCode();
                progress = MicroManager.Current.GetProgress();
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
