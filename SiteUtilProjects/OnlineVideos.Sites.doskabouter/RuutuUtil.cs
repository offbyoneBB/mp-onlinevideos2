using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

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

        private enum RuType { None, Ohjelmat, LapsetSeries };

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.VideoUrl.Contains("series"))
                video.VideoUrl = WebCache.Instance.GetRedirectedUrl(video.VideoUrl);
            string data = GetWebData(GetFormattedVideoUrl(video));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            var vidUrl = doc.SelectSingleNode(@"//Clip/SourceFile").InnerText;
            return vidUrl;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var doc = getDocument(parentCategory);
            if (parentCategory.Name == "Kaikki ohjelmat")
                return AddKaikki(doc, parentCategory);
            if (parentCategory.Name == "Ohjelmat")
                return AddOhjelmat(doc, parentCategory);
            if (RuType.Ohjelmat.Equals(parentCategory.Other) || parentCategory.Name == "Urheilu" || parentCategory.Name == "Uutiset")
                return DiscoverSubs(parentCategory, doc);
            if (parentCategory.Name == "Lapset")
                return DiscoverLapsetSubs(parentCategory, doc);
            if (RuType.LapsetSeries.Equals(parentCategory.Other))
                return DiscoverSubs(parentCategory, doc);
            return 0;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return category.Other as List<VideoInfo>;
        }


        private string getDescription(HtmlNode node)
        {
            var node2 = node.SelectSingleNode(".//p[@class='expanding-card__description']");//kausi
            if (node2 == null)
                node2 = node.SelectSingleNode(".//div[@class='list-item-main1']");//klipit
            if (node2 != null)
                return node2.InnerText;
            return String.Empty;
        }

        private List<VideoInfo> myParse2(HtmlNode node)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var res = node.SelectNodes(".//div[@data-content-type='video']");//tested data-content-type="video"
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
                    if (node2 != null && vid.SelectSingleNode(".//h4 | .//h3") != null)
                    {
                        VideoInfo video = CreateVideoInfo();

                        video.Title = vid.SelectSingleNode(".//h4 | .//h3").InnerText;
                        video.Description = getDescription(vid);
                        video.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, node2.Attributes["href"].Value, "", UrlDecoding.None);
                        video.Thumb = getImageUrl(vid);
                        var airDateNode = vid.SelectSingleNode(@".//div[@class='list-item-prefix']");
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

        private int AddKaikki(HtmlDocument doc, Category parentCategory)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//div[@class='list']");
            parentCategory.SubCategories = new List<Category>();
            foreach (var node in nodes)
            {
                RssLink sub = new RssLink()
                {
                    Name = node.SelectSingleNode(@".//h2").InnerText,
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
            var nodes = parentNode.SelectNodes(@".//div[@data-content-id]/a[@href]");
            foreach (var node in nodes)
            {
                var cat = new RssLink()
                {
                    Name = node.SelectSingleNode(@".//div").InnerText.Trim(),
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, node.Attributes["href"].Value, "", UrlDecoding.None),
                    Other = RuType.Ohjelmat
                };
                res.Add(cat);
            }

            parentCat.SubCategoriesDiscovered = true;
            return res;
        }

        private int DiscoverLapsetSubs(Category parentCategory, HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//a[@href][.//h3[@class='thumbnail-title']]");
            foreach (var node in nodes)
            {
                RssLink sub = new RssLink()
                {
                    Name = node.SelectSingleNode(@".//h3").InnerText,
                    ParentCategory = parentCategory,
                    HasSubCategories = true,
                    Thumb = getImageUrl(node),
                    SubCategories = new List<Category>(),
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, node.Attributes["href"].Value, "", UrlDecoding.None),
                    Other = RuType.LapsetSeries
                };
                parentCategory.SubCategories.Add(sub);
            }

            return AddOhjelmat(doc, parentCategory);
        }

        private int AddOhjelmat(HtmlDocument doc, Category parentCat)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//section[div/h2[@class='header-title'][a]]");//tested
            foreach (var node in nodes)
            {
                Category sub = new Category()
                {
                    Name = node.SelectSingleNode(@".//h2/a/text()").InnerText.Trim(),//tested
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    SubCategories = new List<Category>()
                };
                if (sub.Name == "Lasten elokuvat")
                {
                    sub.HasSubCategories = false;
                    sub.Other = myParse2(node);
                    parentCat.SubCategories.Add(sub);
                }
                else
                {
                    var nodes2 = node.SelectNodes(".//div[@class='hoverbox-container'][.//a[@href]]");//tested
                    if (nodes2 != null)
                    {
                        parentCat.SubCategories.Add(sub);
                        foreach (var node2 in nodes2)
                        {
                            RssLink sub2 = new RssLink()
                            {
                                Name = node2.SelectSingleNode(".//h4").InnerText,
                                ParentCategory = sub,
                                Url = FormatDecodeAbsolutifyUrl(baseUrl, node2.SelectSingleNode(".//a[@href]").Attributes["href"].Value, "", UrlDecoding.None),
                                HasSubCategories = true,
                                Other = RuType.Ohjelmat
                            };//tested
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
                foreach(var src in thumbNode.Attributes)
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
            var nodes = doc.DocumentNode.SelectNodes(@"//section[div/h2[@class='header-title']]");//tested
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
                    if (node.SelectSingleNode(".//h2[@class='header-title']/a/text()") != null)
                        cat.Name = node.SelectSingleNode(".//h2[@class='header-title']/a/text()").InnerText.Trim();
                    else
                        cat.Name = node.SelectSingleNode(".//h2[@class='header-title']").InnerText.Trim();

                    parentCategory.SubCategories.Add(cat);
                }
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
            var doc = new HtmlDocument();
            doc.LoadHtml(webData);
            return doc;
        }

    }
}
