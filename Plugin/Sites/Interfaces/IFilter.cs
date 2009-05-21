using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface IFilter
    {
        List<VideoInfo> filterVideoList(Category category, int maxResult, String orderBy, String timeFrame);
        List<VideoInfo> filterSearchResultList(String query, int maxResult, String orderBy, String timeFrame);
        List<VideoInfo> filterSearchResultList(String query,String category, int maxResult, String orderBy, String timeFrame);
        List<Int32> getResultSteps();
        Dictionary<String,String> getOrderbyList();
        Dictionary<String,String> getTimeFrameList();        
    }
}
