using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net.Http.Json;
using Dalamud.Logging;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

namespace Microdancer
{
    public sealed class LicenseChecker : IDisposable
    {
        public bool? IsValidLicense { get; private set; }

        private bool _disposedValue;

        private const string ENDPOINT = "https://example.com/prod/v1/license";

        public LicenseChecker(ClientState clientState, Service.Locator _)
        {
            Task.Run(
                async () =>
                {
                    PluginLog.Log("Initializing license checker...");

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
                                "Checking license status. name={0}, world={1}",
                                playerName,
                                playerWorld
                            );

                            var response = await client.PostAsJsonAsync(
                                ENDPOINT,
                                new { name = playerName, world = playerWorld }
                            );

                            if (
                                bool.TryParse(await response.Content.ReadAsStringAsync(), out var licenseIsValid)
                                && licenseIsValid
                            )
                            {
                                PluginLog.LogVerbose("License valid.");
                                IsValidLicense = true;
                            }
                            else
                            {
                                PluginLog.LogWarning("Microdancer license invalid.");
                                IsValidLicense = false;
                            }
                        }
                        catch (Exception e)
                        {
                            PluginLog.LogError(e, e.Message);
                            IsValidLicense = false;
                        }
                        finally
                        {
                            await Task.Delay(
                                IsValidLicense == false ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(15)
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

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                // no-op
            }

            _disposedValue = true;
        }
    }
}
