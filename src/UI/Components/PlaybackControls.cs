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

            if (micro != null && _info?.Micro != micro)
            {
                _info = new MicroInfo(micro);
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

            var timecodeSize = new Vector2(-1, ImGui.GetTextLineHeightWithSpacing());

            var label = micro?.Name ?? "------";
            var time = TimeSpan.Zero.ToTimeCode();
            var playing = true;
            if (MicroManager.Current != null && MicroManager.PlaybackState != PlaybackState.Stopped)
            {
                label = MicroManager.Current.Micro.Name ?? "<Unknown Micro>";
                time = MicroManager.Current.CurrentTime.ToTimeCode();
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
            if (ImGuiExt.TintButton(label, timecodeSize, new(0, 0, 0, 0)))
            {
                if (MicroManager.Current != null)
                {
                    Config.LibrarySelection = MicroManager.Current.Micro.Id;
                    PluginInterface.SavePluginConfig(Config);
                }
            }
            if (ImGuiExt.TintButton(time, timecodeSize, new(0, 0, 0, 0)))
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

            ImGui.Spacing();

            var playPauseLabel = $"{FontAwesomeIcon.Play.ToIconString()}##PlayPause";
            var playPauseTooltip = MicroManager.PlaybackState == PlaybackState.Paused ? "Resume" : "Play";

            var controlButtonColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

            if (MicroManager.PlaybackState == PlaybackState.Playing)
            {
                playPauseLabel = $"{FontAwesomeIcon.Pause.ToIconString()}##PlayPause";
                playPauseTooltip = "Pause";
            }

            var buttonSize = ImGuiHelpers.ScaledVector2(42, 42);
            const float buttonCount = 4;

            var spacer = ImGui.GetContentRegionAvail();
            spacer.X -= buttonSize.X * buttonCount;
            spacer.X -= Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize) * buttonCount * 2;
            spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;
            spacer.X /= 2.0f;
            spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;
            spacer.Y = buttonSize.Y;

            if (spacer.X > 0)
            {
                ImGui.InvisibleButton("controls-before", spacer);
                ImGui.SameLine();
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, buttonSize.X * 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(FontAwesomeIcon.FastBackward.ToIconString(), buttonSize, controlButtonColor))
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
            if (ImGuiExt.TintButton(FontAwesomeIcon.FastForward.ToIconString(), buttonSize, controlButtonColor))
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
            ImGui.PopStyleVar();

            ImGui.EndChildFrame();

            return true;
        }
    }
}
