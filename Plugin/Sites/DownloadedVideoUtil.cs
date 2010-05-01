using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class DownloadedVideoUtil : SiteUtilBase, IFilter 
    {
        string lastSort = "date";

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoInfoList = new List<VideoInfo>();
            if (category is RssLink)
            {
                FileInfo[] files = new DirectoryInfo(((RssLink)category).Url).GetFiles();

                foreach (FileInfo file in files)
                {
                    if (isPossibleVideo(file.Name))
                    {
                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.VideoUrl = file.FullName;
                        loVideoInfo.ImageUrl = file.FullName.Substring(0, file.FullName.LastIndexOf(".")) + ".jpg";
                        loVideoInfo.Length = file.LastWriteTime.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                        loVideoInfo.Title = MediaPortal.Util.Utils.GetFilename(file.Name);
                        loVideoInfo.Description = string.Format("{0} MB", (file.Length / 1024 / 1024).ToString("N0"));
                        loVideoInfo.Other = file;
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }

                switch (lastSort)
                {
                    case "name":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        { 
                            return v1.Title.CompareTo(v2.Title); 
                        });
                        break;
                    case "date":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        {
                            return (v2.Other as FileInfo).LastWriteTime.CompareTo((v1.Other as FileInfo).LastWriteTime); 
                        });
                        break;
                    case "size":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2)
                        {
                            return (v2.Other as FileInfo).Length.CompareTo((v1.Other as FileInfo).Length);
                        });
                        break;
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
            lastSort = orderBy;
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
            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add(GUILocalizeStrings.Get(104), "date");
            options.Add(GUILocalizeStrings.Get(365), "name");
            options.Add(GUILocalizeStrings.Get(105), "size");
            return options;
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return new Dictionary<string,string>();
        }

        #endregion
    }
}
