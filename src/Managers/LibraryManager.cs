using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public class LibraryManager : IDisposable
    {
        private bool _disposedValue;

        private readonly Configuration _config;

        private IEnumerable<INode>? _cachedNodes;
        private bool _shouldRebuild;
        private FileSystemWatcher? _fileSystemWatcher;

        public LibraryManager(Configuration config)
        {
            _config = config;

            EnsureWatcher();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
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
                if (_fileSystemWatcher != null)
                {
                    _fileSystemWatcher.Dispose();
                    _fileSystemWatcher = null;
                }
            }

            _disposedValue = true;
        }

        public IEnumerable<INode> GetNodes()
        {
            var configDir = new DirectoryInfo(_config.LibraryPath);
            if (!configDir.Exists)
            {
                return new List<INode>();
            }

            if (_shouldRebuild)
            {
                _cachedNodes = BuildTree(configDir).Children;
                _shouldRebuild = false;
            }

            return _cachedNodes ?? new List<INode>();
        }

        public T? Find<T>(Guid id) where T : INode
        {
            return GetNodes()
                .Select(node =>
                    (T?)Traverse(node, n => n.Children)
                        .FirstOrDefault(n => n is T && n.Id == id))
                .FirstOrDefault(micro => micro != null);
        }

        public T? Find<T>(string search) where T : INode
        {
            var str = System.Text.RegularExpressions.Regex.Unescape(search);

            if (Guid.TryParse(str, out var id))
            {
                return Find<T>(id);
            }

            return GetNodes()
                .Select(node =>
                    (T?)Traverse(node, n => n.Children)
                        .FirstOrDefault(
                            n => n is T && n.Name.StartsWith(search, true, CultureInfo.InvariantCulture)
                        )
                )
                .FirstOrDefault(micro => micro != null);
        }

        internal void MarkAsDirty()
        {
            EnsureWatcher();
            _shouldRebuild = true;
        }

        private INode BuildTree(DirectoryInfo dir)
        {
            var node = new Folder(dir);

            foreach (var subDir in dir.GetDirectories())
            {
                if (subDir.Name.StartsWith("."))
                {
                    continue;
                }
                node.Children.Add(BuildTree(subDir));
            }

            foreach (var microFile in dir.GetFiles("*.micro"))
            {
                if (microFile.Name.StartsWith("."))
                {
                    continue;
                }
                node.Children.Add(new Micro(microFile));
            }

            return node;
        }

        private void EnsureWatcher()
        {
            if (_fileSystemWatcher == null)
            {
                CreateWatcher();
            }
            else if (_fileSystemWatcher.Path != _config.LibraryPath)
            {
                _fileSystemWatcher.Dispose();
                CreateWatcher();
            }
        }

        private void CreateWatcher()
        {
            if (Directory.Exists(_config.LibraryPath))
            {
                _fileSystemWatcher = new FileSystemWatcher(_config.LibraryPath)
                {
                    Filter = "*.micro",
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                };

                _fileSystemWatcher.Changed += new FileSystemEventHandler(WatcherEvent);
                _fileSystemWatcher.Created += new FileSystemEventHandler(WatcherEvent);
                _fileSystemWatcher.Deleted += new FileSystemEventHandler(WatcherEvent);
                _fileSystemWatcher.Renamed += new RenamedEventHandler(WatcherEvent);

                _shouldRebuild = true;
            }
        }

        private void WatcherEvent(object _, FileSystemEventArgs _1)
        {
            MarkAsDirty();
        }

        private static IEnumerable<T> Traverse<T>(T item, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>();
            stack.Push(item);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                {
                    stack.Push(child);
                }
            }
        }
    }
}