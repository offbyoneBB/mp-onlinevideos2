using System;
using System.Collections.Generic;

namespace OnlineVideos
{
	/// <summary>
	/// If a <see cref="SiteUtilBase"/> implements this interface, the site can filter and sort a list of <see cref="VideoInfo"/> objects by its own custom strings.
	/// </summary>
    public interface IFilter
    {
		List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame);
		List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame);
		List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame);
		List<int> getResultSteps();
		Dictionary<string, string> getOrderbyList();
		Dictionary<string, string> getTimeFrameList();
    }
}
