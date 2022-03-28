using System;
using System.Collections.Generic;
using System.IO;

namespace Microdancer
{
    public sealed class LibraryFolderRoot : Folder
    {
        public LibraryFolderRoot(DirectoryInfo dir, List<INode>? children = null) : base(dir, null, children, false)
        {
            Name = "Library";
        }
    }

    public sealed class SharedFolderRoot : Folder
    {
        public SharedFolderRoot(DirectoryInfo dir, List<INode>? children = null) : base(dir, null, children, true)
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
