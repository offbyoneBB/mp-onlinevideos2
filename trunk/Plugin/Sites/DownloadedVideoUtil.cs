using System;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using MediaPortal.Player;
using System.Collections.Generic;
using MediaPortal.GUI.View ;
using MediaPortal.Dialogs;
using MediaPortal.Util;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class DownloadedVideoUtil : SiteUtilBase 
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

                        loVideoInfo.Title = Utils.GetFilename(lsVideo);
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
                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.Title = di.Title;
                        loVideoInfo.ImageUrl = di.ThumbFile;
                        loVideoInfo.Description = string.Format("Download from url: {0} to file {1}", di.Url, di.LocalFile);
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }
            }
            return loVideoInfoList;
        }
            
    }
}
