using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.MediaManagement;

namespace OnlineVideos.MediaPortal2
{
    public class RawUrlMediaProvider : IBaseMediaProvider
    {
        #region Public constants

        /// <summary>
        /// GUID string for the local filesystem media provider.
        /// </summary>
        protected const string RAW_URL_MEDIA_PROVIDER_ID_STR = "{6DA68FCA-4701-47D7-A0B5-1AA982F0200C}";

        /// <summary>
        /// raw url media provider GUID.
        /// </summary>
        public static Guid RAW_URL_MEDIA_PROVIDER_ID = new Guid(RAW_URL_MEDIA_PROVIDER_ID_STR);

        #endregion

        #region Protected fields

        protected MediaProviderMetadata _metadata;

        #endregion

        #region Ctor

        public RawUrlMediaProvider()
        {
            _metadata = new MediaProviderMetadata(RAW_URL_MEDIA_PROVIDER_ID, "OnlineVideos Url mediaprovider");
        }

        #endregion

        #region IBaseMediaProvider Member

		public IResourceAccessor CreateResourceAccessor(string path)
        {
            if (!IsResource(path))
                throw new ArgumentException(string.Format("The resource described by path '{0}' doesn't exist", path));
            return new RawUrlResourceAccessor(this, path);
        }

        public ResourcePath ExpandResourcePathFromString(string pathStr)
        {
            if (IsResource(pathStr))
                return new ResourcePath(new ProviderPathSegment[]
                {
                    new ProviderPathSegment(_metadata.MediaProviderId, pathStr, true), 
                });
            else
                return null;
        }

        public bool IsResource(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Uri uri = null;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    return !uri.IsFile;
                }
            }
            return false;
        }

        #endregion

        #region IMediaProvider Member

        public MediaPortal.Core.MediaManagement.MediaProviderMetadata Metadata
        {
            get { return _metadata; }
        }

        #endregion

        public static ResourcePath ToProviderResourcePath(string path)
        {
            return ResourcePath.BuildBaseProviderPath(RAW_URL_MEDIA_PROVIDER_ID, path);
        }
    }
}
