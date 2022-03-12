using System;
using System.Collections.Generic;
using System.IO;

namespace Microdancer
{
    public sealed class Folder : Node
    {
        public Folder(DirectoryInfo dir, INode? parent = null, List<INode>? children = null) : base(dir, parent)
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
