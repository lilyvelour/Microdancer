﻿using Dalamud.Configuration;
using Dalamud.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public float TimelineZoom { get; set; } = 50.0f;
        public bool IgnoreLooping { get; set; }
        public bool IgnoreAutoCountdown { get; set; }
        public List<Guid> OpenWindows { get; set; } = new();
        public List<Guid> SharedItems { get; set; } = new();
        public List<Guid> StarredItems { get; set; } = new();

        public void View(Guid item)
        {
            OpenWindows = OpenWindows.Distinct().ToList();
            OpenWindows.Add(item);
            OpenWindows.Remove(Guid.Empty);
        }

        public void Close(Guid item)
        {
            OpenWindows = OpenWindows.Distinct().ToList();
            OpenWindows.Remove(item);
            OpenWindows.Remove(Guid.Empty);
        }

        public void Share(Guid item)
        {
            SharedItems.Add(item);
            SharedItems = SharedItems.Distinct().ToList();
        }

        public void Unshare(Guid item)
        {
            SharedItems.Remove(item);
        }

        public void Star(Guid item)
        {
            StarredItems.Add(item);
            StarredItems = StarredItems.Distinct().ToList();
        }

        public void Unstar(Guid item)
        {
            StarredItems.Remove(item);
        }
    }
}
