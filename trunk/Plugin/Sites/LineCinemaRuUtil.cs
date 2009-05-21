using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;

//written by Hersh Shafer (hershs@gmail.com)
namespace OnlineVideos.Sites
{
	public class LineCinemaRuUtil : SiteUtilBase
	{		
        private String browseUrl(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (WebResponse response = request.GetResponse())
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                string str = reader.ReadToEnd();
                return str;
            }
        }

        public List<VideoInfo> parseEpisodes(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();
            String pageContent = browseUrl(fsUrl);

            String[] films = pageContent.Split(new string[] { @"<table class=""gTable""" }, StringSplitOptions.None);
            Boolean isFirst = true;
            foreach (String videoElm in films)
            {
                if (!isFirst)
                {
                    VideoInfo loRssItem;
                    loRssItem = getVideoInfo(videoElm);
                    Log.Info("Found fideo " + loRssItem.ToString());
                    loRssItems.Add(loRssItem);
                }
                isFirst = false;
            }
/*
    loRssItem.Title = node.InnerText;
    loRssItem.Description = node.InnerText;
    loRssItem.ImageUrl = node.InnerText;
    loRssItem.VideoUrl = ac["id"].InnerText;
 */
            return loRssItems;

        }
        private VideoInfo getVideoInfo(String videoElm)
        {
            VideoInfo videoInfo = new VideoInfo();
            int videoUrlIndx = videoElm.IndexOf(@"href=""");
            if (videoUrlIndx != -1)
            {
                int spaceIndx = videoElm.IndexOf(@"""", videoUrlIndx + 6);
                String videoUrl = videoElm.Substring(videoUrlIndx + 6, spaceIndx - videoUrlIndx - 6);
                videoInfo.VideoUrl = videoUrl;
            }
            String regExpStr = @"<span\sstyle="".*>(?<Title>.+)</span>";
            Regex regExp = new Regex(regExpStr, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m = regExp.Match(videoElm);
            if (m.Success)
            {
                String title = m.Groups["Title"].Value;
                videoInfo.Title = title;
            }

            int pos = getStringPostion(videoElm, "<img ", 2);
            if (pos != -1)
            {
                int srcIndx = videoElm.IndexOf(@"src=""", pos);
                int spaceIndx = videoElm.IndexOf(@" ", srcIndx);
                String imgUrl = videoElm.Substring(srcIndx + 5, spaceIndx - srcIndx - 6);
                videoInfo.ImageUrl = imgUrl;
            }
            return videoInfo;
        }

        static private Int32 getStringPostion(String str, String searchStr, Int32 instanceNumber)
        {
            Int32 position = -1;
            for (int i = 0; i < instanceNumber; i++)
            {
                position = str.IndexOf(searchStr, position + 1);
                if (position == -1)
                {
                    return position;
                }
            }
            return position;
        }
        
		public override List<VideoInfo> getVideoList(Category category)
		{
            
            List<VideoInfo> loRssItemList = parseEpisodes(((RssLink)category).Url);
			return loRssItemList;
		}
        
        public override String getUrl(VideoInfo video, SiteSettings foSite)
		{
            String lsHtml = browseUrl(video.VideoUrl);
            int embedIndx = lsHtml.IndexOf("<EMBED ");
            if(embedIndx != -1)
            {
                int embedFinish = lsHtml.IndexOf(">", embedIndx + 5);
                String embedElement = lsHtml.Substring(embedIndx, embedFinish - embedIndx);
                Log.Info("embed element = " + embedElement);
                int fileStart = embedElement.IndexOf("file=");
                if (fileStart != -1)
                {
                    int fileEnd = embedElement.IndexOf("&", fileStart + 5);
                    String fileUrl = embedElement.Substring(fileStart + 5, fileEnd - fileStart - 5);
                    Log.Error("VideoUrl = " + fileUrl);
                    return fileUrl;
                }
                else
                {
                    Log.Error("The file= attribute not found in the embed tag");
                    return "";
                }
            }
            else
            {
                Log.Error("The <EMBED tag not found in the page");
                return "";
            }
		}
	}
}
