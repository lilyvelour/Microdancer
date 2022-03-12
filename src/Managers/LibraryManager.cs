using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dalamud.IoC;
using Dalamud.Plugin;

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

        public LibraryManager(DalamudPluginInterface pluginInterface)
        {
            _config = pluginInterface.Configuration();

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
            var libraryPath = new DirectoryInfo(_config.LibraryPath);
            if (!libraryPath.Exists)
            {
                return new List<INode>();
            }

            if (_shouldRebuild)
            {
                _cachedNodes = BuildTree(libraryPath).Children;
                _shouldRebuild = false;
            }

            return _cachedNodes ?? new List<INode>();
        }

        public T? Find<T>(Guid id) where T : INode
        {
            return GetNodes()
                .Select(node => (T?)Traverse(node, n => n.Children).FirstOrDefault(n => n is T && n.Id == id))
                .FirstOrDefault(micro => micro != null);
        }

        public T? Find<T>(string? search) where T : INode
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return default;
            }

            if (Guid.TryParse(search, out var id))
            {
                return Find<T>(id);
            }

            var nodes = GetNodes();

            var node = nodes
                .Select(
                    node =>
                        (T?)Traverse(node, n => n.Children)
                            .FirstOrDefault(
                                n => n is T && Path.Equals(Path.GetFullPath(n.Path), Path.GetFullPath(search))
                            )
                )
                .FirstOrDefault(node => node != null);

            if (node != null)
            {
                return node;
            }

            node = nodes
                .Select(
                    node =>
                        (T?)Traverse(node, n => n.Children)
                            .FirstOrDefault(
                                n => n is T && n.Name.StartsWith(search, true, CultureInfo.InvariantCulture)
                            )
                )
                .FirstOrDefault(node => node != null);

            return node ?? default;
        }

        internal void MarkAsDirty()
        {
            EnsureWatcher();
            _shouldRebuild = true;
        }

        private INode BuildTree(DirectoryInfo dir, INode? parent = null)
        {
            var node = new Folder(dir, parent);

            foreach (var subDir in dir.GetDirectories())
            {
                if (subDir.Name.StartsWith("."))
                {
                    continue;
                }
                node.Children.Add(BuildTree(subDir, node));
            }

            foreach (var microFile in dir.GetFiles("*.micro"))
            {
                if (microFile.Name.StartsWith("."))
                {
                    continue;
                }
                node.Children.Add(new Micro(microFile, node));
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
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    NotifyFilter =
                        NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.Size
                        | NotifyFilters.LastWrite
                        | NotifyFilters.CreationTime
                        | NotifyFilters.Size
                        | NotifyFilters.Attributes
                        | NotifyFilters.Security
                };

                _fileSystemWatcher.Changed += WatcherEvent;
                _fileSystemWatcher.Created += WatcherEvent;
                _fileSystemWatcher.Deleted += WatcherEvent;
                _fileSystemWatcher.Renamed += WatcherEvent;

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
            while (stack.Count > 0)
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
