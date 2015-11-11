using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace OnlineVideos.Sites
{
    public class SokrostreamUtil: GenericSiteUtil
    {
        string channelCatalog = "http://sokrostream.biz/categories/films";


        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            RssLink cat = null;

            cat = new RssLink();
            cat.Url = channelCatalog; 
            cat.Name = "Films";
            cat.Other = "root";
            cat.Thumb = "http://sokrostream.biz/sokrologo.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

           
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> tVideos = new List<VideoInfo>();
            string sUrl = (category as RssLink).Url;
            string sContent = GetWebData((category as RssLink).Url,null,null,null,null,true );
            sContent = sContent.Replace("Ã©", "é");
            sContent = sContent.Replace("Ã", "à");
            sContent = sContent.Replace("\r", "");
            sContent = sContent.Replace("\n", "");
            sContent = sContent.Replace("\t", "");

            string[] tcontent = sContent.Split(new string[]{"<div class=\"moviefilm\">"}, StringSplitOptions.RemoveEmptyEntries);

            for (int idx = 1; idx < tcontent.Count(); idx++) 
            {

                    VideoInfo vid = new VideoInfo();

                    string sInput = tcontent[idx];

                    string pattern = "<img src=\"(?<jocker>[\\wéàè =':\\/.-]*)\"";
                    Regex rgximg = new Regex(pattern, RegexOptions.Multiline);
                    Match math = rgximg.Match(sInput);
                    if (math.Groups.Count > 0) { vid.Thumb = math.Groups["jocker"].Value; }

                    pattern = "<a href=\"(?<jocker>[\\wéàè =':\\/.-]*)\">(?<title>[\\w',?!:; ]*)<\\/a";
                    rgximg = new Regex(pattern, RegexOptions.Multiline);
                    math = rgximg.Match(sInput);
                    if (math.Groups.Count > 0)
                    {
                        vid.VideoUrl = math.Groups["jocker"].Value;
                        vid.Title = math.Groups["title"].Value;

                    }
                    if (!string.IsNullOrEmpty (vid.Title) )
                        tVideos.Add(vid);

                
            }

       

            //JArray tArray = JArray.Parse(sContent);
            //foreach (JObject obj in tArray)
            //{
            //    try
            //    {
            //        VideoInfo vid = new VideoInfo()
            //        {
            //            Thumb = (string)obj["MEDIA"]["IMAGES"]["PETIT"],
            //            Title = (string)obj["INFOS"]["TITRAGE"]["TITRE"],
            //            Description = (string)obj["INFOS"]["DESCRIPTION"],
            //            Length = (string)obj["DURATION"],
            //            VideoUrl = (string)obj["ID"],
            //            StartTime = (string)obj["INFOS"]["DIFFUSION"]["DATE"]
            //        };
            //        tVideos.Add(vid);
            //    }
            //    catch { }
            //}
            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string sUrl = video.VideoUrl;
            string sContent = GetWebData(sUrl+"/5");
            string content2 = GetWebData("http://hqq.tv/player/embed_player.php?vid=5ADYD8S6YSN4");
        

            //JObject obj = JObject.Parse(sContent);

            //string shls = (string)obj["MEDIA"]["VIDEOS"]["HLS"];

            //string webdata = GetWebData(shls);
            //string rgxstring = @"http:\/\/(?<url>[\w.,?=\/-]*)";
            //Regex rgx = new Regex(rgxstring);
            //var tresult = rgx.Matches(webdata);
            List<string> tUrl = new List<string>();
            //foreach (Match match in tresult)
            //{
            //    tUrl.Add(@"http://" + match.Groups["url"]);
            //}

            //if (tUrl.Count > 2)
            //{
            //    video.PlaybackOptions = new Dictionary<string, string>();
            //    video.PlaybackOptions.Add("SD", tUrl[tUrl.Count - 2]);
            //    video.PlaybackOptions.Add("HD", tUrl[tUrl.Count - 1]);
            //}

            return tUrl[tUrl.Count - 1];
        }


    }
}
