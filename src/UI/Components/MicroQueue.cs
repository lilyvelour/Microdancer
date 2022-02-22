using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class MicroQueue : PluginUiBase, IDrawable
    {
        public void Draw()
        {
            ImGui.Text("Micro Queue");

            if (Condition[ConditionFlag.InCombat])
            {
                ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "All Micros paused while in combat!");
                return;
            }

            ImGui.PushItemWidth(-1f);
            var runningMicrosSize = ImGui.GetContentRegionAvail();
            runningMicrosSize.Y -= ImGuiHelpers.GetButtonSize(" ").Y + (ImGui.GetStyle().WindowPadding.Y * 2);
            runningMicrosSize.Y = Math.Max(runningMicrosSize.Y, 1);

            if (ImGui.BeginListBox("##running-micros", runningMicrosSize))
            {
                var queue = MicroManager.GetQueue();

                if (!queue.Any())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.Text("- None -");
                    ImGui.PopStyleColor();
                }
                else
                {
                    foreach (var microInfo in queue)
                    {
                        var micro = microInfo.Micro;
                        var name = micro.Name;
                        var currentRegion = microInfo.CurrentCommand?.Region;

                        if (currentRegion != null)
                        {
                            name += $" [{currentRegion.Name}]";
                        }

                        var flags = ImGuiSelectableFlags.None;
                        if (MicroManager.IsCancelled(microInfo))
                        {
                            flags = ImGuiSelectableFlags.Disabled;
                            name = $"{name} (Cancelled)";
                        }
                        else if (MicroManager.IsPaused(microInfo))
                        {
                            name = $"{name} (Paused)";
                        }

                        name = $"{name}##{microInfo.Id}";

                        if (
                            ImGui.Selectable(
                                name,
                                Config.QueueSelection == microInfo.Id,
                                flags,
                                ImGui.CalcTextSize(name)
                            )
                        )
                        {
                            Config.QueueSelection = Config.QueueSelection == Guid.Empty ? microInfo.Id : Guid.Empty;
                            PluginInterface.SavePluginConfig(Config);
                        }

                        var currentCommand = microInfo.CurrentCommand;
                        if (currentCommand != null)
                        {
                            var remainingTime = currentCommand.GetRemainingTime();

                            ImGui.Selectable(
                                $"\t{currentCommand.Text ?? string.Empty}",
                                false,
                                ImGuiSelectableFlags.Disabled
                            );

                            ImGui.SameLine();

                            var changeBarColor = remainingTime <= TimeSpan.FromMilliseconds(500);
                            if (changeBarColor)
                            {
                                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.GetColorU32(ImGuiCol.TitleBgActive));
                            }

                            ImGui.PushItemWidth(-1f);

                            ImGui.ProgressBar(
                                currentCommand.GetProgress(),
                                new(0, ImGui.GetTextLineHeightWithSpacing()),
                                remainingTime?.ToString(@"s\.fff\s") ?? string.Empty
                            );

                            ImGui.PopItemWidth();

                            if (changeBarColor)
                            {
                                ImGui.PopStyleColor();
                            }
                        }
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.PopItemWidth();

            var hasSelected = MicroManager.IsRunning(Config.QueueSelection);

            if (
                ImGuiExt.TintButton(
                    hasSelected ? "Cancel Selected" : "Cancel All",
                    new Vector4(0.44705883f, 0.0f, 0.033333335f, 1.0f)
                )
            )
            {
                if (hasSelected)
                {
                    MicroManager.Cancel(Config.QueueSelection);
                }
                else
                {
                    MicroManager.CancelAll();
                }
            }

            if (hasSelected)
            {
                ImGui.SameLine();

                var paused = MicroManager.IsPaused(Config.QueueSelection);
                if (ImGui.Button(paused ? "Resume" : "Pause"))
                {
                    if (paused)
                    {
                        MicroManager.Resume(Config.QueueSelection);
                    }
                    else
                    {
                        MicroManager.Pause(Config.QueueSelection);
                    }
                }
            }
        }
    }
}
