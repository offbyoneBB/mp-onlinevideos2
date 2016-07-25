using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.ResourceAccess;

namespace OnlineVideos.MediaPortal2
{
    public class PlaylistItem : MediaItem
    {
        public PlaylistItem(VideoViewModel videoInfo, string resolvedPlaybackUrl)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>
            {
                { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
                { MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
                { VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) },
                { OnlineVideosAspect.ASPECT_ID, new MediaItemAspect(OnlineVideosAspect.Metadata) },
            })
        {
            SiteName = videoInfo.SiteName;

            Aspects[OnlineVideosAspect.ASPECT_ID].SetAttribute(OnlineVideosAspect.ATTR_SITEUTIL, SiteName);

            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
            if (videoInfo.SiteUtilName == "DownloadedVideo")
            {
                Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath(resolvedPlaybackUrl).Serialize());
                Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/unknown");
            }
            else
            {
                Uri uri;
                // Test if the resolved "url" is a real Uri (Sites can provide any content here)
                var isUriSource = Uri.TryCreate(resolvedPlaybackUrl, UriKind.Absolute, out uri);

                var value = isUriSource
                    ? RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize()
                    : RawTokenResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize();
                Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, value);
                Aspects[OnlineVideosAspect.ASPECT_ID].SetAttribute(OnlineVideosAspect.ATTR_LONGURL, value);

                var isBrowser = videoInfo.SiteSettings.Player == PlayerType.Browser;
                Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE,
                    isBrowser
                        ? WebBrowserVideoPlayer.ONLINEVIDEOSBROWSER_MIMETYPE
                        : OnlineVideosPlayer.ONLINEVIDEOS_MIMETYPE);
            }

            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, videoInfo.Title);

            Aspects[VideoAspect.ASPECT_ID].SetAttribute(VideoAspect.ATTR_STORYPLOT, videoInfo.Description);

            DateTime parsedAirDate;
            if (DateTime.TryParse(videoInfo.VideoInfo.Airdate, out parsedAirDate))
                Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_RECORDINGTIME, parsedAirDate);

        }

        public string SiteName { get; private set; }

        /// <summary>
        /// Returns a resource locator instance for this item.
        /// </summary>
        /// <returns>Resource locator instance or <c>null</c>, if this item doesn't contain a <see cref="ProviderResourceAspect"/>.</returns>
        public override IResourceLocator GetResourceLocator()
        {
            MediaItemAspect onlineVideoAspect;
            MediaItemAspect providerAspect;
            if (!_aspects.TryGetValue(OnlineVideosAspect.ASPECT_ID, out onlineVideoAspect))
                return base.GetResourceLocator();

            if (!_aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerAspect))
                return null;
            string systemId = (string)providerAspect[ProviderResourceAspect.ATTR_SYSTEM_ID];
            string resourceAccessorPath = (string)onlineVideoAspect[OnlineVideosAspect.ATTR_LONGURL];
            return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
        }
    }
}
