using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microdancer
{
    public sealed class Micro : Node
    {
        private IEnumerable<string>? _cache;

        public Micro(FileInfo file) : base(file)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
        }

        public IEnumerable<string> GetBody()
        {
            if (_cache == null)
            {
                _cache = GetBodyImpl();
            }

            return _cache;
        }
        private IEnumerable<string> GetBodyImpl()
        {
            FileStream? fs = null;
            for (int numTries = 0; numTries < 10; numTries++)
            {
                try
                {
                    fs = new(
                        Path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete
                    );

                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                        fs = null;
                    }
                    Thread.Sleep(50);
                }
            }

            if (fs == null)
            {
                yield return string.Empty;
                yield break;
            }

            string? line;
            using StreamReader sr = new(fs!);
            while ((line = sr.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public Micro(FileSystemInfo info) : base(info)
        {
        }
    }
}