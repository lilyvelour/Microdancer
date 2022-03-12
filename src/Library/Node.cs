using System;
using System.Collections.Generic;
using System.IO;
using IOPath = System.IO.Path;

namespace Microdancer
{
    public abstract class Node : INode, IEquatable<Node>
    {
        public Guid Id { get; protected set; }

        public string Name { get; protected set; }

        public string Path { get; protected set; }

        public INode? Parent { get; }

        public List<INode> Children { get; } = new();

        protected FileSystemInfo FileSystemInfo { get; private set; }

        protected Node(FileSystemInfo info, INode? parent = null)
        {
            FileSystemInfo = info;
            Id = GenerateId(info.FullName);
            Name = info.FullName;
            Path = info.FullName;
            Parent = parent;
        }

        public virtual void Move(string newPath)
        {
            newPath = IOPath.GetFullPath(newPath);

            if (FileSystemInfo is FileInfo file)
            {
                if (!newPath.EndsWith(".micro"))
                {
                    newPath += ".micro";
                }

                File.Move(file.FullName, newPath);
                FileSystemInfo = new FileInfo(newPath);
            }
            else if (FileSystemInfo is DirectoryInfo dir)
            {
                Directory.Move(dir.FullName, newPath);
                FileSystemInfo = new DirectoryInfo(newPath);
            }
        }

        public virtual bool Equals(Node? other)
        {
            if (other == null)
            {
                return false;
            }
            if (Equals(this, other))
            {
                return true;
            }

            if (Id != other.Id)
            {
                return false;
            }
            if (Children.Count != other.Children.Count)
            {
                return false;
            }

            for (var i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].Equals(other.Children[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Node);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        private static Guid GenerateId(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Guid.Empty;
            }

            return GuidUtility.Create(GuidUtility.UrlNamespace, path ?? "/");
        }
    }
}
