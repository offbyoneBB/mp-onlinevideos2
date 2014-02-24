using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;
using System.Linq;
using System.Web;

namespace OnlineVideos.Sites
{
    public class BrightCoveUtil : GenericSiteUtil
    {
        protected enum RequestType { ViewerExperienceRequest, FindMediaById };

        [Category("OnlineVideosConfiguration"), Description("HashValue")]
        protected string hashValue = null;
        [Category("OnlineVideosConfiguration"), Description("Url for request")]
        protected string requestUrl = null;
        [Category("OnlineVideosConfiguration"), Description("optional playerId")]
        protected string playerId = null;
        [Category("OnlineVideosConfiguration"), Description("4th value in array")]
        protected string array4 = null;
        [Category("OnlineVideosConfiguration"), Description("Request type")]
        protected RequestType requestType = RequestType.ViewerExperienceRequest;

        public override string getUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            return GetFileUrl(video, webdata);
        }

        protected string GetFileUrl(VideoInfo video, string data)
        {
            Match m = regEx_FileUrl.Match(data);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions;
            if (requestType == RequestType.ViewerExperienceRequest)
                renditions = GetResultsFromViewerExperienceRequest(m, video.VideoUrl);
            else
                renditions = GetResultsFromFindByMediaId(m);

            return FillPlaybackOptions(video, renditions);
        }

        protected AMFArray GetResultsFromViewerExperienceRequest(Match m, string videoUrl)
        {
            AMFObject contentOverride = new AMFObject("com.brightcove.experience.ContentOverride");
            System.Text.RegularExpressions.Group g;
            if ((g = m.Groups["contentId"]).Success)
            {
                Log.Debug("param contentId=" + g.Value);
                contentOverride.Add("contentId", (double)Int64.Parse(g.Value));
            }
            else
                contentOverride.Add("contentId", double.NaN);
            contentOverride.Add("target", "videoPlayer");
            if ((g = m.Groups["contentRefId"]).Success)
            {
                Log.Debug("param contentRefId=" + g.Value);
                contentOverride.Add("contentRefId", g.Value);
            }
            else
                contentOverride.Add("contentRefId", null);

            contentOverride.Add("featuredRefId", null);
            contentOverride.Add("contentRefIds", null);
            contentOverride.Add("featuredId", double.NaN);
            contentOverride.Add("contentIds", null);
            contentOverride.Add("contentType", 0);
            AMFArray array = new AMFArray();
            array.Add(contentOverride);

            AMFObject ViewerExperienceRequest = new AMFObject("com.brightcove.experience.ViewerExperienceRequest");
            ViewerExperienceRequest.Add("TTLToken", String.Empty);
            if ((g = m.Groups["playerKey"]).Success)
            {
                Log.Debug("param playerKey=" + g.Value);
                ViewerExperienceRequest.Add("playerKey", g.Value);
            }
            else
                ViewerExperienceRequest.Add("playerKey", String.Empty);
            ViewerExperienceRequest.Add("deliveryType", double.NaN);
            ViewerExperienceRequest.Add("contentOverrides", array);
            ViewerExperienceRequest.Add("URL", videoUrl);
            Log.Debug("param URL=" + videoUrl);

            if ((g = m.Groups["experienceId"]).Success)
            {
                Log.Debug("param experienceId=" + g.Value);
                ViewerExperienceRequest.Add("experienceId", (double)Int64.Parse(g.Value));
            }
            else
                ViewerExperienceRequest.Add("experienceId", double.NaN);

            AMFSerializer ser = new AMFSerializer();
            byte[] data = ser.Serialize(ViewerExperienceRequest, "com.brightcove.experience.ExperienceRuntimeFacade.getDataForExperience", hashValue);

            AMFObject response = AMFObject.GetResponse(requestUrl, data);
            return response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");
        }

        protected AMFArray GetResultsFromFindByMediaId(Match m)
        {
            AMFSerializer ser = new AMFSerializer();
            object[] values = new object[4];
            values[0] = hashValue;
            values[1] = Convert.ToDouble(playerId);
            values[2] = Convert.ToDouble(m.Groups["mediaId"].Value);
            values[3] = Convert.ToDouble(array4);
            byte[] data = ser.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values);
            AMFObject obj = AMFObject.GetResponse(requestUrl, data);
            return obj.GetArray("renditions");
        }

        protected string FillPlaybackOptions(VideoInfo video, AMFArray renditions)
        {
            video.PlaybackOptions = new Dictionary<string, string>();

            foreach (AMFObject rendition in renditions.OrderBy(u => u.GetIntProperty("encodingRate")))
            {
                string nm = String.Format("{0}x{1} {2}K",
                    rendition.GetIntProperty("frameWidth"), rendition.GetIntProperty("frameHeight"),
                    rendition.GetIntProperty("encodingRate") / 1024);
                string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                if (url.StartsWith("rtmp"))
                {
                    //tested with ztele
                    string auth = String.Empty;
                    if (url.Contains('?'))
                        auth = '?' + url.Split('?')[1];
                    string[] parts = url.Split('&');

                    string rtmp = parts[0] + auth;
                    string playpath = parts[1].Split('?')[0] + auth;
                    url = new MPUrlSourceFilter.RtmpUrl(rtmp) { PlayPath = playpath }.ToString();

                }
                video.PlaybackOptions.Add(nm, url);
            }

            if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
            else
                if (video.PlaybackOptions.Count == 1)
                {
                    string resultUrl = video.PlaybackOptions.Last().Value;
                    video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                    return resultUrl;
                }
                else
                {
                    return video.PlaybackOptions.Last().Value;
                }
        }

    }
}
