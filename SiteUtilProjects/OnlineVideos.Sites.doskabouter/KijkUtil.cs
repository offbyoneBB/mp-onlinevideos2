using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;
using HtmlAgilityPack;
using System.Web;

namespace OnlineVideos.Sites
{

    public class KijkUtil : BrightCoveUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                if (cat.SubCategories != null && cat.SubCategories.Count > 0) //Gemist
                {
                    cat.SubCategoriesDiscovered = true;
                    foreach (Category subcat in cat.SubCategories)  //Alles t/m Veronica
                    {
                        subcat.HasSubCategories = true;
                        subcat.Other = true;
                    }
                }
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            string url = ((RssLink)parentCategory).Url;
            HtmlDocument htmlDoc = GetHtmlDocument(url);

            HtmlNodeCollection nodeCollection = htmlDoc.DocumentNode.SelectNodes("//h2[@class = 'showcase-heading']");
            foreach (HtmlNode node in nodeCollection)
            {
                RssLink cat = new RssLink()
                {
                    ParentCategory = parentCategory,
                    Name = node.InnerText,
                    HasSubCategories = parentCategory.Other == null
                };
                if (!cat.HasSubCategories)
                {
                    cat.Other = node.NextSibling;//ParseVideos(url, node.NextSibling);
                    parentCategory.SubCategories.Add(cat);
                }
                else
                {
                    if (ParseSubCategories(cat, node.NextSibling) > 0)
                        parentCategory.SubCategories.Add(cat);
                }

            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            KijkNextPageCategory kn = (KijkNextPageCategory)category;
            string url = kn.baseUrl + '/' + kn.pagenr + "/10";
            url = FixUrl(baseUrl, url);
            HtmlDocument htmlDoc = GetHtmlDocument(url);

            category.ParentCategory.SubCategories.Remove(category);
            return ParseSubCategories((RssLink)category.ParentCategory,
                htmlDoc.DocumentNode.SelectSingleNode("//h2[@class = 'showcase-heading']").NextSibling, kn.pagenr);
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            HtmlDocument htmlDoc = GetHtmlDocument(url);
            return ParseVideos(url, htmlDoc.DocumentNode);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is HtmlNode)
                return ParseVideos(((RssLink)category).Url, category.Other as HtmlNode);
            return base.GetVideos(category);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string[] parts = video.VideoUrl.Split('/');
            string url = @"http://embed.kijk.nl/?width=868&height=491&video=" + parts[parts.Length - 2];
            string webdata = GetWebData(url, referer: video.VideoUrl);
            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, url);
            return FillPlaybackOptions(video, renditions);
        }

        private int ParseSubCategories(RssLink parentCategory, HtmlNode listNode, int startPageNr = 1)
        {
            if (listNode == null)
                return 0;
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            HtmlNodeCollection nodeCollection = listNode.SelectNodes(".//div[@class = 'item ar16x9 js']");
            if (nodeCollection != null)
            {
                bool moreData = getAttribute(listNode, "data-hasmore") == "1";
                string moreUrl = getAttribute(listNode, "data-id");
                foreach (HtmlNode node in nodeCollection)
                {
                    RssLink cat = new RssLink()
                    {
                        Name = getInnertext(node.SelectSingleNode(".//h3[@itemprop='name']/text()")),
                        Url = getAttribute(node.SelectSingleNode(".//a[@itemprop='url']"), "href"),
                        Thumb = getAttribute(node.SelectSingleNode(".//img[@itemprop='thumbnailUrl']"), "data-src"),
                        Description = getInnertext(node.SelectSingleNode(".//p[@itemprop='description']")),
                        ParentCategory = parentCategory
                    };
                    cat.Url = FixUrl(parentCategory.Url, cat.Url);
                    cat.Thumb = FixUrl(parentCategory.Url, cat.Thumb);
                    parentCategory.SubCategories.Add(cat);
                }
                if (moreData)
                    parentCategory.SubCategories.Add(
                        new KijkNextPageCategory()
                        {
                            baseUrl = "http://www.kijk.nl/ajax/section/overview/" + moreUrl,
                            pagenr = startPageNr + 1,
                            ParentCategory = parentCategory
                        }
                        );
            }
            parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        private string FixUrl(string aBaseUrl, string theUrl)
        {
            if (String.IsNullOrEmpty(aBaseUrl))
                aBaseUrl = baseUrl;
            if (!String.IsNullOrEmpty(theUrl) && !Uri.IsWellFormedUriString(theUrl, System.UriKind.Absolute))
                return new Uri(new Uri(aBaseUrl), theUrl).AbsoluteUri;
            return theUrl;
        }

        private List<VideoInfo> ParseVideos(string url, HtmlNode listNode)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            HtmlNodeCollection nodeCollection = listNode.SelectNodes(".//div[@class = 'item ar16x9 js']");
            List<string> vidUrls = new List<string>();
            if (nodeCollection != null)
                foreach (HtmlNode node in nodeCollection)
                {
                    HtmlNode kijkBtn = node.SelectSingleNode(".//div[@class='button cta']");
                    if (kijkBtn == null || !kijkBtn.InnerText.Contains("vanaf"))
                    {
                        VideoInfo video = new VideoInfo()
                        {
                            Title = getInnertext(node.SelectSingleNode(".//h3[@itemprop='name']/text()")),
                            VideoUrl = FixUrl(baseUrl, getAttribute(node.SelectSingleNode(".//a[@itemprop='url']"), "href")),
                            Thumb = FixUrl(baseUrl, getAttribute(node.SelectSingleNode(".//img[@itemprop='thumbnailUrl']"), "data-src")),
                            Description = getInnertext(node.SelectSingleNode(".//p[@itemprop='description']")),
                            Airdate = getInnertext(node.SelectSingleNode(".//div[@itemprop='datePublished']")),
                            Length = getInnertext(node.SelectSingleNode("(.//span[@itemprop='timeRequired'])[last()]")),
                        };
                        string descr2 = getInnertext(node.SelectSingleNode(".//div[@class='desc meta']/text()"));
                        if (!String.IsNullOrEmpty(descr2))
                            video.Title += " " + descr2;
                        if (!vidUrls.Contains(video.VideoUrl))
                        {
                            videoList.Add(video);
                            vidUrls.Add(video.VideoUrl);
                        }
                    };
                };

            if (getAttribute(listNode, "data-hasmore") == "1")
            {
                string moreUrl = getAttribute(listNode, "data-id");
                nextPageUrl = "http://www.kijk.nl/ajax/section/overview/" + moreUrl + "/2/10";
                nextPageAvailable = !string.IsNullOrEmpty(nextPageUrl);
            }
            else
            {
                nextPageAvailable = false;
                nextPageUrl = "";
            }


            return videoList;
        }

        private string getInnertext(HtmlNode node)
        {
            if (node == null)
                return null;
            return HttpUtility.HtmlDecode(node.InnerText);
        }

        private string getAttribute(HtmlNode node, string attName)
        {
            if (node == null)
                return null;
            HtmlAttribute att = node.Attributes[attName];
            if (att == null)
                return null;
            return att.Value;
        }

        private HtmlDocument GetHtmlDocument(string url)
        {
            HtmlDocument doc = new HtmlDocument();
            string webData = GetWebData(url);
            doc.LoadHtml(webData);
            return doc;
        }

        private class KijkNextPageCategory : NextPageCategory
        {
            public string baseUrl;
            public int pagenr;
        }
    }

}
