using System.IO;
using Dalamud.Plugin;

namespace Microdancer
{
    public static class DalamudPluginExtensions
    {
        public static string SharedFolderPath(this DalamudPluginInterface pluginInterface) =>
            Path.Combine(pluginInterface.GetPluginConfigDirectory(), "shared");
    }
}
