using System.IO;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using System.Diagnostics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

namespace Microdancer
{
    [PluginInterface]
    public abstract class PluginUiBase
    {
        protected DalamudPluginInterface PluginInterface { get; }
        protected ClientState ClientState { get; }
        protected Condition Condition { get; }
        protected LibraryManager Library { get; }
        protected MicroManager MicroManager { get; }
        protected Configuration Config { get; }
        protected Theme Theme { get; }

        protected PluginUiBase()
        {
            PluginInterface = Microdancer.PluginInterface;
            ClientState = CustomService.Get<ClientState>();
            Condition = CustomService.Get<Condition>();
            Library = CustomService.Get<LibraryManager>();
            MicroManager = CustomService.Get<MicroManager>();
            Config = PluginInterface.Configuration();
            Theme = new BurgundyTheme(); // TODO: Configurable themes
        }

        protected void OpenNode(INode? node, bool parent = false)
        {
            var path = Config.LibraryPath;
            if (node != null)
            {
                path = parent ? Path.GetDirectoryName(node.Path) ?? path : node.Path;
            }
            using var _ = Process.Start("explorer", $"\"{path}\"");
        }

        protected void RevealNode(INode node)
        {
            using var _ = Process.Start("explorer", $"/select, \"{node.Path}\"");
        }
    }
}
