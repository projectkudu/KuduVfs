using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;

namespace Vfs
{
    //{
    //  "Name": "35MSSharedLib1024.snk",
    //  "Size": 160,
    //  "MTime": "2012-05-17T11:29:56-07:00",
    //  "Mime": "application/octet-stream",
    //  "Href": "http://localhost:20000/vfs/35MSSharedLib1024.snk"
    //}
    public class VfsFileInfo : FileInfoBase
    {
        internal VfsFileSystem _fileSystem;
        private Uri _href;
        private string _name;
        private string _fullName;
        private string _mime;
        private DateTime _mtime;
        private long _size;
        private bool _exists;

        public VfsFileInfo(VfsFileSystem fileSystem, FileInfo info)
        {
            _fileSystem = fileSystem;
            _href = VfsUtils.ToVfsUrl(fileSystem._uri, info);
            _fullName = info.FullName;
            _name = info.Name;
            _mime = VfsUtils.GetMediaType(info.Extension).MediaType;
            _exists = false;

            ((VfsFileInfoFactory)_fileSystem.FileInfo).UpdateCache(this);
        }

        public VfsFileInfo(VfsFileSystem fileSystem, VfsJsonFileSystemInfo json)
        {
            _fileSystem = fileSystem;
            _href = new Uri(json.Href);
            _fullName = VfsUtils.ToXDrive(_href);
            _name = json.Name;
            _mime = json.Mime;
            _mtime = DateTime.Parse(json.MTime).ToUniversalTime();
            _size = json.Size;
            _exists = true;

            ((VfsFileInfoFactory)_fileSystem.FileInfo).UpdateCache(this);
        }


        public override StreamWriter AppendText()
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase CopyTo(string destFileName, bool overwrite)
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase CopyTo(string destFileName)
        {
            throw new NotImplementedException();
        }

        public override Stream Create()
        {
            throw new NotImplementedException();
        }

        public override StreamWriter CreateText()
        {
            throw new NotImplementedException();
        }

        public override void Decrypt()
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase Directory
        {
            get 
            {
                return _fileSystem.DirectoryInfo.FromDirectoryName(Path.GetDirectoryName(FullName.TrimEnd('\\')));
            }
        }

        public override string DirectoryName
        {
            get { throw new NotImplementedException(); }
        }

        public override void Encrypt()
        {
            throw new NotImplementedException();
        }

        public override FileSecurity GetAccessControl(AccessControlSections includeSections)
        {
            throw new NotImplementedException();
        }

        public override FileSecurity GetAccessControl()
        {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override long Length
        {
            get { return _size; }
        }

        public override void MoveTo(string destFileName)
        {
            throw new NotImplementedException();
        }

        public override Stream Open(FileMode mode, FileAccess access, FileShare share)
        {
            throw new NotImplementedException();
        }

        public override Stream Open(FileMode mode, FileAccess access)
        {
            throw new NotImplementedException();
        }

        public override Stream Open(FileMode mode)
        {
            throw new NotImplementedException();
        }

        public override Stream OpenRead()
        {
            throw new NotImplementedException();
        }

        public override StreamReader OpenText()
        {
            throw new NotImplementedException();
        }

        public override Stream OpenWrite()
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase Replace(string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase Replace(string destinationFileName, string destinationBackupFileName)
        {
            throw new NotImplementedException();
        }

        public override void SetAccessControl(FileSecurity fileSecurity)
        {
            throw new NotImplementedException();
        }

        public override FileAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override DateTime CreationTime
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override DateTime CreationTimeUtc
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Delete()
        {
            using (HttpClient client = VfsUtils.NewHttpClient(_href, _fileSystem._creds))
            {
                client.DefaultRequestHeaders.IfMatch.Add(EntityTagHeaderValue.Any);
                client.DeleteAsync(_href.AbsolutePath).Result.EnsureSuccessful();
            }
        }

        public override bool Exists
        {
            get { return _exists; }
        }

        internal void MarkExists(bool exists)
        {
            _exists = exists;
        }

        public override string Extension
        {
            get { throw new NotImplementedException(); }
        }

        public override string FullName
        {
            get { return _fullName; }
        }

        public override DateTime LastAccessTime
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override DateTime LastAccessTimeUtc
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override DateTime LastWriteTime
        {
            get
            {
                return _mtime.ToLocalTime();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override DateTime LastWriteTimeUtc
        {
            get
            {
                return _mtime.ToUniversalTime();
            }
            set
            {
                _mtime = value;
            }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override void Refresh()
        {
            throw new NotImplementedException();
        }
    }
}