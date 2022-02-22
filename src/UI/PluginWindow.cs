using System;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public abstract class PluginWindow : PluginUiBase, IDrawable, IDisposable
    {
        protected PluginWindow()
        {
            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            ClientState.Logout += Logout;
        }

        private bool _disposedValue;

        public abstract void Draw();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                PluginInterface.UiBuilder.Draw -= Draw;
                PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                ClientState.Logout -= Logout;
            }

            _disposedValue = true;
        }

        private void Logout(object? _, EventArgs _1)
        {
            Config.WindowVisible = false;
            PluginInterface.SavePluginConfig(Config);
        }

        private void OpenConfigUi()
        {
            Config.WindowVisible = true;
            PluginInterface.SavePluginConfig(Config);
        }
    }
}
