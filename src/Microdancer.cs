using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;
using Microdancer.UI;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer [Burgundy Edition]";

        public string Name => PLUGIN_NAME;

        internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public Microdancer(
            DalamudPluginInterface pluginInterface,
            CommandManager commandManager,
            ChatGui chatGui,
            Framework framework
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
            pluginInterface.CreateService<CPoseManager>();
            pluginInterface.CreateService<MicroManager>();
            pluginInterface.CreateService<LibraryManager>();
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
                Service.DisposeAll();
            }

            _disposedValue = true;
        }
    }
}
