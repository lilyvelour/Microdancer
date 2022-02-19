using System;
using System.Collections.Generic;
using Dalamud.Plugin;

namespace Microdancer
{
    public static class CustomService
    {
        private static readonly HashSet<IDisposable> _disposables = new();
        private static Configuration? _configuration;

        public static Configuration Configuration(this DalamudPluginInterface pluginInterface)
        {
            if (_configuration == null)
            {
                _configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            }

            return _configuration;
        }

        public static T Set<T>(T obj)
        {
            return (T)Set(obj!, typeof(T));
        }

        public static T Get<T>()
        {
            return (T)Get(typeof(T));
        }

        public static void DisposeAll()
        {
            foreach(var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }

        // HACK: This is a big giant hack so that we can use Dalamud's built-in IoC
        public static object Set(object obj, Type t)
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

        public static object Get(Type t)
        {
            var type = typeof(DalamudPluginInterface).Assembly.GetType("Dalamud.Service`1");
            var genericType = type!.MakeGenericType(t);
            var method = genericType!.GetMethod("Get");
            return method!.Invoke(null, null)!;
        }
    }
}