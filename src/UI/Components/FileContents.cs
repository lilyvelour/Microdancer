using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class FileContents : PluginUiBase, IDrawable<string[]>
    {
        public void Draw(string[] lines)
        {
            Micro? micro = null;

            if (Config.LibrarySelection != Guid.Empty)
            {
                micro = Library.Find<Micro>(Config.LibrarySelection);
            }

            if (micro == null)
            {
                return;
            }

            ImGui.InvisibleButton("file-contents-spacer", new(-1, 0.0f));

            var framePadding = ImGui.GetStyle().FramePadding;
            var fileContentsSize = ImGui.GetContentRegionAvail();
            fileContentsSize.X -= framePadding.X;
            fileContentsSize.Y -= ImGuiHelpers.GetButtonSize(string.Empty).Y;
            fileContentsSize.Y -= Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).Y;

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
                    var prefixColor = Vector4.Zero;
                    var textColor = Theme.GetColor(ImGuiCol.Text) * 0.75f;
                    textColor.W = 1.0f;

                    if (isRunning && MicroManager.Current?.CurrentCommand?.LineNumber == i + 1)
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
                    ImGui.Text($"{(i + 1).ToString().PadLeft(maxChars)}");
                    ImGui.PopStyleColor();

                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                    ImGui.Text($"{lines[i]}");
                    ImGui.PopStyleColor();

                    if (lines[i].StartsWith("#region "))
                    {
                        var mi = new MicroInfo(micro, lines[i][8..]);
                        var waitTime = mi.Commands.Sum(c => c.WaitTime.TotalSeconds);
                        ImGuiExt.TextTooltip($"{waitTime:#.###} sec");
                    }

                    ImGui.PopFont();

                    ImGui.PopStyleVar();
                }
                ImGui.EndChildFrame();
            }
            if (ImGui.Button("Open File"))
            {
                OpenNode(micro);
            }
        }
    }
}
