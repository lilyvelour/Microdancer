using System;
using System.IO;

namespace Microdancer
{
    public static class IOUtility
    {
        public static string MakeUniqueDir(string dir, string name, string? first = null)
        {
            return MakeUniquePath(dir, name, first, Directory.Exists);
        }

        public static string MakeUniqueFile(string dir, string name, string? first = null)
        {
            return MakeUniquePath(dir, name, first, File.Exists);
        }

        private static string MakeUniquePath(string dir, string name, string? first, Func<string, bool> predicate)
        {
            for (int i = 0; ; ++i)
            {
                var path = Path.Combine(dir, i == 0 && first != null ? first : string.Format(name, i));

                if (!predicate(path))
                    return path;
            }
        }
    }
}
