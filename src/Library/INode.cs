using System;
using System.Collections.Generic;

namespace Microdancer
{
    public interface INode
    {
        Guid Id { get; }
        string Name { get; }
        string Path { get; }
        public INode? Parent { get; }
        List<INode> Children { get; }
        bool IsReadOnly { get; }
        public void Move(string newPath);
    }
}
