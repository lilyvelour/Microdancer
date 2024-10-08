﻿using Dalamud.Configuration;
using Dalamud.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Microdancer
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public string LibraryPath { get; set; } = string.Empty;
        public bool WindowVisible { get; set; } = false;
        public bool SettingsVisible { get; set; } = false;
        public Guid LibrarySelection { get; set; }
        public bool IgnoreLooping { get; set; }
        public bool IgnoreAutoCountdown { get; set; }
        public List<Guid> OpenWindows { get; set; } = [];
        public List<Guid> SharedItems { get; set; } = [];
        public List<Guid> StarredItems { get; set; } = [];
        public string ServerUri { get; set; } = string.Empty;
        public string ServerUsername { get; set; } = string.Empty;
        public string ServerPasswordHash { get; set; } = string.Empty;
        public string ServerPasswordPlaceholder { get; set; } = string.Empty;
        public Vector3 UiColor { get; set; } = new Vector3(0.89411765f, 0.0f, 0.06666667f);
        public Dictionary<Guid, float> TimelineZoomFactor { get; set; } = [];
        public Guid NextFocus { get; set; }

        public void View(Guid item)
        {
            OpenWindows.Remove(Guid.Empty);

            if (item == Guid.Empty)
            {
                return;
            }

            OpenWindows = OpenWindows.Distinct().ToList();
            OpenWindows.Remove(item);
            OpenWindows.Add(item);
            NextFocus = item;
        }

        public void Navigate(Guid from, Guid to)
        {
            if (to == Guid.Empty)
            {
                return;
            }

            var fromIndex = OpenWindows.IndexOf(from);
            var fromExists = fromIndex >= 0;
            var toExists = OpenWindows.Contains(to);

            if (fromExists && !toExists)
            {
                OpenWindows[fromIndex] = to;
            }
            else
            {
                if (fromExists)
                {
                    Close(from);
                }

                View(to);
            }

            if (LibrarySelection == from)
            {
                LibrarySelection = to;
            }

            NextFocus = to;
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
