using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class RuutuUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (RssLink cat in Settings.Categories)
                setSubs(cat, cat.Name == "Leffat");
            return res;
        }

        private void setSubs(Category cat, bool b)
        {
            cat.HasSubCategories = cat.Description == null || (!cat.Description.Contains("episodit") && !cat.Description.Contains("leffat"));
            if (!String.IsNullOrEmpty(cat.Description))
            {
                cat.Other = cat.Description;
                cat.Description = null;
            }
            cat.SubCategoriesDiscovered = (cat.HasSubCategories && cat.SubCategories != null && cat.SubCategories.Count > 0);
            if (cat.SubCategoriesDiscovered)
                foreach (Category subcat in cat.SubCategories)
                    setSubs(subcat, b);
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var doc = getDocument(parentCategory);
            if (parentCategory.Name == "Ohjelmat")
                return AddOhjelmat(doc, parentCategory);
            //if (parentCategory.Name == "Urheilu")
            //  return AddUrheilu(doc, parentCategory);
            parentCategory.SubCategories = new List<Category>();
            parentCategory.HasSubCategories = true;
            var res = doc.DocumentNode.SelectNodes(@"//a[starts-with(@id,'quicktabs-tab-ruutu_series_episodes_by_season-')]");
            if (res != null)//ohjelmat
                foreach (var sub in res)
                {
                    Category subc = new Category()
                    {
                        Name = HttpUtility.HtmlDecode(sub.InnerText),
                        ParentCategory = parentCategory,
                        Other = doc.DocumentNode.SelectSingleNode(@"//div[@id='" + sub.Attributes["id"].Value.Replace("-tab-", "-tabpage-") + "']")
                    };
                    parentCategory.SubCategories.Add(subc);
                }
            else
            {
                if (true.Equals(parentCategory.Other))
                    AddUrheilu(parentCategory);
                else
                    AddLapsetSubs(doc.DocumentNode.SelectSingleNode(@".//div[@id='" + parentCategory.Other as string + "']"), parentCategory);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private string lastid = null;
        public override List<VideoInfo> GetVideos(Category category)
        {
            var node2 = category.Other as HtmlNode;
            if (node2 == null)
            {
                lastid = category.Other as string;
                return Parse(((RssLink)category).Url, null);
            }
            else
                lastid = node2.Attributes["id"].Value;
            return myParse2(node2);
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Category parentCat = category.ParentCategory;
            parentCat.SubCategories.Remove(category);

            string id = category.Other as string;
            var doc = getDocument(category);
            AddLapsetSubs(doc.DocumentNode.SelectSingleNode(@".//div[@id='" + id + "']"), parentCat);
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            string webData = GetWebData(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(webData);
            var node2 = doc.DocumentNode.SelectSingleNode(@".//div[@id='" + lastid + "']");
            if (node2 == null)
                node2 = doc.DocumentNode; //for search

            return myParse2(node2);
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            lastid = null;
            query = HttpUtility.UrlEncode(query);
            return Parse(string.Format(searchUrl, query), null).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        private void AddLapsetSubs(HtmlNode node, Category parentCategory)
        {
            if (parentCategory.SubCategories == null)
                parentCategory.SubCategories = new List<Category>();
            var res = node.SelectNodes(".//article");//normal
            bool isUrheilu = false;
            if (res == null)
            {
                res = node.SelectNodes(".//div[@class='views-row grid-4 highlight-content']");//urheilu
                isUrheilu = res != null;
            }
            if (res != null)
                foreach (var sub in res)
                {
                    RssLink subc = new RssLink() { ParentCategory = parentCategory, HasSubCategories = true };
                    var aNode = sub.SelectSingleNode(@".//h2/a[@href]");
                    subc.Name = HttpUtility.HtmlDecode(cleanup(aNode.InnerText));
                    subc.Url = FormatDecodeAbsolutifyUrl(baseUrl, aNode.Attributes["href"].Value, videoListRegExFormatString, videoListUrlDecoding);
                    var imgNode = sub.SelectSingleNode(@".//img[@src]");
                    if (imgNode != null && !String.IsNullOrEmpty(imgNode.Attributes["src"].Value))
                        subc.Thumb = FormatDecodeAbsolutifyUrl(baseUrl, imgNode.Attributes["src"].Value, videoThumbFormatString, UrlDecoding.None);
                    var descrNode = sub.SelectSingleNode(@".//div[contains(@class,'field-name-field-description')]");
                    if (descrNode != null)
                        subc.Description = descrNode.InnerText;
                    if (isUrheilu)
                        subc.Other = true;
                    parentCategory.SubCategories.Add(subc);
                }
            else
            { //listana
                res = node.SelectNodes(".//div[@class='has-episodes']");
                if (res != null)
                {
                    foreach (var sub in res)
                    {
                        RssLink subc = new RssLink() { ParentCategory = parentCategory, HasSubCategories = true };
                        var aNode = sub.SelectSingleNode(@".//a[@href]");
                        subc.Name = HttpUtility.HtmlDecode(cleanup(aNode.InnerText));
                        subc.Url = FormatDecodeAbsolutifyUrl(baseUrl, aNode.Attributes["href"].Value, videoListRegExFormatString, videoListUrlDecoding);
                        parentCategory.SubCategories.Add(subc);
                    }
                }
            }
            var nextPageNode = node.SelectSingleNode(@".//li[@class='pager-browse pager-browse-next last']/a[@href]");
            if (nextPageNode != null && !String.IsNullOrEmpty(nextPageNode.Attributes["href"].Value))
            {
                parentCategory.SubCategories.Add(new NextPageCategory()
                {
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, nextPageNode.Attributes["href"].Value, nextPageRegExUrlFormatString, nextPageRegExUrlDecoding),
                    Other = node.Attributes["id"].Value,
                    ParentCategory = parentCategory
                });
            }
        }

        private List<VideoInfo> myParse2(HtmlNode node)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var res = node.SelectNodes(".//article");
            if (res != null)//normal
            {
                foreach (var vid in res)
                {
                    VideoInfo video = CreateVideoInfo();
                    var aNode = vid.SelectSingleNode(@".//h2/a");
                    video.Title = HttpUtility.HtmlDecode(cleanup(aNode.InnerText));
                    video.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, aNode.Attributes["href"].Value, videoListRegExFormatString, videoListUrlDecoding);
                    var imgNode = vid.SelectSingleNode(@".//img[@src]");
                    if (imgNode != null && !String.IsNullOrEmpty(imgNode.Attributes["src"].Value))
                        video.ImageUrl = FormatDecodeAbsolutifyUrl(baseUrl, imgNode.Attributes["src"].Value, videoThumbFormatString, UrlDecoding.None);
                    //video.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                    var airDateNode = vid.SelectSingleNode(@".//div[contains(@class,'field-name-field-starttime')]/span");
                    if (airDateNode != null)
                        video.Airdate = Utils.PlainTextFromHtml(airDateNode.InnerText);
                    var descrNode = vid.SelectSingleNode(@".//div[contains(@class,'field-name-field-webdescription')]/p");
                    if (descrNode != null)
                        video.Description = descrNode.InnerText;

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
            else
            {  //leffat/listana 
                res = node.SelectNodes(".//div[@class='field-content']");
                foreach (var vid in res)
                {
                    VideoInfo video = CreateVideoInfo();
                    var aNode = vid.SelectSingleNode(@".//a[@href]");
                    video.Title = HttpUtility.HtmlDecode(cleanup(aNode.InnerText));
                    video.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, aNode.Attributes["href"].Value, videoListRegExFormatString, videoListUrlDecoding);
                    video.Title = cleanup(video.Title);
                    result.Add(video);
                }
            }
            var nextPageNode = node.SelectSingleNode(@".//li[@class='pager-browse pager-browse-next last']/a[@href]");
            nextPageAvailable = nextPageNode != null && !String.IsNullOrEmpty(nextPageNode.Attributes["href"].Value);
            if (nextPageAvailable)
            {
                nextPageUrl = FormatDecodeAbsolutifyUrl(baseUrl, nextPageNode.Attributes["href"].Value, nextPageRegExUrlFormatString, nextPageRegExUrlDecoding);
            }
            else
                nextPageUrl = "";
            return result;
        }

        private int AddUrheilu(Category parentCat)
        {
            var doc = getDocument(parentCat);
            var nodes = doc.DocumentNode.SelectNodes(@".//h2[@class='pane-title']");
            foreach (var node in nodes)
            {
                if (node.InnerText == "Jaksot" || node.InnerText == "Klipit")
                {
                    Category sub = new Category()
                    {
                        Name = node.InnerText,
                        ParentCategory = parentCat,
                        HasSubCategories = true,
                        SubCategoriesDiscovered = true
                    };

                    var nodes2 = nextRealSibbling(node).SelectNodes(@".//a[starts-with(@id,'quicktabs-tab-ruutu_series_quicktabs_video_')]");
                    sub.SubCategories = new List<Category>();
                    foreach (var node2 in nodes2)
                    {
                        Category sub2 = new Category()
                        {
                            Name = node2.InnerText,
                            ParentCategory = sub,
                            Other = doc.DocumentNode.SelectSingleNode(@"//div[@id='" + node2.Attributes["id"].Value.Replace("-tab-", "-tabpage-") + "']")
                        };
                        sub.SubCategories.Add(sub2);
                    }


                    parentCat.SubCategories.Add(sub);
                }
            };
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private int AddOhjelmat(HtmlDocument doc, Category parentCat)
        {
            var nodes = doc.DocumentNode.SelectNodes(@"//h2[@class='block-title']");
            foreach (var node in nodes) //Aakkosittain and Teemoittain
            {
                Category sub = new Category()
                {
                    Name = node.InnerText,
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    SubCategories = new List<Category>()
                };
                parentCat.SubCategories.Add(sub);

                var nodes2 = nextRealSibbling(node).SelectNodes(@".//a[@class='series-group-title']");
                foreach (var node2 in nodes2)//abc,def
                {
                    Category sub2 = new Category()
                    {
                        Name = node2.InnerText,
                        ParentCategory = sub,
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    };
                    sub.SubCategories.Add(sub2);

                    var listNode = nextRealSibbling(node2);
                    while (listNode != null)
                    {
                        var nodes3 = listNode.SelectNodes(@".//li/a");
                        if (nodes3 != null)
                        {
                            foreach (var node3 in nodes3)//afgaan...
                            {
                                RssLink sub3 = new RssLink()
                                {
                                    Name = node3.InnerText,
                                    ParentCategory = sub2,
                                    HasSubCategories = true,
                                    Url = FormatDecodeAbsolutifyUrl(baseUrl, node3.Attributes["href"].Value, "", UrlDecoding.None)
                                };
                                sub2.SubCategories.Add(sub3);
                            }
                        }
                        listNode = nextRealSibbling(listNode);
                    }
                    sub2.SubCategoriesDiscovered = true;
                }
                sub.SubCategoriesDiscovered = true;
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private string cleanup(string s)
        {
            s = s.Replace('\n', ' ');
            while (s.Contains("  "))
                s = s.Replace("  ", " ");
            return s;
        }

        private HtmlNode nextRealSibbling(HtmlNode node)
        {
            var newNode = node.NextSibling;
            while (newNode != null && newNode is HtmlTextNode)
                newNode = newNode.NextSibling;
            return newNode;
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
