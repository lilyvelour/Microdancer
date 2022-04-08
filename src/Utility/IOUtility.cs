using System;
using System.IO;

namespace Microdancer
{
    public static class IOUtility
    {
        public static string SanitizePath(string path, string basePath = "")
        {
            foreach (var invalid in Path.GetInvalidPathChars())
            {
                path = path.Replace(invalid, '_');
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                path = path.Replace(invalid, '_');
            }

            return Path.Combine(basePath, path.TrimEnd('.'));
        }

        public static string MakeUniqueDir(string dir, string name, string first)
        {
            return MakeUniquePath(dir, name, first, Directory.Exists);
        }

        public static string MakeUniqueFile(string dir, string name, string first)
        {
            return MakeUniquePath(dir, name, first, File.Exists);
        }

        private static string MakeUniquePath(string dir, string name, string first, Func<string, bool> exists)
        {
            var i = 2;
            var path = Path.Combine(dir, first);

            while (exists(path) && i < 1000)
            {
                path = Path.Combine(dir, string.Format(name, i));
                i++;
            }

            return path;
        }
    }
}
