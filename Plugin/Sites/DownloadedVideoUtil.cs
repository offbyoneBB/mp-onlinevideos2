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
		    List<VideoInfo>loVideoInfoList = new List<VideoInfo>();
            string[]loVideoList = Directory.GetFiles(((RssLink)category).Url);
            

            VideoInfo loVideoInfo;
            foreach (String lsVideo in loVideoList)
            {
            	if(isPossibleVideo(lsVideo)){
                	loVideoInfo = new VideoInfo();
                	loVideoInfo.VideoUrl = lsVideo;
                
                	loVideoInfo.Title  = Utils.GetFilename(lsVideo);
                	loVideoInfoList.Add(loVideoInfo);
            	}
            }
            return loVideoInfoList;
        }
            
    }
}
