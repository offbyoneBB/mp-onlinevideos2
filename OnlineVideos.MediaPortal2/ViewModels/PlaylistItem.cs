using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using OnlineVideos.MediaPortal2.ResourceAccess;
using OnlineVideos.Sites;

namespace OnlineVideos.MediaPortal2
{
    public class PlaylistItem : MediaItem
    {
        public PlaylistItem(VideoViewModel videoInfo, string resolvedPlaybackUrl)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>
            {
                { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
                { MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
                { VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) }
            })
        {
            SiteName = videoInfo.SiteName;
            Util = OnlineVideoSettings.Instance.SiteUtilsList[SiteName];

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

                Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(
                    ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                    isUriSource
                        ? RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize()
                        : RawTokenResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());

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
        public SiteUtilBase Util { get; private set; }
    }
}
