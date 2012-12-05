using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Vfs;

namespace vfsdel
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var arg = new CommandArguments(args);
                var fileSystem = arg.FileSystem;
                string cd = fileSystem.Directory.GetCurrentDirectory();
                string path = Path.Combine(cd, arg.Path);
                var info = new FileInfo(path);
                DirectoryInfoBase parent = fileSystem.DirectoryInfo.FromDirectoryName(info.Directory.FullName);
                FileSystemInfoBase file = parent.GetFileSystemInfos().Where(f => f.Name == info.Name).FirstOrDefault();

                if (file == null)
                {
                    throw new FileNotFoundException(info.FullName + " does not exists.");
                }
                else if (file is DirectoryInfoBase)
                {
                    throw new NotSupportedException("Delete directory is not supported.");
                }

                file.Delete();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
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

                var isVfs = _path == null || !_path.Contains(":") || _path.StartsWith("x:", StringComparison.OrdinalIgnoreCase);
                _fileSystem = isVfs ? (IFileSystem)new VfsFileSystem(_vfsurl, _username, _password) : new FileSystem();
            }

            public string Path { get { return _path ?? String.Empty; } }

            public IFileSystem FileSystem { get { return _fileSystem; } }

            public string VfsUrl { get { return _vfsurl; } }

            public string UserName { get { return _username; } }

            public string Password { get { return _password; } }

            public Dictionary<string, string> Flags { get { return _flags; } }
        }
    }
}
