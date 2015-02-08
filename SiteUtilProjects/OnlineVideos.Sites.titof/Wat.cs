using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class Wat : TF1Util
    {
        private static string baseVideos = @"http://www.wat.tv";

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(@"http://www.wat.tv/chaines");
                       
            Regex r = new Regex(@"<a\srel=""nofollow""\sid=""themeItem[^""]*""\sclass=""""\shref=""(?<url>[^""]*)""><span\sclass=""curseurChoice"">&nbsp;</span>(?<title>[^<]*)</a>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            Match m = r.Match(webData);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Url = m.Groups["url"].Value;
                cat.Name = Helpers.StringUtils.PlainTextFromHtml(m.Groups["title"].Value);
                cat.HasSubCategories = true;
                Settings.Categories.Add(cat);
                m = m.NextMatch();
            }
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string url = (parentCategory as RssLink).Url;
            url = baseVideos + url;
           
            List<RssLink> listDates = new List<RssLink>();
            string webData = GetWebData(url);

            Regex r = new Regex(@"<div\sclass=""present_chaine"">\s*<div\sclass=""img_chaine"">\s*<a\shref=""(?<url>[^""]*)""><img\sclass=""lazy""\ssrc=""[^""]*""\sdata-src=""(?<thumb>[^""]*)""\salt=""[^""]*""\stitle=""(?<title>[^""]*)""\s/></a>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            Match m = r.Match(webData);
            while (m.Success)
            {
                RssLink date = new RssLink();
                date.Url = m.Groups["url"].Value;
                date.Name = Helpers.StringUtils.PlainTextFromHtml(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                date.Thumb = m.Groups["thumb"].Value;
                date.ParentCategory = parentCategory;
                listDates.Add(date);
                m = m.NextMatch();
            }

            //Recherche des pages suivantes
            r = new Regex(@"<a\sdata-page=""[^""]*""\sdata-href=""(?<url>[^""]*)""\s*href=""[^""]*""\s>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            m = r.Match(webData);
            m = m.NextMatch();
            int nbPages = 0;

            //Pour chaque page suivante dans la limite de 5 pages
            while (m.Success && nbPages <= 5)
            {
                nbPages++;
                string webData2 = GetWebData("http://www.wat.tv" + m.Groups["url"].Value);

                Regex r2 = new Regex(@"<div\sclass=""present_chaine"">\s*<div\sclass=""img_chaine"">\s*<a\shref=""(?<url>[^""]*)""><img\sclass=""lazy""\ssrc=""[^""]*""\sdata-src=""(?<thumb>[^""]*)""\salt=""[^""]*""\stitle=""(?<title>[^""]*)""\s/></a>",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m2 = r2.Match(webData2);
                while (m2.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m2.Groups["url"].Value;
                    cat.Name = Helpers.StringUtils.PlainTextFromHtml(HttpUtility.HtmlDecode(m2.Groups["title"].Value));
                    cat.Thumb = m2.Groups["thumb"].Value;
                    cat.ParentCategory = parentCategory;
                    listDates.Add(cat);
                    m2 = m2.NextMatch();
                }
                m = m.NextMatch();
            }

            parentCategory.SubCategories.AddRange(listDates.ToArray());

            return listDates.Count;

        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {            
            return _getVideosUrl(video);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> listVideos = new List<VideoInfo>();

            string webData = GetWebData((category as RssLink).Url);


            if (listPages.Count == 0)
            {
                listPages.Add((category as RssLink).Url);
                Regex reg_nextPage = new Regex(@"<span\sclass=""next""><a\sdata-page='[^']*'\s*href=""(?<url>[^""]*)""\sdata-href=""[^""]*""\sclass=""[^""]*"">",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                Match m_nextPage = reg_nextPage.Match(webData);

                while (m_nextPage.Success)
                {
                    listPages.Add(m_nextPage.Groups["url"].Value);
                    m_nextPage = m_nextPage.NextMatch();
                }

            }
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(webData);
            HtmlNodeCollection divs = html.DocumentNode.SelectNodes(@"//div[@id = 'contentChannel']//ul/li//div[@class = 'block_lib_media']");
            
            if (divs != null)
            {
                foreach (HtmlNode div in divs)
                {
                    HtmlNode img = div.SelectSingleNode(@"./div/a/img");
                    HtmlNode duration = div.SelectSingleNode(@".//div[@class = 'txtTime']");
                    HtmlNode title = div.SelectSingleNode(@"./div/h4/a");
                    HtmlNode airdate = div.SelectSingleNode(@"./div/p/span");
                    listVideos.Add(new VideoInfo() {
                                      Title = title.InnerText,
                                      VideoUrl = string.Format(@"{0}{1}", baseVideos, title.GetAttributeValue(@"href", string.Empty)),
                                      Thumb = img.GetAttributeValue(@"src", string.Empty),
                                      Length = duration.InnerText,
                                      Airdate = airdate.GetAttributeValue(@"title", string.Empty)
                                   });
                }
            }
            else
            {
                Log.Warn("No videos found");
            }

            return listVideos;
        }
        
    }
}
