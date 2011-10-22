using System.Collections.Generic;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// If a <see cref="SiteUtilBase"/> implements this interface, the site will be asked for a list of videos that are considered new.<br/>
	/// </summary>
	public abstract class LatestVideosSiteUtilBase : SiteUtilBase
	{
		public abstract List<VideoInfo> GetLatestVideos();

		[Category("OnlineVideosUserConfiguration"), Description("Number of videos from this Site used for the latest videos infos. 0 = disable for this site."), LocalizableDisplayName("Latest Videos")]
		protected uint latestVideosCount = 3;
		public virtual uint LatestVideosCount { get { return latestVideosCount; } }
	}
}
