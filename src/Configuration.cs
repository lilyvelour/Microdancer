using Dalamud.Configuration;
using Dalamud.IoC;
using System;

namespace Microdancer
{
    [Serializable]
    [PluginInterface]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public string LibraryPath { get; set; } = "C:\\FFXIV\\Microdancer";
        public bool WindowVisible { get; set; } = false;
        public Guid LibrarySelection { get; set; }
        public Guid QueueSelection { get; set; }
    }
}
