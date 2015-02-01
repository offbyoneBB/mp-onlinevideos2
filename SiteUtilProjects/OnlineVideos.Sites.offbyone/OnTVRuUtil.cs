using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

//written by Hersh Shafer (hershs@gmail.com)
namespace OnlineVideos.Sites
{
	public class OnTVRuUtil : SiteUtilBase
	{
        private List<VideoInfo> parseEpisodes(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();
            String pageContent = GetWebData(fsUrl);

            String ul = getStringBetween(pageContent, "<div class=\"widget_meta", "</ul>");
            String regExpStr = @"href=""(?<link>http://on-tv.ru/[^""]+)"">(<font color=red>|)(?<name>[^<]+)";
            Regex regExp = new Regex(regExpStr, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m = regExp.Match(ul);
            while (m.Success)
            {
                VideoInfo loRssItem = new VideoInfo();
                loRssItem.VideoUrl = m.Groups["link"].Value;
                loRssItem.Title = m.Groups["name"].Value;
                Log.Info("Found fideo " + loRssItem.ToString());
                loRssItems.Add(loRssItem);

                m = m.NextMatch();
            }
            return loRssItems;
        }
        static String getStringBetween(String source, String from, String to)
        {
            int startIndex = source.IndexOf(from);
            if (startIndex == -1)
            {
                return "";
            }
            startIndex += from.Length;
            int endIndex = source.IndexOf(to, startIndex);
            if (endIndex == -1)
            {
                return "";
            }
            return source.Substring(startIndex, endIndex - startIndex);
        }


        
		public override List<VideoInfo> GetVideos(Category category)
		{
            List<VideoInfo> loRssItemList = parseEpisodes(((RssLink)category).Url);
			return loRssItemList;
		}

        public override String GetVideoUrl(VideoInfo video)
		{
            String lsHtml = GetWebData(video.VideoUrl);

            String regExpStr = @"<embed.*src=""(?<link>[^""]+)""";
            Regex regExp = new Regex(regExpStr, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m = regExp.Match(lsHtml);
            Console.WriteLine(m.Length);
            if(m.Success)
            {
                return m.Groups["link"].Value;
            }
            return "";
        }
	}
}
