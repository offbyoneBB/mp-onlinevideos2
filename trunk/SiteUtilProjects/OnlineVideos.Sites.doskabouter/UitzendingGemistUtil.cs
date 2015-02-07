using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class UitzendingGemistUtil : GenericSiteUtil
    {

        public enum VideoQuality { H264_sb, H264_bb, H264_std };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Format"), Description("Prefer this format when there are more than one for the desired quality.")]
        VideoQuality preferredQuality = VideoQuality.H264_std;

        private enum UgType { None, MostViewed, Recent, Omroepen, Genres, AtoZ, Type1, Series };
        private Regex regEx_AtoZ;

        private UgType currenttype = UgType.None;
        private int pageNr = 0;
        private string baseVideoListUrl = null;

        string matchaz = @"<td><a\shref=""(?<url>[^""]*)""(?:\sclass=""active"")?>(?<title>[^<]*)</a></td>";

        public override int DiscoverDynamicCategories()
        {
            regEx_AtoZ = new Regex(matchaz, defaultRegexOptions);

            Settings.Categories.Add(new RssLink() { Name = "Meest bekeken", Url = @"http://www.npo.nl/uitzending-gemist", Other = UgType.MostViewed });
            Settings.Categories.Add(new RssLink() { Name = "Op datum", Url = @"http://www.npo.nl/uitzending-gemist", Other = UgType.Recent });
            Settings.Categories.Add(new RssLink() { Name = "Omroepen", Url = @"http://www.npo.nl/series", Other = UgType.Omroepen });
            Settings.Categories.Add(new RssLink() { Name = "Genres", Url = @"http://www.npo.nl/series", Other = UgType.Genres });
            Settings.Categories.Add(new RssLink() { Name = "Programma’s A-Z", Url = @"http://www.npo.nl/series", Other = UgType.AtoZ });
            foreach (RssLink cat in Settings.Categories)
                cat.HasSubCategories = true;

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            switch ((UgType)parentCategory.Other)
            {
                case UgType.MostViewed: return getSubcats(parentCategory, UgType.None, @"most-viewed-date-range", @"http://www.npo.nl/uitzending-gemist/meest-bekeken?date={0}");
                case UgType.Recent: return getSubcats(parentCategory, UgType.None, @"sort_date", @"http://www.npo.nl/zoeken?utf8=%E2%9C%93&sort_date={0}");
                case UgType.Omroepen: return getSubcats(parentCategory, UgType.Type1, @"broadcaster", @"http://www.npo.nl/series?utf8=%E2%9C%93&genre=&broadcaster={0}&av_type=video");
                case UgType.Genres: return getSubcats(parentCategory, UgType.Type1, @"genre", @"http://www.npo.nl/series?utf8=%E2%9C%93&genre={0}&broadcaster=&av_type=video");
                case UgType.AtoZ: return getAtoZSubcats(parentCategory);
                case UgType.Type1: return getType1Subcats(parentCategory);
            }
            return 0;
        }

        private int getAtoZSubcats(Category parentCat)
        {
            Regex sav = regEx_dynamicSubCategories;
            regEx_dynamicSubCategories = regEx_AtoZ;
            int res = base.DiscoverSubCategories(parentCat);
            foreach (Category cat in parentCat.SubCategories)
            {
                cat.Other = UgType.Type1;
                cat.HasSubCategories = true;
            }
            regEx_dynamicSubCategories = sav;
            return res;
        }


        private string getSubcatWebData(string url)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");
            return GetWebData(url, cookies: GetCookie(), forceUTF8: true, headers: headers);
        }


        public override int ParseSubCategories(Category parentCategory, string data)
        {
            string url = (parentCategory as RssLink).Url;
            int res = base.ParseSubCategories(parentCategory, data);
            if (res > 0)
            {
                pageNr++;
                foreach (Category cat in parentCategory.SubCategories)
                    cat.Other = UgType.Series;
                //always assume next page, no reliable detection possible
                string nextCatPageUrl;
                if (url.Contains('?'))
                    nextCatPageUrl = url + "&page=" + pageNr.ToString();
                else
                    nextCatPageUrl = url + "?page=" + pageNr.ToString();
                if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextCatPageUrl, ParentCategory = parentCategory });
            }
            return res;

        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            string data = getSubcatWebData(category.Url);

            category.ParentCategory.SubCategories.Remove(category);
            int oldAmount = category.ParentCategory.SubCategories.Count;
            return ParseSubCategories(category.ParentCategory, data);
        }

        private int getType1Subcats(Category parentCat)
        {
            pageNr = 1;
            string data = GetWebData((parentCat as RssLink).Url, cookies: GetCookie(), forceUTF8: true);
            return ParseSubCategories(parentCat, data);
        }

        private int getSubcats(Category parentCat, UgType subType, string id, string format)
        {
            HtmlDocument data = GetWebData<HtmlDocument>(((RssLink)parentCat).Url, cookies: GetCookie(), forceUTF8: true);
            var node = data.DocumentNode.Descendants("select").Where(sel => sel.GetAttributeValue("id", "") == id).FirstOrDefault();
            var options = node.Descendants("option").Where(option => option.GetAttributeValue("disabled", "") != "disabled");
            parentCat.SubCategories = new List<Category>();
            foreach (var option in options)
            {
                parentCat.SubCategories.Add(new RssLink()
                {
                    Name = option.NextSibling.InnerText,
                    Url = String.Format(format, option.GetAttributeValue("value", "")),
                    HasSubCategories = subType != UgType.None,
                    Other = subType,
                    ParentCategory = parentCat
                });
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            pageNr = 1;
            baseVideoListUrl = ((RssLink)category).Url;
            currenttype = (UgType)category.Other;
            return lowGetVideoList(baseVideoListUrl, currenttype);
        }

        private List<VideoInfo> lowGetVideoList(string url, UgType type)
        {
            string data = GetWebData(url, cookies: GetCookie(), forceUTF8: true);

            List<VideoInfo> res = base.Parse(url, data);
            foreach (VideoInfo video in res)
            {
                int p = video.Airdate.LastIndexOf('·');
                if (p >= 0)
                {
                    video.Length = video.Airdate.Substring(p + 1).Trim();
                    video.Airdate = video.Airdate.Substring(0, p).Trim();
                }
            }

            pageNr++;

            if (type == UgType.None)
            {
                nextPageAvailable = true;
                nextPageUrl = baseVideoListUrl + "&page=" + pageNr.ToString();
            }
            if (type == UgType.Series)
            {
                nextPageAvailable = res.Count >= 8 && data.Contains(@"<span>Meer afleveringen</span>");
                if (nextPageAvailable)
                    nextPageUrl = baseVideoListUrl + "/search?media_type=broadcast&start_date=&end_date=&start=" + (pageNr * 8 - 8).ToString() + "&rows=8";
                else
                    nextPageUrl = String.Empty;
            }

            return res;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return lowGetVideoList(nextPageUrl, currenttype);
        }


        public override string GetVideoUrl(VideoInfo video)
        {
            string result = String.Empty;

            int p = video.VideoUrl.LastIndexOf('/');
            if (p >= 0)
            {
                string id = video.VideoUrl.Substring(p + 1);
                string webData = GetWebData(@"http://ida.omroep.nl/npoplayer/i.js");


                Match m = Regex.Match(webData, @"token\s*=\s*""(?<token>[^""]*)""", defaultRegexOptions);
                if (m.Success)
                {
                    webData = GetWebData(String.Format(fileUrlFormatString, id) + m.Groups["token"].Value);
                    JObject contentData = (JObject)JObject.Parse(webData);
                    JArray items = contentData["streams"] as JArray;
                    List<KeyValuePair<string, string>> playbackOptions = new List<KeyValuePair<string, string>>();
                    foreach (JToken item in items)
                    {
                        string s = item.Value<string>();

                        m = Regex.Match(s, @"/ida/(?<quality>[^/]*)/");
                        if (m.Success)
                        {
                            string quality = m.Groups["quality"].Value;
                            try
                            {
                                VideoQuality vq = (VideoQuality)Enum.Parse(typeof(VideoQuality), quality, true);
                                if (Enum.IsDefined(typeof(VideoQuality), vq) && vq.Equals(preferredQuality))
                                    result = s;
                            }
                            catch (ArgumentException)
                            {
                            };

                            playbackOptions.Add(new KeyValuePair<string, string>(quality, s));
                        }
                    }
                    playbackOptions.Sort(Compare);
                    video.PlaybackOptions = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, string> kv in playbackOptions)
                        video.PlaybackOptions.Add(kv.Key, kv.Value);
                }

                if (String.IsNullOrEmpty(result))
                    result = video.PlaybackOptions.Last().Value;
            }
            return result;
        }

        private static readonly string[] sortedQualities = new string[] { "h264_sb", "h264_bb", "h264_std" };

        private int Compare(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            int res = Array.IndexOf(sortedQualities, a.Key).CompareTo(Array.IndexOf(sortedQualities, b.Key));
            if (res != 0)
                return res;
            return a.Value.CompareTo(b.Value);
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            pageNr = 1;
            baseVideoListUrl = string.Format(searchUrl, query);
            currenttype = UgType.None;
            return lowGetVideoList(baseVideoListUrl, currenttype).ConvertAll<SearchResultItem>(v => v as SearchResultItem);
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new UZGVideoInfo();
        }

    }

    public class UZGVideoInfo : VideoInfo
    {
        public override string GetPlaybackOptionUrl(string option)
        {
            string s = base.GetPlaybackOptionUrl(option);
            string webData = WebCache.Instance.GetWebData(s);
            Match m = Regex.Match(webData, @"\((?<res>.*)\)");
            if (m.Success)
                webData = m.Groups["res"].Value;
            JObject contentData = (JObject)JObject.Parse(webData);
            return contentData.Value<string>("url");
        }
    }

}
