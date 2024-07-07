using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;
using Microdancer.UI;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer";

        public string Name => PLUGIN_NAME;

        internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        public Microdancer(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IChatGui chatGui,
            IFramework framework
        )
        {
            PluginInterface = pluginInterface;

            // Dalamud services we have to locate manually go here
            pluginInterface.RegisterService(commandManager, ignoreDisposable: true);
            pluginInterface.RegisterService(chatGui, ignoreDisposable: true);
            pluginInterface.RegisterService(framework, ignoreDisposable: true);

            var licenseChecker = pluginInterface.CreateService<LicenseChecker>();

            pluginInterface.CreateService<AudioManager>();
            pluginInterface.CreateService<GameManager>();
            pluginInterface.CreateService<PartyManager>();
            pluginInterface.CreateService<MicroManager>();
            pluginInterface.CreateService<LibraryManager>();
            pluginInterface.CreateService<MidiManager>();
            pluginInterface.CreateService<SharedContentManager>();
            pluginInterface.CreateService<MicrodancerUi>();

            var commandTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsSubclassOf(typeof(CommandBase)))
                .ToArray();

            foreach (var type in commandTypes)
            {
                pluginInterface.CreateService(type);
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
                PluginInterface.SaveConfiguration();
                Service.DisposeAll();
            }

            _disposedValue = true;
        }
    }
}
