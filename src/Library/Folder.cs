using System;
using System.Collections.Generic;
using System.IO;

namespace Microdancer
{
    public sealed class Folder : Node
    {
        public Folder(DirectoryInfo dir, List<INode>? children = null) : base(dir)
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