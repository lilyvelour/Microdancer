using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Net.Http.Json;
using Dalamud.Game.ClientState;
using Dalamud.Logging;

namespace Microdancer
{
    public class Microdancer : IDalamudPlugin
    {
        private bool _disposedValue;

        public const string PLUGIN_NAME = "Microdancer [Burgundy Edition]";

        public string Name => PLUGIN_NAME;

        public static bool? LicenseIsValid { get; private set; }

        public Microdancer(DalamudPluginInterface pluginInterface, ClientState clientState)
        {
            if (pluginInterface.GetPluginConfig() == null)
            {
                pluginInterface.SavePluginConfig(new Configuration());
            }

            CustomService.Set(pluginInterface.Create<GameManager>());
            CustomService.Set(pluginInterface.Create<CPoseManager>());
            CustomService.Set(pluginInterface.Create<MicroManager>());
            CustomService.Set(pluginInterface.Create<LibraryManager>());
            CustomService.Set(pluginInterface.Create<PluginWindow>());

            var commandTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsSubclassOf(typeof(CommandBase)))
                .ToArray();

            foreach (var type in commandTypes)
            {
                var method = typeof(DalamudPluginInterface).GetMethod(nameof(DalamudPluginInterface.Create));
                var generic = method!.MakeGenericMethod(type);
                var svc = generic.Invoke(pluginInterface, new object[] { Array.Empty<object>() })!;
                CustomService.Set(svc, type);
            }

            Task.Run(
                async () =>
                {
                    while (!_disposedValue)
                    {
                        var player = clientState.LocalPlayer;

                        if (player == null)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            continue;
                        }

                        try
                        {
                            var playerName = player.Name.TextValue;
                            var playerWorld = player.HomeWorld.GameData?.Name.RawString ?? string.Empty;

                            using var client = new HttpClient();

                            PluginLog.LogVerbose(
                                "Checking license status. name=\"{0}\", world=\"{1}\"",
                                playerName,
                                playerWorld
                            );

                            var response = await client.PostAsJsonAsync(
                                "https://example.com/prod/v1/license",
                                new { name = playerName, world = playerWorld }
                            );

                            if (
                                bool.TryParse(await response.Content.ReadAsStringAsync(), out var licenseIsValid)
                                && licenseIsValid
                            )
                            {
                                PluginLog.LogVerbose("License valid.");
                                LicenseIsValid = true;
                            }
                            else
                            {
                                PluginLog.LogWarning("Microdancer license invalid.");
                                LicenseIsValid = false;
                            }
                        }
                        catch (Exception e)
                        {
                            PluginLog.LogError(e, e.Message);
                            LicenseIsValid = false;
                        }
                        finally
                        {
                            await Task.Delay(
                                LicenseIsValid == false ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(15)
                            );
                        }
                    }
                }
            );
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
                CustomService.DisposeAll();
            }

            _disposedValue = true;
        }
    }
}
