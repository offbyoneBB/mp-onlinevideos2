using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using OnlineVideos.AMF3;
using System.Net;
using System.Linq;
using System.IO;
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
            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
                return String.Empty;

            AMF3Array renditions;
            if (requestType == RequestType.ViewerExperienceRequest)
                renditions = GetResultsFromViewerExperienceRequest(m, video);
            else
                renditions = GetResultsFromFindByMediaId(m, video);

            return FillPlaybackOpyions(video, renditions);
        }

        private AMF3Array GetResultsFromViewerExperienceRequest(Match m, VideoInfo video)
        {
            AMF3Object contentOverride = new AMF3Object("com.brightcove.experience.ContentOverride");
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
            AMF3Array array = new AMF3Array();
            array.Add(contentOverride);

            AMF3Object ViewerExperienceRequest = new AMF3Object("com.brightcove.experience.ViewerExperienceRequest");
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
            ViewerExperienceRequest.Add("URL", video.VideoUrl);

            if ((g = m.Groups["experienceId"]).Success)
            {
                Log.Debug("param experienceId=" + g.Value);
                ViewerExperienceRequest.Add("experienceId", (double)Int64.Parse(g.Value));
            }
            else
                ViewerExperienceRequest.Add("experienceId", double.NaN);
            Log.Debug("param URL=" + video.VideoUrl);

            AMF3Serializer ser = new AMF3Serializer();
            byte[] data = ser.Serialize(ViewerExperienceRequest, hashValue);

            /*
            using (Stream s = new FileStream(@"E:\request.bin", FileMode.Create, FileAccess.Write))
            {
                BinaryWriter sw = new BinaryWriter(s);
                sw.Write(data);
            }*/

            AMF3Object response = GetResponse(requestUrl, data);
            //Stream stream = new FileStream(@"E:\ztele.txt", FileMode.Open, FileAccess.Read);
            //AMF3Deserializer des = new AMF3Deserializer(stream);
            //AMF3Object response = des.Deserialize();
            return response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");
        }

        private AMF3Array GetResultsFromFindByMediaId(Match m, VideoInfo video)
        {
            AMF3Serializer ser = new AMF3Serializer();
            object[] values = new object[4];
            values[0] = hashValue;
            values[1] = Convert.ToDouble(playerId);
            values[2] = Convert.ToDouble(m.Groups["mediaId"].Value);
            values[3] = Convert.ToDouble(array4);
            byte[] data = ser.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values);
            AMF3Object obj = BrightCoveUtil.GetResponse(requestUrl, data);
            return obj.GetArray("renditions");
        }

        private string FillPlaybackOpyions(VideoInfo video, AMF3Array renditions)
        {


            video.PlaybackOptions = new Dictionary<string, string>();
            for (int i = 0; i < renditions.Count; i++)
            {
                AMF3Object rendition = renditions.GetObject(i);
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

                    url = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}",
                        HttpUtility.UrlEncode(rtmp),
                        HttpUtility.UrlEncode(playpath)));

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

        private static AMF3Object GetResponse(string url, byte[] postData)
        {
            Log.Debug("get webdata from {0}", url);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return null;
            request.Method = "POST";
            request.ContentType = "application/x-amf";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Timeout = 15000;
            request.ContentLength = postData.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);
            requestStream.Close();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();


                AMF3Deserializer des = new AMF3Deserializer(responseStream);
                AMF3Object obj = des.Deserialize();
                return obj;
            }

        }

    }
}
