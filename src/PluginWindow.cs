using System.Linq;
using System.IO;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;

namespace Microdancer
{
    [PluginInterface]
    public unsafe class PluginWindow : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly ClientState _clientState;
        private readonly Condition _condition;
        private readonly LibraryManager _library;
        private readonly MicroManager _microManager;
        private readonly Configuration _config;

        private bool _disposedValue;

        public PluginWindow(
            DalamudPluginInterface pluginInterface,
            ClientState clientState,
            Condition condition,
            LibraryManager library,
            MicroManager microManager)
        {
            _pluginInterface = pluginInterface;
            _clientState = clientState;
            _condition = condition;
            _library = library;
            _microManager = microManager;
            _config = _pluginInterface.Configuration();

            _pluginInterface.UiBuilder.Draw += Draw;
            _pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            _clientState.Logout += Logout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _pluginInterface.UiBuilder.Draw -= Draw;
                _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                _clientState.Logout -= Logout;
            }

            _disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Logout(object? _, EventArgs _1)
        {
            _config.WindowVisible = false;
            _pluginInterface.SavePluginConfig(_config);
        }

        private void OpenConfigUi()
        {
            _config.WindowVisible = true;
            _pluginInterface.SavePluginConfig(_config);
        }

        private readonly Dictionary<ImGuiStyleVar, object> _styles = new Dictionary<ImGuiStyleVar, object>
        {
            { ImGuiStyleVar.WindowPadding, new Vector2(8.0f, 4.0f) },
            { ImGuiStyleVar.FramePadding, new Vector2(4.0f, 4.0f) },
            { ImGuiStyleVar.CellPadding, new Vector2(4.0f, 2.0f) },
            { ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 4.0f) },
            { ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4)},
            { ImGuiStyleVar.IndentSpacing, 21.0f },
            { ImGuiStyleVar.ScrollbarSize, 10.0f },
            { ImGuiStyleVar.GrabMinSize, 13.0f },
            { ImGuiStyleVar.WindowBorderSize, 1.0f },
            { ImGuiStyleVar.ChildBorderSize, 1.0f },
            { ImGuiStyleVar.PopupBorderSize, 0.0f },
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

        private readonly Dictionary<ImGuiCol, Vector4> _colors = new Dictionary<ImGuiCol, Vector4>
        {
            { ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)  },
            { ImGuiCol.TextDisabled, new Vector4(0.5019608f, 0.5019608f, 0.5019608f, 1.0f) },
            { ImGuiCol.WindowBg, new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 0.90f) },
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
            { ImGuiCol.Separator, new Vector4(0.0f, 0.0f, 0.0f, 0.0f) },
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
            { ImGuiCol.PlotHistogram, new Vector4(0.9f, 0.7f, 0.0f, 1.0f) },
            { ImGuiCol.PlotHistogramHovered, new Vector4(1.0f, 0.6f, 0.0f, 1.0f) },
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

        private void BeginBurgundySkin()
        {
            foreach(var (style, value) in _styles)
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

            foreach(var (color, value) in _colors)
            {
                ImGui.PushStyleColor(color, value);

                _colorCount++;
            }
        }

        private void EndBurgundySkin()
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

        private void Draw()
        {
            if (_config.WindowVisible)
            {
                BeginBurgundySkin();

                var windowVisible = true;
                var draw = ImGui.Begin(Microdancer.PLUGIN_NAME, ref windowVisible);
                var windowSize = ImGui.GetWindowSize();

                if (!ImGui.IsWindowCollapsed())
                {
                    ImGui.SetWindowSize(Vector2.Max(windowSize, ImGuiHelpers.ScaledVector2(400, 400)));
                }

                SetVisiblityIfNeeded(windowVisible);
                if (draw)
                {
                    DrawSettings();
                }

                ImGui.End();

                EndBurgundySkin();
            }
        }
        private void SetVisiblityIfNeeded(bool visible)
        {
            if (visible != _config.WindowVisible)
            {
                _config.WindowVisible = visible;
                _pluginInterface.SavePluginConfig(_config);
            }
        }

        private void DrawSettings()
        {
            if (_config.QueueSelection != Guid.Empty && !_microManager.IsRunning(_config.QueueSelection))
            {
                _config.QueueSelection = Guid.Empty;
            }

            if (!_clientState.IsLoggedIn)
            {
                ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "Please log in to open Microdancer.");
                return;
            }
            else if (_clientState.LocalPlayer == null)
            {
                ImGui.TextColored(new(0.67f, 0.67f, 0.67f, 1.0f), "Please wait....");
            }

            ImGui.Columns(1);

            DrawLibraryPath();

            ImGui.Separator();

            ImGui.Columns(2);

            DrawLibrary();

            ImGui.NextColumn();

            DrawContentArea();

            ImGui.Columns(1);

            DrawMicroQueue();
        }

        private void DrawLibraryPath()
        {
            ImGui.Text("Library Path");

            string libPath = _config.LibraryPath;
            if (ImGui.InputText("##lib-path", ref libPath, 8192, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _config.LibraryPath = libPath;
                _pluginInterface.SavePluginConfig(_config);
                _library.MarkAsDirty();
            }

            var hasLibrary = Directory.Exists(_config.LibraryPath);

            ImGui.SameLine();

            if (ImGui.Button(hasLibrary ? "Open Library" : "Create New Library"))
            {
                Directory.CreateDirectory(_config.LibraryPath);
                OpenNode(null);
            }
        }

        private void DrawLibrary()
        {
             ImGui.BeginChildFrame(
                1,
                new(-1,ImGui.GetContentRegionAvail().Y - (180 * ImGuiHelpers.GlobalScale))
            );

            foreach (var node in _library.GetNodes())
            {
                DrawNode(node);
            }

            ImGui.EndChildFrame();

            if (ImGui.IsItemClicked())
            {
                _config.LibrarySelection = Guid.Empty;
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var buttonWidth = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Plus.ToIconString()).X;
            ImGui.PopFont();

            var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
            var buttonGroupWidth = buttonWidth * 2 + itemSpacing;
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var columnPadding = ImGui.GetStyle().ColumnsMinSpacing * 2;

            var spacingWidth =
                availableWidth
                - columnPadding
                - buttonGroupWidth;

            if (spacingWidth > itemSpacing)
            {
                ImGui.Dummy(new(spacingWidth, 0));
                ImGui.SameLine();
            }

            var path = _config.LibraryPath;
            if (Directory.Exists(path))
            {
                INode? node = null;
                if (_config.LibrarySelection != Guid.Empty)
                {
                    node = _library.Find<INode>(_config.LibrarySelection);
                    if (node is Micro)
                    {
                        path = Path.GetDirectoryName(node.Path)!;
                    }
                    else if (node != null)
                    {
                        path = node.Path;
                    }
                }

                if (IconButton(FontAwesomeIcon.Plus, "Create new Micro"))
                {
                    Directory.CreateDirectory(path);
                    File.CreateText(IOUtility.MakeUniqueFile(path, "New Micro ({0}).micro", "New Micro.micro"));
                    _library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (IconButton(FontAwesomeIcon.FolderPlus, "Create new Folder"))
                {
                    Directory.CreateDirectory(
                        IOUtility.MakeUniqueDir(path, "New Folder ({0})", "New Folder"));
                    _library.MarkAsDirty();
                }
            }
        }

        private IEnumerable<INode> DrawNode(INode node, string idPrefix = "")
        {
            var toRemove = new List<INode>();
            ImGui.PushID($"{node.Id}");

            bool open;

            if (node is Micro)
            {
                var flags = ImGuiTreeNodeFlags.Leaf;
                if (_config.LibrarySelection == node.Id)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
                open = ImGui.TreeNodeEx($"{idPrefix}{node.Id}", flags, $"{node.Name}");

                if (ImGui.IsItemClicked())
                {
                    _config.LibrarySelection = _config.LibrarySelection == node.Id ? Guid.Empty : node.Id;
                    _pluginInterface.SavePluginConfig(_config);
                }
            }
            else
            {
                var flags = ImGuiTreeNodeFlags.None;
                if (idPrefix != string.Empty)
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }
                open = ImGui.TreeNodeEx($"{idPrefix}{node.Id}", flags, $"{node.Name}");

                if (idPrefix == string.Empty && ImGui.IsItemClicked() && _config.LibrarySelection != node.Id)
                {
                    _config.LibrarySelection = node.Id;
                    _pluginInterface.SavePluginConfig(_config);
                }
            }

            if (ImGui.BeginPopupContextItem())
            {
                if (node is Micro micro)
                {
                    if (ImGui.Button("Play##context"))
                    {
                        RunMicro(micro);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Queue##context"))
                    {
                        RunMicro(micro, multi: true);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Open File##context"))
                    {
                        OpenNode(node);
                    }
                }
                else
                {
                    if (ImGui.Button("Open Folder##context"))
                    {
                        OpenNode(node);
                    }
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();

            if (open)
            {
                if (node is Folder)
                {
                    foreach (var child in node.Children)
                    {
                        toRemove.AddRange(DrawNode(child));
                    }
                }

                ImGui.TreePop();
            }

            return toRemove;
        }

        private void DrawContentArea()
        {
            ImGui.BeginChildFrame(
                2,
                new(-1, ImGui.GetContentRegionAvail().Y - (180 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar
            );

            INode? node = null;

            if (_config.LibrarySelection != Guid.Empty)
            {
                node = _library.Find<INode>(_config.LibrarySelection);
            }

            if (node == null)
            {
                if (_config.LibrarySelection != Guid.Empty)
                {
                    _config.LibrarySelection = Guid.Empty;
                    _pluginInterface.SavePluginConfig(_config);
                }

                ImGui.TextColored(
                    new(0.68f, 0.68f, 0.68f, 1.0f),
                    "To begin, please select a folder or Micro from the library on the left.");

                ImGui.EndChildFrame();
                return;
            }

            var folderId = Node.GenerateId(Path.GetDirectoryName(node.Path!));
            var folder = _library.Find<Folder>(folderId);
            var relativePath = node.Path[(_config.LibraryPath.Length + 1)..];
            string? breadCrumb = null;
            if (folder != null)
            {
                var extLength = node is Micro ? 7 : 1;
                breadCrumb = "Library  »  ";
                breadCrumb +=
                    relativePath[..^(node.Name.Length + extLength)]
                        .Replace("/", "  »  ")
                        .Replace("\\", "  »  ");
                breadCrumb += "  ";
            }

            if (breadCrumb != null)
            {
                if (ImGui.Selectable(
                    breadCrumb,
                    _config.LibrarySelection == folder?.Id,
                    ImGuiSelectableFlags.None,
                    ImGui.CalcTextSize(breadCrumb)
                ))
                {
                    if (folder != null)
                    {
                        _config.LibrarySelection = folder.Id;
                        _pluginInterface.SavePluginConfig(_config);
                    }
                }
                ImGui.SameLine(ImGui.CalcTextSize(breadCrumb).X + 6);


                ImGui.Text($"»  {node.Name}");
            }
            else
            {
                ImGui.Text($"Library  »  {node.Name}");
            }

            if (node is Micro micro)
            {
                var lines = micro.GetBody().ToArray();
                var regions = lines
                    .Where(l => l.Trim().StartsWith("#region "))
                    .Select(l => l.Trim()[8..])
                    .ToArray();

                var inCombat = _condition[ConditionFlag.InCombat];

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

                    if (TintButton(label, buttonSize, new Vector4(0.0f, 0.44705883f, 0.033333335f, 1.0f)))
                    {
                        RunMicro(micro);
                    }
                    ImGui.PopFont();

                    TextTooltip("Play All");


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

                var info = _microManager.Running.Values.Where(mi => mi.Micro.Id == micro.Id).ToArray();

                if (regions.Length == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.TextWrapped(
                        "Add a region to your file (using #region [name] and #endregion) to have it show up here.");
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
                    for (int i = 0; i < regions.Length; i++)
                    {
                        col++;

                        string? region = regions[i];
                        if (!inCombat)
                        {
                            var regionRunning = info.Any(mi => mi.CurrentRegion == region);

                            if (regionRunning)
                            {
                                var progress = info.Max(mi => mi.CurrentRegionProgress);
                                ImGui.ProgressBar(progress, size, string.Empty);
                            }
                            else if (ImGui.Button($"{i + 1}", size))
                            {
                                RunMicro(micro, region);
                            }
                        }
                        else
                        {
                            ImGui.Selectable($"{i + 1}", false, ImGuiSelectableFlags.Disabled, size);
                        }

                        TextTooltip(region);

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (IconButton(FontAwesomeIcon.Copy, $"Copy run command for {region}"))
                            {
                                ImGui.SetClipboardText($"/runmicro {micro.Id} \"{region}\"");
                            }

                            ImGui.EndPopup();
                        }

                        if (ImGui.GetContentRegionAvail().X - ((size.X + ImGui.GetStyle().ItemSpacing.X) * col) > size.X
                            && i < regions.Length - 1)
                        {
                            ImGui.SameLine();
                        }
                        else
                        {
                            col = 0;
                        }
                    }
                }

                ImGui.Separator();

                ImGui.Text("File Contents");

                var framePadding = ImGui.GetStyle().FramePadding;
                var fileContentsSize = ImGui.GetContentRegionAvail();
                fileContentsSize.X -= framePadding.X;
                fileContentsSize.Y = Math.Max(fileContentsSize.Y + 2, 1);

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(8, 8));
                ImGui.BeginChildFrame(10, fileContentsSize, ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.PopStyleVar();

                var len = lines.Length;
                var maxChars = len.ToString().Length;

                for (var i = 0; i < len; ++i)
                {
                    Vector4 prefixColor = Vector4.Zero;
                    Vector4 textColor = *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);
                    foreach (var mi in info)
                    {
                        if (mi.CurrentLineNumber == i + 1)
                        {
                            prefixColor = *ImGui.GetStyleColorVec4(ImGuiCol.TitleBgActive);
                            textColor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
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

            if (node is Folder)
            {
                ImGui.Separator();
                ImGui.Separator();

                ImGui.Text("Select a subfolder or Micro:");

                foreach (var child in node.Children)
                {
                    DrawNode(child, "right_column");
                }
            }

            ImGui.EndChildFrame();
        }

        private void DrawMicroQueue()
        {
            ImGui.Text("Micro Queue");

            if (_condition[ConditionFlag.InCombat])
            {
                ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "All Micros paused while in combat!");
                return;
            }

            ImGui.PushItemWidth(-1f);
            var runningMicrosSize = ImGui.GetContentRegionAvail();
            runningMicrosSize.Y -= ImGuiHelpers.GetButtonSize(" ").Y + ImGui.GetStyle().WindowPadding.Y * 2;
            runningMicrosSize.Y = Math.Max(runningMicrosSize.Y, 1);

            if (ImGui.BeginListBox("##running-micros", runningMicrosSize))
            {
                if (_microManager.Running.IsEmpty)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.Text("- None -");
                    ImGui.PopStyleColor();
                }
                else
                {
                    foreach (var entry in _microManager.Running)
                    {
                        var microInfo = entry.Value;
                        var micro = entry.Value.Micro;
                        var name = micro.Name;

                        if (!string.IsNullOrWhiteSpace(entry.Value.CurrentRegion))
                        {
                            name += $" [{entry.Value.CurrentRegion}]";
                        }

                        var flags = ImGuiSelectableFlags.None;
                        if (_microManager.IsCancelled(entry.Key))
                        {
                            flags = ImGuiSelectableFlags.Disabled;
                            name = $"{name} (Cancelled)";
                        }
                        else if (_microManager.IsPaused(entry.Key))
                        {
                            name = $"{name} (Paused)";
                        }

                        name = $"{name}##{entry.Key}";

                        if (ImGui.Selectable(
                            name,
                            _config.QueueSelection == entry.Key,
                                flags,
                                ImGui.CalcTextSize(name)
                        ))
                        {
                            _config.QueueSelection = _config.QueueSelection == Guid.Empty ? entry.Key : Guid.Empty;
                            _pluginInterface.SavePluginConfig(_config);
                        }

                        ImGui.Selectable(
                            $"\t{microInfo.CurrentCommand ?? string.Empty}", false, ImGuiSelectableFlags.Disabled);

                        ImGui.SameLine();

                        var changeBarColor = microInfo.CurrentCommandTimeLeft <= TimeSpan.FromMilliseconds(500);
                        if (changeBarColor)
                        {
                            ImGui.PushStyleColor(
                                ImGuiCol.PlotHistogram, ImGui.GetColorU32(ImGuiCol.TitleBgActive));
                        }

                        ImGui.PushItemWidth(-1f);

                        ImGui.ProgressBar(
                            microInfo.CurrentCommandProgress,
                            new(0, ImGui.GetTextLineHeightWithSpacing()),
                            microInfo.CurrentCommandTimeLeft.ToString(@"s\.fff\s"));

                        ImGui.PopItemWidth();

                        if (changeBarColor)
                        {
                            ImGui.PopStyleColor();
                        }
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.PopItemWidth();

            var hasSelected = _microManager.IsRunning(_config.QueueSelection);

            if (TintButton(hasSelected ? "Cancel Selected" : "Cancel All", _colors[ImGuiCol.TitleBgActive]))
            {
                if (hasSelected)
                {
                    _microManager.CancelMicro(_config.QueueSelection);
                }
                else
                {
                    _microManager.CancelAllMicros();
                }
            }

            if (hasSelected)
            {
                ImGui.SameLine();

                var paused = _microManager.IsPaused(_config.QueueSelection);
                if (ImGui.Button(paused ? "Resume" : "Pause"))
                {
                    if (paused)
                    {
                        _microManager.ResumeMicro(_config.QueueSelection);
                    }
                    else
                    {
                        _microManager.PauseMicro(_config.QueueSelection);
                    }
                }
            }
        }
        private void OpenNode(INode? node, bool parent = false)
        {
            using Process fileOpener = new();

            string path = _config.LibraryPath;
            if (node != null)
            {
                if (parent)
                {
                    path = Path.GetDirectoryName(node.Path) ?? path;
                }
                else
                {
                    path = node.Path;
                }
            }

            fileOpener.StartInfo.FileName = "explorer";
            fileOpener.StartInfo.Arguments = "\"" + path + "\"";
            fileOpener.Start();
        }

        private void RunMicro(Micro micro, string? region = null, bool multi = false)
        {
            if (!multi)
            {
                _microManager.CancelAllMicros();
            }

            _microManager.SpawnMicro(micro, region);
        }

        private static bool TintButton(string label, Vector4 color)
        {
            return TintButtonImpl(() => ImGui.Button(label), color);
        }

        private static bool TintButton(string label, Vector2 size, Vector4 color)
        {
            return TintButtonImpl(() => ImGui.Button(label, size), color);
        }

        private static bool TintButtonImpl(Func<bool> button, Vector4 color)
        {
            var activeColor = new Vector4(color.X * 1.5f, color.Y * 1.5f, color.Z * 1.5f, color.W);
            var hoveredColor = new Vector4(color.X * 1.25f, color.Y * 1.25f, color.Z * 1.25f, color.W);
            var lightText = color + new Vector4(0.8f);
            var darkText = color - new Vector4(0.8f);

            var darkDiff = Vector4.DistanceSquared(darkText, Vector4.Min(activeColor, hoveredColor) * 0.67f);
            var lightDiff = Vector4.DistanceSquared(lightText, Vector4.Max(activeColor, hoveredColor));

            lightText = Vector4.Min(lightText, Vector4.One);
            darkText = Vector4.Max(darkText, Vector4.Zero);
            lightText.W = color.W;
            darkText.W = color.W;

            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.Text, darkDiff > lightDiff ? darkText : lightText);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor);

            var pressed = button();

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            return pressed;
        }

        private static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null)
                TextTooltip(tooltip);

            return result;
        }

        private static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }
    }
}
