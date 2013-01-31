using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for MTV.ca.
    /// </summary>
    public class MTVCanadaUtil : GenericSiteUtil
    {
        private static Regex urlRegex = new Regex(@"/(?<id>[^/]*)/0/\d+",
                                                       RegexOptions.Compiled);
        private static Regex episodesUrlRegex = new Regex(@"ajLoadEps\((?<contentType>[^,]*),(?<page>[^,]*),\s'(?<season>[^']*)','(?<showid>[^']*)'",
                                                          RegexOptions.Compiled);

        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Shows", Other = @"showsList", HasSubCategories = true });
            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"MTV Classics", Other = @"mtvClassicsDropDown", HasSubCategories = true });
            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Specials", Other = @"showSpecials", HasSubCategories = true });
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            string url = (parentCategory as RssLink).Url;
            
            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                if (!string.IsNullOrEmpty(parentCategory.Other as string))
                {
                    // main subcategories
                    string xPath = string.Format(@"//div[@id = '{0}']//li", parentCategory.Other as string);
                    foreach (HtmlNode item in document.DocumentNode.SelectNodes(xPath))
                    {
                        HtmlNode anchor = item.SelectSingleNode(@"./a");
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             ParentCategory = parentCategory,
                                                             Name = HttpUtility.HtmlDecode(anchor.InnerText),
                                                             Url = anchor.GetAttributeValue(@"href", string.Empty),
                                                             HasSubCategories = true
                                                         });
                    }
                }
                else
                {
                    // seasons
                    string xPath = @"//div[@id = 'leftNav']//li[@class = 'leftNavItem']";
                    foreach (HtmlNode item in document.DocumentNode.SelectNodes(xPath))
                    {
                        HtmlNode anchor = item.SelectSingleNode(@"./a");
                        string catUrl = anchor.GetAttributeValue(@"href", string.Empty);
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             ParentCategory = parentCategory,
                                                             Name = HttpUtility.HtmlDecode(anchor.InnerText),
                                                             Url = catUrl.StartsWith(@"/") ? string.Format(@"{0}{1}", baseUrl, catUrl) : catUrl,
                                                             HasSubCategories = false
                                                         });
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListForSinglePage(category, (category as RssLink).Url);
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = string.Empty;
            currentCategory = category;

            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                foreach (HtmlNode item in document.DocumentNode.SelectNodes(@"//li[@class = 'videoListItem']"))
                {
                    HtmlNode titleAnchor = item.SelectSingleNode(@".//div[@class = 'vidListTitle']/a");
                    Match urlMatch = urlRegex.Match(titleAnchor.GetAttributeValue(@"href", string.Empty));
                    result.Add(new VideoInfo() {
                                   VideoUrl =urlMatch.Success ? urlMatch.Groups["id"].Value : string.Empty,
                                   Title = titleAnchor.InnerText,
                                   ImageUrl = item.SelectSingleNode(@".//div[@class = 'videoListThumb']//img").GetAttributeValue(@"src", string.Empty),
                                   Description = item.SelectSingleNode(@".//div[@class = 'vidListDescription']").InnerText
                               });
                }
                
                HtmlNode nextPage = document.DocumentNode.SelectSingleNode(@"//a[@class = 'vidNavNext']");
                
                if (nextPage != null)
                {
                    string onclick = nextPage.GetAttributeValue(@"onclick", string.Empty);
                    Match episodesUrlMatch = episodesUrlRegex.Match(onclick);
                    if (episodesUrlMatch.Success)
                    {
                        nextPageUrl = string.Format(@"http://www.mtv.ca/eplist_ajax.jhtml?ctid={0}&page={1}&season={2}&showid={3}&pagetype=shows&sortorder=ascending",
                                                    episodesUrlMatch.Groups["contentType"].Value,
                                                    episodesUrlMatch.Groups["page"].Value,
                                                    episodesUrlMatch.Groups["season"].Value,
                                                    episodesUrlMatch.Groups["showid"].Value);
                    }
                }
            }
            return result;
        }
        
        public override bool HasNextPage {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }
        
        public override List<VideoInfo> getNextPageVideos()
        {
            return getVideoListForSinglePage(currentCategory, nextPageUrl);
        }

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            string url = string.Format(@"{0}/broadband/xml/content.jhtml?id={1}", baseUrl, video.VideoUrl);
            XmlDocument xml = GetWebData<XmlDocument>(url);
            
            if (xml != null)
            {
                string videoId = xml.SelectSingleNode(@"//playlist//videoid").InnerText;
                url = string.Format(@"http://intl.esperanto.mtvi.com/www/xml/media/mediaGen.jhtml?uri=mgid:uma:video:mtv.ca:{0}", videoId);
                
                xml = GetWebData<XmlDocument>(url);
                if (xml != null)
                {
                    foreach (XmlNode rendition in xml.SelectNodes(@"//video/item/rendition"))
                    {
                        int bitrate = int.Parse(rendition.Attributes["bitrate"].Value);
                        XmlNode src = rendition.SelectSingleNode(@"./src");
                        if (!urlsDictionary.ContainsKey(bitrate))
                        {
                            urlsDictionary.Add(bitrate, new MPUrlSourceFilter.RtmpUrl(src.InnerText).ToString());
                        }
                    }
                }
            }

            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
            {
                video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }

            return result;
        }
    }
}
