using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
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
            if (parentCategory.Name == "Ohjelmat")
                return AddOhjelmat(doc, parentCategory);
            if (parentCategory.Other is string)
                return DiscoverSubsNew(parentCategory, doc);
            if (parentCategory.Name == "Urheilu" || parentCategory.Name == "Uutiset" || parentCategory.Name == "Lapset")
                return DiscoverSubs(parentCategory, doc);
            if (RuType.LapsetSeries.Equals(parentCategory.Other))
                return DiscoverSubsNew(parentCategory, doc);
            return 0;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;
            var json = GetWebData<JObject>(((RssLink)category).Url);
            var htmlText = new StringBuilder();
            foreach (var obj in json["items"])
                htmlText.Append(obj.ToString());
            return myParse2(getDocument(htmlText.ToString()).DocumentNode);
        }


        private string getDescription(HtmlNode node)
        {
            var node2 = node.SelectSingleNode(".//p[@class='expanding-card__description']");//kausi
            if (node2 == null)
                node2 = node.SelectSingleNode(".//div[@class='list-item-main1']");//klipit
            if (node2 == null)
                node2 = node.SelectSingleNode(".//section[@class='ruutu-card__supporting-text']");//ohjelmat
            if (node2 != null)
                return node2.InnerText.Trim();
            return String.Empty;
        }

        private List<VideoInfo> myParse2(HtmlNode node)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var res = node.SelectNodes(".//div[@class='component-container__item']/div");
            if (res != null)//normal
            {
                foreach (var vid2 in res)
                {
                    HtmlNode vid;
                    if (vid2.ParentNode.Name == "a") //klipit
                        vid = vid2.ParentNode.ParentNode;
                    else
                        vid = vid2;
                    var node2 = vid.SelectSingleNode(".//a[@href]");
                    if (node2 != null && vid.SelectSingleNode(".//h2") != null)
                    {
                        VideoInfo video = CreateVideoInfo();

                        video.Title = vid.SelectSingleNode(".//h2").InnerText;
                        video.Description = getDescription(vid);
                        video.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, node2.Attributes["href"].Value, "", UrlDecoding.None);
                        video.Thumb = getImageUrl(vid);
                        var airDateNode = vid.SelectSingleNode(@".//div[@class='list-item-prefix']");
                        if (airDateNode == null)
                            airDateNode = vid.SelectSingleNode(@".//span[@class='ruutu-card__date']");
                        if (airDateNode != null)
                            video.Airdate = Helpers.StringUtils.PlainTextFromHtml(airDateNode.InnerText);

                        var kausiNode = vid.SelectSingleNode(@".//div[contains(@class,'field-name-field-season')]");
                        if (kausiNode != null)
                        {
                            string kausi = kausiNode.ChildNodes.Last().InnerText;
                            if (!String.IsNullOrEmpty(kausi))
                                video.Title += " kausi " + kausi;
                        }

                        var jaksoNode = vid.SelectSingleNode(@".//div[contains(@class,'field-name-field-episode')]");
                        if (jaksoNode != null)
                        {
                            string jakso = jaksoNode.ChildNodes.Last().InnerText;
                            if (!String.IsNullOrEmpty(jakso))
                                video.Title += " jakso " + jakso;
                        }
                        video.Title = cleanup(video.Title);
                        result.Add(video);
                    }
                }
            }
            return result;
        }

        private List<VideoInfo> myParse3(HtmlNodeCollection nodes)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var res = nodes;
            if (res != null)//normal
            {
                foreach (var vid in res)
                {
                    var node2 = vid.SelectSingleNode(".//a[@href]");
                    if (node2 != null)
                    {
                        VideoInfo video = CreateVideoInfo();

                        if (vid.SelectSingleNode(@".//span[@class='mdc-list-item__text__primary']") != null)
                            video.Title = vid.SelectSingleNode(@".//span[@class='mdc-list-item__text__primary']").InnerText;
                        video.Description = getDescription(vid);
                        video.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, node2.Attributes["href"].Value, "", UrlDecoding.None);
                        video.Thumb = getImageUrl(vid);
                        var airDateNode = vid.SelectSingleNode(@".//span[@class='prefix-piece']");
                        if (airDateNode != null)
                            video.Airdate = Helpers.StringUtils.PlainTextFromHtml(airDateNode.InnerText);
                        var durationNode = vid.SelectSingleNode(@".//span[@class='prefix-piece prefix-piece--duration']");
                        if (durationNode != null)
                            video.Length = Helpers.StringUtils.PlainTextFromHtml(durationNode.InnerText);

                        video.Title = cleanup(video.Title);
                        result.Add(video);
                    }
                }
            }
            return result;
        }

        private int AddKaikki(HtmlDocument doc, Category parentCategory)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//ul[@class='mdc-list--grid']");
            parentCategory.SubCategories = new List<Category>();
            foreach (var node in nodes)
            {
                RssLink sub = new RssLink()
                {
                    Name = node.SelectSingleNode(@"./li").InnerText,
                    ParentCategory = parentCategory,
                    HasSubCategories = true,
                };
                sub.SubCategories = GetKaikkiSubcats(node, sub);
                parentCategory.SubCategories.Add(sub);

            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public List<Category> GetKaikkiSubcats(HtmlNode parentNode, Category parentCat)
        {
            List<Category> res = new List<Category>();
            var nodes = parentNode.SelectNodes(@"./li[a[@href]]");
            foreach (var node in nodes)
            {
                var cat = new RssLink()
                {
                    Name = node.SelectSingleNode(@".//span[@class='mdc-list-item__text__primary']").InnerText.Trim(),
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, node.SelectSingleNode("a").Attributes["href"].Value, "", UrlDecoding.None),
                    Other = node.Attributes["data-id"].Value
                };
                cat.Name = Regex.Replace(cat.Name.Replace('\n', ' '), @"  +", " ", RegexOptions.Multiline);
                res.Add(cat);
            }

            parentCat.SubCategoriesDiscovered = true;
            return res;
        }

        private int AddOhjelmat(HtmlDocument doc, Category parentCat)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//section[div/h2[@class='component-container__header-title'][a]]");//tested
            foreach (var node in nodes)
            {
                Category sub = new Category()
                {
                    Name = node.SelectSingleNode(@".//h2/a/text()").InnerText.Trim(),
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    SubCategories = new List<Category>()
                };
                if (sub.Name == "Elokuvat")
                {
                    sub.HasSubCategories = false;
                    sub.Other = myParse2(node);
                    parentCat.SubCategories.Add(sub);
                }
                else
                {
                    var nodes2 = node.SelectNodes(".//div[@class='ruutu-card ruutu-card--hoverbox'][.//a[@href]]");//tested
                    if (nodes2 != null)
                    {
                        parentCat.SubCategories.Add(sub);
                        foreach (var node2 in nodes2)
                        {
                            RssLink sub2 = new RssLink()
                            {
                                Name = node2.SelectSingleNode(".//h2").InnerText.Trim(),
                                ParentCategory = sub,
                                Url = FormatDecodeAbsolutifyUrl(baseUrl, node2.SelectSingleNode(".//a[@href]").Attributes["href"].Value, "", UrlDecoding.None),
                                HasSubCategories = true,
                                Other = node2.Attributes["data-series-id"].Value
                            };
                            sub2.Description = getDescription(node2);
                            sub2.Thumb = getImageUrl(node2);
                            sub.SubCategories.Add(sub2);
                        }
                        sub.SubCategoriesDiscovered = true;
                    }
                }
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private string getImageUrl(HtmlNode baseNode)
        {
            var thumbNode = baseNode.SelectSingleNode(".//img");//tested
            if (thumbNode != null)
            {
                foreach (var src in thumbNode.Attributes)
                    if (src.Name == "src" && src.Value.StartsWith("http"))
                        return src.Value;

                foreach (var src in thumbNode.Attributes)
                    if (src.Name == "data-srcset" && src.Value.StartsWith("http"))
                        return src.Value.Split(' ')[0];
            }
            return String.Empty;
        }

        private int DiscoverSubs(Category parentCategory, HtmlDocument doc)
        {
            parentCategory.SubCategories = new List<Category>();
            var nodes = doc.DocumentNode.SelectNodes(@"//section[div[h2[@class='component-container__header-title']|h3[@class='component-container__header-title'] ]]");//tested
            foreach (var node in nodes)
            {
                var videos = myParse2(node);
                if (videos.Count > 0)
                {
                    Category cat = new Category()
                    {
                        ParentCategory = parentCategory,
                        Other = videos
                    };
                    var hNode = node.SelectSingleNode(@".//h2[@class='component-container__header-title']");
                    if (hNode == null)
                        hNode = node.SelectSingleNode(@".//h3[@class='component-container__header-title']");
                    if (hNode.SelectSingleNode("a/text()") != null)
                        cat.Name = hNode.SelectSingleNode("a/text()").InnerText.Trim();
                    else
                        cat.Name = hNode.InnerText.Trim();

                    parentCategory.SubCategories.Add(cat);

                    if (videos.Count >= 12)
                    {
                        var extravids = doc.DocumentNode.SelectNodes(@".//section[@class='player-sidebar-video-thumbnails component-container']//li[@data-index > 11]");
                        videos.AddRange(myParse3(extravids));
                    }
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private int DiscoverSubsNew(Category parentCategory, HtmlDocument doc)
        {
            //other is seriesid. Method finds Kausi 1,2 etc and Klipit
            parentCategory.SubCategories = new List<Category>();
            var nodes = doc.DocumentNode.SelectNodes(@"//a[@data-value]");

            foreach (var node in nodes)
            {
                RssLink cat = new RssLink()
                {
                    ParentCategory = parentCategory,
                    Name = node.InnerText
                };
                if (cat.Name == "Klipit")
                    cat.Url = @"https://www.ruutu.fi/component/726950/update?type=component&content%5Bsource%5D%5Bparams%5D%5Bmedia_type%5D=video_clip&content%5Bsource%5D%5Bparams%5D%5B" +
                        "series%5D=" + (string)parentCategory.Other + @"&content%5Bsource%5D%5Bparams%5D%5Borderby%5D=airtime&content%5Bsource%5D%5Bparams%5D%5Border_direction%5D=desc&content%5Bsource%5D%5Bparams%5D%5Bdrm%5D=1&content%5Bsource%5D%5Bparams%5D%5Blimit%5D=30&content%5Bsource%5D%5Bparams%5D%5Boffset%5D=0&content%5Bsource%5D%5Burl%5D=http%3A%2F%2Fdynamic-gatling.nelonenmedia.fi%2Fcos%2Fvideos%2F2%2F&content%5Btype%5D=video&active=true&formatter=video_cards_grid";
                else
                    cat.Url = @"https://www.ruutu.fi/component/726940/update?type=component&content%5Btype%5D=episode&content%5Bsource%5D%5Bparams%5D%5Bhas_current_or_upcoming_online_rights%5D=960&content%5Bsource%5D%5Bparams%5D%5Blimit%5D=100&content%5Bsource%5D%5Bparams%5D%5Borderby%5D=sequence&content%5Bsource%5D%5Bparams%5D%5Border_direction%5D=asc&content%5Bsource%5D%5Bparams%5D%5B" +
                        "season_id%5D=" + node.Attributes["data-value"].Value + @"&content%5Bsource%5D%5Bparams%5D%5Bdrm%5D=1&content%5Bsource%5D%5Bparams%5D%5B" +
                        "series_id%5D=" + (string)parentCategory.Other + @"&content%5Bsource%5D%5Bparams%5D%5Boffset%5D=0&content%5Bsource%5D%5Burl%5D=http%3A%2F%2Fdynamic-gatling.nelonenmedia.fi%2Fcos%2Fepisode%2F2%2F&active=true&formatter=episode_thumbnails_by_season";
                parentCategory.SubCategories.Add(cat);
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }


        private string htmlValue(HtmlAttribute node)
        {
            if (node == null)
                return String.Empty;
            else
                return node.Value;
        }

        private string cleanup(string s)
        {
            s = s.Replace('\n', ' ');
            while (s.Contains("  "))
                s = s.Replace("  ", " ");
            return s;
        }

        private HtmlDocument getDocument(Category cat)
        {
            string webData = GetWebData(((RssLink)cat).Url);
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
