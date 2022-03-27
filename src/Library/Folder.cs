using System;
using System.Collections.Generic;
using System.IO;

namespace Microdancer
{
    public sealed class LibraryFolder : Folder
    {
        public LibraryFolder(DirectoryInfo dir, INode? parent = null, List<INode>? children = null)
            : base(dir, parent, children, false)
        {
            Name = "Library";
        }
    }

    public sealed class SharedFolder : Folder
    {
        public SharedFolder(DirectoryInfo dir, INode? parent = null, List<INode>? children = null)
            : base(dir, parent, children, true)
        {
            Name = "Shared with Me";
        }
    }

    public class Folder : Node
    {
        public Folder(DirectoryInfo dir, INode? parent = null, List<INode>? children = null, bool isReadOnly = false)
            : base(dir, parent, isReadOnly)
        {
            Id = GuidUtility.Create(GuidUtility.UrlNamespace, dir.FullName);
            Name = dir.Name;
            Path = dir.FullName;

            if (children != null)
            {
                Children.AddRange(children);
            }
        }
    }
}
