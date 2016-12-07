using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.ResourceAccess;
using OnlineVideos.Sites.Interfaces;

namespace OnlineVideos.MediaPortal2
{
    public class PlaylistItem : MediaItem
    {
        public PlaylistItem(VideoViewModel videoInfo, string resolvedPlaybackUrl)
            : base(Guid.Empty, new Dictionary<Guid, IList<MediaItemAspect>>
            {
                { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect[]{ new MultipleMediaItemAspect(ProviderResourceAspect.Metadata) }},
                { MediaAspect.ASPECT_ID, new MediaItemAspect[]{ new SingleMediaItemAspect(MediaAspect.Metadata) }},
                { VideoAspect.ASPECT_ID, new MediaItemAspect[]{ new SingleMediaItemAspect(VideoAspect.Metadata) }},
                { OnlineVideosAspect.ASPECT_ID, new MediaItemAspect[]{ new SingleMediaItemAspect(OnlineVideosAspect.Metadata) }},
            })
        {
            SiteName = videoInfo.SiteName;
            VideoInfo = videoInfo.VideoInfo;

            Aspects[OnlineVideosAspect.ASPECT_ID].First().SetAttribute(OnlineVideosAspect.ATTR_SITEUTIL, SiteName);

            ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();

            IList<MultipleMediaItemAspect> providerResourceAspects;
            MediaItemAspect.TryGetAspects(Aspects, ProviderResourceAspect.Metadata, out providerResourceAspects);
            MultipleMediaItemAspect providerResourceAspect = providerResourceAspects.First();

            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, true);
            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);

            if (videoInfo.SiteUtilName == "DownloadedVideo")
            {
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath(resolvedPlaybackUrl).Serialize());
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/unknown");
            }
            else
            {
                Uri uri;
                // Test if the resolved "url" is a real Uri (Sites can provide any content here)
                var isUriSource = Uri.TryCreate(resolvedPlaybackUrl, UriKind.Absolute, out uri);

                var value = isUriSource
                    ? RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize()
                    : RawTokenResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize();
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, value);
                Aspects[OnlineVideosAspect.ASPECT_ID].First().SetAttribute(OnlineVideosAspect.ATTR_LONGURL, value);

                var isBrowser = videoInfo.SiteSettings.Player == PlayerType.Browser;
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE,
                    isBrowser
                        ? WebBrowserVideoPlayer.ONLINEVIDEOSBROWSER_MIMETYPE
                        : OnlineVideosPlayer.ONLINEVIDEOS_MIMETYPE);
            }

            MediaItemAspect.SetAttribute(Aspects, MediaAspect.ATTR_TITLE, videoInfo.Title);

            // TODO: Restore line after story plot was moved back to VideoAspect!
            // MediaItemAspect.SetAttribute(aspects, VideoAspect.ATTR_STORYPLOT, videoInfo.Description);

            DateTime parsedAirDate;
            if (DateTime.TryParse(videoInfo.VideoInfo.Airdate, out parsedAirDate))
                MediaItemAspect.SetAttribute(Aspects, MediaAspect.ATTR_RECORDINGTIME, parsedAirDate);

        }

        public VideoInfo VideoInfo { get; private set; }

        public string SiteName { get; private set; }

        public IInputStreamSite InputStreamSite
        {
            get { return OnlineVideoSettings.Instance.SiteUtilsList[SiteName] as IInputStreamSite; }
        }

        /// <summary>
        /// Returns a resource locator instance for this item.
        /// </summary>
        /// <returns>Resource locator instance or <c>null</c>, if this item doesn't contain a <see cref="ProviderResourceAspect"/>.</returns>
        public override IResourceLocator GetResourceLocator()
        {
            IList<MediaItemAspect> onlineVideoAspect;
            IList<MediaItemAspect> providerAspect;
            if (!_aspects.TryGetValue(OnlineVideosAspect.ASPECT_ID, out onlineVideoAspect))
                return base.GetResourceLocator();

            if (!_aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerAspect))
                return null;
            string systemId = (string)providerAspect.First()[ProviderResourceAspect.ATTR_SYSTEM_ID];
            string resourceAccessorPath = (string)onlineVideoAspect.First()[OnlineVideosAspect.ATTR_LONGURL];
            return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
        }
    }
}
