using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Vfs
{
    public class VfsDirectoryInfoFactory : IDirectoryInfoFactory
    {
        internal VfsFileSystem _fileSystem;

        private Dictionary<string, VfsDirectoryInfo> _directories;

        public VfsDirectoryInfoFactory(VfsFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _directories = new Dictionary<string, VfsDirectoryInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public DirectoryInfoBase FromDirectoryName(string directoryName)
        {
            string key = directoryName.TrimEnd('\\');
            VfsDirectoryInfo result;
            lock (_directories)
            {
                if (!_directories.TryGetValue(key, out result))
                {
                    _directories[key] = result = new VfsDirectoryInfo(_fileSystem, new DirectoryInfo(directoryName));
                }
            }
            return result;
        }

        internal void UpdateCache(VfsDirectoryInfo info)
        {
            string key = info.FullName.TrimEnd('\\');
            lock (_directories)
            {
                _directories[key] = info;
            }
        }
    }
}
