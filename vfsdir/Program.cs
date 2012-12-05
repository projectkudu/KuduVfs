using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Vfs;

namespace vfsdir
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var arg = new CommandArguments(args);
                var fileSystem = arg.FileSystem;
                try
                {
                    ListDirectory(fileSystem.DirectoryInfo.FromDirectoryName(arg.Path));
                }
                catch (Exception)
                {
                    ListFile(fileSystem.FileInfo.FromFileName(arg.Path));
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        static void ListFile(FileInfoBase file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException(file.FullName + " does not exist.");
            }

            Console.WriteLine();
            Console.WriteLine(" Directory of {0}", file.Directory.FullName.TrimEnd('\\'));
            Console.WriteLine();
            Console.WriteLine(String.Format("{0} {1,17} {2}", ToDisplayString(file.LastWriteTime), file.Length.ToString("#,##0"), file.Name));
            Console.WriteLine(String.Format("{0,16} File(s) {1,14} bytes", 1, file.Length.ToString("#,##0")));
        }

        static void ListDirectory(DirectoryInfoBase dir)
        {
            FileSystemInfoBase[] children = dir.GetFileSystemInfos();

            Console.WriteLine();
            Console.WriteLine(" Directory of {0}", dir.FullName.TrimEnd('\\'));
            Console.WriteLine();

            foreach (DirectoryInfoBase info in children.Where(d => d is DirectoryInfoBase))
            {
                Console.WriteLine(String.Format("{0}    <DIR>          {1}", ToDisplayString(info.LastWriteTime), info.Name));
            }

            int count = 0;
            long total = 0;
            foreach (FileInfoBase info in children.Where(d => !(d is DirectoryInfoBase)))
            {
                FileInfoBase file = (FileInfoBase)info;
                Console.WriteLine(String.Format("{0} {1,17} {2}", ToDisplayString(info.LastWriteTime), file.Length.ToString("#,##0"), info.Name));
                total += file.Length;
                ++count;
            }

            Console.WriteLine(String.Format("{0,16} File(s) {1,14} bytes", count.ToString("#,##0"), total.ToString("#,##0")));
        }

        static string ToDisplayString(DateTime dt)
        {
            // 12/01/2012  11:14 PM
            return String.Format("{0:00}/{1:00}/{2,4}  {3:00}:{4:00} {5}",
                dt.Month, dt.Day, dt.Year, dt.Hour % 12, dt.Minute, dt.Hour >= 12 ? "PM" : "AM");
        }

        class CommandArguments
        {
            private string _path;
            private string _vfsurl;
            private string _username;
            private string _password;
            private IFileSystem _fileSystem;
            private Dictionary<string, string> _flags = new Dictionary<string, string>();

            public CommandArguments(string[] args)
            {
                _vfsurl = ConfigurationManager.AppSettings["vfsurl"];
                _username = ConfigurationManager.AppSettings["username"];
                _password = ConfigurationManager.AppSettings["password"];

                foreach (string arg in args)
                {
                    if (arg.StartsWith("/"))
                    {
                        _flags[arg] = arg;
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(_path))
                        {
                            throw new ArgumentException("mulitple path arguments!");
                        }

                        _path = arg;
                    }
                }

                var localFileSystem = new FileSystem();
                var remoteFileSystem = new VfsFileSystem(_vfsurl, _username, _password);

                if (_path == null)
                {
                    _path = remoteFileSystem.Directory.GetCurrentDirectory();
                }
                else if (!_path.Contains(":"))
                {
                    _path = System.IO.Path.Combine(remoteFileSystem.Directory.GetCurrentDirectory(), _path);
                }

                var isVfs = !_path.Contains(":") || _path.StartsWith("x:", StringComparison.OrdinalIgnoreCase);
                _fileSystem = isVfs ? (IFileSystem)remoteFileSystem : localFileSystem;
            }

            public string Path { get { return _path; } }

            public IFileSystem FileSystem { get { return _fileSystem; } }

            public Dictionary<string, string> Flags { get { return _flags; } }
        }
    }
}
