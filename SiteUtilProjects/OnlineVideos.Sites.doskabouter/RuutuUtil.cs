using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class RuutuUtil : GenericSiteUtil
    {
        //First character of html with videos used to discriminate between uusimmat en katsotuimmat
        // and that is copied at the end of the videourl

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (RssLink cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                if (cat.Url == baseUrl)
                    AddOhjelmat(cat);
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (true.Equals(parentCategory.Other))// from ohjelmat AddSubSubcats
            {
                //add jaksot and viihde for http://www.ruutu.fi/ohjelmat/alaston-piilokamera
                string[] subs2 = splitVideoList2(GetWebData(((RssLink)parentCategory).Url));

                parentCategory.SubCategories = new List<Category>();
                parentCategory.HasSubCategories = true;
                int cnt = 0;
                foreach (string sub in subs2)
                {
                    Match m = Regex.Match(sub, @">(?<title>[^<]*)</h2>");
                    if (m.Success)
                    {
                        if (m.Index == 1)
                        {
                            Category subc = new Category()
                            {
                                Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                                ParentCategory = parentCategory,
                                Other = cnt.ToString() + sub
                            };
                            parentCategory.SubCategories.Add(subc);
                        }
                        cnt++;
                    }
                }

                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }

            if (parentCategory.SubCategories != null && parentCategory.SubCategories.Count > 0)
            {
                foreach (Category subcat in parentCategory.SubCategories)
                    subcat.HasSubCategories = true;
                parentCategory.SubCategoriesDiscovered = true;
                return 0;
            }

            // add uusimmat en katsotuimmat

            parentCategory.SubCategories = new List<Category>();
            string webData = GetWebData(((RssLink)parentCategory).Url);
            string[] subs = splitVideoList(webData);
            if (subs.Length == 2)
            {
                Category sub = new Category()
                {
                    Name = "Katsotuimmat",
                    ParentCategory = parentCategory,
                    Other = "K" + subs[0]
                };
                parentCategory.SubCategories.Add(sub);

                sub = new Category()
                {
                    Name = "Uusimmat",
                    ParentCategory = parentCategory,
                    Other = "U" + subs[1]
                };
                parentCategory.SubCategories.Add(sub);
            }
            else
            {
                Category sub = new Category()
                {
                    Name = "Uusimmat",
                    ParentCategory = parentCategory,
                    Other = "Z" + webData
                };
                parentCategory.SubCategories.Add(sub);
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private string[] splitVideoList(string webData)
        {
            return webData.Split(new[] { "quicktabs-tabpage quicktabs-hide" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string[] splitVideoList2(string webData)
        {
            return webData.Split(new[] { "block-title theme-color theme-after-background" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string webData = category.Other as string;
            if (webData != null)
            {
                nextPageRegExUrlFormatString = "{0}" + webData[0];
                return myParse(webData);
            }
            return Parse(((RssLink)category).Url, null);

        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            char last = url[url.Length - 1];
            string webData = GetWebData(url.Substring(0, url.Length - 1));
            string[] lists;
            if (last == '0' || last == '1' || last == '2')
                lists = splitVideoList2(webData);
            else
                lists = splitVideoList(webData);

            nextPageRegExUrlFormatString = "{0}" + last;

            switch (last)
            {
                case 'K':
                case '0': return myParse(lists[0]);
                case 'U':
                case '1': return myParse(lists[1]);
                case '2': return myParse(lists[2]);
                default: return myParse(webData);
            }
        }

        private void AddOhjelmat(RssLink parentCat)
        {
            string webData = GetWebData(parentCat.Url);
            string[] subcats = webData.Split(new[] { @"class=""block-title""" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string subs in subcats) //Aakkosittain and Teemoittain
            {
                Match m = Regex.Match(subs, @">(?<title>[^<]*)</h2>");
                if (m.Success)
                {
                    Category sub = new Category()
                    {
                        Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                        ParentCategory = parentCat
                    };
                    AddSubcats(sub, subs);
                    parentCat.SubCategories.Add(sub);
                }
            }
            parentCat.SubCategoriesDiscovered = true;
        }

        private List<VideoInfo> myParse(string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            Match m = regEx_VideoList.Match(data);
            while (m.Success)
            {
                VideoInfo videoInfo = CreateVideoInfo();
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                // get, format and if needed absolutify the video url
                videoInfo.VideoUrl = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["VideoUrl"].Value, videoListRegExFormatString, videoListUrlDecoding);
                // get, format and if needed absolutify the thumb url
                if (!String.IsNullOrEmpty(m.Groups["ImageUrl"].Value))
                    videoInfo.ImageUrl = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["ImageUrl"].Value, videoThumbFormatString, UrlDecoding.None);
                videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                videoInfo.Description = m.Groups["Description"].Value;
                string jakso = m.Groups["Jakso"].Value;
                string kausi = m.Groups["Kausi"].Value;
                if (!String.IsNullOrEmpty(kausi))
                    videoInfo.Title += " kausi " + kausi;
                if (!String.IsNullOrEmpty(jakso))
                    videoInfo.Title += " jakso " + jakso;

                videoList.Add(videoInfo);
                m = m.NextMatch();
            }

            Match mNext = regEx_NextPage.Match(data);
            if (mNext.Success)
            {
                nextPageUrl = FormatDecodeAbsolutifyUrl(baseUrl, mNext.Groups["url"].Value, nextPageRegExUrlFormatString, nextPageRegExUrlDecoding);
                nextPageAvailable = !string.IsNullOrEmpty(nextPageUrl);
            }
            else
            {
                nextPageAvailable = false;
                nextPageUrl = "";
            }
            return videoList;
        }

        private void AddSubcats(Category parentCat, string webData)
        {
            string[] subcats = webData.Split(new[] { "series-group-title" }, StringSplitOptions.RemoveEmptyEntries);
            parentCat.SubCategories = new List<Category>();
            parentCat.HasSubCategories = true;

            foreach (string subs in subcats) //abc def etc
            {
                Match m = Regex.Match(subs, @">(?<title>[^<]*)</a>");
                if (m.Success)
                {
                    Category sub = new RssLink()
                    {
                        Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                        ParentCategory = parentCat
                    };
                    AddSubSubcats(sub, subs);
                    parentCat.SubCategories.Add(sub);
                }
            }
            parentCat.SubCategoriesDiscovered = true;
        }

        private void AddSubSubcats(Category parentCat, string webData)
        {
            Match m = Regex.Match(webData, @"<a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a>");
            parentCat.SubCategories = new List<Category>();
            parentCat.HasSubCategories = true;

            while (m.Success)
            {
                RssLink cat = new RssLink()
                { //Villa helena etc
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, "{0}", dynamicCategoryUrlDecoding),
                    Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                    ParentCategory = parentCat,
                    HasSubCategories = true,
                    Other = true
                };
                parentCat.SubCategories.Add(cat);
                m = m.NextMatch();
            }
            parentCat.SubCategoriesDiscovered = true;
        }
    }
}
