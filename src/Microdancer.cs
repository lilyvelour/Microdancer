using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer [Burgundy Edition]";

        public string Name => PLUGIN_NAME;

        internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public Microdancer(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;

            CustomService.Set(pluginInterface.Create<LicenseChecker>());

            if (pluginInterface.GetPluginConfig() == null)
            {
                pluginInterface.SavePluginConfig(new Configuration());
            }

            CustomService.Set(pluginInterface.Create<GameManager>());
            CustomService.Set(pluginInterface.Create<PartyManager>());
            CustomService.Set(pluginInterface.Create<CPoseManager>());
            CustomService.Set(pluginInterface.Create<MicroManager>());
            CustomService.Set(pluginInterface.Create<LibraryManager>());
            CustomService.Set(pluginInterface.Create<SharedContentManager>());
            CustomService.Set(pluginInterface.Create<MicrodancerUi>());

            var commandTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsSubclassOf(typeof(CommandBase)))
                .ToArray();

            foreach (var type in commandTypes)
            {
                var method = typeof(DalamudPluginInterface).GetMethod(nameof(DalamudPluginInterface.Create));
                var generic = method!.MakeGenericMethod(type);
                var svc = generic.Invoke(pluginInterface, new object[] { Array.Empty<object>() })!;
                CustomService.Set(svc, type);
            }
        }

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
                CustomService.DisposeAll();
            }

            _disposedValue = true;
        }
    }
}
