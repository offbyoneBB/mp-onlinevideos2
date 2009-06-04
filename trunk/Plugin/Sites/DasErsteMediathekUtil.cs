using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : SiteUtilBase
    {
        public enum DasErsteVideoQuality { Low, High };

        protected string ConvertUmlaut(string strIN)
        {
            return strIN.Replace("\x00c4", "Ae").Replace("\x00e4", "ae").Replace("\x00d6", "Oe").Replace("\x00f6", "oe").Replace("\x00dc", "Ue").Replace("\x00fc", "ue");
        }

        protected static string convertUnicodeD(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf("&#", startIndex);
                if (startIndex > 0)
                {
                    int num3;
                    int index = source.IndexOf(";", startIndex);
                    int.TryParse(source.Substring(startIndex + 2, (index - startIndex) - 2), out num3);
                    source = source.Replace(source.Substring(startIndex, (index - startIndex) + 1), char.ConvertFromUtf32(num3).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }

        protected static string convertUnicodeU(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf(@"\u", startIndex);
                if (startIndex > 0)
                {
                    int num2;
                    int.TryParse(source.Substring(startIndex + 2, 4), NumberStyles.HexNumber, null, out num2);
                    source = source.Replace(source.Substring(startIndex, 6), char.ConvertFromUtf32(num2).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }
        
        protected string getCachedHTMLData(string fsUrl)
        {
            return convertUnicodeU(convertUnicodeD(GetWebData(fsUrl)));
        }

        public override List<Category> getDynamicCategories()        
        {
            string lsUrl = "http://mediathek.daserste.de/daserste/servlet/content/487872";

            string str4;
            string source = getCachedHTMLData(lsUrl);
            List<Category> list = new List<Category>();
            string begTag = "<div id=\"alphapane\" class=\"jScrollPane\">";
            string endTag = "</div><!-- .module -->";
            if (getTagValues(source, begTag, endTag, out str4, 0) > 0)
            {
                int beginIndex = 0;
                do
                {
                    string str5;
                    beginIndex = getTagValues(str4, "<li><a href=\"", "</span></a></li>", out str5, beginIndex);
                    if (beginIndex > 0)
                    {
                        string str6;
                        string str7;
                        RssLink item = new RssLink();
                        int num2 = getTagValues(str5, null, "\"><span class=\"title\">", out str6, 0);
                        num2 = getTagValues(str5, null, "</span><span class=\"count\">", out str7, num2);                        
                        item.Name = System.Web.HttpUtility.HtmlDecode(str7);
                        uint count = 0;
                        if (uint.TryParse(str5.Substring(num2), out count)) item.EstimatedVideoCount = count;
                        item.Url = "http://mediathek.daserste.de" + str6 + "&goto=1";
                        list.Add(item);
                    }
                }
                while (beginIndex > 0);
            }
            return list;
        }        

        protected static int getTagValues(string source, string begTag, string endTag, out string value, int beginIndex)
        {
            int num;
            value = "";
            if (begTag != null)
            {
                num = source.IndexOf(begTag, beginIndex);
            }
            else
            {
                num = beginIndex;
            }
            if (num < 0)
            {
                return -1;
            }
            if (begTag != null)
            {
                num += begTag.Length;
            }
            int index = source.IndexOf(endTag, num);
            if (index < 0)
            {
                return -1;
            }
            value = source.Substring(num, index - num);
            return (index + endTag.Length);
        }

        Regex videoSearchRegExp_Low = new Regex(@"player.avaible_url\['(?<type>flashmedia|microsoftmedia)'\]\['1'\]\s*=\s*""(?<videoUrl>http://[^""]+)""");
        Regex videoSearchRegExp_High = new Regex(@"player.avaible_url\['(?<type>flashmedia|microsoftmedia)'\]\['2'\]\s*=\s*""(?<videoUrl>http://[^""]+)""");
        Regex cachedRegExp;
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            if (cachedRegExp == null)
            {
                if (OnlineVideoSettings.getInstance().DasErsteQuality == DasErsteVideoQuality.Low)
                    cachedRegExp = videoSearchRegExp_Low;
                else
                    cachedRegExp = videoSearchRegExp_High;
            }

            string fsId = video.VideoUrl;
            string str = getCachedHTMLData(fsId);
            Match match = cachedRegExp.Match(str);
            string resultUrl = "";
            while (match.Success)
            {
                resultUrl = match.Groups["videoUrl"].Value;
                if (match.Groups["type"].Value == "flashmedia") break; // prefer flash over wmv
                match = match.NextMatch();
            }
            return resultUrl;
        }

        protected List<VideoInfo> getVideoListForCurrentCategory()
        {
            List<VideoInfo> listOfLinks = new List<VideoInfo>();

            string source = getCachedHTMLData(lastQueriedCategoryUrl);
            int beginIndex = 0;
            string begTag = "<div class=\"teaser\">";
            do
            {
                string str3;
                int num3 = getTagValues(source, begTag, begTag, out str3, beginIndex) - begTag.Length;
                if (num3 < 0)
                {
                    beginIndex = getTagValues(source, begTag, "</div><!-- .teaserBox -->", out str3, beginIndex);
                }
                else
                {
                    beginIndex = num3;
                }
                if ((beginIndex > 0) && (str3.IndexOf("<a class=\"podcast\" href=\"") < 0))
                {
                    string str4;
                    string str5;
                    string str6;
                    string str7;
                    int num4 = getTagValues(str3, "<div class=\"image\"><img src=\"", "\" alt=", out str4, 0);
                    num4 = getTagValues(str3, "<p class=\"desc\">", "</p>", out str5, num4);
                    num4 = getTagValues(str3, "<p class=\"info\">", "</p>", out str6, num4);
                    num4 = getTagValues(str3, "<a class=\"camera\" href=\"", "title=\"", out str7, num4);
                    VideoInfo link = new VideoInfo();
                    link.VideoUrl = "http://mediathek.daserste.de" + str7;
                    link.ImageUrl = "http://mediathek.daserste.de" + str4;
                    link.Length = str6.Trim();
                    link.Title = string.Format("{0} {1}", link.Length, str5.Trim());
                    listOfLinks.Add(link);
                }
            }
            while (beginIndex > 0);
            if (source.IndexOf("<a class=\"next\" href=\"") > 0)
            {
                lastQueriedCategoryHasNextPage = true;
            }
            else
            {
                lastQueriedCategoryHasNextPage = false;
            }
            if (source.IndexOf("<a class=\"prev\" href=\"") > 0)
            {
                lastQueriedCategoryHasPreviousPage = true;
            }
            else
            {
                lastQueriedCategoryHasPreviousPage = false;
            }

            return listOfLinks;            
        }

        string lastQueriedCategoryUrl = "";
        uint lastQueriedCategoryPageIndex = 1;
        bool lastQueriedCategoryHasPreviousPage = false;
        bool lastQueriedCategoryHasNextPage = false;        
        public override List<VideoInfo> getVideoList(Category category)
        {
            lastQueriedCategoryUrl = (category as RssLink).Url;
            lastQueriedCategoryPageIndex = 1;
            lastQueriedCategoryHasPreviousPage = false;
            lastQueriedCategoryHasNextPage = false;        
            return getVideoListForCurrentCategory();
        }        
        
        public override bool hasNextPage()
        {
            return lastQueriedCategoryHasNextPage;
        }        

        public override bool hasPreviousPage()
        {
            return lastQueriedCategoryHasPreviousPage;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            if (lastQueriedCategoryHasNextPage)
            {
                lastQueriedCategoryPageIndex++;
                lastQueriedCategoryUrl = lastQueriedCategoryUrl.Replace("goto=" + (lastQueriedCategoryPageIndex - 1), "goto=" + lastQueriedCategoryPageIndex);
                return getVideoListForCurrentCategory();
            }
            else
            {
                return new List<VideoInfo>();
            }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            if (lastQueriedCategoryHasPreviousPage)
            {
                lastQueriedCategoryPageIndex--;
                lastQueriedCategoryUrl = lastQueriedCategoryUrl.Replace("goto=" + (lastQueriedCategoryPageIndex + 1), "goto=" + lastQueriedCategoryPageIndex);
                return getVideoListForCurrentCategory();
            }
            else
            {
                return new List<VideoInfo>();
            }
        }
    }
}

