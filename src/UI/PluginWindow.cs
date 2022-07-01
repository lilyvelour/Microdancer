using System;
using Dalamud.Game.ClientState;

namespace Microdancer
{
    public abstract class PluginWindow : PluginUiBase, IDisposable
    {
        protected ClientState ClientState { get; }

        protected PluginWindow(ClientState clientState)
        {
            ClientState = clientState;
            this.ClientState.Logout += Logout;

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        }

        protected bool _disposedValue;

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
                this.ClientState.Logout -= Logout;
            }

            _disposedValue = true;
        }

        private void Logout(object? _, EventArgs _1)
        {
            Config.WindowVisible = false;
        }

        private void OpenConfigUi()
        {
            Config.WindowVisible = true;
        }
    }
}
