using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class TVOUtil : GenericSiteUtil
    {
        private static string baseUrlPrefix = @"http://ww3.tvo.org";
        private static string mainCategoriesUrlPrefix = baseUrlPrefix + @"/views/ajax?view_name=video_landing_page&view_display_id=";
        private static string videoListUrl = baseUrlPrefix + @"/views/ajax?field_web_master_series_nid_1={0}&view_name=video_landing_page&view_display_id={1}";
        private static string brightcoveUrl = @"http://c.brightcove.com/services/messagebroker/amf";
        private static string hashValue = @"82c0aa70e540000aa934812f3573fd475d131a63";
        // if this ever changes, it can be found by looking at AMF Response
        private static string publisherId = @"18140038001";
        
        private static Regex nidRegex = new Regex(@"(?<nid>[\d]+)\-wrapper$", RegexOptions.Compiled);
        private static Regex rtmpUrlRegex = new Regex(@"(?<rtmp>rtmpe?)://(?<host>[^/]+)/(?<app>[^&]*)&(?<leftover>.*)", RegexOptions.Compiled);
        private static Regex nextPageLinkRegex = new Regex(@"(/video|/views/ajax)\?page=(?<page>\d+)", RegexOptions.Compiled);
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "All Programs", Url = mainCategoriesUrlPrefix + "page_1", HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Documentaries", Url = mainCategoriesUrlPrefix + "page_4", HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Dramas", Url = mainCategoriesUrlPrefix + "page_5", HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "TVO Archive", Url = mainCategoriesUrlPrefix + "page_3", HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Topics", Url = mainCategoriesUrlPrefix + "page_2", HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Playlists", Url = mainCategoriesUrlPrefix + "page_7", HasSubCategories = true }
               );

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string url = ((RssLink) parentCategory).Url;
            string viewDisplayId = HttpUtility.ParseQueryString(new Uri(url).Query)["view_display_id"];
            
            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                string display = json.Value<string>("display");
                
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(display);
                
                foreach (HtmlNode div in html.DocumentNode.SelectNodes("//div[@class='form-item']"))
                {
                    HtmlNode label = div.SelectSingleNode("./label");
                    
                    RssLink cat = new RssLink();
                    cat.ParentCategory = parentCategory;
                    cat.Name = label.InnerText.Replace("&lt;Any&gt;", "All");

                    string id = div.Attributes["id"].Value;
                    Match nidMatch = nidRegex.Match(id);
                    
                    if (nidMatch.Success)
                    {
                        cat.Url = String.Format(videoListUrl, nidMatch.Groups["nid"], viewDisplayId);
                    }
                    else if (id.EndsWith("All-wrapper"))
                    {
                        cat.Url = String.Format(videoListUrl, "All", viewDisplayId);
                    }
                    cat.HasSubCategories = false;
                    Log.Debug("text: {0}, id: {1}", cat.Name, cat.Url);

                    parentCategory.SubCategories.Add(cat);
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListForSinglePage(category, ((RssLink) category).Url);
        }

        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = "";
            currentCategory = category;

            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                string display = json.Value<string>("display");
                
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(display);

                foreach (HtmlNode td in html.DocumentNode.SelectNodes("//td"))
                {
                    Log.Debug("<td> length: {0}", td.InnerHtml.Trim().Length);

                    HtmlNode anchor = td.SelectSingleNode("./span[@class='views-field-field-thumbnail-url-value']//a");                    
                    HtmlNode lengthNode = td.SelectSingleNode("./span[@class='views-field-field-length-value']");
                    HtmlNode releaseNode = td.SelectSingleNode("./span[@class='views-field-field-release-date-value']");
                    HtmlNode titleNode = td.SelectSingleNode(".//h5");
                    HtmlNode descriptionNode = td.SelectSingleNode(".//span[@class='views-field-field-description-value']");
                    
                    if (anchor != null)
                    {
                        result.Add(new VideoInfo() {
                                       VideoUrl = anchor.Attributes["href"].Value,
                                       ImageUrl = anchor.SelectSingleNode("./img").Attributes["src"].Value,
                                       Length = lengthNode.SelectSingleNode(".//span[@class='field-length-value']").InnerText,
                                       Airdate = releaseNode.SelectSingleNode(".//span[@class='date-display-single']/span[@class='date-display-single']").InnerText,
                                       Title = titleNode.InnerText,
                                       Description = descriptionNode.SelectSingleNode("./span[@class='field-content']").InnerText
                                   });
                    }
                }
                
                HtmlNode nextPageNode = html.DocumentNode.SelectSingleNode("//li[@class='pager-next']");
                if (nextPageNode != null)
                {
                    string link = nextPageNode.SelectSingleNode("./a").Attributes["href"].Value;
                    Match nextPageLinkMatch = nextPageLinkRegex.Match(link);
                    if (nextPageLinkMatch.Success)
                    {
                        string page = nextPageLinkMatch.Groups["page"].Value;
                        nextPageUrl = String.Format(@"{0}&page={1}", ((RssLink) category).Url, page);
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
            /*
               sample AMF input (expressed in pretty-printed JSON for clarity - captured using Flashbug in Firebug)
            [
                {"targetURI":"com.brightcove.experience.ExperienceRuntimeFacade.getDataForExperience",
                 "responseURI":"/1",
                 "length":"461 B",
                 "data":["82c0aa70e540000aa934812f3573fd475d131a63",
                    {"contentOverrides":[
                        {"contentType":0,
                         "target":"videoPlayer",
                         "featuredId":null,
                         "contentId":1411956407001,
                         "featuredRefId":null,
                         "contentIds":null,
                         "contentRefId":null,
                         "contentRefIds":null,
                         "__traits":
                            {"type":"com.brightcove.experience.ContentOverride",
                             "members":["contentType","target","featuredId","contentId","featuredRefId","contentIds","contentRefId","contentRefIds"],
                             "count":8,
                             "externalizable":false,
                             "dynamic":false}}],
                      "TTLToken":"",
                      "deliveryType":null,
                      "playerKey":"AQ~~,AAAABDk7A3E~,xYAUE9lVY9-LlLNVmcdybcRZ8v_nIl00",
                      "URL":"http://ww3.tvo.org/video/171480/safe-houses",
                      "experienceId":756015080001,
                      "__traits":
                        {"type":"com.brightcove.experience.ViewerExperienceRequest",
                         "members":["contentOverrides","TTLToken","deliveryType","playerKey","URL","experienceId"],
                         "count":6,
                         "externalizable":false,
                         "dynamic":false}}]}]
            */
           
            string result = string.Empty;
            
            HtmlDocument document = GetWebData<HtmlDocument>(video.VideoUrl);

            if (document != null)
            {
                HtmlNode brightcoveExperience = document.DocumentNode.SelectSingleNode(@"//object[@class='BrightcoveExperience']");
                string playerId = brightcoveExperience.SelectSingleNode(@"./param[@name='playerID']").GetAttributeValue("value", "");
                string videoId = brightcoveExperience.SelectSingleNode(@"./param[@name='@videoPlayer']").GetAttributeValue("value", "");
                Log.Debug("PlayerId: {0}, VideoId: {1}", playerId, videoId);
                
                // content override
                AMFObject contentOverride = new AMFObject(@"com.brightcove.experience.ContentOverride");
                contentOverride.Add("contentId", videoId);
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
                viewerExperenceRequest.Add("experienceId", playerId);
                viewerExperenceRequest.Add("deliveryType", null);
                viewerExperenceRequest.Add("playerKey", string.Empty);
                viewerExperenceRequest.Add("URL", video.VideoUrl);
                viewerExperenceRequest.Add("TTLToken", string.Empty);

                AMFSerializer serializer = new AMFSerializer();
                AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize(viewerExperenceRequest, hashValue));
                
                //Log.Debug("AMF Response: {0}", response.ToString());

                AMFArray renditions = response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");

                video.PlaybackOptions = new Dictionary<string, string>();

                for (int i = 0; i < renditions.Count; i++)
                {
                    AMFObject rendition = renditions.GetObject(i);
                    string optionKey = String.Format("{0}x{1} {2}K",
                        rendition.GetIntProperty("frameWidth"),
                        rendition.GetIntProperty("frameHeight"),
                        rendition.GetIntProperty("encodingRate") / 1024);
                    string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                    Log.Debug("Option: {0} URL: {1}", optionKey, url);

                    // typical rtmp url from "defaultURL" is:
                    // rtmp://brightcove.fcod.llnwd.net/a500/d20/&mp4:media/1351824783/1351824783_1411974095001_109856X-640x360-1500k.mp4&1330020000000&1b163106256f448754aff72969869479
                    // 
                    // following rtmpdump style command works
                    // rtmpdump 
                    // --app 'a500/d20/?videoId=1411956407001&lineUpId=&pubId=18140038001&playerId=756015080001&playerTag=&affiliateId='
                    // --auth 'mp4:media/1351824783/1351824783_1411974097001_109856X-400x224-300k.mp4&1330027200000&23498dd8f4659cd07ad1b6c4ee5a013d'
                    // --rtmp 'rtmp://brightcove.fcod.llnwd.net/a500/d20/?videoId=1411956407001&lineUpId=&pubId=18140038001&playerId=756015080001&playerTag=&affiliateId='
                    // --flv 'TVO_org_-_Safe_as_Houses.flv'
                    // --playpath 'mp4:media/1351824783/1351824783_1411974097001_109856X-400x224-300k.mp4'
                    
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);
                    
                    if (rtmpUrlMatch.Success)
                    {
                        string query = String.Format(@"videoId={0}&lineUpId=&pubId={1}&playerId={2}&playerTag=&affiliateId=",
                                                    videoId, publisherId, playerId);
                        string app = String.Format("{0}?{1}", rtmpUrlMatch.Groups["app"], query);
                        string auth = rtmpUrlMatch.Groups["leftover"].Value;
                        string rtmpUrl = String.Format("{0}://{1}/{2}?{3}",
                                                       rtmpUrlMatch.Groups["rtmp"].Value,
                                                       rtmpUrlMatch.Groups["host"].Value,
                                                       rtmpUrlMatch.Groups["app"].Value,
                                                       query);
                        string leftover = rtmpUrlMatch.Groups["leftover"].Value;
                        string playPath = leftover.Substring(0, leftover.IndexOf('&'));;
                        Log.Debug(@"rtmpUrl: {0}, PlayPath: {1}, App: {2}, Auth: {3}", rtmpUrl, playPath, app, auth);
                        
                        // --auth xxxxxx data can be expressed equivalently as arbitrary data -C B:0 -C S:xxxxxx (boolean followed by string)
                        /*
                        RtmpObjectArbitraryData arbitraryData = new RtmpObjectArbitraryData();
                        arbitraryData.Objects.Add(new RtmpBooleanArbitraryData(false));
                        arbitraryData.Objects.Add(new RtmpStringArbitraryData(auth));
                        
                        RtmpUrl rtmp = new MPUrlSourceFilter.RtmpUrl(rtmpUrl) {
                            PlayPath = playPath,
                            App = app};
                        rtmp.ArbitraryData.Add(arbitraryData);                       
                        */
                        string rtmp = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                                                       string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&app={1}&playpath={2}&auth={3}",
                                                                                     HttpUtility.UrlEncode(rtmpUrl),
                                                                                     HttpUtility.UrlEncode(app),
                                                                                     HttpUtility.UrlEncode(playPath),
                                                                                     HttpUtility.UrlEncode(auth)));
                         video.PlaybackOptions.Add(optionKey, rtmp.ToString());
                    }
                }

                if (video.PlaybackOptions.Count > 0)
                {
                    if (video.PlaybackOptions.Count == 1)
                    {

                        result = video.PlaybackOptions.Last().Value;
                        // only one URL found, so PlaybackOptions not needed
                        video.PlaybackOptions = null;
                    }
                    else
                    {
                        // last value will be selected
                        result = video.PlaybackOptions.Last().Value;
                    }
                }
            }
            
            return result;
        }
    }
}
