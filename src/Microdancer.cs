﻿using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;
using Microdancer.UI;
using Dalamud.Plugin.Services;
using System.Diagnostics.CodeAnalysis;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer";

        public string Name => PLUGIN_NAME;

        internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        [AllowNull]
        internal static IPluginLog PluginLog { get; private set; }

        public Microdancer(
            IDalamudPluginInterface pluginInterface,
            IPluginLog pluginLog,
            ICommandManager commandManager,
            IChatGui chatGui,
            IFramework framework
        )
        {
            PluginInterface = pluginInterface;
            PluginLog = pluginLog;

            // Dalamud services we have to locate manually go here
            pluginInterface.RegisterService(commandManager, ignoreDisposable: true);
            pluginInterface.RegisterService(chatGui, ignoreDisposable: true);
            pluginInterface.RegisterService(framework, ignoreDisposable: true);

            pluginInterface.CreateService<GameManager>();
            pluginInterface.CreateService<PartyManager>();
            pluginInterface.CreateService<MicroManager>();
            pluginInterface.CreateService<LibraryManager>();
            pluginInterface.CreateService<SharedContentManager>();
            pluginInterface.CreateService<MicrodancerUi>();
            pluginInterface.CreateService<SettingsUi>();

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
