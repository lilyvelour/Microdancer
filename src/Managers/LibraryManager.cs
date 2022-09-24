using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Microdancer
{
    public class LibraryManager : IDisposable
    {
        private bool _disposedValue;

        private readonly DalamudPluginInterface _pluginInterface;
        private readonly ConcurrentBag<INode> _cachedNodes = new();
        private bool _shouldRebuild;
        private bool _isBuilding;
        private FileSystemWatcher? _libraryWatcher;
        private FileSystemWatcher? _sharedFolderWatcher;

        public LibraryManager(DalamudPluginInterface pluginInterface, Service.Locator _)
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

        public IList<INode> GetNodes()
        {
            var nodes = _cachedNodes.ToArray();
            if (_shouldRebuild && !_isBuilding)
            {
                _shouldRebuild = false;
                _isBuilding = true;
                Task.Run(BuildNodes); // Offload this to a worker thread
            }
            return nodes;
        }

        private void BuildNodes()
        {
            try
            {
                PluginLog.Log("[LibraryManager] Building nodes...");

                var config = _pluginInterface.Configuration();

                var libraryPath = new DirectoryInfo(config.LibraryPath);
                var sharedPathName = Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "shared");
                var sharedPath = Directory.CreateDirectory(sharedPathName);

                var library = BuildLibrary(libraryPath, out var starred);
                var sharedWithMe = BuildSharedWithMe(sharedPath);

                _cachedNodes.Clear();
                if (starred.Children.Count > 0)
                {
                    _cachedNodes.Add(starred);
                }
                _cachedNodes.Add(library);
                _cachedNodes.Add(sharedWithMe);

                var ids = new HashSet<Guid>(config.SharedItems.Concat(config.StarredItems));
                var nodes = _cachedNodes.SelectMany(node => Traverse(node, n => n.Children)).ToList();

                foreach (var id in ids)
                {
                    if (nodes.Find(n => n.Id == id) == null)
                    {
                        config.SharedItems.Remove(id);
                        config.StarredItems.Remove(id);
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                _shouldRebuild = true;
            }
            finally
            {
                _isBuilding = false;
            }
        }

        public T? Find<T>(Guid id) where T : INode
        {
            if (id == Guid.Empty)
            {
                return default;
            }

            try
            {
                return GetNodes()
                    .Select(node => (T?)Traverse(node, n => n.Children).FirstOrDefault(n => n is T && n.Id == id))
                    .FirstOrDefault(node => node != null);
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

        internal void MarkAsDirty(bool forceReload = false)
        {
            EnsureWatchers();

            if (forceReload)
            {
                // HACK: This could race
                Task.Run(async () =>
                {
                    while (_isBuilding)
                    {
                        await Task.Delay(10);
                    }
                    _cachedNodes.Clear();

                    _shouldRebuild = true;
                    GetNodes();
                });
            }
        }

        private INode BuildLibrary(DirectoryInfo dir, out INode starred)
        {
            starred = new StarredFolderRoot(dir);
            return BuildTree(dir, null, false, starred);
        }

        private INode BuildSharedWithMe(DirectoryInfo dir)
        {
            return BuildTree(dir, null, true, null);
        }

        private INode BuildTree(DirectoryInfo dir, INode? parent, bool isSharedFolder, INode? starred)
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
                folder.Children.Add(BuildTree(subDir, folder, isSharedFolder, starred));
            }

            foreach (var microFile in dir.GetFiles("*.micro"))
            {
                if (microFile.Name.StartsWith("."))
                {
                    continue;
                }

                var micro = new Micro(microFile, folder, isReadOnly: isSharedFolder);
                folder.Children.Add(micro);

                if (
                    starred != null
                    && _pluginInterface.GetPluginConfig() is Configuration config
                    && config.StarredItems.Contains(micro.Id)
                )
                {
                    starred.Children.Add(new Micro(microFile, starred, isSharedFolder));
                }
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
