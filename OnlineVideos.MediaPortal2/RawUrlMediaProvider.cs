using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement;

namespace OnlineVideos.MediaPortal2
{
	public class RawUrlMediaProvider : IBaseResourceProvider
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

		protected ResourceProviderMetadata _metadata;

        #endregion

        #region Ctor

        public RawUrlMediaProvider()
        {
			_metadata = new ResourceProviderMetadata(RAW_URL_MEDIA_PROVIDER_ID, "OnlineVideos Url mediaprovider", "Provides Access to Raw Uri", true);
        }

        #endregion

		#region IBaseResourceProvider Member

		public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
        {
			result = null;
			if (!IsResource(path))
				return false;
			else
			{
				result = new RawUrlResourceAccessor(this, path);
				return true;
			}
        }

        public ResourcePath ExpandResourcePathFromString(string pathStr)
        {
            if (IsResource(pathStr))
                return new ResourcePath(new ProviderPathSegment[]
                {
                    new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true), 
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

		#region IResourceProvider Member

		public ResourceProviderMetadata Metadata
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
