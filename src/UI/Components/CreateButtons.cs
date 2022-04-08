using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class CreateButtons : PluginUiBase, IDrawable<string>
    {
        private readonly ButtonStyle _buttonStyle;

        private string _newItemName = string.Empty;
        private bool _newMicro;
        private bool _newFolder;

        public CreateButtons(ButtonStyle buttonStyle = ButtonStyle.Buttons)
        {
            _buttonStyle = buttonStyle;
        }

        public enum ButtonStyle
        {
            Buttons,
            Icons,
            ContextMenu,
        }

        public bool Draw(string basePath)
        {
            if (!_newMicro && !_newFolder)
            {
                switch (_buttonStyle)
                {
                    case ButtonStyle.Icons:
                        _newMicro = ImGuiExt.IconButton(FontAwesomeIcon.Plus, "Create new Micro");
                        ImGui.SameLine();
                        _newFolder = ImGuiExt.IconButton(FontAwesomeIcon.FolderPlus, "Create new Folder");
                        break;
                    case ButtonStyle.ContextMenu:
                        _newMicro = ImGui.Selectable("New Micro");
                        _newFolder = ImGui.Selectable("New Folder");
                        break;
                    default:
                    case ButtonStyle.Buttons:
                        _newMicro = ImGui.Button("Create new Micro");
                        ImGui.SameLine();
                        _newFolder = ImGui.Button("Create new Folder");
                        break;
                }
            }

            if (_newMicro || _newFolder)
            {
                var itemName = _newMicro ? "New Micro" : "New Folder";

                if (_buttonStyle == ButtonStyle.ContextMenu)
                {
                    if (_newMicro)
                    {
                        CreateMicro(basePath, itemName);
                    }
                    if (_newFolder)
                    {
                        CreateFolder(basePath, itemName);
                    }
                    _newItemName = string.Empty;
                    _newMicro = false;
                    _newFolder = false;
                }
                else
                {
                    if (ImGuiExt.BeginCursorPopup("##new-item-popup"))
                    {
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
                                if (_newMicro)
                                {
                                    CreateMicro(basePath, itemName);
                                }
                                if (_newFolder)
                                {
                                    CreateFolder(basePath, itemName);
                                }
                            }

                            _newItemName = string.Empty;
                            _newMicro = false;
                            _newFolder = false;
                        }

                        ImGui.SameLine();

                        ImGui.PushItemWidth(-1);

                        if (ImGuiExt.IconButton(FontAwesomeIcon.TimesCircle))
                        {
                            _newItemName = string.Empty;
                            _newMicro = false;
                            _newFolder = false;
                        }

                        ImGui.PopItemWidth();
                    }
                    else if (_buttonStyle != ButtonStyle.ContextMenu)
                    {
                        _newItemName = string.Empty;
                        _newMicro = false;
                        _newFolder = false;
                    }

                    ImGuiExt.EndCursorPopup();
                }
            }

            return true;
        }
    }
}
