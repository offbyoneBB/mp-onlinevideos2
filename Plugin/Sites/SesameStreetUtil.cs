using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Drawing;

namespace OnlineVideos.Sites
{
    public class SesameStreetUtil : SiteUtilBase
    {
        private string baseUrl = "http://www.sesamestreet.org";

        private string subCategoryRegex = @"<a\shref=""(?<url>[^""]+)"".*?span>(?<title>[^<]+)<";
        private string videoListRegex = @"<div\sclass=""thumb-image"">.*?<img\ssrc=""(?<thumb>[^""]+)"".*?a\shref=""(?<url>[^""]+)"".*?<span>(?<title>[^<]+)<.*?class=""description"">(?<descr>[^<]+)<";
        private string urlRegex = @",file:""(?<url>[^""]+)""";
        private int pageNr = 1;
        private bool hasNextPage = false;
        private string firstPageUrl;

        public SesameStreetUtil()
        {
        }

        private Regex regEx_SubCategory;
        private Regex regEx_VideoList;
        private Regex regEx_GetUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_GetUrl = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink cat = new RssLink();
            cat.Name = "All";
            cat.Url = @"http://www.sesamestreet.org/browseallvideos?p_p_id=browsegpv_WAR_browsegpvportlet&p_p_lifecycle=0&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_browsegpv_WAR_browsegpvportlet_cmd=search&_browsegpv_WAR_browsegpvportlet_assetType=VIDEO&_browsegpv_WAR_browsegpvportlet_keywords=&_browsegpv_WAR_browsegpvportlet_sortType=asc&_browsegpv_WAR_browsegpvportlet_field=title&_browsegpv_WAR_browsegpvportlet_viewType=tiled&_browsegpv_WAR_browsegpvportlet_subject=&_browsegpv_WAR_browsegpvportlet_theme=&_browsegpv_WAR_browsegpvportlet_character=&_browsegpv_WAR_browsegpvportlet_muppetURL=";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "By Subject";
            cat.Url = @"http://www.sesamestreet.org/browsevideosbysubject";
            cat.Other = @"http://www.sesamestreet.org/sesamestreet-theme/images/custom/rounded-container/browse_by_subject_bg.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "By Theme";
            cat.Url = @"http://www.sesamestreet.org/browsevideosbytheme";
            cat.Other = @"http://www.sesamestreet.org/sesamestreet-theme/images/custom/rounded-container/browse_by_theme_bg.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "By Character";
            cat.Url = @"http://www.sesamestreet.org/browsevideosbycharacter";
            cat.Other = @"http://www.sesamestreet.org/sesamestreet-theme/images/custom/rounded-container/browse_by_characters_bg.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Songs";
            cat.Url = @"http://www.sesamestreet.org/browseallvideos?p_p_id=browsegpv_WAR_browsegpvportlet&p_p_lifecycle=1&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_browsegpv_WAR_browsegpvportlet_assetType=SONG";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Classic clips";
            cat.Url = @"http://www.sesamestreet.org/browseallvideos?p_p_id=browsegpv_WAR_browsegpvportlet&p_p_lifecycle=1&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_browsegpv_WAR_browsegpvportlet_assetType=CLASSIC";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;

            List<Category> categories = new List<Category>();
            string webData = GetWebData(url);
            webData = GetSubString(webData, "main-section", "footer");
            int splitInd = webData.IndexOf("orange");
            int nWithImage = 0;

            if (!string.IsNullOrEmpty(webData))
            {
                List<string> names = new List<string>();
                Match m = regEx_SubCategory.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    if (!names.Contains(cat.Name))
                    {
                        cat.Url = m.Groups["url"].Value;
                        cat.HasSubCategories = false;
                        categories.Add(cat);
                        cat.ParentCategory = parentCategory;
                        names.Add(cat.Name);
                        if (splitInd == -1 || m.Index <= splitInd)
                            nWithImage++;
                    }
                    m = m.NextMatch();
                }

                if (parentCategory.Other != null)
                {

                    string pngName = (string)parentCategory.Other;
                    try
                    {
                        WebRequest request = WebRequest.Create(pngName);
                        WebResponse response = request.GetResponse();
                        Stream responseStream = response.GetResponseStream();
                        Bitmap png = new Bitmap(responseStream);

                        string bareFinalUrl = System.IO.Path.ChangeExtension(pngName, String.Empty);
                        string lsThumbLocation = OnlineVideoSettings.getInstance().msThumbLocation;
                        int newHeight = png.Height / nWithImage;
                        for (int i = 0; i < nWithImage; i++)
                        {
                            Bitmap newPng = new Bitmap(png.Width, newHeight);
                            Graphics g = Graphics.FromImage(newPng);
                            g.DrawImage(png, 0, -i * newHeight);
                            g.Dispose();

                            string finalUrl = bareFinalUrl + '_' + i.ToString() + ".PNG";
                            categories[i].Thumb = finalUrl;
                            string name = MediaPortal.Util.Utils.GetThumb(finalUrl);
                            name = System.IO.Path.GetFileNameWithoutExtension(name);

                            string imageLocation = lsThumbLocation + name + "L.jpg";
                            newPng.Save(imageLocation);
                        }
                    }
                    catch (Exception)
                    {
                        Log.Info("image not found : " + pngName);
                    }

                }
                parentCategory.SubCategoriesDiscovered = true;
            }

            parentCategory.SubCategories = categories;
            return parentCategory.SubCategories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            data = baseUrl + GetSubString(data, @"""configUrl"",escape(""", @"""");

            string uid = GetSubString(data, "&uid=", "&");

            XmlDocument doc = new XmlDocument();
            doc.Load(data);
            XmlNodeList nl = doc.SelectNodes(@"//itemList/video/myStreetUrl");
            foreach (XmlNode nd in nl)
            {
                if (nd.InnerText.Contains(uid))
                {
                    string url = nd.ParentNode.SelectSingleNode("filename").InnerText;
                    if (url.StartsWith("rtmp"))
                        return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
                    //return url;
                }
            }
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            firstPageUrl = ((RssLink)category).Url;
            pageNr = 1;
            return getPagedVideoList(firstPageUrl);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(firstPageUrl);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(firstPageUrl);
        }

        private List<VideoInfo> getPagedVideoList(string url)
        {
            if (pageNr > 1)
                url = url + "&_browsegpv_WAR_browsegpvportlet_pageNumber=" + pageNr.ToString();
            string webData = GetWebData(url);
            hasNextPage = webData.Contains(@"<span>More</span>");
            webData = GetSubString(webData, "tile-content-display", "footer");
            List<VideoInfo> videos = new List<VideoInfo>();

            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = m.Groups["url"].Value;
                    video.ImageUrl = baseUrl + m.Groups["thumb"].Value;
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override bool HasPreviousPage
        {
            get { return pageNr > 1; }
        }

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            string searchUrl = @"http://www.sesamestreet.org/browseallvideos?p_p_id=browsegpv_WAR_browsegpvportlet&p_p_lifecycle=0&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_browsegpv_WAR_browsegpvportlet_cmd=search&_browsegpv_WAR_browsegpvportlet_assetType=VIDEO&_browsegpv_WAR_browsegpvportlet_keywords={0}&_browsegpv_WAR_browsegpvportlet_sortType=asc&_browsegpv_WAR_browsegpvportlet_field=&_browsegpv_WAR_browsegpvportlet_viewType=tiled&_browsegpv_WAR_browsegpvportlet_subject=&_browsegpv_WAR_browsegpvportlet_theme=&_browsegpv_WAR_browsegpvportlet_character=&_browsegpv_WAR_browsegpvportlet_muppetURL=";
            firstPageUrl = String.Format(searchUrl, query);
            return getPagedVideoList(firstPageUrl);
        }

        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }
}
