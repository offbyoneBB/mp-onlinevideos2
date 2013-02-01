using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for biography.
    /// </summary>
    public class BiographyUtil : GenericSiteUtil
    {
        // following were found by using AMFParser as an inspector in Fiddler2
        private static string hashValue = @"95333ebe36672a53f8f07f648022c66273340325";
        private static string experienceId = @"1835242367001";
        private static string brightcoveUrl = @"http://c.brightcove.com/services/messagebroker/amf";

        private static Regex alphabeticalCategoriesRegex = new Regex(@"Full Bios|Full Episodes|Mini Bios",
                                                                     RegexOptions.Compiled);
        private static Regex contentIdRegex = new Regex(@"embedPlayer\(""[^""]*"",\s""(?<contentId>[^""]*)""",
                                                        RegexOptions.Compiled);
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
                    NameValueCollection parameters = HttpUtility.ParseQueryString(new Uri(url).Query);
                    int nextPageNumber = int.Parse(parameters["page-number"]) + 1;
                    parameters["page-number"] = Convert.ToString(nextPageNumber);
                    Uri nextPageUri = new UriBuilder(url) { Query = parameters.ToString() }.Uri;
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
            AMFArray renditions = null;
            
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match contentIdMatch = contentIdRegex.Match(data);
                if (contentIdMatch.Success)
                {
                    // content override
                    AMFObject contentOverride = new AMFObject(@"com.brightcove.experience.ContentOverride");
                    contentOverride.Add("contentId", contentIdMatch.Groups["contentId"].Value);
                    contentOverride.Add("contentIds", null);
                    contentOverride.Add("contentRefId", null);
                    contentOverride.Add("contentRefIds", null);
                    contentOverride.Add("contentType", 0);
                    contentOverride.Add("featuredId", double.NaN);
                    contentOverride.Add("featuredRefId", null);
                    contentOverride.Add("target", "videoPlayer");
                    AMFArray contentOverrideArray = new AMFArray();
                    contentOverrideArray.Add(contentOverride);
    
                    // viewer experience request
                    AMFObject viewerExperenceRequest = new AMFObject(@"com.brightcove.experience.ViewerExperienceRequest");
                    viewerExperenceRequest.Add("contentOverrides", contentOverrideArray);
                    viewerExperenceRequest.Add("experienceId", experienceId);
                    viewerExperenceRequest.Add("deliveryType", null);
                    viewerExperenceRequest.Add("playerKey", string.Empty);
                    viewerExperenceRequest.Add("URL", video.VideoUrl);
                    viewerExperenceRequest.Add("TTLToken", string.Empty);
    
                    //Log.Debug("About to make AMF call: {0}", viewerExperenceRequest.ToString());
                    AMFSerializer serializer = new AMFSerializer();
                    AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize(viewerExperenceRequest, hashValue));
                    //Log.Debug("AMF Response: {0}", response.ToString());
    
                    renditions = response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");

                    video.PlaybackOptions = new Dictionary<string, string>();
                    // keep track of sizes and URLs
                    Dictionary<double, string> urlsDictionary = new Dictionary<double, string>();
                    
                    for (int i = 0; i < renditions.Count; i++)
                    {
                        AMFObject rendition = renditions.GetObject(i);
                        
                        double size = rendition.GetDoubleProperty("size");
                        string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                        string[] urlParts = url.Split(new string[] { @"mp4:" }, StringSplitOptions.None);
                        string rtmpUrl = urlParts[0];
                        string playPath = string.Format(@"mp4:{0}", urlParts[1]);
                        Log.Debug(@"Size: {0}", size);

                        if (!urlsDictionary.ContainsKey(size))
                        {
                            urlsDictionary.Add(size, new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString());
                        }
                    }
                    
                    // sort the URLs ascending by size
                    foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                    {
                        video.PlaybackOptions.Add(string.Format("Total Size: {0:N2}MiB", item.Key / 1048576), item.Value);
                        // return last URL as the default (will be the highest bitrate)
                        result = item.Value;
                    }
                }
            }
            if (renditions == null) return result;
            
            return result;
        }
    }
}
