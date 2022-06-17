using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using Dalamud.Game;

namespace Microdancer.UI
{
    [PluginInterface]
    public class MicrodancerUi : PluginWindow
    {
        private readonly LicenseChecker _license;
        private readonly Framework _framework;
        private readonly LibraryPath _libraryPath = new();
        private readonly DisplayLibrary _library = new();
        private readonly PlaybackControls _playbackControls = new();
        private readonly RegionBar _regionBar = new();

        private readonly Dictionary<Guid, ContentArea> _contentAreas = new();
        private readonly Dictionary<Guid, Timeline> _timelines = new();

        private Guid _focused;
        private readonly Dictionary<Guid, float> _dockReleased = new();
        private string? _previousConfigJson;
        private int _frameCount;

        public MicrodancerUi(LicenseChecker license, Framework framework)
        {
            _license = license;
            _framework = framework;

            _framework.Update += Update;
        }

        public override void Draw()
        {
            if (!Config.WindowVisible || !ClientState.IsLoggedIn)
            {
                return;
            }

            Theme.Begin();

            var windowVisible = true;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));

            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(400, 400), ImGui.GetMainViewport().WorkSize);
            var draw = ImGui.Begin(Microdancer.PLUGIN_NAME, ref windowVisible, ImGuiWindowFlags.NoDocking);
            ImGui.PopStyleVar();

            if (draw)
            {
                if (ClientState.LocalPlayer == null || _license.IsValidLicense == null)
                {
                    ImGui.TextColored(new(0.67f, 0.67f, 0.67f, 1.0f), "Please wait....");
                }
                else if (_license.IsValidLicense == false)
                {
                    ImGui.TextColored(
                        new(1.0f, 0.0f, 0.0f, 1.0f),
                        "Microdancer is not currently licensed for this character. Please contact Dance Mom for access!"
                    );
                }
                else
                {
                    DrawMainContent();
                    DrawDockWindows();
                }
            }

            ImGui.End();

            Theme.End();

            if (windowVisible != Config.WindowVisible)
            {
                Config.WindowVisible = windowVisible;
            }
        }

        private void DrawDockWindows()
        {
            var openWindows = Config.OpenWindows.ToList();
            var previewWindow = false;

            if (Config.LibrarySelection != Guid.Empty && !openWindows.Contains(Config.LibrarySelection))
            {
                openWindows.Add(Config.LibrarySelection);
                previewWindow = true;
            }

            if (openWindows.Count == 0)
            {
                openWindows.Add(Guid.Empty);
            }

            openWindows = openWindows.Distinct().ToList();

            for (int i = 0; i < openWindows.Count; i++)
            {
                var guid = openWindows[i];
                var additionalNode = Library.Find<INode>(guid);

                EnsureUiComponents(guid);

                var childWindowVisible = true;

                ImGui.SetNextWindowDockID(439839, ImGuiCond.Appearing);

                if (!_dockReleased.ContainsKey(guid))
                {
                    ImGui.SetNextWindowDockID(439839, ImGuiCond.Always);
                    _dockReleased[guid] = 0.0f;
                }
                else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    var releaseTime = _dockReleased.GetValueOrDefault(guid);
                    if (releaseTime > 0.25f)
                    {
                        ImGui.SetNextWindowDockID(439839, ImGuiCond.Always);
                        _dockReleased[guid] = 0.0f;
                    }
                    else
                    {
                        _dockReleased[guid] = releaseTime + ImGui.GetIO().DeltaTime;
                    }
                }

                var name = additionalNode?.Name ?? "Home";

                if (additionalNode is Micro micro)
                {
                    for (var j = 0; j < i; ++j)
                    {
                        var otherGuid = openWindows[j];
                        var otherNode = Library.Find<INode>(otherGuid);

                        if (name == otherNode?.Name)
                        {
                            var path = micro.Path[(Config.LibraryPath.Length + 1)..];

                            name += $"→ {path}";
                        }
                    }
                }

                if (previewWindow && guid == Config.LibrarySelection)
                {
                    name = $"< {name} >";
                }

                name = $"{name}##{guid}_{i}";

                if (guid != Guid.Empty && guid == Config.NextFocus)
                {
                    ImGui.SetNextWindowFocus();
                    Config.NextFocus = Guid.Empty;
                }

                ImGui.SetNextWindowSizeConstraints(
                    ImGuiHelpers.ScaledVector2(400, 400),
                    ImGui.GetMainViewport().WorkSize
                );

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleColor(ImGuiCol.Border, Vector4.One);
                bool open;
                var flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar;

                if (guid == Guid.Empty)
                {
                    open = ImGui.Begin(name, flags);
                }
                else
                {
                    open = ImGui.Begin(name, ref childWindowVisible, flags);
                }

                ImGui.PopStyleColor();
                ImGui.PopStyleVar();

                if (ImGui.IsWindowDocked())
                {
                    _dockReleased[guid] = 0.0f;
                }

                if (open)
                {
                    if (ImGui.IsWindowFocused())
                    {
                        _focused = guid;

                        if (guid == Config.LibrarySelection && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            View(Config.LibrarySelection);
                        }
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                    _contentAreas.GetValueOrDefault(guid)?.Draw(additionalNode);
                    _timelines.GetValueOrDefault(guid)?.Draw(additionalNode);
                    ImGui.PopStyleVar();
                }

                ImGui.End();

                if (!childWindowVisible)
                {
                    if (guid == Config.LibrarySelection)
                    {
                        DeselectAll();
                    }

                    Close(guid);
                }
            }
        }

        private void DrawMainContent()
        {
            ImGui.Columns(1);

            _libraryPath.Draw();

            ImGui.Spacing();

            var guid = _focused;
            if (guid == Guid.Empty)
            {
                guid = Config.LibrarySelection;
            }

            var node = Library.Find<INode>(guid);
            var micro = node as Micro;

            ImGui.Columns(3);

            ImGui.BeginChildFrame(
                101010,
                new(-1, ImGui.GetContentRegionAvail().Y - (112 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            _library.Draw();

            ImGui.EndChildFrame();

            ImGui.NextColumn();

            ImGui.BeginChildFrame(
                101011,
                new(-1, ImGui.GetContentRegionAvail().Y - (112 * ImGuiHelpers.GlobalScale)),
                ImGuiWindowFlags.NoBackground
            );

            ImGui.PushStyleColor(ImGuiCol.TitleBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Vector4.Zero);

            ImGui.DockSpace(439839, new(-1, -1));

            ImGui.PopStyleColor(2);

            ImGui.EndChildFrame();

            ImGui.NextColumn();

            if (micro != null)
            {
                _regionBar.Draw(micro);
            }

            ImGui.Columns(1, "playback-controls", false);

            _playbackControls.Draw(node);
        }

        private void EnsureUiComponents(Guid guid)
        {
            if (!_contentAreas.ContainsKey(guid))
            {
                _contentAreas[guid] = new ContentArea();
            }

            if (!_timelines.ContainsKey(guid))
            {
                _timelines[guid] = new Timeline();
            }
        }

        private void Update(Framework framework)
        {
            _frameCount++;

            if (_frameCount % 200 == 0)
            {
                var configJson = JsonSerializer.Serialize(Config);
                if (configJson != _previousConfigJson)
                {
                    PluginInterface.SavePluginConfig(Config);
                    _previousConfigJson = configJson;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _framework.Update -= Update;
            }

            base.Dispose(disposing);
        }
    }
}
