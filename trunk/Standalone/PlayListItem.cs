using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Standalone
{
	public class PlayList : List<PlayListItem>
	{
		public bool IsPlayAll { get; set; }
	}

	public class PlayListItem
	{
		public PlayListItem(OnlineVideos.VideoInfo video, OnlineVideos.Sites.SiteUtilBase util)
		{
			Video = video;
			Util = util;
		}

		public string Title { get { return Video.Title + (string.IsNullOrEmpty(ChosenPlaybackOption) ? "" : " (" + ChosenPlaybackOption + ")"); } }

		public OnlineVideos.VideoInfo Video { get; protected set; }

		public OnlineVideos.Sites.SiteUtilBase Util { get; protected set; }

		public string FileName { get; set; }

		public string ChosenPlaybackOption { get; set; }
	}
}
