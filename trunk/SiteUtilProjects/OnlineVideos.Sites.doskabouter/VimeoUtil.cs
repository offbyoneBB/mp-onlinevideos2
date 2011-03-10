using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.ComponentModel;
using OnlineVideos.Sites.doskabouter.Vimeo;

namespace OnlineVideos.Sites
{
    public class VimeoUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;
        [Category("OnlineVideosUserConfiguration"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 26;

        private const string StandardAdvancedApiUrl = "http://vimeo.com/api/rest/v2";
        private string currentVideoListUrl;
        private int pageNr = 0;
        private int nPages = 0;

        public override void Initialize(SiteSettings siteSettings)
        {

            resolveHoster = HosterResolving.FromUrl;

            // for dynamic categories
            baseUrl = @"http://vimeo.com/ajax/user/home_explore";
            dynamicCategoriesRegEx = @"<div\sclass=""thumbnail""><a\shref=""(?<url>[^""]*)"">\s<img\ssrc=""(?<thumb>[^""]*)""[^>]*>\s*</a></div>\s*<div\sclass=""digest"">\s*<h3><a[^>]*>(?<title>[^<]*)</a></h3>";
            dynamicSubCategoriesRegEx = @"<li><a\shref=""(?<url>/categories/[^""]*)"">(?<title>[^<]*)</a></li>";
            dynamicSubCategoryUrlFormatString = @"{0}/videos/sort:newest/format:detail";
            videoListRegEx = @"<div\sclass=""thumbnail_box"">\s*<a\sclass=""thumbnail""\shref=""(?<VideoUrl>[^""]*)""[^>]*>\s*<img\ssrc=""(?<ImageUrl>[^""]*)""[^>]*>\s*</a>\s*</div>\s*<div\sclass=""detail"">\s*<div\sclass=""title"">\s*<a[^>]*>(?<Title>[^<]*)</a>\s*</div>\s*<div\sclass=""date"">(?<Airdate>[^<]*)<span\sclass=""stats"">\s*<span\sclass=""plays"">[^<]*</span>\s*<span\sclass=""likes"">[^<]*</span>\s*<span\sclass=""comments"">[^<]*</span>\s*</span>\s*</div>\s*<div\sclass=""description"">(?<Description>[^<]*)<";
            nextPageRegEx = @"<li\sclass=""arrow""><a\shref=""(?<url>[^""]*)""><img\ssrc=""http://a\.vimeocdn\.com/images/page_arrow_next_on\.gif""\salt=""next""\s/>";
            nextPageRegExUrlFormatString = @"{0}/format:detail";

            base.Initialize(siteSettings);
        }

        #region overrides

        public override int DiscoverDynamicCategories()
        {
            int res = 0;
            if (useDynamicCategories)
                res = base.DiscoverDynamicCategories();
            Settings.DynamicCategoriesDiscovered = true;
            foreach (Category cat in Settings.Categories)
            {
                Match m = Regex.Match(((RssLink)cat).Url, @"http://vimeo.com/user[^/]*/(?<kind>[^/]*)");
                if (m.Success)
                    cat.HasSubCategories = "channels".Equals(m.Groups["kind"].Value) ||
                        "groups".Equals(m.Groups["kind"].Value) || "albums".Equals(m.Groups["kind"].Value);
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Match m = Regex.Match(((RssLink)parentCategory).Url, @"http://vimeo.com/(?<username>user[^/]*)/(?<kind>[^/]*)");
            if (!m.Success)
                return base.DiscoverSubCategories(parentCategory);
            else
            {
                string kind = m.Groups["kind"].Value;
                string url = StandardAdvancedApiUrl + "?method=vimeo." + kind + ".getall&user_id=" + m.Groups["username"].Value;
                return subcatsFromVimeo(parentCategory, url, kind);
            }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentVideoListUrl = null;
            string url = ((RssLink)category).Url;
            if (url.ToLowerInvariant().StartsWith(@"http://vimeo.com/categories/"))
                return base.getVideoList(category);
            else
            {
                string query = null;
                Match m = Regex.Match(url, @"http://vimeo.com/(?<id>user[^/]*)/(?<kind>[^/]*)");
                if (m.Success)
                {
                    switch (m.Groups["kind"].Value)
                    {
                        case "videos": query = "vimeo.videos.getuploaded&user_id={1}"; break;
                        case "likes": query = "vimeo.videos.getlikes&user_id={1}"; break;
                    }
                }
                else
                {
                    m = Regex.Match(url, @"http://vimeo.com/(?<kind>[^/]*)/(?<id>[^/]*)");
                    if (m.Success)
                    {
                        switch (m.Groups["kind"].Value)
                        {
                            case "album": query = "vimeo.albums.getvideos&album_id={1}"; break;
                            case "groups": query = "vimeo.groups.getvideos&group_id={1}"; break;
                            case "channels": query = "vimeo.channels.getvideos&channel_id={1}"; break;
                        }
                    }
                }

                if (query != null)
                {
                    currentVideoListUrl = String.Format(
                        StandardAdvancedApiUrl + "?method=" + query + "&per_page={0}&full_response=1&page=",
                        pageSize, m.Groups["id"].Value);
                    pageNr = 1;
                    return videoListFromVimeo(currentVideoListUrl + pageNr.ToString());
                }
                else
                    return null;
            }
        }

        public override bool CanSearch { get { return true; } }

        public override bool HasNextPage
        {
            get
            {
                if (currentVideoListUrl == null)
                    return base.HasNextPage;
                else
                    return pageNr < nPages;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            if (currentVideoListUrl == null)
                return base.getNextPageVideos();
            else
            {
                pageNr++;
                return videoListFromVimeo(currentVideoListUrl + pageNr.ToString());
            }
        }

        public override List<VideoInfo> Search(string query)
        {
            currentVideoListUrl = String.Format(
                StandardAdvancedApiUrl + "?method=vimeo.videos.search&per_page={0}&query={1}&full_response=1&page=", pageSize, query);
            pageNr = 1;
            return videoListFromVimeo(currentVideoListUrl + pageNr.ToString());
        }

        #endregion

        private int subcatsFromVimeo(Category parentCategory, string url, string key)
        {
            XmlDocument xmlDoc = new XmlDocument();
            parentCategory.SubCategories = new List<Category>();
            xmlDoc.Load(AuthBase.BuildOAuthApiRequestUrl(url));
            XmlNodeList categNodes = xmlDoc.SelectNodes("//" + key + "/" + key.TrimEnd('s'));
            foreach (XmlNode categNode in categNodes)
            {
                RssLink cat = new RssLink();
                XmlNode nd = categNode.SelectSingleNode("name");
                if (nd != null)
                    cat.Name = nd.InnerText;
                else
                    cat.Name = categNode.SelectSingleNode("title").InnerText;

                nd = categNode.SelectSingleNode("description");
                if (nd != null)
                    cat.Description = nd.InnerText;

                cat.Url = categNode.SelectSingleNode("url").InnerText;

                nd = categNode.SelectSingleNode("logo_url");
                if (nd != null)
                    cat.Thumb = nd.InnerText;
                else
                    cat.Thumb = getThumbUrl(categNode.SelectNodes("//thumbnail"));
                cat.ParentCategory = parentCategory;

                parentCategory.SubCategories.Add(cat);
            }
            parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        private List<VideoInfo> videoListFromVimeo(string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AuthBase.BuildOAuthApiRequestUrl(url));
            double totalVideos = Int32.Parse(xmlDoc.SelectSingleNode("//videos").Attributes["total"].Value);
            nPages = Convert.ToInt32(Math.Ceiling(totalVideos / pageSize));
            XmlNodeList videoNodes = xmlDoc.SelectNodes("//videos/video");
            foreach (XmlNode videoNode in videoNodes)
            {
                VideoInfo video = new VideoInfo();
                video.Title = videoNode.SelectSingleNode("title").InnerText;
                video.Description = videoNode.SelectSingleNode("description").InnerText;
                video.VideoUrl = videoNode.SelectSingleNode("urls/url").InnerText;
                video.ImageUrl = getThumbUrl(videoNode.SelectNodes("thumbnails/thumbnail"));

                video.Length = TimeSpan.FromSeconds(Int32.Parse(videoNode.SelectSingleNode("duration").InnerText)).ToString();
                string Airdate = videoNode.SelectSingleNode("upload_date").InnerText;
                if (!String.IsNullOrEmpty(Airdate))
                    video.Length = video.Length + '|' + Translation.Airdate + ": " + Airdate;

                result.Add(video);
            }
            return result;
        }

        private string getThumbUrl(XmlNodeList nodeList)
        {
            string res = String.Empty;
            int max = 0;
            foreach (XmlNode thumbNode in nodeList)
            {
                int curr = Int32.Parse(thumbNode.Attributes["height"].Value);
                if (curr > max)
                {
                    if (String.IsNullOrEmpty(res) || curr < 400)
                        res = thumbNode.InnerText;
                    max = curr;
                }
            }
            return res;
        }
    }
}
