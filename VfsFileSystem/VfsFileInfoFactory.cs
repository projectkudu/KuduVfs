using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Vfs
{
    public class VfsFileInfoFactory : IFileInfoFactory
    {
        internal VfsFileSystem _fileSystem;

        private Dictionary<string, VfsFileInfo> _files;

        public VfsFileInfoFactory(VfsFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _files = new Dictionary<string, VfsFileInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public FileInfoBase FromFileName(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            lock (_files)
            {
                VfsFileInfo file;
                if (!_files.TryGetValue(fileInfo.FullName, out file))
                {
                    DirectoryInfoBase info = _fileSystem.DirectoryInfo.FromDirectoryName(fileInfo.Directory.FullName);
                    if (info.Exists)
                    {
                        file = (VfsFileInfo)info.GetFiles().Where(f => f.Name == fileInfo.Name).FirstOrDefault();
                        if (file == null)
                        {
                            file = new VfsFileInfo(_fileSystem, fileInfo);
                        }
                    }
                    else
                    {
                        file = new VfsFileInfo(_fileSystem, fileInfo);
                    }

                    _files[fileInfo.FullName] = file;
                }
                return file;
            }
        }

        internal void UpdateLastWriteTimeUtc(string fileName, DateTime lastWriteTimeUtc)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            lock (_files)
            {
                VfsFileInfo file;
                if (!_files.TryGetValue(fileInfo.FullName, out file))
                {
                    _files[fileInfo.FullName] = file = new VfsFileInfo(_fileSystem, fileInfo);
                }

                file.LastWriteTimeUtc = lastWriteTimeUtc;
                file.MarkExists(true);
            }
        }

        internal void UpdateCache(VfsFileInfo info)
        {
            lock (_files)
            {
                _files[info.FullName] = info;
            }
        }
    }
}
