using System;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public abstract class PluginWindow : PluginUiBase, IDisposable
    {
        protected IClientState ClientState { get; }

        protected PluginWindow(IClientState clientState)
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
                ClientState.Logout -= Logout;
            }

            _disposedValue = true;
        }

        private void Logout()
        {
            Config.WindowVisible = false;
        }

        private void OpenConfigUi()
        {
            Config.WindowVisible = true;
        }
    }
}
