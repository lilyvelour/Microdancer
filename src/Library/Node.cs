using System;
using System.Collections.Generic;
using System.IO;

namespace Microdancer
{
    public abstract class Node : INode
    {
        public Guid Id { get; protected set; }

        public string Name { get; protected set; }

        public string Path { get; protected set; }

        public List<INode> Children { get; } = new();

        public Node(FileSystemInfo info)
        {
            Id = GenerateId(info.FullName);
            Name = info.FullName;
            Path = info.FullName;
        }

        public static Guid GenerateId(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Guid.Empty;
            }

            return GuidUtility.Create(GuidUtility.UrlNamespace, path ?? "/");
        }
    }
}