using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// If a <see cref="SiteUtilBase"/> implements this interface, the site can filter and sort a list of <see cref="VideoInfo"/> objects by its own custom strings.
	/// </summary>
    public interface IFilter
    {
		List<VideoInfo> FilterVideos(Category category, int maxResult, string orderBy, string timeFrame);
		List<VideoInfo> FilterSearchResults(string query, int maxResult, string orderBy, string timeFrame);
		List<VideoInfo> FilterSearchResults(string query, string category, int maxResult, string orderBy, string timeFrame);
		List<int> GetResultSteps();
		Dictionary<string, string> GetOrderByOptions();
		Dictionary<string, string> GetTimeFrameOptions();
    }
}
