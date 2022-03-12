using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using IOPath = System.IO.Path;

namespace Microdancer
{
    public sealed class Micro : Node
    {
        private string[]? _cache;
        private DateTime _cacheTime = DateTime.MinValue;

        private DateTime? _modifiedTime;

        public Micro(FileInfo file, INode? parent = null) : base(file, parent)
        {
            Name = IOPath.GetFileNameWithoutExtension(file.FullName);
            _modifiedTime = file.LastWriteTime;
        }

        public IEnumerable<string> GetBody()
        {
            if (_cache != null && DateTime.Now - _cacheTime > TimeSpan.FromSeconds(1.5))
            {
                try
                {
                    var fi = new FileInfo(Path);
                    if (fi.LastAccessTime != _modifiedTime)
                    {
                        _cache = null;
                    }
                    else
                    {
                        _cacheTime = DateTime.Now;
                    }
                }
                catch
                {
                    _cache = null;
                }
            }

            if (_cache == null)
            {
                for (var numTries = 0; numTries < 10; numTries++)
                {
                    try
                    {
                        if (File.Exists(Path))
                        {
                            var fi = new FileInfo(Path);
                            _cache = File.ReadAllLines(Path);
                            _cacheTime = DateTime.Now;
                            _modifiedTime = fi.LastWriteTime;
                            break;
                        }
                        else
                        {
                            _cache = null;
                            _cacheTime = DateTime.MinValue;
                        }
                    }
                    catch
                    {
                        _cache = null;
                        _cacheTime = DateTime.MinValue;
                        Thread.Sleep(50);
                    }
                }
            }

            return _cache ?? new[] { string.Empty };
        }

        public Micro(FileSystemInfo info, INode? parent = null) : base(info, parent) { }

        public override bool Equals(Node? other)
        {
            if (base.Equals(other) == false)
            {
                return false;
            }

            return _modifiedTime == ((Micro)other)._modifiedTime;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Node);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), _modifiedTime);
        }
    }
}
