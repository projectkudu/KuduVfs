using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;

namespace Vfs
{
    //{
    //  "Name": "TestMvcApp",
    //  "Size": 0,
    //  "MTime": "2012-03-01T19:56:45.6824399-08:00",
    //  "Mime": "inode/directory",
    //  "Href": "http://localhost:20000/vfs/TestMvcApp/"
    //}    
    public class VfsDirectoryInfo : DirectoryInfoBase
    {
        internal VfsFileSystem _fileSystem;
        private Uri _href;
        private string _fullName;
        private string _mime;
        private DateTime _mtime;
        private FileSystemInfoBase[] _infos;
        private string _name;
        private Nullable<bool> _exists;

        public VfsDirectoryInfo(VfsFileSystem fileSystem, DirectoryInfo info)
        {
            _fileSystem = fileSystem;
            _href = VfsUtils.ToVfsUrl(_fileSystem._uri, info);
            _name = info.Name;
            _fullName = info.FullName;
            _mime = VfsDirectory.Mime;
            _mtime = DateTime.MinValue;

            ((VfsDirectoryInfoFactory)_fileSystem.DirectoryInfo).UpdateCache(this);
        }

        public VfsDirectoryInfo(VfsFileSystem fileSystem, VfsJsonFileSystemInfo json)
        {
            _fileSystem = fileSystem;
            _href = new Uri(json.Href);
            _name = json.Name;
            _fullName = VfsUtils.ToXDrive(_href);
            _mime = json.Mime;
            _mtime = DateTime.Parse(json.MTime).ToUniversalTime();
            _exists = true;

            ((VfsDirectoryInfoFactory)_fileSystem.DirectoryInfo).UpdateCache(this);
        }

        public override void Create(DirectorySecurity directorySecurity)
        {
            throw new NotImplementedException();
        }

        public override void Create()
        {
            Uri uri = new Uri(_href, ".empty");
            EntityTagHeaderValue etag;
            using (HttpClient client = VfsUtils.NewHttpClient(uri, _fileSystem._creds))
            {
                var content = new StringContent(String.Empty);
                HttpResponseMessage response = client.PutAsync(uri.AbsolutePath, content).Result;
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    response.EnsureSuccessful();
                }

                etag = response.Headers.ETag;
            }

            using (HttpClient client = VfsUtils.NewHttpClient(uri, _fileSystem._creds))
            {
                client.DefaultRequestHeaders.IfMatch.Add(EntityTagHeaderValue.Any);

                HttpResponseMessage response = client.DeleteAsync(uri.AbsolutePath).Result;
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    response.EnsureSuccessful();
                }
            }
        }

        public override DirectoryInfoBase CreateSubdirectory(string path, DirectorySecurity directorySecurity)
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase CreateSubdirectory(string path)
        {
            throw new NotImplementedException();
        }

        public override void Delete(bool recursive)
        {
            throw new NotImplementedException();
        }

        public override DirectorySecurity GetAccessControl(AccessControlSections includeSections)
        {
            throw new NotImplementedException();
        }

        public override DirectorySecurity GetAccessControl()
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase[] GetDirectories(string searchPattern)
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase[] GetDirectories()
        {
            return GetFileSystemInfosInternal().Where(f => f is DirectoryInfoBase).Select(f => (DirectoryInfoBase)f).ToArray();
        }

        public override FileSystemInfoBase[] GetFileSystemInfos(string searchPattern)
        {
            throw new NotImplementedException();
        }

        public override FileSystemInfoBase[] GetFileSystemInfos()
        {
            return GetFileSystemInfosInternal();
        }

        public override FileInfoBase[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase[] GetFiles(string searchPattern)
        {
            throw new NotImplementedException();
        }

        public override FileInfoBase[] GetFiles()
        {
            return GetFileSystemInfosInternal().Where(f => f is FileInfoBase).Select(f => (FileInfoBase)f).ToArray();
        }

        public override void MoveTo(string destDirName)
        {
            throw new NotImplementedException();
        }

        public override DirectoryInfoBase Parent
        {
            get 
            {
                return _fileSystem.DirectoryInfo.FromDirectoryName(Path.GetDirectoryName(FullName.TrimEnd('\\')));
            }
        }

        public override DirectoryInfoBase Root
        {
            get { throw new NotImplementedException(); }
        }

        public override void SetAccessControl(DirectorySecurity directorySecurity)
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
            throw new NotImplementedException();
        }

        public override bool Exists
        {
            get 
            {
                if (!_exists.HasValue)
                {
                    try
                    {
                        _exists = _infos != null || this.Parent.GetDirectories().Any(d => d.Name == Name);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        _exists = false;
                    }
                }

                return _exists.Value;
            }
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
                throw new NotImplementedException();
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

        private FileSystemInfoBase[] GetFileSystemInfosInternal()
        {
            if (_infos == null)
            {
                using (var client = VfsUtils.NewHttpClient(_href, _fileSystem._creds))
                {
                    using (HttpResponseMessage response = client.GetAsync(_href.AbsolutePath).Result)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new DirectoryNotFoundException(this.FullName + " does not exists.");
                        }

                        response.EnsureSuccessful();

                        if (response.Content.Headers.ContentType.MediaType != "application/json")
                        {
                            throw new IOException("The directory name is invalid.");
                        }

                        var array = response.Content.ReadAsAsync<VfsJsonFileSystemInfo[]>().Result;
                        _infos = array.Select(json =>
                        {
                            FileSystemInfoBase info;
                            if (json.Mime == "inode/directory")
                            {
                                info = new VfsDirectoryInfo(_fileSystem, json);
                            }
                            else
                            {
                                info = new VfsFileInfo(_fileSystem, json);
                            }
                            return info;
                        }).ToArray();
                    }
                }
            }

            return _infos;
        }
    }
}