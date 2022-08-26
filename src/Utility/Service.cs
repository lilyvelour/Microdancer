using System;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Plugin;

namespace Microdancer
{
    public static class Service
    {
        private static readonly HashSet<object> _services = new();
        private static readonly HashSet<IDisposable> _disposables = new();
        private static Configuration? _configuration;

        public class Locator
        {
            internal Locator() { }

            public T Get<T>() where T : class
            {
                return (T)_services.First((svc) => svc is T);
            }

            public object Get(Type type)
            {
                return _services.First((svc) => svc.GetType().IsAssignableFrom(type));
            }
        }

        public static Configuration Configuration(this DalamudPluginInterface pluginInterface)
        {
            return _configuration ??= pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }

        public static void SaveConfiguration(this DalamudPluginInterface pluginInterface)
        {
            pluginInterface.SavePluginConfig(Configuration(pluginInterface));
        }

        public static T? RegisterService<T>(this DalamudPluginInterface _, T? service, bool ignoreDisposable = false)
            where T : class
        {
            if (service != null)
            {
                _services.Add(service);
            }

            if (!ignoreDisposable && service is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }

            return service;
        }

        public static T? CreateService<T>(this DalamudPluginInterface pluginInterface) where T : class
        {
            var service = pluginInterface.Create<T>(new Locator());
            return RegisterService(pluginInterface, service);
        }

        public static object? CreateService(this DalamudPluginInterface pluginInterface, Type type)
        {
            // HACK: Not as risky since this is public code
            var method = typeof(DalamudPluginInterface).GetMethod(nameof(DalamudPluginInterface.Create));
            var generic = method!.MakeGenericMethod(type);
            var service = generic.Invoke(pluginInterface, new object[] { new object[] { new Locator() } });

            return RegisterService(pluginInterface, service);
        }

        public static void DisposeAll()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
    }
}
