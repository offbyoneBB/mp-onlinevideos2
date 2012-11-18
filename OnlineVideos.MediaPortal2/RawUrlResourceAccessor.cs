using System;
using System.Linq;
using MediaPortal.Common.ResourceAccess;

namespace OnlineVideos.MediaPortal2
{
    /// <summary>
	/// Simple <see cref="INetworkResourceAccessor"/> implementation that handles a raw url.
	/// Bound to the <see cref="RawUrlMediaProvider"/>.
    /// </summary>
	public class RawUrlResourceAccessor : INetworkResourceAccessor
	{
		protected string rawUrl = string.Empty;

		public RawUrlResourceAccessor(string url)
		{
			rawUrl = url;
		}

		public string URL
		{
			get { return rawUrl; }
		}

		public ResourcePath CanonicalLocalResourcePath
		{
			get { return ResourcePath.BuildBaseProviderPath(RawUrlMediaProvider.RAW_URL_MEDIA_PROVIDER_ID, RawUrlMediaProvider.ToProviderResourcePath(rawUrl).Serialize()); }
		}

		public IResourceAccessor Clone()
		{
			return new RawUrlResourceAccessor(rawUrl);
		}

		public IResourceProvider ParentProvider
		{
			get { throw new NotImplementedException(); }
		}

		public string Path
		{
			get { return ResourcePath.BuildBaseProviderPath(RawUrlMediaProvider.RAW_URL_MEDIA_PROVIDER_ID, rawUrl).Serialize(); }
		}

		public string ResourceName
		{
			get { return new Uri(rawUrl).Segments.Last(); }
		}

		public string ResourcePathName
		{
			get { return rawUrl; }
		}

		public void Dispose()
		{
			// nothing to free
		}
	}
}
