using System.IO;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using System.Diagnostics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using System;
using Microsoft.VisualBasic.FileIO;
using System.Linq;

namespace Microdancer
{
    public abstract class PluginUiBase
    {
        protected DalamudPluginInterface PluginInterface { get; }
        protected GameManager GameManager { get; }
        protected LibraryManager Library { get; }
        protected MicroManager MicroManager { get; }
        protected Configuration Config { get; }
        protected Theme Theme { get; }

        protected PluginUiBase()
        {
            var serviceLocator = new Service.Locator();

            PluginInterface = Microdancer.PluginInterface;
            GameManager = serviceLocator.Get<GameManager>();
            Library = serviceLocator.Get<LibraryManager>();
            MicroManager = serviceLocator.Get<MicroManager>();
            Config = PluginInterface.Configuration();
            Theme = new BurgundyTheme(); // TODO: Configurable themes
        }

        protected void Open(string path)
        {
            using var _ = Process.Start("explorer", $"\"{path}\"");
        }

        protected void OpenNode(INode node, bool parent = false)
        {
            var path = parent ? Path.GetDirectoryName(node.Path)! : node.Path;
            Open(path);
        }

        protected void View(INode node)
        {
            View(node.Id);
        }

        protected void View(Guid id)
        {
            Config.View(id);
        }

        protected void Navigate(Guid from, Guid to)
        {
            Config.Navigate(from, to);
        }

        protected void Close(INode node)
        {
            Config.Close(node.Id);
        }

        protected void Close(Guid id)
        {
            Config.Close(id);
        }

        protected void Select(Guid id)
        {
            Config.LibrarySelection = id;
            Config.NextFocus = id;
        }

        protected void Select(INode node)
        {
            Select(node.Id);
        }

        protected void DeselectAll()
        {
            Config.LibrarySelection = Guid.Empty;
            Config.NextFocus = Config.OpenWindows.LastOrDefault();
        }

        protected bool SelectByName(string name)
        {
            var node = Library.Find<Node>(name);
            if (node != null)
            {
                Select(node);
                return true;
            }

            return false;
        }

        protected void ToggleSelect(INode node)
        {
            ToggleSelect(node.Id);
        }

        protected void ToggleSelect(Guid id)
        {
            if (Config.LibrarySelection == id)
            {
                Config.LibrarySelection = Guid.Empty;
            }
            else
            {
                Config.LibrarySelection = id;
            }
        }

        protected void RevealNode(INode node)
        {
            using var _ = Process.Start("explorer", $"/select, \"{node.Path}\"");
        }

        protected void RenameNode(INode node, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return;
                }

                if (node?.IsReadOnly != false)
                {
                    return;
                }

                var path = node.Path;
                var basePath = Path.GetDirectoryName(node.Path) ?? Config.LibraryPath;
                var newPath = Path.Combine(basePath, IOUtility.SanitizePath(newName));

                if (node is Micro)
                {
                    if (!newPath.EndsWith(".micro"))
                    {
                        newPath += ".micro";
                    }

                    File.Move(path, newPath);
                }
                else
                {
                    Directory.Move(path, newPath);
                }

                var newId = Node.GenerateId(newPath);

                if (Config.LibrarySelection == node.Id)
                {
                    Config.LibrarySelection = newId;
                }

                if (Config.SharedItems.Contains(node.Id))
                {
                    Config.Share(newId);
                    Config.Unshare(node.Id);
                }

                if (Config.StarredItems.Contains(node.Id))
                {
                    Config.Star(newId);
                    Config.Unstar(node.Id);
                }

                if (Config.TimelineZoomFactor.TryGetValue(node.Id, out var timelineZoomFactor))
                {
                    Config.TimelineZoomFactor[newId] = timelineZoomFactor;
                    Config.TimelineZoomFactor.Remove(node.Id);
                }
            }
            catch
            {
                // no-op
                // TODO: Error when file exists
            }
        }

        protected void DeleteNode(INode node)
        {
            try
            {
                if (node?.IsReadOnly != false)
                {
                    return;
                }

                // Sanity checks! Never EVER delete these.
                if (node is LibraryFolderRoot || node is SharedFolderRoot)
                {
                    return;
                }

                // We want to send the file to the recycle bin and prompt the user
                if (node is Micro)
                {
                    FileSystem.DeleteFile(node.Path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }
                else
                {
                    FileSystem.DeleteDirectory(node.Path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }

                if (Config.LibrarySelection == node.Id)
                {
                    Config.LibrarySelection = node.Parent?.Id ?? Guid.Empty;
                }

                Config.Close(node.Id);
                Config.Unshare(node.Id);
                Config.Unstar(node.Id);
                Config.TimelineZoomFactor.Remove(node.Id);
            }
            catch
            {
                // no-op
            }
        }

        protected void CreateMicro(string basePath, string name, bool template = true)
        {
            try
            {
                var path = IOUtility.MakeUniqueFile(basePath, $"{name} ({{0}}).micro", $"{name}.micro");
                Directory.CreateDirectory(basePath);
                if (template)
                {
                    File.WriteAllText(path, GetMicroTemplate(name));
                }
                else
                {
                    File.CreateText(path);
                }

                Config.LibrarySelection = Node.GenerateId(path);

                Library.MarkAsDirty();
            }
            catch
            {
                // no-op
            }
        }

        protected void CreateFolder(string basePath, string name)
        {
            try
            {
                var path = IOUtility.MakeUniqueDir(basePath, $"{name} ({{0}})", $"{name}");
                Directory.CreateDirectory(path);
                Library.MarkAsDirty();
            }
            catch
            {
                // no-op
            }
        }

        private string GetMicroTemplate(string name)
        {
            return $@"# =====================================================
# {name}
# Choreography © {DateTime.Now.Year} by {GameManager.PlayerName ?? "[Author]"}
# =====================================================

/autocountdown <wait.0>
/autobusy <wait.0>
/automare <wait.0>

#region :Before
/target <me> <wait.0>
/bm off <wait.0>
/setpose weapon 1 <wait.0>
/setpose stand 1 <wait.0>
/snapchange ""[My Gear Set]"" <wait.2>
#endregion

#region Verse 1
#endregion

#region Chorus 1
#endregion

#region Verse 2
#endregion

#region Chorus 2
#endregion

#region Bridge
#endregion

#region Outro
#endregion";
        }
    }
}
