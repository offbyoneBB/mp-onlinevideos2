using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class FiveMin : SiteUtilBase
    {

        string baseUrl = "http://www.5min.com/";
        int maxPages = 0;
        int pageCounter = 0;
        string pageUrl = "";

        /*<li  style="background-image: url('http://pshared.5min.com/Graphics/MenuIcons/cat_2.gif');"
			>
			
			<a href="http://www.5min.com/Category/Business">
				Business</a>*/

        string categoryRegex = @"<li\s*style=""background-image:\surl\('(?<thumb>[^']+)'\);""\s*>\s*<a\shref=""(?<url>[^""]+)"">\s*(?<title>[^<]+)</a>";

        /*				<li>

					<a  href="http://www.5min.com/Category/Arts/Balloons">
						Balloons</a> 
				</li>
*/

        string subCategoryRegex = @"<li>\s*<a\s*href=""(?<url>[^""]+)"">\s*(?<title>[^<]+)</a>";

        /*<a href="http://www.5min.com/Video/How-to-make-a-balloon-shark-1088" id="hrefVideoLeaf">
                       <img id="imgPreviewLeaf" title="video: How to make a balloon shark Learn how to make a balloon shark following some simple and easy steps" src="http://pthumbnails.5min.com/22/1088_2.jpg" alt="*/
        string videoListRegex = @"<a\s*href=""(?<url>[^""]+)""\s*id=""hrefVideoLeaf"">\s*<img\sid=""imgPreviewLeaf""\stitle=""(?<title>[^""]+)""\ssrc=""(?<thumb>[^""]+)""\salt";


        //>4</a></span><span class="padding_left_5"></span><span><a class="color_095a8d font_size_11 lnkOuter" href="http://www.5min.com/PagerServer.aspx?origURL=http://www.5min.com/Category/Arts/Balloons/Filter/Featured/Today/page2">Next</a>
        string maxPagesRegex = @">(?<title>[^<]+)</a></span><span\sclass=""padding_left_5""></span><span><a\sclass=""color_095a8d\sfont_size_11\slnkOuter""\shref=""(?<url>[^""]+)"">Next</a>";

        //videoUrl=http%3a%2f%2flvideos.5min.com%2f102935%2f10293461.flv&pageUrl
        string videoUrlRegex = @"videoUrl=(?<url>[^&]+)&pageUrl";

        Regex regEx_Category;
        Regex regEx_SubCategory;
        Regex regEx_VideoList;
        Regex regEx_MaxPages;
        Regex regEx_VideoUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_MaxPages = new Regex(maxPagesRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    //TODO: Convert gif to jpg to avoid display issues
                    //cat.Thumb = m.Groups["thumb"].Value;
                    cat.Url = m.Groups["url"].Value;
                    cat.HasSubCategories = true;

                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            data = data.Substring(data.IndexOf("<ul class=\"sub\">"),data.IndexOf("</ul>",data.IndexOf("<ul class=\"sub\">")) - data.IndexOf("<ul class=\"sub\">"));

            parentCategory.SubCategories = new List<Category>();
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_SubCategory.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.SubCategoriesDiscovered = true;
                    cat.HasSubCategories = false;

                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;

                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_VideoUrl.Match(data);
                if (m.Success)
                {
                    string url = m.Groups["url"].Value;
                    url = url.Replace("%3b", ";");
                    url = url.Replace("%3f", "?");
                    url = url.Replace("%2f", "/");
                    url = url.Replace("%3a", ":");
                    url = url.Replace("%23", "#");
                    url = url.Replace("%24", "&");
                    url = url.Replace("%3d", "=");
                    url = url.Replace("%2b", "+");
                    url = url.Replace("%26", "$");
                    url = url.Replace("%2c", ",");
                    url = url.Replace("%25", "%");
                    url = url.Replace("%3c", "<");
                    url = url.Replace("%3e", ">");
                    url = url.Replace("%7e", "~");
                    return url;
                }
            }
            return null;
        }

        protected List<VideoInfo> getVideoListForCurrentCategory()
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData(pageUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.Title = video.Title.Replace("video: ","");
                    video.VideoUrl = m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;

                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            pageUrl = (category as RssLink).Url;
            string data = GetWebData(pageUrl);
            pageCounter = 1;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_MaxPages.Match(data);
                if (m.Success)
                {
                    maxPages = Convert.ToInt32(m.Groups["title"].Value);
                    
                    pageUrl = m.Groups["url"].Value;
                    if(pageUrl.Contains("origURL"))
                        pageUrl = pageUrl.Substring(pageUrl.IndexOf("origURL") + 8);
                    pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
                    pageUrl = pageUrl + "/page" + pageCounter;
                }
                else
                    maxPages = 1;
            }
            return getVideoListForCurrentCategory();
        }

        public override bool HasNextPage
        {
            get { return pageCounter + 1 < maxPages; }
        }

        public override bool HasPreviousPage
        {
            get { return pageCounter - 1 > 0; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageCounter++;
            pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
            pageUrl = pageUrl + "/page" + pageCounter;
            return getVideoListForCurrentCategory();
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageCounter--;
            pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
            pageUrl = pageUrl + "/page" + pageCounter;
            return getVideoListForCurrentCategory();
        }
    }
}