using System;
using System.Collections.Generic;

namespace Microdancer
{
    public interface INode
    {
        Guid Id { get; }
        string Name { get; }
        string Path { get; }
        List<INode> Children { get; }
        public void Move(string newPath);
    }
}
