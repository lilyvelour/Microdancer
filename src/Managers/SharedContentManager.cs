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
    [PluginInterface]
    public sealed class SharedContentManager : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly LibraryManager _library;
        private readonly ClientState _clientState;
        private readonly PartyManager _partyManager;
        private readonly ObjectTable _objectTable;

        private readonly CancellationTokenSource _tokenSource = new();
        private bool _disposedValue;

        private const string ENDPOINT = "https://example.com/prod/v1/share";

        public SharedContentManager(
            DalamudPluginInterface pluginInterface,
            LibraryManager libraryManager,
            ClientState clientState,
            PartyManager partyManager,
            ObjectTable objectTable
        )
        {
            _pluginInterface = pluginInterface;
            _library = libraryManager;
            _clientState = clientState;
            _partyManager = partyManager;
            _objectTable = objectTable;

            var cancellationToken = _tokenSource.Token;
            Task.Run(() => SharedContentUpdate(cancellationToken), cancellationToken);
        }

        private async void SharedContentUpdate(CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient();
                var tickRate = TimeSpan.FromSeconds(2.5f);

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        ClearSharedFolder();
                        break;
                    }

                    if (!_clientState.IsLoggedIn)
                    {
                        ClearSharedFolder();
                        await Task.Delay(tickRate, cancellationToken);
                        continue;
                    }

                    var player = _clientState.LocalPlayer;
                    if (player == null)
                    {
                        await Task.Delay(tickRate, cancellationToken);
                        continue;
                    }

                    var nearby = _objectTable
                        .Where(o => o.ObjectKind == ObjectKind.Player)
                        .Where(o => o.ObjectId != player.ObjectId)
                        .Where(o => Vector3.Distance(o.Position, player.Position) < 40.0f)
                        .OrderBy(o => Vector3.DistanceSquared(o.Position, player.Position))
                        .Take(20)
                        .Select(o => (PlayerCharacter)o)
                        .Select(pc => $"{pc.Name}@{pc.HomeWorld.GameData?.Name.RawString ?? string.Empty}")
                        .ToHashSet();

                    foreach (
                        var partyMember in _partyManager
                            .GetInfoFromParty()
                            .Where(
                                p =>
                                    !(
                                        p.Name == player.Name.ToString()
                                        && p.World == player.HomeWorld.GameData?.Name.RawString
                                    )
                            )
                    )
                    {
                        nearby.Add($"{partyMember.Name}@{partyMember.World}");
                    }

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

                    var response = await client.PostAsJsonAsync(
                        ENDPOINT,
                        request,
                        cancellationToken: cancellationToken
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var pathsToKeep = new HashSet<string>();

                        try
                        {
                            var sharedWithMe = await response.Content.ReadFromJsonAsync<SharedContentResponse>(
                                cancellationToken: cancellationToken
                            );

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
                                            var current = await File.ReadAllTextAsync(path, cancellationToken);
                                            skipWrite = current == sharedMicro.Body;
                                        }

                                        if (!skipWrite)
                                        {
                                            await File.WriteAllTextAsync(path, sharedMicro.Body, cancellationToken);
                                        }

                                        pathsToKeep.Add(path);
                                    }
                                }

                                ClearSharedFolder(pathsToKeep);
                            }
                        }
                        catch (Exception e)
                        {
                            PluginLog.Warning(e.Message);
                        }
                    }

                    await Task.Delay(tickRate, cancellationToken);
                }
            }
            catch (TaskCanceledException) { }
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
                if (!_tokenSource.IsCancellationRequested)
                {
                    _tokenSource.Cancel(false);
                }
            }

            _disposedValue = true;
        }

        private void ClearSharedFolder()
        {
            ClearSharedFolder(new HashSet<string>());
        }

        private void ClearSharedFolder(HashSet<string> pathsToKeep, DirectoryInfo? folder = null)
        {
            try
            {
                var canDelete = folder != null;

                folder ??= new DirectoryInfo(_pluginInterface.SharedFolderPath());
                Directory.CreateDirectory(folder.FullName);

                foreach (var file in folder.EnumerateFiles())
                {
                    if (!pathsToKeep.Contains(file.FullName))
                    {
                        file.Delete();
                    }
                }

                foreach (var dir in folder.EnumerateDirectories())
                {
                    ClearSharedFolder(pathsToKeep, dir);
                }

                if (canDelete && !folder.EnumerateFileSystemInfos().Any())
                {
                    try
                    {
                        folder.Delete();
                    }
                    catch { }
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
