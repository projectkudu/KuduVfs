using System;
using System.IO.Abstractions;
using System.Net;

namespace Vfs
{
    public class VfsFileSystem : IFileSystem
    {
        internal Uri _uri;
        internal NetworkCredential _creds;

        private VfsDirectory _directory;
        private IDirectoryInfoFactory _directoryInfoFactory;
        private VfsFile _file;
        private IFileInfoFactory _fileInfoFactory;

        public VfsFileSystem(string url, string username = null, string password = null)
        {
            _uri = new Uri(url);
            _creds = String.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password);
        }

        public DirectoryBase Directory
        {
            get { return _directory ?? (_directory = new VfsDirectory(this)); }
        }

        public IDirectoryInfoFactory DirectoryInfo
        {
            get { return _directoryInfoFactory ?? (_directoryInfoFactory = new VfsDirectoryInfoFactory(this)); }
        }

        public FileBase File
        {
            get { return _file ?? (_file = new VfsFile(this)); }
        }

        public IFileInfoFactory FileInfo
        {
            get { return _fileInfoFactory ?? (_fileInfoFactory = new VfsFileInfoFactory(this)); }
        }

        public PathBase Path
        {
            get { throw new NotImplementedException(); }
        }
    }
}