using ImGuiNET;
using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Utility;

namespace Microdancer.UI
{
    public class SettingsUi : PluginWindow
    {
        private readonly IClientState _clientState;

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

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));

            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(400, 400), ImGui.GetMainViewport().WorkSize);
            var draw = ImGui.Begin($"{Microdancer.PLUGIN_NAME} Settings", ref settingsVisible, ImGuiWindowFlags.NoDocking);
            ImGui.PopStyleVar();

            if (draw)
            {
                // TODO: Configuration UI
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
