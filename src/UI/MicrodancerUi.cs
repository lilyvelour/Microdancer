using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using System.Collections.Generic;
using System;

namespace Microdancer
{
    [PluginInterface]
    public class MicrodancerUi : PluginWindow
    {
        private readonly LicenseChecker _license;

        private readonly LibraryPath _libraryPath;
        private readonly DisplayLibrary _library;
        private readonly Dictionary<Guid, ContentArea> _contentAreas = new();
        private readonly Dictionary<Guid, Timeline> _timelines = new();
        private readonly Dictionary<Guid, PlaybackControls> _playbackControls = new();

        public MicrodancerUi(LicenseChecker license)
        {
            _license = license;

            _libraryPath = new LibraryPath();
            _library = new DisplayLibrary();
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
                DrawWindowContent();
            }

            ImGui.End();

            Theme.End();

            if (windowVisible != Config.WindowVisible)
            {
                Config.WindowVisible = windowVisible;
                PluginInterface.SavePluginConfig(Config);
            }
        }

        private void DrawWindowContent()
        {
            if (ClientState.LocalPlayer == null || _license.IsValidLicense == null)
            {
                ImGui.TextColored(new(0.67f, 0.67f, 0.67f, 1.0f), "Please wait....");
                return;
            }
            else if (_license.IsValidLicense == false)
            {
                ImGui.TextColored(
                    new(1.0f, 0.0f, 0.0f, 1.0f),
                    "Microdancer is not currently licensed for this character. Please contact Dance Mom for access!"
                );

                return;
            }

            ImGui.Columns(1);

            _libraryPath.Draw();

            ImGui.Spacing();

            ImGui.Columns(2);

            ImGui.BeginChildFrame(
                101010,
                new(-1, ImGui.GetContentRegionAvail().Y - 112 * ImGuiHelpers.GlobalScale),
                ImGuiWindowFlags.NoBackground
            );

            _library.Draw();

            ImGui.EndChildFrame();

            ImGui.NextColumn();

            ImGui.BeginChildFrame(
                101011,
                new(-1, ImGui.GetContentRegionAvail().Y - 112 * ImGuiHelpers.GlobalScale),
                ImGuiWindowFlags.NoBackground
            );

            var node = Library.Find<INode>(Config.LibrarySelection);

            EnsureUiComponents(Config.LibrarySelection);

            _contentAreas[Config.LibrarySelection].Draw(node);
            _timelines[Config.LibrarySelection].Draw(node);

            for (var i = 0; i < Config.OpenWindows.Count; i++)
            {
                var guid = Config.OpenWindows[i];
                var additionalNode = Library.Find<INode>(guid);

                EnsureUiComponents(guid);

                var windowVisible = true;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
                ImGui.SetNextWindowSizeConstraints(
                    ImGuiHelpers.ScaledVector2(400, 400),
                    ImGui.GetMainViewport().WorkSize
                );
                var open = ImGui.Begin($"{additionalNode?.Name ?? "Home"}##Microdancer", ref windowVisible);
                ImGui.PopStyleVar();

                if (open)
                {
                    ImGui.BeginChildFrame(
                        101011 + (uint)i,
                        new(-1, ImGui.GetContentRegionAvail().Y - 112 * ImGuiHelpers.GlobalScale),
                        ImGuiWindowFlags.NoBackground
                    );

                    _contentAreas[guid].Draw(additionalNode);
                    _timelines[guid].Draw(additionalNode);

                    ImGui.EndChildFrame();

                    _playbackControls[guid].Draw(additionalNode, true);

                    ImGui.End();
                }

                if (!windowVisible)
                {
                    Close(guid);
                }
            }

            ImGui.EndChildFrame();

            ImGui.Columns(1, "playback-controls", false);

            _playbackControls[Config.LibrarySelection].Draw(node, false);
        }

        private void EnsureUiComponents(Guid guid)
        {
            if (!_contentAreas.ContainsKey(guid))
            {
                _contentAreas[guid] = new ContentArea();
                _timelines[guid] = new Timeline();
                _playbackControls[guid] = new PlaybackControls();
            }
        }
    }
}
