using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public class MicrodancerUi : PluginWindow
    {
        private readonly LicenseChecker _license;

        private readonly LibraryPath _libraryPath;
        private readonly DisplayLibrary _library;
        private readonly ContentArea _contentArea;
        private readonly Timeline _timeline;

        public MicrodancerUi(LicenseChecker license)
        {
            _license = license;

            _libraryPath = new LibraryPath();
            _library = new DisplayLibrary();
            _contentArea = new ContentArea();
            _timeline = new Timeline();
        }

        public override void Draw()
        {
            if (Config.WindowVisible)
            {
                Theme.Begin();

                var windowVisible = true;
                var draw = ImGui.Begin(Microdancer.PLUGIN_NAME, ref windowVisible);
                var windowSize = ImGui.GetWindowSize();

                if (!ImGui.IsWindowCollapsed())
                {
                    ImGui.SetWindowSize(Vector2.Max(windowSize, ImGuiHelpers.ScaledVector2(400, 400)));
                }

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
        }

        private void DrawWindowContent()
        {
            if (Config.QueueSelection != Guid.Empty && MicroManager.Current != null)
            {
                Config.QueueSelection = Guid.Empty;
                PluginInterface.SavePluginConfig(Config);
            }

            if (!ClientState.IsLoggedIn)
            {
                ImGui.TextColored(new(1.0f, 0.0f, 0.0f, 1.0f), "Please log in to open Microdancer.");
                return;
            }
            else if (ClientState.LocalPlayer == null || _license.IsValidLicense == null)
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

            ImGui.Separator();

            ImGui.Columns(2);

            _library.Draw();

            ImGui.NextColumn();

            _contentArea.Draw();

            ImGui.Columns(1);

            _timeline.Draw();
        }
    }
}
