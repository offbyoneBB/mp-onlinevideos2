using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for biography.
    /// </summary>
    public class BiographyUtil : GenericSiteUtil
    {
        private static Regex alphabeticalCategoriesRegex = new Regex(@"Full Bios|Full Episodes|Mini Bios",
                                                                     RegexOptions.Compiled);
        private static Regex playListRegex = new Regex(@"playList\.push\({\s+'videoID':'[^']*',\s+'videoURLs':{\s+releaseURL:\s'(?<releaseURL>[^']*)',.*?'siteUrl':'(?<siteUrl>[^']*)'",
                                                       RegexOptions.Singleline | RegexOptions.Compiled);
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            string url = string.Format(@"{0}/videos", baseUrl);
            HtmlDocument document = GetWebData<HtmlDocument>(url);
            
            if (document != null)
            {
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(@"//ul[@class = 'content-subnav']/li/a"))
                {
                    string name = anchor.InnerText;
                    if (@"All Videos".Equals(name)) continue;

                    Settings.Categories.Add(new RssLink() {
                                                Name = name,
                                                Url = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                                Other = alphabeticalCategoriesRegex.Match(name).Success ? @"alphabetical" : @"most-recent",
                                                HasSubCategories = false
                                            });
                }
                Settings.Categories.Add(new RssLink() {
                                            Name = @"Shows/Political Figures",
                                            Url = url,
                                            Other = @"most-recent",
                                            HasSubCategories = true
                                        });
            }
                        
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
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(@"//div[@class = 'explorer-tool']//li[not(@class)]/a"))
                {
                    parentCategory.SubCategories.Add(new RssLink() {
                                                         ParentCategory = parentCategory,
                                                         Name = HttpUtility.HtmlDecode(anchor.InnerText),
                                                         Url = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                                         Other = @"most-recent",
                                                         HasSubCategories = false
                                                     });
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = string.Format(@"{0}?page-number=1&pagination-sort-by={1}&previous-sort={1}&pagination-per-page=20&prev-per-page=20",
                                       (category as RssLink).Url, category.Other as string);

            return getVideoListForSinglePage(category, url);
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = string.Empty;
            currentCategory = category;

            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                if (alphabeticalCategoriesRegex.Match(category.Name).Success)
                {
                    // results position-wise are as follows (in batches of 4 - with 20 per page)
                    // 0 4  8 12 16
                    // 1 5  9 13 17
                    // 2 6 10 14 18
                    // 3 7 11 15 19
                    // need to rearrange the final sequence to be:
                    // 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19
                    //
                    // iteration=0 use indices (0, 1, 2, 3)
                    // iteration=1 use indices (1, 3, 5, 7)
                    // iteration=2 use indices (2, 5, 8, 11)
                    // iteration=3 use indices (3, 7, 11, 15)
                    int iteration = 0;
                    foreach (HtmlNode unorderedList in document.DocumentNode.SelectNodes(@"//div[contains(@class, 'video-results')]/ul"))
                    {
                        int videoCount = 0;
                        foreach (HtmlNode item in unorderedList.SelectNodes(@"./li"))
                        {
                            HtmlNode anchor = item.SelectSingleNode(@"./a");
                            HtmlNode duration = item.SelectSingleNode(@".//p/span[@class = 'video-duration']");
                            HtmlNode description = item.SelectSingleNode(@"./following-sibling::div//p[contains(@class, 'show-description')]");
                            VideoInfo video = new VideoInfo() {
                                Title = anchor.InnerText,
                                VideoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                Length = duration.InnerText.Replace("(", string.Empty).Replace(")", string.Empty),
                                Description = description.InnerText
                            };
                            if (videoCount == 4)
                            {
                                // 5th video should be added to the end
                                result.Add(video);
                            }
                            else
                            {
                                // all other videos should be inserted at a specific index
                                result.Insert(iteration + videoCount * (iteration + 1), video);
                            }
                            videoCount++;
                        }
                        iteration++;
                    }
                }
                else
                {
                    HtmlNodeCollection items = document.DocumentNode.SelectNodes(@"//div[contains(@class, 'video-results')]//li");
                    if (items != null)
                    {
                        foreach (HtmlNode item in items)
                        {
                            HtmlNode img = item.SelectSingleNode(@".//img");
                            HtmlNode anchor = item.SelectSingleNode(@".//p/a");
                            HtmlNode duration = item.SelectSingleNode(@".//p/span[@class = 'video-duration']");
                            HtmlNode description = item.SelectSingleNode(@"./following-sibling::div//p[contains(@class, 'show-description')]");
                            result.Add(new VideoInfo() {
                                           Title = anchor.InnerText,
                                           ImageUrl = img.GetAttributeValue(@"src", string.Empty),
                                           VideoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                           Length = duration.InnerText.Replace("(", string.Empty).Replace(")", string.Empty),
                                           Description = description.InnerText
                                       });
                        }
                    }
                }
                HtmlNode pageNumberIncrease = document.DocumentNode.SelectSingleNode(@"//a[@class = 'form-page-number-increase']");
                if (pageNumberIncrease != null)
                {
                    UriBuilder builder = new UriBuilder(url);
                    NameValueCollection parameters = HttpUtility.ParseQueryString(builder.Query);
                    int nextPageNumber = int.Parse(parameters["page-number"]) + 1;
                    parameters["page-number"] = Convert.ToString(nextPageNumber);
                    builder.Query = parameters.ToString();
                    Uri nextPageUri = builder.Uri;
                    Log.Debug("Next Page URL: {0}", nextPageUri);
                    nextPageUrl = nextPageUri.ToString();
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
            
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match playListMatch = playListRegex.Match(data);
                if (playListMatch.Success)
                {
                    string releaseURL = string.Empty;
                    
                    foreach (Match m in playListRegex.Matches(data))
                    {
                        if (video.VideoUrl.Contains(m.Groups["siteUrl"].Value))
                        {
                            releaseURL = m.Groups["releaseURL"].Value;
                            break;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(releaseURL))
                    {
                        XmlDocument xml = GetWebData<XmlDocument>(releaseURL);

                        XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
                        nsmRequest.AddNamespace("a", @"http://www.w3.org/2005/SMIL21/Language");
            
                        XmlNode metaBase = xml.SelectSingleNode(@"//a:meta", nsmRequest);
                        // base URL may be stored in the base attribute of <meta> tag
                        string url = metaBase != null ? metaBase.Attributes["base"].Value : string.Empty;
            
                        foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
                        {
                            int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                            // do not bother unless bitrate is non-zero
                            if (bitrate == 0) continue;
            
                            if (url.StartsWith("rtmp") && !urlsDictionary.ContainsKey(bitrate / 1000))
                            {
                                string src = node.Attributes["src"].Value;
                                string[] srcParts = src.Split('?');
                                string playPath = srcParts[0];
                                if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                                {
                                    // prepend with mp4:
                                    playPath = @"mp4:" + playPath;
                                }
                                else if (playPath.EndsWith(@".flv"))
                                {
                                    // strip extension
                                    playPath = playPath.Replace(@".flv", string.Empty);
                                }
                                string rtmpUrl = string.Format(@"{0}?{1}", url, srcParts[1]);
                                Log.Debug(@"bitrate: {0}, rtmpUrl: {1}, PlayPath: {2}", bitrate / 1000, rtmpUrl, playPath);
                                urlsDictionary.Add(bitrate / 1000, new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString());
                            }
                        }

                        // sort the URLs ascending by bitrate
                        foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                        {
                            video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                            // return last URL as the default (will be the highest bitrate)
                            result = item.Value;
                        }
                        
                        // if result is still empty then perhaps we are geo-locked
                        if (string.IsNullOrEmpty(result))
                        {
                            XmlNode geolockReference = xml.SelectSingleNode(@"//a:seq/a:ref", nsmRequest);
                            if (geolockReference != null)
                            {
                                string message = geolockReference.Attributes["abstract"] != null ?
                                    geolockReference.Attributes["abstract"].Value :
                                    @"This content is not available in your location.";
                                Log.Error(message);
                                throw new OnlineVideosException(message, true);
                            }
                        }
                    }
                }
            }
            
            return result;
        }
    }
}
