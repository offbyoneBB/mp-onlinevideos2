using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class MetaCafeUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to check if video is linked to youtube. Value of the group named yt should hold the youtube video id.")]
        protected string youtubeCheckRegEx;

        protected Regex regEx_youtubeCheckRegEx;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(youtubeCheckRegEx)) regEx_youtubeCheckRegEx = new Regex(youtubeCheckRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);            
        }

        public override string getUrl(VideoInfo video)
        {
            string dataPage = GetWebData(video.VideoUrl);
            Match matchYouTube = regEx_youtubeCheckRegEx != null ? regEx_youtubeCheckRegEx.Match(dataPage) : Match.Empty;
            if (matchYouTube.Success)
            {
                video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(matchYouTube.Groups["yt"].Value);
                return (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0) ? "" : video.PlaybackOptions.First().Value;
            }
            else
            {
                string result = base.getUrl(video);
                if (!string.IsNullOrEmpty(result))
                {
                    result = result.Replace("\\", "");
                    return result;
                }
                else
                {
                    Match m = Regex.Match(dataPage, "&errorTitle=(?<error>.*?)&", RegexOptions.Singleline | RegexOptions.Multiline);
                    if (m.Success)
                    {
                        string error = HttpUtility.UrlDecode(m.Groups["error"].Value);
                        if (!string.IsNullOrEmpty(error)) throw new OnlineVideosException(error);
                    }

                    string id = Regex.Match(dataPage, "value=\"itemID=(.+?)&").Groups[1].Value;
                    dataPage = GetWebData("http://release.theplatform.com/content.select?format=SMIL&Tracking=true&balance=true&pid=" + id);
                    System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();
                    xDoc.LoadXml(dataPage);
                    System.Xml.XmlElement elem = xDoc.SelectSingleNode("//*[local-name() = 'ref'][not(@tags)]") as System.Xml.XmlElement;
                    string src = elem.GetAttribute("src");
                    if (src.StartsWith("rtmp"))
                    {
                        string playpath = src.Substring(src.IndexOf("<break>") + "<break>".Length);
                        if (playpath.ToLower().EndsWith(".mp4")) playpath = "mp4:" + playpath;
                        src = src.Replace("ondemand/?", "ondemand/?ovpfv=1.1&?");
                        src = src.Substring(0, src.IndexOf("<break>"));
                        
                        return ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&pageurl={1}&swfurl={2}&playpath={3}", 
                                    System.Web.HttpUtility.UrlEncode(src), 
                                    System.Web.HttpUtility.UrlEncode(video.VideoUrl),
                                    System.Web.HttpUtility.UrlEncode("http://www.cbs.com/thunder/chromeless/metacafe.swf"),
                                    System.Web.HttpUtility.UrlEncode(playpath)));
                    }
                    else return "";
                }
            }
        }
    }
}
