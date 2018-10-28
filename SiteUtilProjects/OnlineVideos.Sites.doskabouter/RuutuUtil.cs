using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class RuutuUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
            foreach (RssLink cat in Settings.Categories)
                cat.HasSubCategories = true;
            return res;
        }

        private enum RuType { None, LapsetSeries };

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.VideoUrl.Contains("series"))
                video.VideoUrl = WebCache.Instance.GetRedirectedUrl(video.VideoUrl);
            string data = GetWebData(GetFormattedVideoUrl(video));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            var vidUrl = doc.SelectSingleNode(@"//Clip/SourceFile").InnerText;
            if (vidUrl.ToLowerInvariant().Contains("[not-used]"))
            {
                vidUrl = doc.SelectSingleNode(@"//Clip/WebHLSMediaFiles/WebHLSMediaFile").InnerText;
                data = GetWebData(vidUrl);
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(data, vidUrl, (x, y) => y.Bandwidth.CompareTo(x.Bandwidth), (x) => x.Width + "x" + x.Height);
                return video.GetPreferredUrl(true);
            }
            else
                return vidUrl;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var doc = getDocument(parentCategory);

            if (parentCategory.Name == "Kaikki ohjelmat")
                return AddKaikki(doc, parentCategory);

            if (((RssLink)parentCategory).ParentCategory == null)
                return AddOhjelmat(doc, parentCategory);

            if (parentCategory.Other is int)
            {
                if (parentCategory.Name == "Uutiset")
                    return Series(doc, parentCategory, "video");
                else
                    return Series(doc, parentCategory, "series");
            }
            else
            {
                if (!((RssLink)parentCategory).Url.Contains("prod-component-api"))
                {
                    var m = Regex.Match(doc.DocumentNode.OuterHtml, @"series-(?<id>\d+)");
                    if (m.Success)
                    {
                        return OneSeries(parentCategory, @"https://prod-component-api.nm-services.nelonenmedia.fi/api/series/" + m.Groups["id"].Value + "?userroles=anonymous&clients=ruutufi%2Cruutufi-react");
                    }
                }
                return OneSeries(parentCategory, ((RssLink)parentCategory).Url);
            }

        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return Series(getDocument(category), category, "series");
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;
            var json = GetWebData<JObject>(((RssLink)category).Url);
            List<VideoInfo> res = new List<VideoInfo>();

            foreach (var item in json["items"])
            {
                if (item.SelectToken("link.href") != null)
                {
                    VideoInfo vid = new VideoInfo()
                    {
                        Title = item["title"].Value<string>(),
                        Description = item["description"].Value<string>(),
                        Thumb = item["media"]["images"]["640x360"].Value<string>(),
                        VideoUrl = @"https://www.ruutu.fi" + item["link"]["href"].Value<string>()
                    };
                    res.Add(vid);
                }
            }
            return res;
        }

        private int OneSeries(Category parentcat, string url)
        {
            parentcat.SubCategories = new List<Category>();

            var json = GetWebData<JObject>(url);
            foreach (var component in json["components"])
            {
                if (component["type"].Value<string>() == "TabContainer")
                {
                    foreach (var item in component["content"]["items"])
                    {
                        var query = item["content"]["items"][0]["content"]["query"];
                        RssLink cat = new RssLink()
                        {
                            Name = item["label"]["text"].Value<String>(),
                            Url = query["url"].Value<String>() + "?offset=0&limit=20&current_season_id=" + query["params"]["current_season_id"] + "&current_series_id=" + query["params"]["current_series_id"],
                            ParentCategory = parentcat
                        };
                        parentcat.SubCategories.Add(cat);
                    }
                }
            }
            parentcat.SubCategoriesDiscovered = true;
            return parentcat.SubCategories.Count;
        }


        private int Series(HtmlDocument doc, Category parentcat, string kind)
        {
            int offset = (int)parentcat.Other;

            var json = GetWebData<JObject>(((RssLink)parentcat).Url + offset.ToString());
            parentcat.SubCategories = new List<Category>();
            foreach (var item in json["items"])
            {
                RssLink cat = new RssLink()
                {
                    Name = item["title"].Value<string>(),
                    Description = item["description"].Value<string>(),
                    Thumb = item["media"]["images"]["640x360"].Value<string>(),
                    Url = @"https://prod-component-api.nm-services.nelonenmedia.fi/api/" + kind + "/" + item["link"]["target"]["value"].Value<string>() + "?userroles=anonymous&clients=ruutufi%2Cruutufi-react",
                    HasSubCategories = true,
                    ParentCategory = parentcat

                };
                parentcat.SubCategories.Add(cat);
            }

            if (json["hits"].Value<int>() >= offset + parentcat.SubCategories.Count)
            {
                var nextPage = new NextPageCategory() { Url = ((RssLink)parentcat).Url, Other = offset + 20 };
                parentcat.SubCategories.Add(nextPage);
            }
            parentcat.SubCategoriesDiscovered = true;
            return parentcat.SubCategories.Count;
        }

        private int AddKaikki(HtmlDocument doc, Category parentCategory)
        {
            var root = doc.DocumentNode.SelectSingleNode(@"//section/div");
            parentCategory.SubCategories = new List<Category>();
            RssLink sub = null;
            foreach (var node in root.ChildNodes)
            {
                if (node.Attributes["id"] != null)
                {
                    sub = new RssLink()
                    {
                        Name = node.Attributes["id"].Value,
                        ParentCategory = parentCategory,
                        HasSubCategories = true,
                        SubCategoriesDiscovered = true
                    };
                    sub.SubCategories = new List<Category>();
                    parentCategory.SubCategories.Add(sub);
                }
                else
                {
                    var subsub = new RssLink()
                    {
                        Name = node.SelectSingleNode(@".//h2").InnerText,
                        ParentCategory = sub,
                        HasSubCategories = true,
                        Url = FormatDecodeAbsolutifyUrl(baseUrl, node.SelectSingleNode(@".//a").Attributes["href"].Value, "", UrlDecoding.None)
                    };
                    sub.SubCategories.Add(subsub);
                }

            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private int AddOhjelmat(HtmlDocument doc, Category parentCat)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//section[@data-id and (@data-element-type='CardHoverbox' or @data-element-type='CardDefault')][h4]");
            parentCat.SubCategories = new List<Category>();
            foreach (var node in nodes)
            {
                RssLink sub = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(node.InnerText),
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    Url = @"https://prod-component-api.nm-services.nelonenmedia.fi/api/component/" + node.Attributes["data-id"].Value + "?limit=20&offset=",
                    Other = 0
                };
                parentCat.SubCategories.Add(sub);
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private HtmlDocument getDocument(Category cat)
        {
            string webData = GetWebData(((RssLink)cat).Url, forceUTF8: true);
            return getDocument(webData);
        }

        private HtmlDocument getDocument(string htmlText)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            return doc;
        }

    }
}
