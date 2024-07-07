using System;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public abstract class PluginWindow : PluginUiBase, IDisposable
    {
        protected PluginWindow() : base()
        {
            PluginInterface.UiBuilder.Draw += Draw;
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
            }

            _disposedValue = true;
        }
    }
}
