using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site utility for The CW.
    /// </summary>
    public class TheCWUtil : GenericSiteUtil
    {
        private static string urlFormatString = @"http://metaframe.digitalsmiths.tv/v2/CWtv/assets/{0}/partner/132?format=json";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            HtmlDocument document = GetWebData<HtmlDocument>(string.Format(@"{0}/cw-video/", baseUrl));
            if (document != null)
            {
                foreach (HtmlNode item in document.DocumentNode.SelectNodes(@"//div[@id = 'shows-video']/ul/li"))
                {
                    HtmlNode anchor = item.SelectSingleNode(@"./a");
                    HtmlNode p = anchor.SelectSingleNode(@"./p[@class = 't']");
                    HtmlNode img = anchor.SelectSingleNode(@"./div/img");
                    Settings.Categories.Add(new RssLink() {
                                                Url = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue("href", string.Empty)),
                                                Name = p.InnerText,
                                                Thumb = img.GetAttributeValue("src", string.Empty),
                                                HasSubCategories = true
                                            });
                }
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
                foreach (HtmlNode item in document.DocumentNode.SelectNodes(@"//div[@id = 'videotabs']/ul/li"))
                {
                    // only need the text from the title (drop the video count)
                    string title = HttpUtility.HtmlDecode(item.InnerText).Split('(')[0];
                    parentCategory.SubCategories.Add(new RssLink() {
                                                         ParentCategory = parentCategory,
                                                         Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower()),
                                                         Url = url,
                                                         Thumb = parentCategory.Thumb,
                                                         Other = item.GetAttributeValue("id", string.Empty),
                                                         HasSubCategories = false
                                                     });
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            HtmlDocument document = GetWebData<HtmlDocument>((category as RssLink).Url);
            string xPath = string.Format(@"//div[@id = '{0}' and @class = 'videotabcontents']//a",
                                         (category.Other as string).Replace(@"videotab_", @"videotabcontents_"));
            if (document != null)
            {
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(xPath))
                {
                    HtmlNode img = anchor.SelectSingleNode(@"./div/img");
                    HtmlNode details = anchor.SelectSingleNode(@"./div[@class = 'videodetails']");
                    HtmlNode title = details.SelectSingleNode(@"./p[@class = 'et']");
                    HtmlNode description = details.SelectSingleNode(@"./p[@class = 'd3']");
                    HtmlNode season = details.SelectSingleNode(@"./p[@class = 'd2']");
                    HtmlNode airdate = season.SelectSingleNode(@"./span[@class = 'd4']");
                    result.Add(new VideoInfo() {
                                   VideoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                   ImageUrl = img.GetAttributeValue(@"src", string.Empty),
                                   Title = title.InnerText,
                                   Description = string.Format(@"{0}: {1}", season.SelectSingleNode(@"text()").InnerText, description.InnerText),
                                   Airdate = airdate != null ? airdate.InnerText.Replace(@"Original Air Date: ", string.Empty) : string.Empty
                               });
                }
            }
            return result;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            string guid = video.VideoUrl.Split('=')[1];
            JObject json = GetWebData<JObject>(string.Format(urlFormatString, guid));
            
            if (json != null)
            {
                JToken videos = json["videos"];
                Log.Debug(@"# of bitrate options: {0}", videos.Children().Count());
                foreach (var child in videos.Children())
                {
                    // get first element
                    JToken item = child.First();
                    int bitrate = int.Parse(item.Value<string>(@"bitrate"));
                    string url = item.Value<string>(@"uri");
                    if (!urlsDictionary.ContainsKey(bitrate))
                    {
                        Log.Debug(@"Bitrate: {0}, Url: {1}", bitrate, url);
                        string[] urlParts = url.Split(new string[] { @"mp4:" }, StringSplitOptions.None);
                        string rtmpUrl = urlParts[0];
                        string playPath = string.Format(@"mp4:{0}", urlParts[1]);
                        urlsDictionary.Add(bitrate, new MPUrlSourceFilter.RtmpUrl(url) {
                                               PlayPath = playPath,
                                               SwfUrl = @"http://pdl.warnerbros.com/cwtv/digital-smiths/production_player/vsplayer.swf"
                                           }.ToString());
                    }
                }

                // sort the URLs ascending by bitrate
                foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    // return last URL as the default (will be the highest bitrate)
                    result = item.Value;
                }
            }
            return result;
        }
    }
}
