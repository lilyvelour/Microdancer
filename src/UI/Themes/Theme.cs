using System.Numerics;
using ImGuiNET;

namespace Microdancer
{
    public unsafe abstract class Theme
    {
        public abstract void Begin();

        public abstract void End();

        public virtual Vector4 GetColor(ImGuiCol color)
        {
            return *ImGui.GetStyleColorVec4(color);
        }

        public virtual T GetStyle<T>(ImGuiStyleVar style) where T : struct
        {
            return style switch
            {
                ImGuiStyleVar.Alpha => (T)(object)ImGui.GetStyle().Alpha,
                ImGuiStyleVar.WindowPadding => (T)(object)ImGui.GetStyle().WindowPadding,
                ImGuiStyleVar.WindowRounding => (T)(object)ImGui.GetStyle().WindowRounding,
                ImGuiStyleVar.WindowBorderSize => (T)(object)ImGui.GetStyle().WindowBorderSize,
                ImGuiStyleVar.WindowMinSize => (T)(object)ImGui.GetStyle().WindowMinSize,
                ImGuiStyleVar.WindowTitleAlign => (T)(object)ImGui.GetStyle().WindowTitleAlign,
                ImGuiStyleVar.ChildRounding => (T)(object)ImGui.GetStyle().ChildRounding,
                ImGuiStyleVar.ChildBorderSize => (T)(object)ImGui.GetStyle().ChildBorderSize,
                ImGuiStyleVar.PopupRounding => (T)(object)ImGui.GetStyle().PopupRounding,
                ImGuiStyleVar.PopupBorderSize => (T)(object)ImGui.GetStyle().PopupBorderSize,
                ImGuiStyleVar.FramePadding => (T)(object)ImGui.GetStyle().FramePadding,
                ImGuiStyleVar.FrameRounding => (T)(object)ImGui.GetStyle().FrameRounding,
                ImGuiStyleVar.FrameBorderSize => (T)(object)ImGui.GetStyle().FrameBorderSize,
                ImGuiStyleVar.ItemSpacing => (T)(object)ImGui.GetStyle().ItemSpacing,
                ImGuiStyleVar.ItemInnerSpacing => (T)(object)ImGui.GetStyle().ItemInnerSpacing,
                ImGuiStyleVar.IndentSpacing => (T)(object)ImGui.GetStyle().IndentSpacing,
                ImGuiStyleVar.CellPadding => (T)(object)ImGui.GetStyle().CellPadding,
                ImGuiStyleVar.ScrollbarSize => (T)(object)ImGui.GetStyle().ScrollbarSize,
                ImGuiStyleVar.ScrollbarRounding => (T)(object)ImGui.GetStyle().ScrollbarRounding,
                ImGuiStyleVar.GrabMinSize => (T)(object)ImGui.GetStyle().GrabMinSize,
                ImGuiStyleVar.GrabRounding => (T)(object)ImGui.GetStyle().GrabRounding,
                ImGuiStyleVar.TabRounding => (T)(object)ImGui.GetStyle().TabRounding,
                ImGuiStyleVar.ButtonTextAlign => (T)(object)ImGui.GetStyle().ButtonTextAlign,
                ImGuiStyleVar.SelectableTextAlign => (T)(object)ImGui.GetStyle().SelectableTextAlign,
                _ => default,
            };
        }
    }
}
