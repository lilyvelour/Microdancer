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
        public bool IsReadOnly { get; }
        public List<INode> Children { get; } = new();

        protected FileSystemInfo FileSystemInfo { get; private set; }

        protected Node(FileSystemInfo info, INode? parent = null, bool isReadOnly = false)
        {
            FileSystemInfo = info;
            Id = GenerateId(info.FullName);
            Name = info.FullName;
            Path = info.FullName;
            Parent = parent;
            IsReadOnly = isReadOnly;
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
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
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
                Node? child = Children[i] as Node;
                Node? otherChild = other.Children[i] as Node;

                if (child?.Equals(otherChild) != true)
                {
                    return false;
                }
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

        public static bool operator ==(Node? obj1, Node? obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            if (obj1 is null || obj2 is null)
            {
                return false;
            }
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Node? obj1, Node? obj2) => !(obj1 == obj2);

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
