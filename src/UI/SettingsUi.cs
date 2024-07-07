using ImGuiNET;
using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Utility;

namespace Microdancer.UI
{
    public class SettingsUi : PluginWindow
    {
        private readonly IClientState _clientState;
        private readonly LibraryPath _libraryPath = new();
        private readonly ServerCredentials _serverCredentials = new();

        public SettingsUi(IClientState clientState)
            : base()
        {
            _clientState = clientState;

            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            _clientState.Logout += Logout;
        }

        public override void Draw()
        {
            if (!Config.SettingsVisible || !_clientState.IsLoggedIn)
            {
                return;
            }

            Theme.Begin();

            var settingsVisible = true;

            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(640, 400), ImGui.GetMainViewport().WorkSize);
            var draw = ImGui.Begin($"{Microdancer.PLUGIN_NAME} Settings", ref settingsVisible, ImGuiWindowFlags.NoDocking);

            if (draw)
            {
                _libraryPath.Draw();

                ImGui.Separator();

                _serverCredentials.Draw();
            }

            ImGui.End();

            Theme.End();

            if (settingsVisible != Config.SettingsVisible)
            {
                Config.SettingsVisible = settingsVisible;
            }
        }

        private void OpenConfigUi()
        {
            Config.SettingsVisible = true;
        }

        private void Logout()
        {
            Config.SettingsVisible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                _clientState.Logout -= Logout;
            }

            base.Dispose(disposing);
        }
    }
}
