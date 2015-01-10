using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;

namespace OnlineVideos.MediaPortal2
{
	public class PlaylistItem : MediaItem
	{
		public PlaylistItem(VideoViewModel videoInfo, string resolvedPlaybackUrl)
			: base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
			{
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) }
			})
		{
            SiteName = videoInfo.SiteName;

			Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
			if (videoInfo.SiteUtilName == "DownloadedVideo")
			{
				Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath(resolvedPlaybackUrl).Serialize());
				Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/unknown");
			}
			else
			{
				Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());
				Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, OnlineVideosPlayer.ONLINEVIDEOS_MIMETYPE);
			}
			
			Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, videoInfo.Title);
			
			Aspects[VideoAspect.ASPECT_ID].SetAttribute(VideoAspect.ATTR_STORYPLOT, videoInfo.Description);

			DateTime parsedAirDate;
			if (DateTime.TryParse(videoInfo.VideoInfo.Airdate, out parsedAirDate))
				Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_RECORDINGTIME, parsedAirDate);

		}

        public string SiteName { get; private set; }
	}
}
