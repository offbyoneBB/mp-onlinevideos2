using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface ISearch
    {
        Dictionary<string, string> GetSearchableCategories();
        List<VideoInfo> Search(string query);
        List<VideoInfo> Search(string query, string category);
    }
}
