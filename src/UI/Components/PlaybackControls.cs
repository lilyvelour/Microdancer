using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class PlaybackControls : PluginUiBase, IDrawable
    {
        private MicroInfo? _info;

        public void Draw()
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

            ImGui.BeginChildFrame(3, new(-1, -1), ImGuiWindowFlags.NoBackground);

            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 6.0f));

            ImGuiExt.TintButton(
                string.Empty,
                new(
                    ImGui.GetContentRegionAvail().X
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.WindowPadding).X * 2.0f
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).X,
                    1
                ),
                new(0, 0, 0, 1)
            );

            ImGui.PopStyleVar(2);

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

            ImGui.Separator();

            var playPauseLabel = $"{FontAwesomeIcon.Play.ToIconString()}##PlayPause";
            var playPauseTooltip = MicroManager.PlaybackState == PlaybackState.Paused ? "Resume" : "Play";

            var controlButtonColor = Theme.GetColor(ImGuiCol.FrameBg);
            var playPauseColor =
                MicroManager.PlaybackState == PlaybackState.Playing
                    ? new Vector4(0.0f, 0.44705883f, 0.0f, 1.0f)
                    : controlButtonColor;
            var stopButtonColor = controlButtonColor;

            if (MicroManager.PlaybackState == PlaybackState.Playing)
            {
                playPauseLabel = $"{FontAwesomeIcon.Pause.ToIconString()}##PlayPause";
                playPauseTooltip = "Pause";
                playPauseColor = controlButtonColor;
            }

            var buttonSize = ImGuiHelpers.ScaledVector2(48, 36);

            var spacer = ImGui.GetContentRegionAvail();
            spacer.X -= buttonSize.X * 2.0f;
            spacer.X -= Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize) * 4.0f;
            spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;
            spacer.X /= 2.0f;
            spacer.X -= Theme.GetStyle<Vector2>(ImGuiStyleVar.ItemSpacing).X;
            spacer.Y = buttonSize.Y;

            if (spacer.X > 0)
            {
                ImGui.InvisibleButton("controls-before", spacer);
                ImGui.SameLine();
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0.0f);

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGuiExt.TintButton(playPauseLabel, buttonSize, playPauseColor))
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
