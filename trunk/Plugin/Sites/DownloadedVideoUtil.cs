using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public class DownloadedVideoUtil : SiteUtilBase, IFilter 
    {                 
		public override List<VideoInfo> getVideoList(Category category)
		{
            List<VideoInfo> loVideoInfoList = new List<VideoInfo>();
            if (category is RssLink)
            {
                string[] loVideoList = Directory.GetFiles(((RssLink)category).Url);
                
                foreach (String lsVideo in loVideoList)
                {
                    if (isPossibleVideo(lsVideo))
                    {
                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.VideoUrl = lsVideo;
                        loVideoInfo.ImageUrl = lsVideo.Substring(0, lsVideo.LastIndexOf(".")) + ".jpg";

                        loVideoInfo.Title = MediaPortal.Util.Utils.GetFilename(lsVideo);
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }
            }
            else
            {
                lock (GUIOnlineVideos.currentDownloads)
                {
                    foreach (DownloadInfo di in GUIOnlineVideos.currentDownloads.Values)
                    {
                        string progressInfo = (di.PercentComplete != 0 || di.KbTotal != 0) ? string.Format(" | {0}% / {1} KB", di.PercentComplete, di.KbTotal.ToString("n0")) : "";

                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.Title = di.Title;                        
                        loVideoInfo.ImageUrl = di.ThumbFile;
                        loVideoInfo.Length = di.Start.ToString("HH:mm:ss") + progressInfo;
                        loVideoInfo.Description = string.Format("Download from {0} to {1}", di.Url, di.LocalFile, progressInfo);
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }
            }
            return loVideoInfoList;
        }


        #region IFilter Member

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            return getVideoList(category);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            return null;
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            return null;
        }

        public List<int> getResultSteps()
        {
            return new List<int>();
        }

        public Dictionary<string, string> getOrderbyList()
        {
            return new Dictionary<string,string>();
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return new Dictionary<string,string>();
        }

        #endregion
    }
}
