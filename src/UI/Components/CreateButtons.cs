using System;
using System.Numerics;
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
            ContextMenu,
        }

        public bool Draw(string basePath)
        {
            var canCreate = !_newMicro && !_newFolder;

            switch (_buttonStyle)
            {
                case ButtonStyle.ContextMenu:
                    _newMicro |= ImGui.Selectable("New Micro") && canCreate;
                    _newFolder |= ImGui.Selectable("New Folder") && canCreate;
                    break;
                default:
                case ButtonStyle.Buttons:
                    var rect = ImGui.GetContentRegionAvail();
                    rect.Y = ImGuiHelpers.GlobalScale * 40.0f;

                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

                    ImGui.InvisibleButton($"create-before-spacing-{basePath}", new(rect.X * 0.15f, rect.Y));
                    ImGui.SameLine();
                    _newMicro |= ImGui.Button("Create new Micro", new(rect.X * 0.3125f, rect.Y)) && canCreate;
                    ImGui.SameLine();
                    ImGui.InvisibleButton($"create-middle-spacing-{basePath}", new(rect.X * 0.025f, rect.Y));
                    ImGui.SameLine();
                    _newFolder |= ImGui.Button("Create new Folder", new(rect.X * 0.3125f, rect.Y)) && canCreate;
                    ImGui.SameLine();
                    ImGui.InvisibleButton($"create-after-spacing-{basePath}", new(rect.X * 0.15f, rect.Y));

                    ImGui.PopStyleVar();

                    ImGui.Spacing();

                    break;
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

                    _newMicro = false;
                    _newFolder = false;
                }
                else
                {
                    var width = 298 * ImGuiHelpers.GlobalScale;
                    var height = ImGuiHelpers.GetButtonSize(" ").Y + ImGui.GetStyle().ItemSpacing.Y * 2;

                    ImGuiExt.BeginCursorPopup("##new-item-popup", new Vector2(width, height), canCreate);

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

                    ImGuiExt.EndCursorPopup();
                }
            }

            return true;
        }
    }
}
