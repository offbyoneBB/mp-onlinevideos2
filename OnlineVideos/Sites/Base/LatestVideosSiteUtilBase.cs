using System.Collections.Generic;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// If a site implementation inhertits from this abstract class, the site will be asked periodically for a list of videos that are considered new.
	/// </summary>
	public abstract class LatestVideosSiteUtilBase : SiteUtilBase
	{
		/// <summary>
		/// This methid is called to get the latest videos of the site.<br/>
		/// It should honor the user's setting for <see cref="LatestVideosCount"/>.
		/// </summary>
		/// <returns>A list of <see cref="VideoInfo"/> objects that are new on the site.</returns>
		public abstract List<VideoInfo> GetLatestVideos();

		[Category("OnlineVideosUserConfiguration"), Description("Number of videos from this Site used for the latest videos infos. 0 = disable for this site."), LocalizableDisplayName("Latest Videos")]
		protected uint latestVideosCount = 3;
		public virtual uint LatestVideosCount { get { return latestVideosCount; } }
	}
}
