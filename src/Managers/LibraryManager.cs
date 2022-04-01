using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Microdancer
{
    [PluginInterface]
    public class LibraryManager : IDisposable
    {
        private bool _disposedValue;

        private readonly DalamudPluginInterface _pluginInterface;

        private readonly List<INode> _cachedNodes = new();
        private bool _shouldRebuild;
        private FileSystemWatcher? _libraryWatcher;
        private FileSystemWatcher? _sharedFolderWatcher;

        public LibraryManager(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;

            EnsureWatchers();
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
                _libraryWatcher?.Dispose();
                _sharedFolderWatcher?.Dispose();
                _libraryWatcher = null;
                _sharedFolderWatcher = null;
            }

            _disposedValue = true;
        }

        public IEnumerable<INode> GetNodes()
        {
            try
            {
                if (_shouldRebuild)
                {
                    var libraryPath = new DirectoryInfo(_pluginInterface.Configuration().LibraryPath);
                    var sharedPathName = Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "shared");
                    var sharedPath = Directory.CreateDirectory(sharedPathName);

                    var library = BuildTree(libraryPath);
                    var sharedWithMe = BuildTree(sharedPath, isSharedFolder: true);

                    _cachedNodes.Clear();
                    _cachedNodes.Add(library);
                    _cachedNodes.Add(sharedWithMe);

                    _shouldRebuild = false;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                _shouldRebuild = true;
            }

            return _cachedNodes.ToArray();
        }

        public T? Find<T>(Guid id) where T : INode
        {
            try
            {
                return GetNodes()
                    .Select(node => (T?)Traverse(node, n => n.Children).FirstOrDefault(n => n is T && n.Id == id))
                    .FirstOrDefault(micro => micro != null);
            }
            catch
            {
                return default;
            }
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
            EnsureWatchers();
            _shouldRebuild = true;
        }

        private INode BuildTree(DirectoryInfo dir, INode? parent = null, bool isSharedFolder = false)
        {
            Folder folder;
            if (parent == null)
            {
                folder = isSharedFolder ? new SharedFolderRoot(dir) : new LibraryFolderRoot(dir);
            }
            else
            {
                folder = new Folder(dir, parent, isReadOnly: isSharedFolder);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                if (subDir.Name.StartsWith("."))
                {
                    continue;
                }
                folder.Children.Add(BuildTree(subDir, folder, isSharedFolder));
            }

            foreach (var microFile in dir.GetFiles("*.micro"))
            {
                if (microFile.Name.StartsWith("."))
                {
                    continue;
                }
                folder.Children.Add(new Micro(microFile, folder, isReadOnly: isSharedFolder));
            }

            return folder;
        }

        private void EnsureWatchers()
        {
            var libraryPath = _pluginInterface.Configuration().LibraryPath;
            var sharedPath = _pluginInterface.SharedFolderPath();

            if (_libraryWatcher == null || _libraryWatcher.Path != libraryPath)
            {
                _libraryWatcher?.Dispose();
                _libraryWatcher = CreateWatcher(libraryPath);
            }
            if (_sharedFolderWatcher == null || _sharedFolderWatcher.Path != sharedPath)
            {
                _sharedFolderWatcher?.Dispose();
                _sharedFolderWatcher = CreateWatcher(sharedPath);
            }
        }

        private FileSystemWatcher? CreateWatcher(string path)
        {
            if (Directory.Exists(path))
            {
                var watcher = new FileSystemWatcher(path)
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

                watcher.Changed += WatcherEvent;
                watcher.Created += WatcherEvent;
                watcher.Deleted += WatcherEvent;
                watcher.Renamed += WatcherEvent;

                _shouldRebuild = true;

                return watcher;
            }

            return null;
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
