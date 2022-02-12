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

namespace Microdancer
{
    public class PluginWindow : IDisposable
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
            MicroManager microManager,
            Configuration config)
        {
            _pluginInterface = pluginInterface;
            _clientState = clientState;
            _condition = condition;
            _library = library;
            _microManager = microManager;
            _config = config;

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
                var windowVisible = true;
                var draw = ImGui.Begin(Microdancer.PLUGIN_NAME, ref windowVisible);
                var windowSize = ImGui.GetWindowSize();
                ImGui.SetWindowSize(Vector2.Max(windowSize, new(400, 600)));

                SetVisiblityIfNeeded(windowVisible);
                if (draw)
                {
                    DrawSettings();
                }
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

            ImGui.Columns(1);

            DrawLibraryPath();

            ImGui.Separator();

            ImGui.Columns(2, "LibraryContent", true);

            DrawLibrary();

            ImGui.NextColumn();

            DrawContentArea();

            ImGui.Columns(1);

            ImGui.Separator();

            DrawMicroQueue();

            ImGui.End();
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
            ImGui.BeginChildFrame(1, new(-1,ImGui.GetContentRegionAvail().Y - 200));

            foreach (var node in _library.GetNodes())
            {
                DrawNode(node);
            }

            ImGui.EndChildFrame();
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

            if (ImGui.BeginPopupContextItem() && _clientState.LocalPlayer != null)
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
            ImGui.BeginChildFrame(2, new(-1,ImGui.GetContentRegionAvail().Y - 200), ImGuiWindowFlags.NoBackground);

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
                else if (_clientState.LocalPlayer == null)
                {
                    ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "Cannot run Micros while logged out!");
                }
                else
                {
                    if (_clientState.LocalPlayer != null)
                    {
                        var sz = new Vector2(ImGui.GetColumnWidth(), ImGui.GetTextLineHeight() + 20);

                        if (TintButton("Play All", sz, new(0.0f, 0.8f, 0.0f, 1.0f)))
                        {
                            RunMicro(micro);
                        }

                        if (ImGui.Button("Queue All", sz))
                        {
                            RunMicro(micro, multi: true);
                        }
                    }
                }

                ImGui.Separator();
                ImGui.Separator();

                var size = Vector2.Zero;

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

                    size.X += 40;
                    size.Y += 20;

                    var info = _microManager.Running.Values.Where(mi => mi.Micro.Id == micro.Id).ToArray();
                    var running = info.Length > 0;

                    var col = 0;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        col++;

                        string? region = regions[i];
                        if (_clientState.LocalPlayer != null && !inCombat)
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

                        if (ImGui.GetContentRegionAvail().X - ((size.X + 2) * (col + 1)) > size.X
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

                if (ImGui.TreeNode("Content"))
                {
                    var len = lines.Length;
                    var maxChars = (len + 1).ToString().Length;

                    for (var i = 0; i < len; ++i)
                    {
                        lines[i] = $"{(i + 1).ToString().PadLeft(maxChars)} | {lines[i]}";
                    }

                    var contents = string.Join('\n', lines);
                    ImGui.PushItemWidth(-1f);
                    ImGui.PushFont(UiBuilder.MonoFont);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                    ImGui.InputTextMultiline(
                        $"##{micro.Id}-editor", ref contents, 10_000, new Vector2(0, 250), ImGuiInputTextFlags.ReadOnly);
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                    ImGui.PopItemWidth();


                    ImGui.TreePop();
                }
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
            if (ImGui.BeginListBox("##running-micros", new Vector2(0, 100)))
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
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.GetColorU32(ImGuiCol.CheckMark));
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

            if (TintButton(hasSelected ? "Cancel Selected" : "Cancel All", new(0.8f, 0.0f, 0.0f, 1.0f)))
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
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(color.X * 0.67f, color.Y * 0.67f, color.Z * 0.67f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, color.Length() > 0.5f ? Vector4.One : Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
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
