using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace OnlineVideos.MediaPortal2
{
    /// <summary>
    /// ILocalFsResourceAccessor implementation is a hack to fool the VideoPlayer into playing an Url
    /// </summary>
    public class RawUrlResourceAccessor : IResourceAccessor, ILocalFsResourceAccessor
    {
        #region Protected fields

        protected RawUrlMediaProvider provider;
        protected string rawUrl = "";

        #endregion

        #region Ctor

        public RawUrlResourceAccessor(RawUrlMediaProvider provider, string url)
        {
            this.provider = provider;
            this.rawUrl = url;
        }

        #endregion

        #region IResourceAccessor Member

		public bool Exists { get { return true; } }

        public bool IsFile
        {
            get { return false; }
        }

        public DateTime LastChanged
        {
            get { return DateTime.Now; }
        }

        public ResourcePath LocalResourcePath
        {
            get { return ResourcePath.BuildBaseProviderPath(RawUrlMediaProvider.RAW_URL_MEDIA_PROVIDER_ID, rawUrl); }
        }

        public System.IO.Stream OpenRead()
        {
            return null;
        }

        public System.IO.Stream OpenWrite()
        {
            return null;
        }

        public IMediaProvider ParentProvider
        {
            get { return provider; }
        }

        public void PrepareStreamAccess()
        {
            // Nothing to do
        }

        public string ResourceName
        {
            get { return new Uri(rawUrl).Segments.Last(); }
        }

        public string ResourcePathName
        {
            get { return rawUrl; }
        }

        public long Size
        {
            get { return -1; }
        }

        #endregion

        #region IDisposable Member

        public void Dispose() { }

        #endregion

        #region ILocalFsResourceAccessor Member

        public string LocalFileSystemPath
        {
            get { return rawUrl; }
        }

        #endregion

        #region IFileSystemResourceAccessor Member

        public bool ResourceExists(string path)
        {
            return true;
        }

        public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
        {
            return null;
        }

        public ICollection<IFileSystemResourceAccessor> GetFiles()
        {
            return null;
        }

        public IResourceAccessor GetResource(string path)
        {
            return null;
        }

        public bool IsDirectory
        {
            get { return false; }
        }

        #endregion
    }
}
