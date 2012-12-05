using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Win32;

namespace Vfs
{
    static class VfsUtils
    {
        const string XDrive = @"x:\";

        public static string ToXDrive(Uri url)
        {
            string path = url.AbsolutePath.TrimStart('/').Replace("/", @"\");
            return Path.Combine(XDrive, path);
        }

        public static Uri ToVfsUrl(Uri uri, FileSystemInfo info)
        {
            string path = info.FullName;
            Debug.Assert(path.StartsWith(XDrive, StringComparison.OrdinalIgnoreCase));
            string relative = path.Substring(XDrive.Length - 1).Replace(@"\", "/").TrimEnd('/');
            if (info is DirectoryInfo)
            {
                return new Uri(uri, relative + "/");
            }
            else
            {
                return new Uri(uri, relative);
            }
        }

        public static HttpClient NewHttpClient(Uri uri, ICredentials creds)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = uri;
            if (creds != null)
            {
                client.SetClientCredentials(creds);
            }
            return client;
        }

        public static DateTime GetLastWriteTimeUtc(string etag)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < etag.Length; ++i)
            {
                byte b = (byte)(ToByte(etag[i]) << 4);
                b += ToByte(etag[++i]);
                bytes.Add(b);
            }

            Debug.Assert(bytes.Count == 16);
            long ticks = BitConverter.ToInt64(bytes.ToArray(), 8);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static byte ToByte(char ch)
        {
            if (ch >= 'a' && ch <= 'f')
            {
                return (byte)((ch - 'a') + 10);
            }
            else
            {
                Debug.Assert(ch >= '0' && ch <= '9');
                return (byte)(ch - '0');
            }
        }

        static MediaTypeHeaderValue _defaultMediaType = MediaTypeHeaderValue.Parse("application/octet-stream");
        static ConcurrentDictionary<string, MediaTypeHeaderValue> _mediatypeMap = new ConcurrentDictionary<string, MediaTypeHeaderValue>(StringComparer.OrdinalIgnoreCase);

        public static MediaTypeHeaderValue GetMediaType(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException("fileExtension");
            }

            return _mediatypeMap.GetOrAdd(fileExtension,
                (extension) =>
                {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(fileExtension))
                    {
                        if (key != null)
                        {
                            string keyValue = key.GetValue("Content Type") as string;
                            MediaTypeHeaderValue mediaType;
                            if (keyValue != null && MediaTypeHeaderValue.TryParse(keyValue, out mediaType))
                            {
                                return mediaType;
                            }
                        }

                        return _defaultMediaType;
                    }
                });
        }

    }
}
