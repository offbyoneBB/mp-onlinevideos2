using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TVNZOnDemandUtil : GenericSiteUtil
    {

        private int MyDiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url, GetCookie());
            if (!string.IsNullOrEmpty(data))
            {
                parentCategory.SubCategories = new List<Category>();
                Match m = regEx_dynamicSubCategories.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    if (!string.IsNullOrEmpty(dynamicSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubCategoryUrlFormatString, cat.Url);
                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                    if (dynamicSubCategoryUrlDecoding) cat.Url = HttpUtility.HtmlDecode(cat.Url);
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.Thumb = m.Groups["thumb"].Value;
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.Description = m.Groups["description"].Value;

                    cat.Url = @"http://tvnz.co.nz/search/ta_ent_search_tv_skin.xhtml?requiredfields=type:media.";
                    if (m.Groups["type"].Value == "Extra")
                    {
                        cat.Name += ": Extras";
                        cat.Url += @"(format:extras|format:preview)";
                    }
                    else
                        cat.Url += @"format:full+episode";
                    cat.Url += @"&partialfields=programme:%3B" + m.Groups["id"].Value + @"&tab=tvmedia&start=0&sort=date:D:S:d1";
                    cat.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> TotalSubCategories = new List<Category>();
            Regex npRegex = new Regex(@"href=""(?<url>[^""]*)""\stitle=""Next\spage""");
            string tmpUrl = ((RssLink)parentCategory).Url;
            string bareUrl = tmpUrl.Split('#')[0];
            do
            {
                ((RssLink)parentCategory).Url = tmpUrl;
                MyDiscoverSubCategories(parentCategory);
                TotalSubCategories.AddRange(parentCategory.SubCategories);

                string webData = GetWebData(tmpUrl);
                Match m = npRegex.Match(webData);
                if (m.Success)
                    tmpUrl = bareUrl + m.Groups["url"].Value;
                else
                    tmpUrl = null;
            } while (!String.IsNullOrEmpty(tmpUrl));

            ((RssLink)parentCategory).Url = bareUrl;

            parentCategory.SubCategories = TotalSubCategories;
            return parentCategory.SubCategories.Count;
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            List<string> res = new List<string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(video.VideoUrl);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/ns/SMIL");
            XmlNodeList nodes = doc.SelectNodes("//a:smil/a:body/a:seq", nsmRequest);

            foreach (XmlNode node in nodes)
            {
                XmlNode vid = node.SelectSingleNode("a:video", nsmRequest);
                if (vid == null)
                {
                    int largestBitrate = 0;
                    foreach (XmlNode sub in node.SelectNodes("a:par/a:video", nsmRequest))
                    {
                        int bitRate = int.Parse(sub.Attributes["systemBitrate"].InnerText);
                        if (bitRate > largestBitrate)
                        {
                            largestBitrate = bitRate;
                            vid = sub;
                        }
                    }
                }
                res.Add(vid.Attributes["src"].InnerText);
            }
            return res;
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
