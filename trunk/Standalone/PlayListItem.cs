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
		public OnlineVideos.VideoInfo Video { get; set; }

		public OnlineVideos.Sites.SiteUtilBase Util { get; set; }

		public string FileName { get; set; }

		public string ChosenPlaybackOption { get; set; }
	}
}
