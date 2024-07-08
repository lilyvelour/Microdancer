using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using IOPath = System.IO.Path;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microdancer
{
    public sealed class SharedContentManager : IDisposable
    {
        private readonly IDalamudPluginInterface _pluginInterface;
        private readonly IFramework _framework;
        private readonly IClientState _clientState;
        private readonly IObjectTable _objectTable;
        private readonly LibraryManager _library;
        private readonly PartyManager _partyManager;
        private readonly IPluginLog _pluginLog;

        private bool _disposedValue;

        private string? _playerName;
        private string? _playerWorld;
        private HashSet<string>? _nearby;
        private bool _shouldUpdateNearby;

        public bool Connected { get; private set; }
        public string LastError { get; private set;}

        public SharedContentManager(
            IDalamudPluginInterface pluginInterface,
            IFramework framework,
            IClientState clientState,
            IObjectTable objectTable,
            IPluginLog pluginLog,
            Service.Locator serviceLocator
        )
        {
            _pluginInterface = pluginInterface;
            _framework = framework;
            _clientState = clientState;
            _objectTable = objectTable;
            _pluginLog = pluginLog;

            _library = serviceLocator.Get<LibraryManager>();
            _partyManager = serviceLocator.Get<PartyManager>();

            _framework.Update += UpdateNearby;

            Task.Run(SharedContentUpdate);
        }

        private void UpdateNearby(IFramework framework)
        {
            if (!_shouldUpdateNearby)
            {
                return;
            }

            if (!_clientState.IsLoggedIn || _clientState.LocalPlayer == null)
            {
                _playerName = null;
                _playerWorld = null;
                _nearby = null;
                _shouldUpdateNearby = false;
                return;
            }

            var player = _clientState.LocalPlayer;
            var playerName = player.Name.ToString();
            var playerWorld = player.HomeWorld.GameData?.Name.RawString ?? string.Empty;

            _playerName = playerName;
            _playerWorld = playerWorld;
            _nearby = _objectTable
                .Where(o => o.ObjectKind == ObjectKind.Player)
                .Where(o => o.GameObjectId != player.GameObjectId)
                .OrderBy(o => Vector3.DistanceSquared(o.Position, player.Position))
                .Select(o => (IPlayerCharacter)o)
                .Select(pc => $"{pc.Name}@{pc.HomeWorld.GameData?.Name.RawString ?? string.Empty}")
                .ToHashSet();

            var party = _partyManager
                .GetInfoFromParty()
                .Where(p => !(p.Name == playerName && p.World == playerWorld));

            foreach (var partyMember in party)
            {
                _nearby.Add($"{partyMember.Name}@{partyMember.World}");
            }

            _shouldUpdateNearby = false;
        }

        private async void SharedContentUpdate()
        {
            using var client = new HttpClient();
            var tickRate = TimeSpan.FromSeconds(3.5f);

            while (!_disposedValue)
            {
                try
                {
                    var serverUri = _pluginInterface.Configuration().ServerUri;

                    if (string.IsNullOrWhiteSpace(serverUri) || !_clientState.IsLoggedIn)
                    {
                        await ClearSharedFolder();
                        await Task.Delay(tickRate);
                        continue;
                    }

                    _shouldUpdateNearby = true;

                    while(_shouldUpdateNearby)
                    {
                        await Task.Delay(tickRate);
                    }

                    var playerName = _playerName;
                    var playerWorld = _playerWorld;
                    var nearby = _nearby;
                    if (playerName == null || playerWorld == null || nearby == null)
                    {
                        await Task.Delay(tickRate);
                        continue;
                    }

                    var shared = _pluginInterface
                        .Configuration()
                        .SharedItems.Select(_library.Find<Micro>)
                        .Where(micro => micro != null)
                        .Select(micro => new SharedMicro(micro!, _pluginInterface.Configuration().LibraryPath))
                        .ToArray();

                    var requestContent = new SharedContent
                    {
                        Name = playerName,
                        World = playerWorld,
                        Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                        Nearby = [.. nearby],
                        Shared = shared,
                    };

                    var options = new JsonSerializerOptions {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    var json = JsonSerializer.Serialize(requestContent, options);
                    _pluginLog.Info(json);

                    var request = new HttpRequestMessage(HttpMethod.Post, serverUri)
                    {
                        Content = new StringContent(json),
                    };
                    request.Headers.Authorization =
                        new BasicAuthenticationHeaderValue(
                            _pluginInterface.Configuration().ServerUsername,
                            _pluginInterface.Configuration().ServerPasswordHash);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        Connected = true;
                        LastError = string.Empty;

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
                            Connected = false;
                            LastError = e.Message;
                            _pluginLog.Warning(e.Message);
                        }
                    }
                    else
                    {
                        Connected = false;
                        LastError = $"{(int)response.StatusCode} {response.StatusCode}";
                    }

                    await Task.Delay(tickRate);
                }
                catch (Exception e)
                {
                    Connected = false;
                    _pluginLog.Warning(e.Message);
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

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _framework.Update -= UpdateNearby;
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
                        _pluginLog.Error(e, e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                _pluginLog.Error(e, e.Message);
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
