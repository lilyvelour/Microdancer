using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer [Burgundy Edition]";

        public string Name => PLUGIN_NAME;

        private readonly HashSet<IDisposable> _disposables = new();

        public Microdancer(DalamudPluginInterface pluginInterface)
        {
            var config = (Configuration)(pluginInterface.GetPluginConfig() ?? new Configuration());

            SetService(pluginInterface.Create<GameManager>());
            SetService(pluginInterface.Create<CPoseManager>());
            SetService(pluginInterface.Create<MicroManager>());
            SetService(pluginInterface.Create<LibraryManager>(config));
            SetService(pluginInterface.Create<PluginWindow>(config));

            var commandTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsSubclassOf(typeof(CommandBase)))
                .ToArray();

            foreach (var type in commandTypes)
            {
                var method = typeof(DalamudPluginInterface).GetMethod(nameof(DalamudPluginInterface.Create));

                var generic = method!.MakeGenericMethod(type);

                var svc = generic.Invoke(pluginInterface, new object[] { new object[] { config } })!;
                SetService(svc, type);
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
                foreach(var disposable in _disposables)
                {
                    disposable?.Dispose();
                }
            }

            _disposedValue = true;
        }

        private T SetService<T>(T obj)
        {
            return (T)SetService(obj!, typeof(T));
        }

        // HACK: This is a big giant hack so that we can use Dalamud's built-in IoC
        private object SetService(object obj, Type t)
        {
            var type = typeof(DalamudPluginInterface).Assembly.GetType("Dalamud.Service`1");
            var genericType = type!.MakeGenericType(t);
            var method = genericType!.GetMethod("Set", new Type[] { t });
            var result = method!.Invoke(null, new object[] { obj! })!;

            if (result is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }

            return result;
        }
    }
}
