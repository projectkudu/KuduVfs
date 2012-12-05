using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vfs;

namespace vfsxcopy
{
    class Program
    {
        static Semaphore _semaphore = new Semaphore(5, 5);
        static List<Task> _tasks = new List<Task>();
        static int _totalFiles = 0;
        static int _totalBytes = 0;

        static int Main(string[] args)
        {
            try
            {
                DateTime start = DateTime.Now;
                var arg = new CommandArguments(args);
                IFileSystem srcFileSystem = arg.SrcFileSystem;
                FileSystemInfoBase srcFileInfo = GetSourceFileInfo(srcFileSystem, arg.Src);

                IFileSystem dstFileSystem = arg.DstFileSystem;
                FileSystemInfoBase dstFileInfo = GetDestinationFileInfo(dstFileSystem, arg.Dst, srcFileInfo);

                if (srcFileInfo is DirectoryInfoBase)
                {
                    CopyInternal(srcFileSystem, (DirectoryInfoBase)srcFileInfo, dstFileSystem, (DirectoryInfoBase)dstFileInfo);
                }
                else
                {
                    CopyInternalAsync(srcFileSystem, (FileInfoBase)srcFileInfo, dstFileSystem, (FileInfoBase)dstFileInfo).Wait();
                }

                Task.WaitAll(_tasks.ToArray());

                Console.WriteLine();
                Console.WriteLine("{0,12} files copied", _totalFiles.ToString("#,##0"));
                Console.WriteLine("{0,12} bytes copied", _totalBytes.ToString("#,##0"));

                if (srcFileSystem is VfsFileSystem || dstFileSystem is VfsFileSystem)
                {
                    Console.WriteLine("{0,12} secs elapsed", (DateTime.Now -start).TotalSeconds.ToString("#,##0"));
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        static void CopyInternal(IFileSystem srcFileSystem, DirectoryInfoBase srcFileInfo, IFileSystem dstFileSystem, DirectoryInfoBase dstFileInfo)
        {
            foreach (FileSystemInfoBase src in srcFileInfo.GetFileSystemInfos())
            {
                if (src is FileInfoBase)
                {
                    var dst = dstFileSystem.FileInfo.FromFileName(Path.Combine(dstFileInfo.FullName, src.Name));
                    _semaphore.WaitOne();
                    _tasks.Add(CopyInternalAsync(srcFileSystem, (FileInfoBase)src, dstFileSystem, (FileInfoBase)dst).Finally(() => _semaphore.Release()));
                }
                else
                {
                    // recursive
                    var dst = dstFileSystem.DirectoryInfo.FromDirectoryName(Path.Combine(dstFileInfo.FullName, src.Name));
                    CopyInternal(srcFileSystem, (DirectoryInfoBase)src, dstFileSystem, (DirectoryInfoBase)dst);
                }
            }
        }

        static Task CopyInternalAsync(IFileSystem srcFileSystem, FileInfoBase srcFileInfo, IFileSystem dstFileSystem, FileInfoBase dstFileInfo)
        {
            return TaskHelpers.RunSynchronously(() =>
            {
                if (srcFileInfo.Exists && dstFileInfo.Exists && srcFileInfo.LastWriteTimeUtc == dstFileInfo.LastWriteTimeUtc)
                {
                    lock (_semaphore)
                    {
                        Console.WriteLine("          File up-to-date from \"" + srcFileInfo.FullName + "\" to \"" + dstFileInfo.FullName + "\"");
                    }
                }
                else
                {
                    byte[] bytes = srcFileSystem.File.ReadAllBytes(srcFileInfo.FullName);
                    if (dstFileSystem is FileSystem)
                    {
                        EnsureDirectory(Path.GetDirectoryName(dstFileInfo.FullName));
                    }
                    dstFileSystem.File.WriteAllBytes(dstFileInfo.FullName, bytes);

                    if (dstFileSystem is FileSystem)
                    {
                        dstFileInfo.LastWriteTimeUtc = srcFileInfo.LastWriteTimeUtc;
                    }
                    else if (srcFileSystem is FileSystem)
                    {
                        srcFileInfo.LastWriteTimeUtc = dstFileSystem.FileInfo.FromFileName(dstFileInfo.FullName).LastWriteTimeUtc;
                    }

                    lock (_semaphore)
                    {
                        Console.WriteLine("{0,12} bytes copied from \"" + srcFileInfo.FullName + "\" to \"" + dstFileInfo.FullName + "\"", bytes.Length.ToString("#,##0"));
                        ++_totalFiles;
                        _totalBytes += bytes.Length;
                    }
                }
            });
        }

        static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static FileSystemInfoBase GetSourceFileInfo(IFileSystem srcFileSystem, string srcFile)
        {
            if (srcFile.EndsWith("\\"))
            {
                return srcFileSystem.DirectoryInfo.FromDirectoryName(srcFile);
            }

            var srcParent = srcFile.Substring(0, srcFile.LastIndexOf('\\') + 1);
            if (srcParent.Length < "x:\\".Length)
            {
                throw new ArgumentException("Invalid destination!");
            }
            else if (srcParent.Equals("x:\\", StringComparison.OrdinalIgnoreCase))
            {
                return srcFileSystem.DirectoryInfo.FromDirectoryName(srcFile);
            }

            string name = srcFile.Substring(srcFile.LastIndexOf('\\') + 1);
            DirectoryInfoBase parent = srcFileSystem.DirectoryInfo.FromDirectoryName(srcParent);
            FileSystemInfoBase match = parent.GetFileSystemInfos().Where(f => f.Name == name).FirstOrDefault();
            if (match == null)
            {
                throw new FileNotFoundException(srcFile + " does not exists");
            }

            return match;
        }

        static FileSystemInfoBase GetDestinationFileInfo(IFileSystem dstFileSystem, string dstFile, FileSystemInfoBase srcFileInfo)
        {
            if (dstFile.EndsWith("\\"))
            {
                if (srcFileInfo is FileInfoBase)
                {
                    dstFile = Path.Combine(dstFile, srcFileInfo.Name);
                    return dstFileSystem.FileInfo.FromFileName(dstFile);
                }
                else
                {
                    return dstFileSystem.DirectoryInfo.FromDirectoryName(dstFile);
                }
            }

            var dstFileInfo = new FileInfo(dstFile);
            var dstParent = dstFileInfo.Directory;
            if (dstParent.FullName.Length < "x:\\".Length)
            {
                throw new ArgumentException("Invalid destination!");
            }
            else if (dstParent.FullName.Equals("x:\\", StringComparison.OrdinalIgnoreCase))
            {
                if (srcFileInfo is FileInfoBase)
                {
                    dstFile = Path.Combine(dstFile, srcFileInfo.Name);
                    return dstFileSystem.FileInfo.FromFileName(dstFile);
                }
                else
                {
                    return dstFileSystem.DirectoryInfo.FromDirectoryName(dstFile);
                }
            }

            DirectoryInfoBase parent = dstFileSystem.DirectoryInfo.FromDirectoryName(dstParent.FullName);
            FileSystemInfoBase match = parent.GetFileSystemInfos().Where(f => f.Name == dstFileInfo.Name).FirstOrDefault();
            if (match is DirectoryInfoBase)
            {
                if (srcFileInfo is DirectoryInfoBase)
                {
                    return match;
                }
                else
                {
                    return dstFileSystem.FileInfo.FromFileName(Path.Combine(match.FullName, srcFileInfo.Name));
                }
            }
            else if (match is FileInfoBase)
            {
                if (srcFileInfo is DirectoryInfoBase)
                {
                    throw new InvalidOperationException("Cannot xcopy dir to file.");
                }
                else
                {
                    return match;
                }
            }
            else
            {
                if (srcFileInfo is DirectoryInfoBase)
                {
                    throw new DirectoryNotFoundException(dstFileInfo.FullName + " does not exist.");
                }
                else
                {
                    return dstFileSystem.FileInfo.FromFileName(dstFile);
                }
            }
        }


        class CommandArguments
        {
            private string _src;
            private IFileSystem _srcFileSystem;
            private string _dst;
            private IFileSystem _dstFileSystem;
            private string _vfsurl;
            private string _username;
            private string _password;
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
                        if (!String.IsNullOrEmpty(_src) && !String.IsNullOrEmpty(_dst))
                        {
                            throw new ArgumentException("mulitple path arguments!");
                        }

                        if (_src == null)
                        {
                            _src = arg;
                        }
                        else
                        {
                            _dst = arg;
                        }
                    }
                }

                if (_src == null)
                {
                    throw new ArgumentNullException("path", "Missing source path argument.");
                }

                var localFileSystem = new FileSystem();
                var remoteFileSystem = new VfsFileSystem(_vfsurl, _username, _password);

                if (!_src.Contains(":"))
                {
                    _src = Path.Combine(remoteFileSystem.Directory.GetCurrentDirectory(), _src);
                }

                var isVfs = _src.StartsWith("x:", StringComparison.OrdinalIgnoreCase);
                _srcFileSystem = isVfs ? (IFileSystem)remoteFileSystem : localFileSystem;


                if (_dst == null)
                {
                    _dstFileSystem = (_srcFileSystem is VfsFileSystem) ? (IFileSystem)localFileSystem : remoteFileSystem;
                    _dst = _dstFileSystem.Directory.GetCurrentDirectory();
                }
                else
                {
                    if (!_dst.Contains(":"))
                    {
                        _dst = Path.Combine(remoteFileSystem.Directory.GetCurrentDirectory(), _dst);
                    }

                    isVfs = _dst.StartsWith("x:", StringComparison.OrdinalIgnoreCase);
                    _dstFileSystem = isVfs ? (IFileSystem)remoteFileSystem : localFileSystem;
                }
            }

            public string Src { get { return _src; } }

            public IFileSystem SrcFileSystem { get { return _srcFileSystem; } }

            public string Dst { get { return _dst; } }

            public IFileSystem DstFileSystem { get { return _dstFileSystem; } }

            public Dictionary<string, string> Flags { get { return _flags; } }
        }
    }
}
