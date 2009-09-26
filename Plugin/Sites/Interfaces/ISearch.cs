using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface ISearch
    {
        Dictionary<string, string> GetSearchableCategories(IList<Category> configuredCategories);
        List<VideoInfo> Search(string searchUrl, string query);
        List<VideoInfo> Search(string searchUrl, string query, string category);
    }
}
