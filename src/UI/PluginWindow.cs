using System.Linq;
using System.IO;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public class PluginWindow : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly ClientState _clientState;
        private readonly Condition _condition;
        private readonly LibraryManager _library;
        private readonly MicroManager _microManager;
        private readonly Configuration _config;
        private readonly Theme _theme;

        private bool _disposedValue;

        public PluginWindow(
            DalamudPluginInterface pluginInterface,
            ClientState clientState,
            Condition condition,
            LibraryManager library,
            MicroManager microManager
        )
        {
            _pluginInterface = pluginInterface;
            _clientState = clientState;
            _condition = condition;
            _library = library;
            _microManager = microManager;
            _config = _pluginInterface.Configuration();
            _theme = new BurgundyTheme();

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

        private void Draw()
        {
            if (_config.WindowVisible)
            {
                _theme.Begin();

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

                _theme.End();
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
            else if (_clientState.LocalPlayer == null || Microdancer.LicenseIsValid == null)
            {
                ImGui.TextColored(new(0.67f, 0.67f, 0.67f, 1.0f), "Please wait....");
                return;
            }
            else if (Microdancer.LicenseIsValid == false)
            {
                ImGui.TextColored(
                    new(1.0f, 0.0f, 0.0f, 1.0f),
                    "Microdancer is not currently licensed for this character. Please contact Dance Mom for access!"
                );

                return;
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
            ImGui.BeginChildFrame(1, new(-1, ImGui.GetContentRegionAvail().Y - (180 * ImGuiHelpers.GlobalScale)));

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
            var buttonGroupWidth = (buttonWidth * 2) + itemSpacing;
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var columnPadding = ImGui.GetStyle().ColumnsMinSpacing * 2;

            var spacingWidth = availableWidth - columnPadding - buttonGroupWidth;

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

                if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "Create new Micro"))
                {
                    Directory.CreateDirectory(path);
                    File.CreateText(IOUtility.MakeUniqueFile(path, "New Micro ({0}).micro", "New Micro.micro"));
                    _library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (ImGuiExt.IconButton(FontAwesomeIcon.FolderPlus, "Create new Folder"))
                {
                    Directory.CreateDirectory(IOUtility.MakeUniqueDir(path, "New Folder ({0})", "New Folder"));
                    _library.MarkAsDirty();
                }
            }
        }

        private void DrawNode(INode node, string idPrefix = "")
        {
            ImGui.PushID($"{node.Id}");

            bool open;

            if (node.Children.Count == 0)
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

                if (idPrefix?.Length == 0 && ImGui.IsItemClicked() && _config.LibrarySelection != node.Id)
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
                foreach (var child in node.Children)
                {
                    DrawNode(child);
                }

                ImGui.TreePop();
            }
        }

        private void DrawContentArea()
        {
            INode? node = null;

            if (_config.LibrarySelection != Guid.Empty)
            {
                node = _library.Find<INode>(_config.LibrarySelection);
            }

            ImGui.BeginChildFrame(
                2,
                new(-1, ImGui.GetContentRegionAvail().Y - (180 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            DrawBreadcrumb(node);

            if (node is Micro micro)
            {
                var lines = micro.GetBody().ToArray();
                var regions = lines.Where(l => l.Trim().StartsWith("#region ")).Select(l => l.Trim()[8..]).ToArray();

                var inCombat = _condition[ConditionFlag.InCombat];
                var info = _microManager.Find(micro.Id).ToArray();

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
                        Vector4 textColor = _theme.GetColor(ImGuiCol.TextDisabled);
                        foreach (var mi in info)
                        {
                            if (mi.CurrentCommand?.LineNumber == i + 1)
                            {
                                prefixColor = _theme.GetColor(ImGuiCol.TitleBgActive);
                                textColor = _theme.GetColor(ImGuiCol.Text);
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

                var nodes = node?.Children ?? _library.GetNodes().ToList();

                if (nodes.Count > 0)
                {
                    foreach (var child in nodes)
                    {
                        DrawNode(child, "right_column");
                    }
                }
                else
                {
                    ImGui.Text("This folder is lonely...let's get started!");
                }

                var basePath = (node as Folder)?.Path ?? _config.LibraryPath;

                ImGui.Separator();

                if (ImGui.Button("Create new Micro"))
                {
                    Directory.CreateDirectory(basePath);
                    File.CreateText(IOUtility.MakeUniqueFile(basePath, "New Micro ({0}).micro", "New Micro.micro"));
                    _library.MarkAsDirty();
                }

                ImGui.SameLine();

                if (ImGui.Button("Create new Folder"))
                {
                    Directory.CreateDirectory(IOUtility.MakeUniqueDir(basePath, "New Folder ({0})", "New Folder"));
                    _library.MarkAsDirty();
                }
            }

            ImGui.EndChildFrame();
        }

        private void DrawBreadcrumb(INode? node)
        {
            if (node == null)
            {
                if (_config.LibrarySelection != Guid.Empty)
                {
                    _config.LibrarySelection = Guid.Empty;
                    _pluginInterface.SavePluginConfig(_config);
                }

                ImGui.Text("Library");
            }
            else if (node != null)
            {
                if (ImGui.Selectable("Library", false, ImGuiSelectableFlags.None, ImGui.CalcTextSize("Library")))
                {
                    _config.LibrarySelection = Guid.Empty;
                    _pluginInterface.SavePluginConfig(_config);
                }

                var relativePath = node.Path[(_config.LibraryPath.Length + 1)..];
                var breadCrumb = relativePath.Split(new[] { '/', '\\' });

                var currentPath = string.Empty;
                foreach (var segment in breadCrumb)
                {
                    if (string.IsNullOrWhiteSpace(segment))
                    {
                        continue;
                    }

                    ImGui.SameLine();
                    ImGui.Text("»");
                    ImGui.SameLine();

                    currentPath += $"/{segment}";

                    var parent = _library.Find<Folder>(Path.GetFullPath(_config.LibraryPath + currentPath));

                    if (segment.EndsWith(".micro"))
                    {
                        ImGui.Text(segment[..^6]);
                    }
                    else
                    {
                        if (
                            ImGui.Selectable(segment, false, ImGuiSelectableFlags.None, ImGui.CalcTextSize(segment))
                            && parent != null
                        )
                        {
                            _config.LibrarySelection = parent.Id;
                            _pluginInterface.SavePluginConfig(_config);
                        }
                    }
                }
            }
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
            runningMicrosSize.Y -= ImGuiHelpers.GetButtonSize(" ").Y + (ImGui.GetStyle().WindowPadding.Y * 2);
            runningMicrosSize.Y = Math.Max(runningMicrosSize.Y, 1);

            if (ImGui.BeginListBox("##running-micros", runningMicrosSize))
            {
                var queue = _microManager.GetQueue();

                if (!queue.Any())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.Text("- None -");
                    ImGui.PopStyleColor();
                }
                else
                {
                    foreach (var microInfo in queue)
                    {
                        var micro = microInfo.Micro;
                        var name = micro.Name;
                        var currentRegion = microInfo.CurrentCommand?.Region;

                        if (currentRegion != null)
                        {
                            name += $" [{currentRegion.Name}]";
                        }

                        var flags = ImGuiSelectableFlags.None;
                        if (_microManager.IsCancelled(microInfo))
                        {
                            flags = ImGuiSelectableFlags.Disabled;
                            name = $"{name} (Cancelled)";
                        }
                        else if (_microManager.IsPaused(microInfo))
                        {
                            name = $"{name} (Paused)";
                        }

                        name = $"{name}##{microInfo.Id}";

                        if (
                            ImGui.Selectable(
                                name,
                                _config.QueueSelection == microInfo.Id,
                                flags,
                                ImGui.CalcTextSize(name)
                            )
                        )
                        {
                            _config.QueueSelection = _config.QueueSelection == Guid.Empty ? microInfo.Id : Guid.Empty;
                            _pluginInterface.SavePluginConfig(_config);
                        }

                        var currentCommand = microInfo.CurrentCommand;
                        if (currentCommand != null)
                        {
                            var remainingTime = currentCommand.GetRemainingTime();

                            ImGui.Selectable(
                                $"\t{currentCommand.Text ?? string.Empty}",
                                false,
                                ImGuiSelectableFlags.Disabled
                            );

                            ImGui.SameLine();

                            var changeBarColor = remainingTime <= TimeSpan.FromMilliseconds(500);
                            if (changeBarColor)
                            {
                                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.GetColorU32(ImGuiCol.TitleBgActive));
                            }

                            ImGui.PushItemWidth(-1f);

                            ImGui.ProgressBar(
                                currentCommand.GetProgress(),
                                new(0, ImGui.GetTextLineHeightWithSpacing()),
                                remainingTime?.ToString(@"s\.fff\s") ?? string.Empty
                            );

                            ImGui.PopItemWidth();

                            if (changeBarColor)
                            {
                                ImGui.PopStyleColor();
                            }
                        }
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.PopItemWidth();

            var hasSelected = _microManager.IsRunning(_config.QueueSelection);

            if (
                ImGuiExt.TintButton(
                    hasSelected ? "Cancel Selected" : "Cancel All",
                    new Vector4(0.44705883f, 0.0f, 0.033333335f, 1.0f)
                )
            )
            {
                if (hasSelected)
                {
                    _microManager.Cancel(_config.QueueSelection);
                }
                else
                {
                    _microManager.CancelAll();
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
                        _microManager.Resume(_config.QueueSelection);
                    }
                    else
                    {
                        _microManager.Pause(_config.QueueSelection);
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
                _microManager.CancelAll();
            }

            _microManager.RunMicro(micro, region);
        }
    }
}
