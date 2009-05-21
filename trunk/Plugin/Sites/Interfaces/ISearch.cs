using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface ISearch
    {
        Dictionary<string, string> getSearchableCategories();        
        List<VideoInfo> search(string searchUrl, string query);
        List<VideoInfo> search(string searchUrl, string query, string category);
    }
}
