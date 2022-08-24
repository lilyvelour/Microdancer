using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using IOPath = System.IO.Path;

namespace Microdancer
{
    public sealed class SharedContentManager : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly ClientState _clientState;
        private readonly ObjectTable _objectTable;
        private readonly LibraryManager _library;
        private readonly PartyManager _partyManager;

        private bool _disposedValue;

        private const string ENDPOINT = "https://example.com/prod/v1/share";

        public SharedContentManager(
            DalamudPluginInterface pluginInterface,
            ClientState clientState,
            ObjectTable objectTable,
            Service.Locator serviceLocator
        )
        {
            _pluginInterface = pluginInterface;
            _clientState = clientState;
            _objectTable = objectTable;

            _library = serviceLocator.Get<LibraryManager>();
            _partyManager = serviceLocator.Get<PartyManager>();

            Task.Run(() => SharedContentUpdate());
        }

        private async void SharedContentUpdate()
        {
            using var client = new HttpClient();
            var tickRate = TimeSpan.FromSeconds(2.5f);

            while (!_disposedValue)
            {
                try
                {
                    if (!_clientState.IsLoggedIn)
                    {
                        await ClearSharedFolder();
                        await Task.Delay(tickRate);
                        continue;
                    }

                    var player = _clientState.LocalPlayer;
                    if (player == null)
                    {
                        await Task.Delay(tickRate);
                        continue;
                    }

                    var playerName = player.Name.ToString();
                    var playerWorld = player.HomeWorld.GameData?.Name.RawString;

                    var nearby = _objectTable
                        .Where(o => o.ObjectKind == ObjectKind.Player)
                        .Where(o => o.ObjectId != player.ObjectId)
                        .OrderBy(o => Vector3.DistanceSquared(o.Position, player.Position))
                        .Select(o => (PlayerCharacter)o)
                        .Select(pc => $"{pc.Name}@{pc.HomeWorld.GameData?.Name.RawString ?? string.Empty}")
                        .ToHashSet();

                    // var party = _partyManager
                    //     .GetInfoFromParty()
                    //     .Where(p => !(p.Name == playerName && p.World == playerWorld));

                    // foreach (var partyMember in party)
                    // {
                    //     nearby.Add($"{partyMember.Name}@{partyMember.World}");
                    // }

                    var shared = _pluginInterface
                        .Configuration()
                        .SharedItems.Select(id => _library.Find<Micro>(id))
                        .Where(micro => micro != null)
                        .Select(micro => new SharedMicro(micro!, _pluginInterface.Configuration().LibraryPath))
                        .ToArray();

                    var request = new SharedContent
                    {
                        Name = player.Name.ToString(),
                        World = player.HomeWorld.GameData?.Name.RawString ?? string.Empty,
                        Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                        Nearby = nearby.ToArray(),
                        Shared = shared,
                    };

                    var response = await client.PostAsJsonAsync(ENDPOINT, request);

                    if (response.IsSuccessStatusCode)
                    {
                        var pathsToKeep = new HashSet<string>();

                        try
                        {
                            var sharedWithMe = await response.Content.ReadFromJsonAsync<SharedContentResponse>();

                            if (sharedWithMe != null)
                            {
                                var sharedFolder = new DirectoryInfo(_pluginInterface.SharedFolderPath());

                                foreach (var content in sharedWithMe.Content)
                                {
                                    var folderName = $"{content.Name}@{content.World}";

                                    var userFolder = sharedFolder.CreateSubdirectory(folderName);
                                    foreach (var sharedMicro in content.Shared)
                                    {
                                        var fileName = $"{sharedMicro.Name}.micro";
                                        string? dir = null;
                                        string path;
                                        if (!string.IsNullOrWhiteSpace(sharedMicro.Path))
                                        {
                                            dir = IOPath.Combine(userFolder.FullName, sharedMicro.Path);
                                            path = IOPath.Combine(dir, fileName);
                                        }
                                        else
                                        {
                                            path = IOPath.Combine(userFolder.FullName, fileName);
                                        }

                                        var skipWrite = false;

                                        if (dir != null)
                                        {
                                            Directory.CreateDirectory(dir);
                                        }

                                        if (File.Exists(path))
                                        {
                                            var current = await File.ReadAllTextAsync(path);
                                            skipWrite = current == sharedMicro.Body;
                                        }

                                        if (!skipWrite)
                                        {
                                            await File.WriteAllTextAsync(path, sharedMicro.Body);
                                        }

                                        pathsToKeep.Add(path);
                                    }
                                }

                                await ClearSharedFolder(pathsToKeep);
                            }
                        }
                        catch (Exception e)
                        {
                            PluginLog.Warning(e.Message);
                        }
                    }

                    await Task.Delay(tickRate);
                }
                catch
                {
                    await Task.Delay(tickRate);
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool _)
        {
            if (_disposedValue)
            {
                return;
            }

            _disposedValue = true;
        }

        private Task ClearSharedFolder()
        {
            return ClearSharedFolder(new HashSet<string>());
        }

        private async Task ClearSharedFolder(HashSet<string> pathsToKeep, DirectoryInfo? folder = null)
        {
            try
            {
                var canDelete = folder != null;

                folder ??= new DirectoryInfo(_pluginInterface.SharedFolderPath());
                Directory.CreateDirectory(folder.FullName);
                var config = _pluginInterface.Configuration();
                var selectedOrOpen = config.OpenWindows.ToList();
                selectedOrOpen.Add(config.LibrarySelection);

                foreach (var file in folder.EnumerateFiles())
                {
                    if (!pathsToKeep.Contains(file.FullName))
                    {
                        var id = Node.GenerateId(file.FullName);
                        if (!selectedOrOpen.Contains(id))
                        {
                            file.Delete();
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }

                foreach (var dir in folder.EnumerateDirectories())
                {
                    await ClearSharedFolder(pathsToKeep, dir);
                }

                if (canDelete && !folder.EnumerateFileSystemInfos().Any())
                {
                    try
                    {
                        folder.Delete();
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e, e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, e.Message);
            }
        }

        private class SharedContentResponse
        {
            public SharedContent[] Content { get; set; } = Array.Empty<SharedContent>();
        }

        private class SharedContent
        {
            public string Name { get; set; } = string.Empty;
            public string World { get; set; } = string.Empty;
            public long Timestamp { get; set; }
            public string[] Nearby { get; set; } = Array.Empty<string>();
            public SharedMicro[] Shared { get; set; } = Array.Empty<SharedMicro>();
        }

        private class SharedMicro
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;

            public SharedMicro() { }

            public SharedMicro(Micro micro, string libraryPath)
            {
                Name = micro.Name;
                var relPath = IOPath.GetRelativePath(libraryPath, micro.Path);
                Path = relPath != null ? IOPath.GetDirectoryName(relPath) ?? string.Empty : string.Empty;
                Body = string.Join('\n', micro.GetBody());
            }
        }
    }
}
