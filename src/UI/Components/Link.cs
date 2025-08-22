using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Microdancer
{
    public struct LinkData
    {
        public string Label;
        public string Tooltip;
        public string Url;
    }

    public class Link : PluginUiBase, IDrawable<LinkData>
    {
        public bool Draw(LinkData link)
        {
            ImGui.TextUnformatted(link.Label);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                using var tooltip = ImRaii.Tooltip();
                if (tooltip.Success)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
                    ImGui.TextUnformatted(link.Tooltip);
                    ImGui.PopStyleColor();


                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
                    ImGui.TextUnformatted(link.Url);
                    ImGui.PopStyleColor();
                }
            }

            if (ImGui.IsItemClicked())
            {
                Task.Run(() => Util.OpenLink(link.Url));
            }

            return true;
        }
    }
}