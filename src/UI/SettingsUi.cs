using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;

namespace Microdancer.UI
{
    public class SettingsUi : PluginWindow
    {
        private readonly IClientState _clientState;
        private readonly LibraryPath _libraryPath = new();
        private readonly ServerCredentials _serverCredentials = new();
        private readonly NewFileTemplate _newFileTemplate = new();
        private readonly ThemeSettings _themeSettings = new();
        private readonly Link _link = new();

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
            ImGui.SetNextWindowSizeConstraints(
                ImGuiHelpers.ScaledVector2(720, 240),
                ImGui.GetMainViewport().WorkSize);
            var draw = ImGui.Begin($"{Microdancer.PLUGIN_NAME} Settings", ref settingsVisible, ImGuiWindowFlags.NoDocking);

            if (draw)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.BeginChildFrame(67876, new Vector2(-1, ImGui.GetContentRegionAvail().Y - (20 * ImGuiHelpers.GlobalScale)));
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                _libraryPath.Draw();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                _themeSettings.Draw();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                _newFileTemplate.Draw();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                _serverCredentials.Draw();

                ImGui.EndChild();

                _link.Draw(new()
                {
                    Label = "GitHub",
                    Url = "https://github.com/lilyvelour/microdancer",
                    Tooltip = "Official GitHub project for Microdancer",
                });

                ImGui.SameLine();
                ImGui.TextUnformatted("•");
                ImGui.SameLine();

                _link.Draw(new()
                {
                    Label = "Discord",
                    Url = "https://discord.gg/kN4f5PbCDR",
                    Tooltip = "Official Discord server for Microdancer",
                });

                ImGui.SameLine();
                ImGui.TextUnformatted("•");
                ImGui.SameLine();

                _link.Draw(new()
                {
                    Label = "Ko-fi",
                    Url = "https://ko-fi.com/lily",
                    Tooltip = "Any support is truly appreciated. Thank you!",
                });

#if DEBUG
                ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetContentRegionAvail() - ImGui.CalcTextSize("dev"));
                ImGui.TextUnformatted("dev");
#else
                var version = GetType().Assembly.GetName().Version;
                if (version != null)
                {
                    var ver = $"v{version.ToString()}";
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetContentRegionAvail() - ImGui.CalcTextSize(ver));
                    ImGui.TextUnformatted(ver);
                }
#endif
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

        private void Logout(int _, int _1)
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
