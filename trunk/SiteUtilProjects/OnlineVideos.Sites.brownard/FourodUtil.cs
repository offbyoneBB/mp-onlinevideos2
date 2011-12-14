using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RTMP_LIB;

namespace OnlineVideos.Sites
{
    public class FourodUtil : SiteUtilBase
    {
        DateTime lastRefesh = DateTime.MinValue;

        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefesh).TotalMinutes > 15)
            {
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        cat.HasSubCategories = true;
                        cat.SubCategoriesDiscovered = false;
                        if(string.IsNullOrEmpty(cat.Thumb))
                            cat.Thumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, Settings.Name);
                    }
                }
                lastRefesh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url + "/title/brand-list/page-{0}";
            int page = 1;

            int cats = 50;

            List<Category> subCats = new List<Category>();

            while (cats == 50)
            {
                cats = 0;
                string html = GetWebData(string.Format(url, page));
                foreach (Match catMatch in new Regex("<li.*?<a class=\".*?\" href=\"/programmes/(.*?)/4od\".*?<img src=\"(.*?)\".*?<p class=\"title\">(.*?)</p>.*?<p class=\"synopsis\">(.*?)</p>", RegexOptions.Singleline).Matches(html))
                {
                    Category cat = new Category();
                    cat.Name = cleanString(catMatch.Groups[3].Value);
                    cat.Description = cleanString(catMatch.Groups[4].Value);
                    cat.Thumb = "http://www.channel4.com" + catMatch.Groups[2].Value;
                    cat.Other = catMatch.Groups[1].Value;
                    cat.ParentCategory = parentCategory;
                    subCats.Add(cat);
                    cats++;
                }
                page++;
            }
            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }


        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListInternal((string)category.Other, category.Name);
        }

        List<VideoInfo> getVideoListInternal(string epId, string seriesTitle)
        {
            List<VideoInfo> vids = new List<VideoInfo>();

            string html = GetWebData("http://www.channel4.com/programmes/" + epId + "/4od");

            if (Regex.IsMatch(html, @"<li id=""series\d+"" [^>]*>\s*<ol class=""episode-list"))
                html = Regex.Replace(html, @"<li id=""recentlyOn""[^>]*>\s*<ol[^>]*>.*?</ol>", "", RegexOptions.Singleline);

            string epHtml = new Regex("<ol class=\"all-series\">(.*?)</div>", RegexOptions.Singleline).Match(html).Groups[1].Value;

            foreach (Match vidMatch in new Regex("<li.*?data-episode-number=\"(.*?)\".*?data-assetid=\"(.*?)\".*?data-episodeUrl=\"(.*?)\".*?data-image-url=\"(.*?)\".*?data-episodeTitle=\"(.*?)\".*?data-episodeInfo=\"(.*?)\".*?data-episodeSynopsis=\"(.*?)\".*?data-series-number=\"(.*?)\"", RegexOptions.Singleline).Matches(epHtml))
            {
                VideoInfo vid = new VideoInfo();

                string epTitle = vidMatch.Groups[5].Value;
                string epInfo = vidMatch.Groups[6].Value;

                if (cleanString(epTitle).ToLower() == seriesTitle.ToLower() && epInfo != "")
                    vid.Title = epInfo;
                else
                {
                    vid.Title = epTitle;
                    if (!string.IsNullOrEmpty(epInfo))
                        vid.Airdate = epInfo;
                }

                vid.Title = cleanString(vid.Title);

                string img = vidMatch.Groups[4].Value;
                if (img == "")
                    vid.ImageUrl = new Regex("<meta property=\"og:image\" content=\"(.*?)\"", RegexOptions.Singleline).Match(html).Groups[1].Value;
                else
                    vid.ImageUrl = "http://www.channel4.com" + img;

                vid.Description = stripTags(vidMatch.Groups[7].Value);
                vid.VideoUrl = vidMatch.Groups[2].Value;

                vids.Add(vid);
            }

            return vids;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> cats = new List<ISearchResultItem>();
            string searchReg = "{\"imgUrl\":\"(.*?)\".*?\"value\": \"(.*?)\".*?\"siteUrl\":\"(.*?)\",\"fourOnDemand\":\"true\"}";

            string searchHTML = GetWebData("http://www.channel4.com/search/predictive/?q=" + System.Web.HttpUtility.UrlEncode(query));
            foreach (Match result in new Regex(searchReg, RegexOptions.Singleline).Matches(searchHTML))
            {
                Category cat = new Category();
                cat.Thumb = "http://www.channel4.com" + result.Groups[1].Value;
                cat.Name = cleanString(result.Groups[2].Value);
                cat.Other = new Regex("programmes/(.*?)/4od").Match(result.Groups[3].Value).Groups[1].Value;
                cats.Add(cat);
            }

            return cats;
        }

        string stripTags(string s)
        {
            s = cleanString(s);
            return s.Replace("<p>", "").Replace("</p>", "\r\n");
        }

        string cleanString(string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Trim();
        }

        string playUrl = "";
        string playPath = "";
        string auth = "";

        public override string getUrl(VideoInfo video)
        {
            string epId = video.VideoUrl;
            string url = string.Format("http://ais.channel4.com/asset/{0}", epId);

            string xml = GetWebData(url);
            string uriData = new Regex("<uriData>(.*?)</uriData>", RegexOptions.Singleline).Match(xml).Groups[1].Value;

            string streamUri = new Regex("<streamUri>(.*?)</streamUri>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            string token = new Regex("<token>(.*?)</token>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            string cdn = new Regex("<cdn>(.*?)</cdn>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            string decryptedToken = new FourodDecrypter().Decode4odToken(token);

            if (cdn == "ll")
            {
                string ip = new Regex("<ip>(.*?)</ip>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                string e = new Regex("<e>(.*?)</e>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                auth = string.Format("e={0}&ip={1}&h={2}", e, ip, decryptedToken);
            }
            else
            {
                string fingerprint = new Regex("<fingerprint>(.*?)</fingerprint>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                string slist = new Regex("<slist>(.*?)</slist>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                auth = string.Format("auth={0}&aifp={1}&slist={2}", decryptedToken, fingerprint, slist);
            }

            playUrl = new Regex("(.*?)mp4:", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playUrl = playUrl.Replace(".com/", ".com:1935/");

            playPath = new Regex("(mp4:.*)", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playPath = playPath + "?" + auth;

            return new MPUrlSourceFilter.RtmpUrl(playUrl + "?ovpfv=1.1&" + auth)
            {
                PlayPath = playPath,
                SwfUrl = "http://www.channel4.com/static/programmes/asset/flash/swf/4odplayer-11.8.5.swf",
                SwfVerify = true,
                Live = false
            }.ToString();
        }
    }
}
