using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Abstractions;
using Vfs;

namespace vfsmkdir
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var arg = new CommandArguments(args);
                var fileSystem = arg.FileSystem;
                var info = fileSystem.DirectoryInfo.FromDirectoryName(arg.Path);
                if (!info.Exists)
                {
                    info.Create();
                }
                else
                {
                    Console.WriteLine("\"" + info.FullName + "\" already exists.");
                }
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

                if (_path == null)
                {
                    throw new ArgumentNullException("path", "Missing path argument.");
                }

                var localFileSystem = new FileSystem();
                var remoteFileSystem = new VfsFileSystem(_vfsurl, _username, _password);

                if (!_path.Contains(":"))
                {
                    _path = System.IO.Path.Combine(remoteFileSystem.Directory.GetCurrentDirectory(), _path);
                }

                var isVfs = _path.StartsWith("x:", StringComparison.OrdinalIgnoreCase);
                _fileSystem = isVfs ? (IFileSystem)remoteFileSystem : localFileSystem;
            }

            public string Path { get { return _path; } }

            public IFileSystem FileSystem { get { return _fileSystem; } }

            public Dictionary<string, string> Flags { get { return _flags; } }
        }
    }
}