using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace Microdancer
{
    public class CreateButtons : PluginUiBase, IDrawable<string>
    {
        private readonly ButtonStyle _buttonStyle;

        private string _newItemName = string.Empty;
        private CreateState _createState;
        private bool _shouldReposition;

        public CreateButtons(ButtonStyle buttonStyle = ButtonStyle.Buttons)
        {
            _buttonStyle = buttonStyle;
        }

        public enum ButtonStyle
        {
            Buttons,
            ContextMenu,
        }

        private enum CreateState
        {
            Unselected,
            NewMicro,
            NewFolder,
        }

        public bool Draw(string basePath)
        {
            switch (_buttonStyle)
            {
                case ButtonStyle.ContextMenu:
                    if (ImGui.Selectable("New Micro"))
                    {
                        ToggleState(CreateState.NewMicro);
                    }
                    if (ImGui.Selectable("New Folder"))
                    {
                        ToggleState(CreateState.NewFolder);
                    }
                    break;
                default:
                case ButtonStyle.Buttons:
                    var rect = ImGui.GetContentRegionAvail();
                    rect.Y = ImGuiHelpers.GlobalScale * 40.0f;

                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

                    ImGui.InvisibleButton($"create-before-spacing-{basePath}", new(rect.X * 0.15f, rect.Y));
                    ImGui.SameLine();
                    if (ImGui.Button("Create new Micro", new(rect.X * 0.3125f, rect.Y)))
                    {
                        ToggleState(CreateState.NewMicro);
                    }
                    ImGui.SameLine();
                    ImGui.InvisibleButton($"create-middle-spacing-{basePath}", new(rect.X * 0.025f, rect.Y));
                    ImGui.SameLine();
                    if (ImGui.Button("Create new Folder", new(rect.X * 0.3125f, rect.Y)))
                    {
                        ToggleState(CreateState.NewFolder);
                    }
                    ImGui.SameLine();
                    ImGui.InvisibleButton($"create-after-spacing-{basePath}", new(rect.X * 0.15f, rect.Y));

                    ImGui.PopStyleVar();

                    break;
            }

            if (_createState != CreateState.Unselected)
            {
                var itemName = _createState == CreateState.NewMicro ? "New Micro" : "New Folder";

                if (_buttonStyle == ButtonStyle.ContextMenu)
                {
                    if (_createState == CreateState.NewMicro)
                    {
                        CreateMicro(basePath, itemName);
                    }
                    else
                    {
                        CreateFolder(basePath, itemName);
                    }

                    ToggleState(CreateState.Unselected);
                }
                else
                {
                    var width = 298 * ImGuiHelpers.GlobalScale;
                    var height = ImGuiHelpers.GetButtonSize(" ").Y + ImGui.GetStyle().ItemSpacing.Y * 2;

                    ImGuiExt.BeginCursorPopup("##new-item-popup", new Vector2(width, height), _shouldReposition);
                    _shouldReposition = false;

                    if (string.IsNullOrWhiteSpace(_newItemName))
                    {
                        _newItemName = itemName;
                    }

                    if (
                        ImGui.InputText(
                            "##new-item",
                            ref _newItemName,
                            1024,
                            ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll
                        )
                    )
                    {
                        if (!string.IsNullOrWhiteSpace(_newItemName))
                        {
                            itemName = _newItemName;

                            if (_createState == CreateState.NewMicro)
                            {
                                CreateMicro(basePath, itemName);
                            }
                            else
                            {
                                CreateFolder(basePath, itemName);
                            }
                        }

                        ToggleState(CreateState.Unselected);
                    }

                    ImGui.SameLine();

                    ImGui.PushItemWidth(-1);

                    if (ImGuiExt.IconButton(FontAwesomeIcon.TimesCircle))
                    {
                        ToggleState(CreateState.Unselected);
                    }

                    ImGui.PopItemWidth();

                    ImGuiExt.EndCursorPopup();
                }
            }

            return true;
        }

        private void ToggleState(CreateState state)
        {
            var originalState = _createState;

            if (state == CreateState.Unselected)
            {
                _createState = state;
            }
            else
            {
                _createState = _createState == state ? CreateState.Unselected : state;
            }

            if (originalState != state)
            {
                _newItemName = string.Empty;
            }

            _shouldReposition = true;
        }
    }
}
