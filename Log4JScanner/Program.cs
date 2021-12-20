using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Log4JScanner
{
    class Program
    {
        public const string DEFAULT_ROOT = "c:\\";
        public const string DEFAULT_MASK = "*log4j.*";
        static void Main(string[] args)
        {
            var root = args.Length > 1 ? (args[0]).Trim() : DEFAULT_ROOT;
            var searchMask = args.Length > 2 ? (args[1]).Trim() : DEFAULT_MASK;
            var pathToFolder = Path.Combine(root);
            var fileSystem = new FileSystemEnumerable(pathToFolder, searchMask);
            long counter = 0;

            foreach (FileSystemInfo file in fileSystem)
            {
                Console.WriteLine(file.FullName);
                counter += 1;
            }

            Console.WriteLine($"Scan finished, found {counter} files from log4j, please check the logs");
        }
    }

    public class FileSystemEnumerable : IEnumerable<FileSystemInfo>
    {
        private readonly DirectoryInfo _root;
        private readonly string _pattern;

        public FileSystemEnumerable(string root, string pattern) : this(new DirectoryInfo(root), pattern)
        {
        }

        public FileSystemEnumerable(DirectoryInfo root, string pattern)
        {
            _root = root;
            _pattern = pattern;
        }

        public IEnumerator<FileSystemInfo> GetEnumerator()
        {
            if (_root == null || !_root.Exists)
            {
                yield break;
            }

            IEnumerable<FileSystemInfo> matches = new List<FileSystemInfo>();
            try
            {
                matches = matches.Concat(_root.EnumerateFileSystemInfos(_pattern, SearchOption.TopDirectoryOnly));
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Unable to access '{_root.FullName}'. Skipping...");
                yield break;
            }
            catch (PathTooLongException)
            {
                Console.Error.WriteLine($@"Could not process path '{_root.Parent.FullName}\{_root.Name}'.");
                yield break;
            }
            catch (IOException)
            {
                Console.Error.WriteLine($@"Could not process path (check SymlinkEvaluation rules)'{_root.Parent.FullName}\{ _root.Name}'.");
                yield break;
            }

            foreach (var file in matches)
            {
                yield return file;
            }

            foreach (var dir in _root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var files = new FileSystemEnumerable(dir, _pattern);
                foreach (var file in files)
                {
                    _counter += 1;
                    yield return file;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
