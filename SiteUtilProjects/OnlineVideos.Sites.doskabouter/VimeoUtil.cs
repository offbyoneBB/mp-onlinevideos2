using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
using System.ComponentModel;
using OnlineVideos.Sites.doskabouter.Vimeo;

namespace OnlineVideos.Sites
{
    public class VimeoUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;
        [Category("OnlineVideosUserConfiguration"), Description("Defines the default number of videos to display per page. (max 50)")]
        int pageSize = 26;

        private const string StandardAdvancedApiUrl = "http://vimeo.com/api/rest/v2";
        private string currentVideoListUrl;
        private Regex urlRegex;
        private int pageNr = 0;
        private int nPages = 0;

        public override void Initialize(SiteSettings siteSettings)
        {
            resolveHoster = HosterResolving.FromUrl;
            urlRegex = new Regex(@"http://vimeo.com/(?<id>[^/]*)/(?<kind>(channels|videos|likes|groups|albums))/", defaultRegexOptions);
            base.Initialize(siteSettings);
        }

        #region overrides

        public override int DiscoverDynamicCategories()
        {

            if (!useDynamicCategories)
            {
                Settings.DynamicCategoriesDiscovered = true;

                foreach (Category cat in Settings.Categories)
                {
                    Match m = Regex.Match(((RssLink)cat).Url, @"http://vimeo.com/[^/]*/(?<kind>(channels|groups|albums)*)/");
                    if (m.Success)
                        cat.HasSubCategories = "channels".Equals(m.Groups["kind"].Value) ||
                            "groups".Equals(m.Groups["kind"].Value) || "albums".Equals(m.Groups["kind"].Value);
                }
                return 0;
            }


            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            string url = StandardAdvancedApiUrl + "?method=vimeo.categories.getall&page=0&per_page=50";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AuthBase.BuildOAuthApiRequestUrl(url));

            Settings.DynamicCategoriesDiscovered = true;

            foreach (XmlNode categNode in xmlDoc.SelectNodes(@"//rsp/categories/category"))
            {
                RssLink cat = new RssLink();
                XmlNode nd = categNode.SelectSingleNode("name");
                if (nd != null)
                    cat.Name = nd.InnerText;
                else
                    cat.Name = categNode.SelectSingleNode("title").InnerText;

                cat.Url = categNode.SelectSingleNode("url").InnerText;
                cat.Other = categNode.Attributes["word"].Value;
                AddSubcats(cat, categNode);
                Settings.Categories.Add(cat);
            };

            return Settings.Categories.Count;
        }

        private void AddSubcats(Category parentCat, XmlNode categNode)
        {
            parentCat.HasSubCategories = true;
            parentCat.SubCategories = new List<Category>();
            foreach (XmlNode subCategNode in categNode.SelectNodes(@"subcategories/subcategory"))
            {
                RssLink cat = new RssLink();
                cat.Name = subCategNode.Attributes["name"].Value;
                cat.Url = subCategNode.Attributes["url"].Value;
                cat.Other = subCategNode.Attributes["word"].Value;
                cat.ParentCategory = parentCat;
                parentCat.SubCategories.Add(cat);
            };
            RssLink videosCat = new RssLink()
            {
                Name = "Videos",
                Url = categNode.SelectSingleNode("url").InnerText,
                Other = categNode.Attributes["word"].Value,
                ParentCategory = parentCat
            };
            parentCat.SubCategories.Add(videosCat);
            parentCat.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Match m = urlRegex.Match(((RssLink)parentCategory).Url);
            if (!m.Success)
                return base.DiscoverSubCategories(parentCategory);
            else
            {
                string kind = m.Groups["kind"].Value;
                string url = StandardAdvancedApiUrl + "?method=vimeo." + kind + ".getall&user_id=" + m.Groups["id"].Value;
                return subcatsFromVimeo(parentCategory, url, kind);
            }
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            currentVideoListUrl = null;
            string url = ((RssLink)category).Url;
            string key = category.Other as string;
            if (!String.IsNullOrEmpty(key))
            {
                currentVideoListUrl = String.Format("{0}?method=vimeo.categories.getRelatedVideos&category={1}&per_page={2}&full_response=1&page=",
                    StandardAdvancedApiUrl, key, pageSize);
                pageNr = 1;
                return videoListFromVimeo(currentVideoListUrl + pageNr.ToString());
            }
            if (url.ToLowerInvariant().StartsWith(@"http://vimeo.com/categories/"))
                return base.GetVideos(category);
            else
            {
                string query = null;
                Match m = urlRegex.Match(url);
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
                    m = Regex.Match(url, @"http://vimeo.com/(?<kind>(channels|videos|likes|groups|albums))/(?<id>[^/]*)");
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

        public override List<VideoInfo> GetNextPageVideos()
        {
            if (currentVideoListUrl == null)
                return base.GetNextPageVideos();
            else
            {
                pageNr++;
                return videoListFromVimeo(currentVideoListUrl + pageNr.ToString());
            }
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            currentVideoListUrl = String.Format(
                StandardAdvancedApiUrl + "?method=vimeo.videos.search&per_page={0}&query={1}&full_response=1&page=", pageSize, query);
            pageNr = 1;
            return 
                videoListFromVimeo(currentVideoListUrl + pageNr.ToString())
                .ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = null;
            string res = base.GetVideoUrl(video);
            var vimeoHoster = OnlineVideos.Hoster.HosterFactory.GetHoster("vimeo") as OnlineVideos.Hoster.Vimeo;
            if (vimeoHoster != null)
                video.SubtitleText = vimeoHoster.subtitleText;

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                return video.PlaybackOptions.First().Value;
            else
                return res;

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
                    video.Length = video.Length + '|' + Translation.Instance.Airdate + ": " + Airdate;

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
