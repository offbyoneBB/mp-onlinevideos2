using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// base class for some Canadian BrightCove based sites (different from doskabouter's BrightCoveUtil)
    /// </summary>
    public abstract class CanadaBrightCoveUtilBase : GenericSiteUtil
    {
        protected abstract string hashValue { get; }
        protected abstract string playerId { get; }
        protected abstract string publisherId { get; }
        
        protected enum BrightCoveType { ViewerExperienceRequest, FindMediaById };

        protected virtual BrightCoveType RequestType { get { return BrightCoveType.ViewerExperienceRequest; } }
        protected virtual string brightcoveUrl { get { return @"http://c.brightcove.com/services/messagebroker/amf"; } }
        
        private static Regex rtmpUrlRegex = new Regex(@"(?<rtmp>rtmpe?)://(?<host>[^/]+)/(?<app>[^&]*)&(?<leftover>.*)", RegexOptions.Compiled);

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            string videoId = string.Empty;
            AMFArray renditions = null;
            
            if (RequestType.Equals(BrightCoveType.FindMediaById))
            {
                /*
                 * sample AMF input (expressed as JSON)
                 * ["466faf0229239e70a6df8fe66fc04f25f50e6fa7",48543011001,1401332946001,15364602001]
                 */
                videoId = video.VideoUrl;
                
                object[] values = new object[4];
                values[0] = hashValue;
                values[1] = Convert.ToDouble(playerId);
                values[2] = Convert.ToDouble(videoId);
                values[3] = Convert.ToDouble(publisherId);
    
                AMFSerializer serializer = new AMFSerializer();
                AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values));
                //Log.Debug("AMF Response: {0}", response.ToString());
    
                renditions = response.GetArray("renditions");
            }
            else
            {
                /*
                   sample AMF input for ViewerExperience (expressed in pretty-printed JSON for clarity - captured using Flashbug in Firebug)
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
                videoId = getBrightCoveVideoIdForViewerExperienceRequest(video.VideoUrl);
                if (!string.IsNullOrEmpty(videoId))
                {
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
    
                    //Log.Debug("About to make AMF call: {0}", viewerExperenceRequest.ToString());
                    AMFSerializer serializer = new AMFSerializer();
                    AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize(viewerExperenceRequest, hashValue));
                    //Log.Debug("AMF Response: {0}", response.ToString());
    
                    renditions = response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");
                }
            }
            
            if (renditions == null) return result;

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<string, string> urlsDictionary = new Dictionary<string, string>();

            for (int i = 0; i < renditions.Count; i++)
            {
                AMFObject rendition = renditions.GetObject(i);
                int bitrate = rendition.GetIntProperty("encodingRate");
                string optionKey = String.Format("{0}x{1} {2}K",
                    rendition.GetIntProperty("frameWidth"),
                    rendition.GetIntProperty("frameHeight"),
                    bitrate / 1024);
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
                    string leftover = rtmpUrlMatch.Groups["leftover"].Value;
                    string query = string.Format(@"videoId={0}&lineUpId=&pubId={1}&playerId={2}&playerTag=&affiliateId=",
                                                 videoId, publisherId, playerId);
                    
                    int questionMarkPosition = leftover.IndexOf('?');
                    // use existing query (if present) in the new query string
                    if (questionMarkPosition != -1)
                    {
                        query = string.Format(@"{0}{1}", leftover.Substring(questionMarkPosition + 1, leftover.Length - questionMarkPosition - 1), query);
                    }

                    string app = String.Format("{0}?{1}", rtmpUrlMatch.Groups["app"], query);
                    string auth = leftover;
                    string rtmpUrl = String.Format("{0}://{1}/{2}?{3}",
                                                   rtmpUrlMatch.Groups["rtmp"].Value,
                                                   rtmpUrlMatch.Groups["host"].Value,
                                                   rtmpUrlMatch.Groups["app"].Value,
                                                   query);
                    int ampersandPosition = leftover.IndexOf('&');
                    string playPath = ampersandPosition == -1 ? leftover : leftover.Substring(0, ampersandPosition);
                    Log.Debug(@"rtmpUrl: {0}, PlayPath: {1}, App: {2}, Auth: {3}", rtmpUrl, playPath, app, auth);
                    urlsDictionary.Add(optionKey, new RtmpUrl(rtmpUrl) {
                                                 PlayPath = playPath,
                                                 App = app,
                                                 Auth = auth
                                              }.ToString());
                }
            }
            
            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key, new BitrateComparer()))
            {
                video.PlaybackOptions.Add(item.Key, item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            return result;
        }
        
        public virtual string getBrightCoveVideoIdForViewerExperienceRequest(string videoUrl)
        {
            string videoId = string.Empty;
            HtmlDocument document = GetWebData<HtmlDocument>(videoUrl);

            if (document != null)
            {
                HtmlNode brightcoveExperience = document.DocumentNode.SelectSingleNode(@"//object[@class='BrightcoveExperience']");
                if (brightcoveExperience != null)
                {
                    videoId = brightcoveExperience.SelectSingleNode(@"./param[@name='@videoPlayer']").GetAttributeValue("value", "");                    
                }
                else
                {
                    Log.Warn("BrightcoveExperience object not found");
                }
                Log.Debug("VideoId: {0}", videoId);
            }
            return videoId;
        }
    }
    
    class BitrateComparer : IComparer<string>
    {
        private static Regex bitrateRegex = new Regex(@"\d+x\d+\s(?<bitrate>\d+)K", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            int xKbps = 0, yKbps = 0;
            Match match;
            match = bitrateRegex.Match(x);
            if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out xKbps)) return 1;
            match = bitrateRegex.Match(y);
            if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out yKbps)) return -1;
            return xKbps.CompareTo(yKbps);
        }
    }
}
