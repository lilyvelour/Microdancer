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
                        - Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).Y
                        - Theme.GetStyle<float>(ImGuiStyleVar.FrameBorderSize)
                )
            );

            var hasResults = false;

            foreach (var node in Library.GetNodes())
            {
                if (!string.IsNullOrWhiteSpace(_search) && node is StarredFolderRoot)
                {
                    continue;
                }

                hasResults |= _node.Draw(node, _search);
            }

            if (!string.IsNullOrWhiteSpace(_search) && !hasResults)
            {
                ImGui.TextDisabled("No results found.");
            }

            ImGui.EndChildFrame();

            if (ImGui.IsItemClicked())
            {
                DeselectAll();
            }

            return true;
        }
    }
}
