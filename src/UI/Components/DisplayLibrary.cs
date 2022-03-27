using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class DisplayLibrary : PluginUiBase, IDrawable
    {
        private readonly DisplayNode _node;

        private string _search = string.Empty;

        public DisplayLibrary()
        {
            _node = new DisplayNode("library");
        }

        public bool Draw()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
            ImGuiExt.TintButton(FontAwesomeIcon.Search.ToIconString(), Vector4.Zero);
            ImGui.PopStyleColor();
            ImGui.PopFont();

            ImGui.SameLine();

            ImGui.PushItemWidth(-1);
            ImGui.InputText("##search", ref _search, 1024);
            ImGui.PopItemWidth();

            ImGui.BeginChildFrame(
                1,
                new(
                    -1,
                    ImGui.GetContentRegionAvail().Y
                        - ImGuiHelpers.GetButtonSize(string.Empty).Y
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).Y
                        - Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize)
                )
            );

            var hasResults = false;

            foreach (var node in Library.GetNodes())
            {
                hasResults |= _node.Draw(node, _search);
            }

            if (!string.IsNullOrWhiteSpace(_search) && !hasResults)
            {
                ImGui.TextDisabled("No results found.");
            }

            ImGui.EndChildFrame();

            if (ImGui.IsItemClicked())
            {
                Config.LibrarySelection = Guid.Empty;
                PluginInterface.SavePluginConfig(Config);
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var buttonWidth = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Plus.ToIconString()).X;
            ImGui.PopFont();

            var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
            var buttonGroupWidth = (buttonWidth * 2) + itemSpacing;
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var columnPadding = ImGui.GetStyle().ColumnsMinSpacing * 2;

            var spacingWidth = availableWidth - columnPadding - buttonGroupWidth;

            if (spacingWidth > itemSpacing)
            {
                ImGui.Dummy(new(spacingWidth, 0));
                ImGui.SameLine();
            }

            var path = Config.LibraryPath;
            if (Directory.Exists(path))
            {
                if (Config.LibrarySelection != Guid.Empty)
                {
                    INode? node = Library.Find<INode>(Config.LibrarySelection);
                    if (node is Micro)
                    {
                        path = Path.GetDirectoryName(node.Path)!;
                    }
                    else if (node != null)
                    {
                        path = node.Path;
                    }
                }

                if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "Create new Micro"))
                {
                    Directory.CreateDirectory(path);
                    File.CreateText(IOUtility.MakeUniqueFile(path, "New Micro ({0}).micro", "New Micro.micro"));
                    Library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (ImGuiExt.IconButton(FontAwesomeIcon.FolderPlus, "Create new Folder"))
                {
                    Directory.CreateDirectory(IOUtility.MakeUniqueDir(path, "New Folder ({0})", "New Folder"));
                    Library.MarkAsDirty();
                }
            }

            return true;
        }
    }
}
