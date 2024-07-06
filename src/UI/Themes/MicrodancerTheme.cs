using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Microdancer
{
    public sealed class MicrodancerTheme : Theme
    {
        private readonly Dictionary<ImGuiStyleVar, object> _styles =
            new()
            {
                { ImGuiStyleVar.WindowPadding, new Vector2(8.0f, 4.0f) },
                { ImGuiStyleVar.FramePadding, new Vector2(4.0f, 4.0f) },
                { ImGuiStyleVar.CellPadding, new Vector2(4.0f, 2.0f) },
                { ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 4.0f) },
                { ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4) },
                { ImGuiStyleVar.IndentSpacing, 10.0f },
                { ImGuiStyleVar.ScrollbarSize, 10.0f },
                { ImGuiStyleVar.GrabMinSize, 13.0f },
                { ImGuiStyleVar.WindowBorderSize, 1.0f },
                { ImGuiStyleVar.ChildBorderSize, 1.0f },
                { ImGuiStyleVar.PopupBorderSize, 1.0f },
                { ImGuiStyleVar.FrameBorderSize, 1.0f },
                { ImGuiStyleVar.WindowRounding, 0.0f },
                { ImGuiStyleVar.ChildRounding, 0.0f },
                { ImGuiStyleVar.FrameRounding, 0.0f },
                { ImGuiStyleVar.PopupRounding, 0.0f },
                { ImGuiStyleVar.ScrollbarRounding, 6.0f },
                { ImGuiStyleVar.GrabRounding, 12.0f },
                { ImGuiStyleVar.TabRounding, 0.0f },
                { ImGuiStyleVar.WindowTitleAlign, new Vector2(0.0f, 0.5f) },
                { ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f) },
                { ImGuiStyleVar.SelectableTextAlign, Vector2.Zero },
            };

        private readonly Dictionary<ImGuiCol, Vector4> _colors =
            new()
            {
                { ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f) },
                { ImGuiCol.TextDisabled, new Vector4(0.5019608f, 0.5019608f, 0.5019608f, 1.0f) },
                { ImGuiCol.WindowBg, new Vector4(0.12941177f * 0.6f, 0.1254902f * 0.6f, 0.12941177f * 0.6f, 0.95f) },
                { ImGuiCol.ChildBg, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
                { ImGuiCol.PopupBg, new Vector4(0.08955222f, 0.08955222f, 0.08955222f, 1.0f) },
                { ImGuiCol.Border, new Vector4(0.0f, 0.0f, 0.0f, 1.0f) },
                { ImGuiCol.BorderShadow, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
                { ImGuiCol.FrameBg, new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 0.8f) },
                { ImGuiCol.FrameBgHovered, new Vector4(0.22352941f, 0.22352941f, 0.22352941f, 1.0f) },
                { ImGuiCol.TitleBg, new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 1.0f) },
                { ImGuiCol.TitleBgActive, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.TitleBgCollapsed, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.MenuBarBg, new Vector4(0.14f, 0.14f, 0.14f, 1.0f) },
                { ImGuiCol.ScrollbarBg, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
                { ImGuiCol.ScrollbarGrab, new Vector4(0.24313726f, 0.24313726f, 0.24313726f, 1.0f) },
                { ImGuiCol.ScrollbarGrabHovered, new Vector4(0.27601808f, 0.2760153f, 0.27601808f, 1.0f) },
                { ImGuiCol.ScrollbarGrabActive, new Vector4(0.27450982f, 0.27450982f, 0.27450982f, 1.0f) },
                { ImGuiCol.CheckMark, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.SliderGrab, new Vector4(0.39800596f, 0.39800596f, 0.39800596f, 1.0f) },
                { ImGuiCol.SliderGrabActive, new Vector4(0.4825822f, 0.4825822f, 0.4825822f, 1.0f) },
                { ImGuiCol.Button, new Vector4(0.12941177f, 0.12941177f, 0.12941177f, 1.0f) },
                { ImGuiCol.ButtonHovered, new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1.0f) },
                { ImGuiCol.ButtonActive, new Vector4(0.22352941f, 0.22352941f, 0.22352941f, 1.0f) },
                { ImGuiCol.Header, new Vector4(0.0f, 0.0f, 0.0f, 0.23529412f) },
                { ImGuiCol.HeaderHovered, new Vector4(0.0f, 0.0f, 0.0f, 0.3529412f) },
                { ImGuiCol.HeaderActive, new Vector4(0.0f, 0.0f, 0.0f, 0.47058824f) },
                { ImGuiCol.Separator, new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1.0f) },
                { ImGuiCol.SeparatorHovered, new Vector4(0.89411765f, 0.0f, 0.06666667f, 0.5f) },
                { ImGuiCol.SeparatorActive, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.ResizeGrip, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
                { ImGuiCol.ResizeGripHovered, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
                { ImGuiCol.ResizeGripActive, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.Tab, new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1.0f) },
                { ImGuiCol.TabHovered, new Vector4(0.44705883f, 0.0f, 0.033333335f, 1.0f) },
                { ImGuiCol.TabActive, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.TabUnfocused, new Vector4(0.16078432f, 0.15294118f, 0.16078432f, 1.0f) },
                { ImGuiCol.TabUnfocusedActive, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.DockingPreview, new Vector4(0.89411765f, 0.0f, 0.06666667f, 0.5f) },
                { ImGuiCol.DockingEmptyBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f) },
                { ImGuiCol.PlotLines, new Vector4(0.61f, 0.61f, 0.61f, 1.0f) },
                { ImGuiCol.PlotLinesHovered, new Vector4(1.0f, 0.43f, 0.35f, 1.0f) },
                { ImGuiCol.PlotHistogram, new Vector4(0.89411765f * 0.8f, 0.0f, 0.06666667f * 0.8f, 1.0f) },
                { ImGuiCol.PlotHistogramHovered, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.TableHeaderBg, new Vector4(0.19f, 0.19f, 0.2f, 1.0f) },
                { ImGuiCol.TableBorderStrong, new Vector4(0.31f, 0.31f, 0.45f, 1.0f) },
                { ImGuiCol.TableBorderLight, new Vector4(0.23f, 0.23f, 0.25f, 1.0f) },
                { ImGuiCol.TableRowBg, new Vector4(1.0f, 1.0f, 1.0f, 0.06f) },
                { ImGuiCol.TextSelectedBg, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.DragDropTarget, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.NavHighlight, new Vector4(0.89411765f, 0.0f, 0.06666667f, 1.0f) },
                { ImGuiCol.NavWindowingHighlight, new Vector4(1.0f, 1.0f, 1.0f, 0.7f) },
                { ImGuiCol.NavWindowingDimBg, new Vector4(0.8f, 0.8f, 0.8f, 0.2f) },
                { ImGuiCol.ModalWindowDimBg, new Vector4(0.8f, 0.8f, 0.8f, 0.35f) },
            };

        private int _styleCount = 0;
        private int _colorCount = 0;

        public override void Begin()
        {
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            foreach (var (style, value) in _styles)
            {
                if (value is float f)
                {
                    ImGui.PushStyleVar(style, f);
                }
                else if (value is Vector2 v)
                {
                    ImGui.PushStyleVar(style, v);
                }
                else
                {
                    continue;
                }

                _styleCount++;
            }

            foreach (var (color, value) in _colors)
            {
                ImGui.PushStyleColor(color, value);

                _colorCount++;
            }
        }

        public override void End()
        {
            if (_styleCount > 0)
            {
                ImGui.PopStyleVar(_styleCount);
                _styleCount = 0;
            }

            if (_colorCount > 0)
            {
                ImGui.PopStyleColor(_colorCount);
                _colorCount = 0;
            }
        }

        public override Vector4 GetColor(ImGuiCol color)
        {
            if (_colors.TryGetValue(color, out var value))
            {
                return value;
            }

            return base.GetColor(color);
        }

        public override T GetStyle<T>(ImGuiStyleVar style) where T : struct
        {
            if (_styles.TryGetValue(style, out var value) && value is Vector2)
            {
                return (T)value;
            }

            return base.GetStyle<T>(style);
        }
    }
}
