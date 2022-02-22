using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class ContentArea : PluginUiBase, IDrawable
    {
        private readonly Breadcrumb _breadcrumb;
        private readonly DisplayNode _node;

        public ContentArea()
        {
            _breadcrumb = new Breadcrumb();
            _node = new DisplayNode("content-area");
        }

        public void Draw()
        {
            INode? node = null;

            if (Config.LibrarySelection != Guid.Empty)
            {
                node = Library.Find<INode>(Config.LibrarySelection);
            }

            ImGui.BeginChildFrame(
                2,
                new(-1, ImGui.GetContentRegionAvail().Y - (180 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            _breadcrumb.Draw(node);

            if (node is Micro micro)
            {
                var lines = micro.GetBody().ToArray();
                var regions = lines.Where(l => l.Trim().StartsWith("#region ")).Select(l => l.Trim()[8..]).ToArray();

                var inCombat = Condition[ConditionFlag.InCombat];
                var info = MicroManager.Find(micro.Id).ToArray();

                if (inCombat)
                {
                    ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "All Micros paused while in combat!");
                }
                else
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    var label = $"{FontAwesomeIcon.Play.ToIconString()}##Play All";
                    var buttonSize = ImGuiHelpers.GetButtonSize(label);
                    buttonSize.X = -1;
                    buttonSize.Y += 20 * ImGuiHelpers.GlobalScale;

                    if (info.Length > 0)
                    {
                        var progress = info.Max(mi => mi.GetProgress());

                        ImGui.ProgressBar(progress, buttonSize, string.Empty);
                    }
                    else if (ImGuiExt.TintButton(label, buttonSize, new Vector4(0.0f, 0.44705883f, 0.033333335f, 1.0f)))
                    {
                        RunMicro(micro);
                    }
                    ImGui.PopFont();

                    ImGuiExt.TextTooltip("Play All");

                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Button("Queue All"))
                        {
                            RunMicro(micro, multi: true);
                        }

                        ImGui.EndPopup();
                    }
                }

                ImGui.Separator();
                ImGui.Separator();

                var size = Vector2.Zero;

                if (regions.Length == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.TextWrapped(
                        "Add a region to your file (using #region [name] and #endregion) to have it show up here."
                    );
                    ImGui.PopStyleColor();
                }
                else
                {
                    for (int i = 0; i < regions.Length; i++)
                    {
                        var sz = ImGui.CalcTextSize($"{i + 1}");
                        if (sz.X >= size.X)
                        {
                            size = sz;
                        }
                    }

                    size.X += 40 * ImGuiHelpers.GlobalScale;
                    size.Y += 20 * ImGuiHelpers.GlobalScale;

                    var running = info.Length > 0;

                    var col = 0;
                    var regionNumber = 1;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        col++;

                        var region = regions[i];
                        var isNamedRegion = false;
                        var buttonSize = size;

                        if (region.StartsWith(":"))
                        {
                            buttonSize.X = -1;
                            isNamedRegion = true;
                        }

                        if (!inCombat)
                        {
                            var currentRegion = Array.Find(info, mi => mi.CurrentCommand?.Region?.Name == region);

                            if (currentRegion != null)
                            {
                                ImGui.ProgressBar(currentRegion.GetProgress(), buttonSize, string.Empty);
                                regionNumber++;
                            }
                            else if (ImGui.Button(isNamedRegion ? region[1..] : $"{regionNumber++}", buttonSize))
                            {
                                RunMicro(micro, region);
                            }
                        }
                        else
                        {
                            ImGui.Selectable(
                                isNamedRegion ? region[1..] : $"{regionNumber++}",
                                false,
                                ImGuiSelectableFlags.Disabled,
                                buttonSize
                            );
                        }

                        ImGuiExt.TextTooltip(isNamedRegion ? region[1..] : region);

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGuiExt.IconButton(FontAwesomeIcon.Copy, $"Copy run command for {region}"))
                            {
                                ImGui.SetClipboardText($"/runmicro {micro.Id} \"{region}\"");
                            }

                            ImGui.EndPopup();
                        }

                        if (
                            i < regions.Length - 1
                            && !isNamedRegion
                            && ImGui.GetContentRegionAvail().X - ((buttonSize.X + ImGui.GetStyle().ItemSpacing.X) * col)
                                > buttonSize.X
                        )
                        {
                            var nextRegion = i == regions.Length - 1 ? null : regions[i + 1];

                            if (nextRegion?.StartsWith(":") != true)
                            {
                                ImGui.SameLine();
                            }
                        }
                        else
                        {
                            col = 0;
                        }
                    }
                }

                ImGui.Separator();

                var framePadding = ImGui.GetStyle().FramePadding;
                var fileContentsSize = ImGui.GetContentRegionAvail();
                fileContentsSize.X -= framePadding.X;

                if (fileContentsSize.Y > ImGui.GetTextLineHeightWithSpacing())
                {
                    ImGui.Text("File Contents");
                    fileContentsSize.Y -= ImGui.GetTextLineHeightWithSpacing();

                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(8, 8));
                    ImGui.BeginChildFrame(10, fileContentsSize, ImGuiWindowFlags.HorizontalScrollbar);
                    ImGui.PopStyleVar();

                    var len = lines.Length;
                    var maxChars = len.ToString().Length;

                    for (var i = 0; i < len; ++i)
                    {
                        Vector4 prefixColor = Vector4.Zero;
                        Vector4 textColor = Theme.GetColor(ImGuiCol.TextDisabled);
                        foreach (var mi in info)
                        {
                            if (mi.CurrentCommand?.LineNumber == i + 1)
                            {
                                prefixColor = Theme.GetColor(ImGuiCol.TitleBgActive);
                                textColor = Theme.GetColor(ImGuiCol.Text);
                                break;
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

                        ImGui.PushStyleColor(ImGuiCol.Text, textColor * 0.8f);
                        ImGui.Text($"{(i + 1).ToString().PadLeft(maxChars)}");
                        ImGui.PopStyleColor();

                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                        ImGui.Text($"{lines[i]}");
                        ImGui.PopStyleColor();

                        ImGui.PopFont();

                        ImGui.PopStyleVar();
                    }
                    ImGui.EndChildFrame();
                }
            }
            else
            {
                ImGui.Separator();
                ImGui.Separator();

                var nodes = node?.Children ?? Library.GetNodes().ToList();

                if (nodes.Count > 0)
                {
                    foreach (var child in nodes)
                    {
                        _node.Draw(child);
                    }
                }
                else
                {
                    ImGui.Text("This folder is lonely...let's get started!");
                }

                var basePath = (node as Folder)?.Path ?? Config.LibraryPath;

                ImGui.Separator();

                if (ImGui.Button("Create new Micro"))
                {
                    Directory.CreateDirectory(basePath);
                    File.CreateText(IOUtility.MakeUniqueFile(basePath, "New Micro ({0}).micro", "New Micro.micro"));
                    Library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (ImGui.Button("Create new Folder"))
                {
                    Directory.CreateDirectory(IOUtility.MakeUniqueDir(basePath, "New Folder ({0})", "New Folder"));
                    Library.MarkAsDirty();
                }
            }

            ImGui.EndChildFrame();
        }
    }
}
