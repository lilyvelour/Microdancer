using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microdancer
{
    public sealed class Micro : Node
    {
        private IEnumerable<string>? _cache;
        private DateTime _cacheTime = DateTime.MinValue;

        public Micro(FileInfo file) : base(file)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
        }

        public IEnumerable<string> GetBody()
        {
            if (DateTime.Now - _cacheTime > TimeSpan.FromSeconds(1.5))
            {
                _cache = null;
            }

            if (_cache == null)
            {
                for (var numTries = 0; numTries < 10; numTries++)
                {
                    try
                    {
                        if (File.Exists(Path))
                        {
                            _cache = File.ReadAllLines(Path);
                            _cacheTime = DateTime.Now;
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

        public Micro(FileSystemInfo info) : base(info)
        {
        }
    }
}