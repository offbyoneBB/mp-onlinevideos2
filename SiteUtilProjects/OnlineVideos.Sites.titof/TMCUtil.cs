using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TMCUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int nbCat = 0;
            baseUrl = @"http://www.tmc.tv/liste-programme-tv/index-848-UyBUSVRSRQ==.html";
            nbCat += base.DiscoverDynamicCategories();

            string webData = GetWebData(baseUrl);
            Regex reg_nextPage = new Regex(@"(?<!class=""dernier"".*)(?<=pPrecedentGris.*)<li.*?><a\shref=""(?<url>[^""]*)""\sclass=""[^""]*"">[^<]*</a></li>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            Match m_nextPage = reg_nextPage.Match(webData);

            while (m_nextPage.Success)
            {
                baseUrl = "http://www.tmc.tv" + m_nextPage.Groups["url"].Value;
                nbCat += base.DiscoverDynamicCategories();
                m_nextPage = m_nextPage.NextMatch();
            }
            
            return nbCat;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            
            string webData = GetWebData(((RssLink)category).Url);
            Regex reg = new Regex(@"rel=""nofollow"">\s<img\ssrc=""(?<ImageUrl>[^""]*)""\scp=""[^""]*""\salt=""[^""]*"">\s</a>\s<a\shref=""(?<VideoUrl>[^""]*)""\sclass=""[^""]*""><div\sclass=""left"">(?<Duration>[^<]*)</div></a>\s</div>\s<div\sclass=""[^""]*"">\s<div\sclass=""[^""]*"">\s<span\sclass=""[^""]*""\sstyle=""[^""]*"">[^<]*</span>[^<]*<span>[^<]*</span>[^<]*<span><img\ssrc=""[^""]*""\salt=""[^""]*""\sclass=""[^""]*"">[^<]*</span>\s</div>\s<h3\sclass=""[^""]*"">\s<a\shref=""[^""]*"">(?<Title>[^<]*)</a>\s</h3>\s<p\sclass=""[^""]*"">(?<Description>[^<]*)</p>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            Match m = reg.Match(webData);

            while (m.Success)
            {
                VideoInfo videoInfo = CreateVideoInfo();
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                videoInfo.Description = m.Groups["Description"].Value;
                videoList.Add(videoInfo);
                m = m.NextMatch();
            }
            
            videoList.AddRange(base.getVideoList(category));


            return videoList;
            
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            return TF1Util._getVideosUrl(video);
            
        }

        
    }
}
