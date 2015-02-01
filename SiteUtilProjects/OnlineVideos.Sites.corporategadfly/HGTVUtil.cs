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
    /// site utility for HGTV
    /// </summary>
    public class HGTVUtil : GenericSiteUtil
    {
        private static Regex showIdRegex = new Regex(@"var\ssnap\s=\snew\sSNI\.HGTV\.Player\.FullSize\('[^']*','(?<showId>[^']*)'",
                                                     RegexOptions.Compiled);
        private static Regex ampersandRegex = new Regex(@"&(?!amp;)",
                                                        RegexOptions.Compiled);
        
        private static string showListingUrl = @"http://www.hgtv.com/hgtv/channel/xml/0,,{0},00.xml";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            List<RssLink> mainCategories = new List<RssLink>();
            
            HtmlDocument document = GetWebData<HtmlDocument>(string.Format(@"{0}/full-episodes/package/index.html", baseUrl));
            if (document != null)
            {
                foreach (HtmlNode item in document.DocumentNode.SelectNodes(@"//ol[@id = 'fe-list']/li/ol/li"))
                {
                    HtmlNode title = item.SelectSingleNode(@"./h2");
                    if ("Featured Series".Equals(title.InnerText)) continue;

                    HtmlNode anchor = item.SelectSingleNode(@".//a[@class = 'button']");
                    HtmlNode img = item.SelectSingleNode(@".//img");
                    HtmlNode p = item.SelectSingleNode(@"./p");
                    mainCategories.Add(new RssLink() {
                                           Url = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue("href", string.Empty)),
                                           Name = HttpUtility.HtmlDecode(title.InnerText),
                                           Thumb = img.GetAttributeValue("src", string.Empty),
                                           Description = p.InnerText,
                                           HasSubCategories = true
                                       });
                }
            }
            // there are a couple of more shows missing from the "full episodes" listings,
            // let's add these as well
            mainCategories.Add(new RssLink() {
                                   Url = @"http://www.hgtv.com/hgtv-property-brothers/videos/index.html",
                                   Name = @"Property Brothers",
                                   Description = @"The Property Brothers are determined to help couples find, buy and transform extreme fixer-uppers into the ultimate dream home, and since it's hard to see beyond a dated property's shortcomings, they're using state-of-the-art CGI to reveal their vision of the future.",
                                   HasSubCategories = true
                               });
            mainCategories.Add(new RssLink() {
                                   Url = @"http://www.hgtv.com/candice-tells-all-full-episodes/videos/index.html",
                                   Name = @"Candice Tells All",
                                   Description = @"Designer Candice Olson delivers the awe-inspiring transformations we have come to expect, but this time she pulls back the curtain to get down to the basics of design.",
                                   HasSubCategories = true
                               });
            
            // sort categories by category name
            foreach (RssLink category in mainCategories.OrderBy(c => c.Name))
            {
                Settings.Categories.Add(category);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            string url = (parentCategory as RssLink).Url;
            string data = GetWebData(url);
            if (data != null)
            {
                Match showIdMatch = showIdRegex.Match(data);
                if (showIdMatch.Success)
                {
                    string xmlUrl = string.Format(showListingUrl, showIdMatch.Groups["showId"].Value);
                    data = GetWebData(xmlUrl);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(ampersandRegex.Replace(data, @"&amp;"));
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                    namespaceManager.AddNamespace("up", @"http://www.scrippsnetworks.com/up/schemas/FFChannel");

                    XmlNode title = xml.SelectSingleNode(@"//title", namespaceManager);
                    if (title != null)
                    {
                        // grab current season title
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             ParentCategory = parentCategory,
                                                             Name = HttpUtility.HtmlDecode(title.InnerText),
                                                             Url = url,
                                                             HasSubCategories = false
                                                         });
                    }
                }
                
                // look for additional season titles
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(data);
                HtmlNodeCollection items = document.DocumentNode.SelectNodes(@"//ul[@class = 'channel-list']/li");
                if (items != null)
                {
                    foreach (HtmlNode item in items)
                    {
                        HtmlNode heading = item.SelectSingleNode(@"./h4");
                        HtmlNode div = item.SelectSingleNode(@"./div[@class = 'crsl']");
                        HtmlNode anchor = div.SelectSingleNode(@".//a");
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             ParentCategory = parentCategory,
                                                             Name = HttpUtility.HtmlDecode(heading.InnerText),
                                                             Url = anchor.GetAttributeValue(@"href", string.Empty),
                                                             HasSubCategories = false
                                                         });
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            string data = GetWebData((category as RssLink).Url);
            if (data != null)
            {
                Match showIdMatch = showIdRegex.Match(data);
                if (showIdMatch.Success)
                {
                    string xmlUrl = string.Format(showListingUrl, showIdMatch.Groups["showId"].Value);
                    data = GetWebData(xmlUrl);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(ampersandRegex.Replace(data, @"&amp;"));
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                    namespaceManager.AddNamespace("up", @"http://www.scrippsnetworks.com/up/schemas/FFChannel");

                    XmlNodeList videos = xml.SelectNodes(@"//video", namespaceManager);
                    if (videos != null)
                    {
                        foreach (XmlNode video in videos)
                        {
                            XmlNode title = video.SelectSingleNode(@"./clipName", namespaceManager);
                            XmlNode videoUrl = video.SelectSingleNode(@"./videoUrl", namespaceManager);
                            XmlNode thumbnail = video.SelectSingleNode(@"./thumbnailUrl", namespaceManager);
                            XmlNode description = video.SelectSingleNode(@"./abstract", namespaceManager);
                            XmlNode duration = video.SelectSingleNode(@"./length", namespaceManager);
                            result.Add(new VideoInfo() {
                                           VideoUrl = videoUrl.InnerText,
                                           ImageUrl = thumbnail.InnerText.Replace(@"_92x69", @"_480x360"),
                                           Title = HttpUtility.HtmlDecode(title.InnerText),
                                           Description = HttpUtility.HtmlDecode(description.InnerText),
                                           Length = duration.InnerText
                                       });
                        }
                    }
                }
            }
            return result;
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            string rtmp = @"rtmp://flash.scrippsnetworks.com:1935/ondemand?ovpfv=1.1";
            string playPath = video.VideoUrl.Replace(@"http://wms.scrippsnetworks.com", string.Empty).Replace(@".wmv", string.Empty);
            return new MPUrlSourceFilter.RtmpUrl(rtmp) { PlayPath = playPath }.ToString();
        }
    }
}
