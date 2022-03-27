using Dalamud.Configuration;
using Dalamud.IoC;
using System;
using System.Collections.Generic;

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
        public float TimelineZoom { get; set; } = 50.0f;
        public bool IgnoreLooping { get; set; }
        public bool IgnoreAutoCountdown { get; set; }
        public Dictionary<string, bool> SharedContent { get; set; } = new();
    }
}
